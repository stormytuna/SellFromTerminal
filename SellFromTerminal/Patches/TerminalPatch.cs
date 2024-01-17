using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace SellFromTerminal.Patches
{
	[HarmonyPatch(typeof(Terminal))]
	public class TerminalPatch
	{
		// Hack so we can display how much the scrap sold for without recalculating it
		private static int sellScrapFor;

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.Awake))]
		public static void AddTerminalNodes(Terminal __instance) {
			// TODO: Probably update to use terminal API? idk...
			TerminalKeyword confirmKeyword = __instance.terminalNodes.allKeywords.First(kw => kw.name == "Confirm");
			TerminalKeyword denyKeyword = __instance.terminalNodes.allKeywords.First(kw => kw.name == "Deny");

			TerminalNode sellQuotaConfirmNode = new TerminalNode {
				name = "sellQuotaConfirm",
				displayText = "Transaction complete. Sold scrap for [sellScrapFor] credits.\n\n\n",
				clearPreviousText = true,
				terminalEvent = "sellQuota"
			};
			TerminalNode sellQuotaDenyNode = new TerminalNode {
				name = "sellQuotaDeny",
				displayText = "Transaction cancelled.\n\n\n",
				clearPreviousText = true
			};

			TerminalNode sellQuotaNode = new TerminalNode {
				name = "sellQuota",
				displayText = "Beginning transaction.\nRequesting to sell scrap as close to quota as possible.[companyBuyingRateWarning]\n\nPlease CONFIRM or DENY.\n\n\n",
				isConfirmationNode = true,
				clearPreviousText = true,
				overrideOptions = true,
				terminalOptions = new[] {
					new CompatibleNoun {
						noun = confirmKeyword,
						result = sellQuotaConfirmNode
					},
					new CompatibleNoun {
						noun = denyKeyword,
						result = sellQuotaDenyNode
					}
				}
			};

			TerminalNode sellAllConfirmNode = new TerminalNode {
				name = "sellAllConfirm",
				displayText = "Transaction complete. Sold all scrap for [sellScrapFor] credits.\n\n\n",
				clearPreviousText = true,
				terminalEvent = "sellAll"
			};
			TerminalNode sellAllDenyNode = new TerminalNode {
				name = "sellAllConfirm",
				displayText = "Transaction cancelled.\n\n\n",
				clearPreviousText = true,
				terminalEvent = "giveLootHack" // TODO: Remove for obvious reasons lol
			};

			TerminalNode sellAllNode = new TerminalNode {
				name = "sellAll",
				displayText = "Beginning transaction.\nRequesting to sell ALL scrap ([numScrap]) for [sellScrapFor] credits.[companyBuyingRateWarning]\n\nPlease CONFIRM or DENY.\n\n\n",
				isConfirmationNode = true,
				clearPreviousText = true,
				overrideOptions = true,
				terminalOptions = new[] {
					new CompatibleNoun {
						noun = confirmKeyword,
						result = sellAllConfirmNode
					},
					new CompatibleNoun {
						noun = denyKeyword,
						result = sellAllDenyNode
					}
				}
			};

			TerminalKeyword allKeyword = new TerminalKeyword {
				name = "All",
				word = "all"
			};

			TerminalKeyword quotaKeyword = new TerminalKeyword {
				name = "Quota",
				word = "quota"
			};

			TerminalKeyword sellKeyword = new TerminalKeyword {
				name = "Sell",
				word = "sell",
				isVerb = true,
				compatibleNouns = new[] {
					new CompatibleNoun {
						noun = allKeyword,
						result = sellAllNode
					},
					new CompatibleNoun {
						noun = quotaKeyword,
						result = sellQuotaNode
					}
				}
			};

			allKeyword.defaultVerb = sellKeyword;
			quotaKeyword.defaultVerb = sellKeyword;

			__instance.terminalNodes.allKeywords = __instance.terminalNodes.allKeywords.AddRangeToArray(new[] { sellKeyword, allKeyword, quotaKeyword });
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.TextPostProcess))]
		public static string ProcessCustomText(string __result) {
			__result = __result.Replace("[numScrap]", ScrapHelpers.CountAllScrapInShip().ToString()); // TODO: Doesnt take company buying rate into account
			__result = __result.Replace("[sellScrapFor]", sellScrapFor.ToString());
			string companyBuyingRateWarning = StartOfRound.Instance.companyBuyingRate == 1f ? "" : $"\n\nWARNING: Company buying rate is currently at {StartOfRound.Instance.companyBuyingRate:P0}\n\n";
			__result = __result.Replace("[companyBuyingRateWarning]", companyBuyingRateWarning);

			return __result;
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.ParsePlayerSentence))]
		public static void SetSellScrapForHack(Terminal __instance) {
			// We set sellScrapFor here if we're trying to sell anything so that the value is only calculated once and will be the same for both nodes ('sell' and then 'confirm')
			string terminalInput = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);

			if (terminalInput.ToLower() == "sell all") {
				sellScrapFor = ScrapHelpers.GetTotalScrapValueInShip();
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.RunTerminalEvents))]
		public static void RunTerminalEvents(TerminalNode node) {
			if (node.terminalEvent == "sellAll") {
				NetworkHandler.Instance.SellAllScrapServerRpc();
			}

			if (node.terminalEvent == "sellQuota") {
				NetworkHandler.Instance.SellQuotaScrapServerRpc();
			}

			if (node.terminalEvent == "giveLootHack") {
				for (int i = 0; i < 50; i++) {
					Random rand = new Random();
					int nextScrap = rand.Next(16, 68);
					GameObject scrap = Object.Instantiate(StartOfRound.Instance.allItemsList.itemsList[nextScrap].spawnPrefab, GameNetworkManager.Instance.localPlayerController.transform.position, Quaternion.identity);
					scrap.GetComponent<GrabbableObject>().fallTime = 0f;
					int scrapValue = rand.Next(20, 120);
					scrap.AddComponent<ScanNodeProperties>().scrapValue = scrapValue;
					scrap.GetComponent<GrabbableObject>().scrapValue = scrapValue;
					scrap.GetComponent<NetworkObject>().Spawn();
					RoundManager.Instance.scrapCollectedThisRound.Add(scrap.GetComponent<GrabbableObject>());
				}
			}
		}
	}
}
