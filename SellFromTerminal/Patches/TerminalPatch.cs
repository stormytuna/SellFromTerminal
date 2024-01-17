using System;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace SellFromTerminal.Patches
{
	[HarmonyPatch(typeof(Terminal))]
	public class TerminalPatch
	{
		// Hack so we can display how much the scrap sold for without recalculating it
		public static int sellScrapFor;
		private static TerminalNode sellAmountNode;

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.Awake))]
		public static void AddTerminalNodes(Terminal __instance) {
			TerminalKeyword confirmKeyword = __instance.terminalNodes.allKeywords.First(kw => kw.name == "Confirm");
			TerminalKeyword denyKeyword = __instance.terminalNodes.allKeywords.First(kw => kw.name == "Deny");

			TerminalNode sellQuotaConfirmNode = new TerminalNode {
				name = "sellQuotaConfirm",
				displayText = "Transaction complete.\n\n\n",
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
				displayText = "Beginning transaction.\nRequesting to sell scrap as close to [sellScrapFor].[companyBuyingRateWarning]\n\nPlease CONFIRM or DENY.\n\n\n",
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

			TerminalNode sellAmountConfirmNode = new TerminalNode {
				name = "sellAmountConfirm",
				displayText = "Transaction complete.\n\n\n",
				clearPreviousText = true,
				terminalEvent = "sellAmount"
			};
			TerminalNode sellAmountDenyNode = new TerminalNode {
				name = "sellAllConfirm",
				displayText = "Transaction cancelled.\n\n\n",
				clearPreviousText = true
			};
			sellAmountNode = new TerminalNode {
				name = "sellAmount",
				displayText = "Beginning transaction.\nRequesting to sell scrap as close to [sellScrapFor].[companyBuyingRateWarning]\n\nPlease CONFIRM or DENY.\n\n\n",
				isConfirmationNode = true,
				clearPreviousText = true,
				overrideOptions = true,
				terminalOptions = new[] {
					new CompatibleNoun {
						noun = confirmKeyword,
						result = sellAmountConfirmNode
					},
					new CompatibleNoun {
						noun = denyKeyword,
						result = sellAmountDenyNode
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
		public static void SetSellScrapForHack(TerminalNode __result, Terminal __instance) {
			// We set sellScrapFor here if we're trying to sell anything so that the value is only calculated once and will be the same for both nodes ('sell' and then 'confirm')
			// For 'sell <amount>' we set it further down in TryParseSellAmount
			if (__result.name == "sellAll") {
				sellScrapFor = ScrapHelpers.GetTotalScrapValueInShip();
			}

			if (__result.name == "sellQuota") {
				sellScrapFor = TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.ParsePlayerSentence))]
		public static TerminalNode TryParseSellAmount(TerminalNode __result, Terminal __instance) {
			string terminalInput = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);

			Regex regex = new Regex(@"^sell (\d+$)$");
			Match match = regex.Match(terminalInput.ToLower());
			if (match.Success) {
				SellFromTerminalBase.Log.LogInfo(match.Groups[1].Value);
				sellScrapFor = Convert.ToInt32(match.Groups[1].Value);
				return sellAmountNode;
			}

			return __result;
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.RunTerminalEvents))]
		public static void RunTerminalEvents(TerminalNode node) {
			if (node.terminalEvent == "sellAll") {
				NetworkHandler.Instance.SellAllScrapServerRpc();
			}

			if (node.terminalEvent == "sellQuota" || node.terminalEvent == "sellAmount") {
				NetworkHandler.Instance.SellAmountServerRpc(sellScrapFor);
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
