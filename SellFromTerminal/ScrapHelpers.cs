﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SellFromTerminal
{
	public static class ScrapHelpers
	{
		public static IEnumerable<GrabbableObject> GetAllScrapInShip() {
			GameObject ship = GameObject.Find("Environment/HangarShip");
			GrabbableObject[] grabbableObjectsInShip = ship.GetComponentsInChildren<GrabbableObject>();
			return grabbableObjectsInShip.Where(go => go.CanSellItem());
		}

		public static IEnumerable<GrabbableObject> GetScrapForAmount(int amount) {
			// Sanity check that we actually have enough
			// Technically should never happen since terminal doesn't allow player to sell scrap if it won't meet the amount
			int totalScrapValue = GetTotalScrapValueInShip();
			if (totalScrapValue < amount) {
				SellFromTerminalBase.Log.LogInfo($"Cannot reach required amount of {amount}! Total value: {totalScrapValue}, total num scrap: {CountAllScrapInShip()}");
				return Enumerable.Empty<GrabbableObject>();
			}

			int nextScrapIndex = 0;
			List<GrabbableObject> allScrap = GetAllScrapInShip().OrderByDescending(scrap => scrap.scrapValue).ThenBy(scrap => scrap.NetworkObjectId).ToList();
			List<GrabbableObject> scrapForQuota = new List<GrabbableObject>();

			int amountNeeded = amount;
			// Highest value scrap is 210, we only want to go 2 sums deep to keep computational complexity to a minimum so we keep taking until we have 420 credits left
			while (amountNeeded > 420) {
				GrabbableObject nextScrap = allScrap[nextScrapIndex++];
				scrapForQuota.Add(nextScrap);
				amountNeeded -= nextScrap.ActualSellValue();
			}

			// Time to actually be precise
			allScrap = allScrap.Skip(nextScrapIndex).OrderBy(scrap => scrap.scrapValue).ThenBy(scrap => scrap.NetworkObjectId).ToList();
			nextScrapIndex = 0;

			while (amountNeeded > 0) {
				List<(GrabbableObject first, GrabbableObject second, int sum)> sums = new List<(GrabbableObject first, GrabbableObject second, int sum)>();
				for (int i = nextScrapIndex; i < allScrap.Count; i++) {
					for (int j = i + 1; j < allScrap.Count; j++) {
						// Starting second loop at i+1 lets us skip redundant sums
						sums.Add((allScrap[i], allScrap[j], allScrap[i].ActualSellValue() + allScrap[j].ActualSellValue()));
					}
				}

				(GrabbableObject first, GrabbableObject second, int sum) foundSum = sums.FirstOrDefault(sum => sum.sum >= amountNeeded && sum.sum <= amountNeeded + SellFromTerminalBase.ConfigExactAmountAllowance.Value);
				if (foundSum != default) {
					scrapForQuota.Add(foundSum.first);
					scrapForQuota.Add(foundSum.second);
					return scrapForQuota;
				}

				// If we haven't found a sum, we take the next scrap and continue our sums
				GrabbableObject nextScrap = allScrap[nextScrapIndex++];
				scrapForQuota.Add(nextScrap);
				amountNeeded -= nextScrap.ActualSellValue();
			}

			// Worst case scenario, we found no sums :(
			// Whatever we have will have to do
			SellFromTerminalBase.Log.LogInfo("Couldn't find a way to perfectly meet quota :(");
			return scrapForQuota;
		}

		public static int CountAllScrapInShip() => GetAllScrapInShip().Count();

		public static int GetTotalScrapValueInShip() => GetAllScrapInShip().Sum(scrap => scrap.ActualSellValue());

		public static bool CanSellItem(this GrabbableObject item) {
			bool canSell = item.itemProperties.isScrap && item.scrapValue > 0 && !item.isHeld;
			bool canSellShotgun = item.itemProperties.name != "Shotgun" || SellFromTerminalBase.ConfigCanSellShotgunAndShells.Value;
			bool canSellShotgunShell = item.itemProperties.name != "GunAmmo" || SellFromTerminalBase.ConfigCanSellShotgunAndShells.Value;
			bool canSellGift = item.itemProperties.name != "GiftBox" || SellFromTerminalBase.ConfigCanSellGifts.Value;
			bool canSellPickles = item.itemProperties.name != "PickleJar" || SellFromTerminalBase.ConfigCanSellPickles.Value;
			return canSell && canSellShotgun && canSellShotgunShell && canSellGift && canSellPickles;
		}

		public static int ActualSellValue(this GrabbableObject scrap) {
			float actualSellValue = scrap.scrapValue * StartOfRound.Instance.companyBuyingRate;
			return (int)Math.Round(actualSellValue);
		}
	}
}
