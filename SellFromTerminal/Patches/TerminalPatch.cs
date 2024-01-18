using System;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace SellFromTerminal.Patches
{
	[HarmonyPatch(typeof(Terminal))]
	public class TerminalPatch
	{
		// Hack so we can display how much the scrap sold for without recalculating it
		public static int sellScrapFor;
		public static int numScrapSold;

		private static TerminalNode sellAmountNode;
		private static TerminalNode specialQuotaAlreadyMetNode;
		private static TerminalNode specialNotEnoughScrapNode;
		private static bool patchedTerminal;

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.Awake))]
		public static void AddTerminalNodes(Terminal __instance) {
			if (patchedTerminal) {
				return;
			}

			specialQuotaAlreadyMetNode = new TerminalNode {
				name = "quotaAlreadyMet",
				displayText = "Quota already met.\n\n\n",
				clearPreviousText = true
			};
			specialNotEnoughScrapNode = new TerminalNode {
				name = "notEnoughScrap",
				displayText = "Not enough scrap to meet [sellScrapFor] credits.\nTotal value: [totalScrapValue]\n\n\n",
				clearPreviousText = true
			};

			TerminalKeyword confirmKeyword = __instance.terminalNodes.allKeywords.First(kw => kw.name == "Confirm");
			TerminalKeyword denyKeyword = __instance.terminalNodes.allKeywords.First(kw => kw.name == "Deny");

			TerminalNode sellQuotaConfirmNode = new TerminalNode {
				name = "sellQuotaConfirm",
				displayText = "Transaction complete.\nSold [numScrapSold] scrap for [sellScrapFor] credits.\n\nThe company is not responsible for any calculation errors.\n\n\n",
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
				displayText = "Beginning transaction.\nRequesting to sell scrap as close to [sellScrapFor] credits as possible.[companyBuyingRateWarning]\n\nPlease CONFIRM or DENY.\n\n\n",
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
				displayText = "Transaction complete. Sold all scrap for [sellScrapFor] credits.\n\nThe company is not responsible for any calculation errors.\n\n\n",
				clearPreviousText = true,
				terminalEvent = "sellAll"
			};
			TerminalNode sellAllDenyNode = new TerminalNode {
				name = "sellAllConfirm",
				displayText = "Transaction cancelled.\n\n\n",
				clearPreviousText = true
			};
			TerminalNode sellAllNode = new TerminalNode {
				name = "sellAll",
				displayText = "Beginning transaction.\nRequesting to sell ALL scrap ([numScrap]) for [totalScrapValue] credits.[companyBuyingRateWarning]\n\nPlease CONFIRM or DENY.\n\n\n",
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
				displayText = "Transaction complete.\nSold [numScrapSold] scrap for [sellScrapFor].\n\nThe company is not responsible for any calculation errors.\n\n\n",
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
				displayText = "Beginning transaction.\nRequesting to sell scrap as close to [sellScrapFor] credits as possible.[companyBuyingRateWarning]\n\nPlease CONFIRM or DENY.\n\n\n",
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

			TerminalNode otherCommandsNode = __instance.terminalNodes.allKeywords.First(node => node.name == "Other").specialKeywordResult;
			otherCommandsNode.displayText = otherCommandsNode.displayText.Substring(0, otherCommandsNode.displayText.Length - 1) + ">SELL [ALL|QUOTA|<AMOUNT>]\nTo sell items on the ship.\n\n\n";

			patchedTerminal = true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.TextPostProcess))]
		public static string ProcessCustomText(string __result) {
			__result = __result.Replace("[numScrap]", ScrapHelpers.CountAllScrapInShip().ToString());
			__result = __result.Replace("[sellScrapFor]", sellScrapFor.ToString());
			__result = __result.Replace("[numScrapSold]", numScrapSold.ToString());
			__result = __result.Replace("[totalScrapValue]", ScrapHelpers.GetTotalScrapValueInShip().ToString());
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
				sellScrapFor = Convert.ToInt32(match.Groups[1].Value);
				if (sellScrapFor > 0) {
					return sellAmountNode;
				}
			}

			return __result;
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Terminal.ParsePlayerSentence))]
		public static TerminalNode TryReturnSpecialNodes(TerminalNode __result, Terminal __instance) {
			if (__result.name == "sellQuota" && TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled <= 0) {
				return specialQuotaAlreadyMetNode;
			}

			if ((__result.name == "sellQuota" || __result.name == "sellAmount") && sellScrapFor > ScrapHelpers.GetTotalScrapValueInShip()) {
				return specialNotEnoughScrapNode;
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
		}
	}
}
