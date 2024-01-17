﻿using System;
using Unity.Netcode;

namespace SellFromTerminal
{
	public class NetworkHandler : NetworkBehaviour
	{
		public static NetworkHandler Instance { get; private set; }

		[ClientRpc]
		public void SellAllScrapClientRpc() {
			int totalItemsSold = 0;
			int totalCreditsGained = 0;

			foreach (GrabbableObject scrap in ScrapHelpers.GetAllScrapInShip()) {
				if (scrap.itemProperties.itemName == "Gift" || scrap.isHeld || scrap.scrapValue <= 0) {
					continue;
				}

				float actualSellValue = scrap.scrapValue * StartOfRound.Instance.companyBuyingRate;
				int creditsToGain = (int)Math.Round(actualSellValue);

				totalItemsSold++;
				totalCreditsGained += creditsToGain;

				HUDManager.Instance.terminalScript.groupCredits += creditsToGain;
				StartOfRound.Instance.gameStats.scrapValueCollected += creditsToGain;
				TimeOfDay.Instance.quotaFulfilled += creditsToGain;
				TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();

				if (NetworkManager.Singleton.IsServer) {
					scrap.GetComponent<NetworkObject>().Despawn();
				}

				if (scrap.radarIcon != null) {
					Destroy(scrap.radarIcon.gameObject);
				}
			}

			HUDManager.Instance.DisplayGlobalNotification($"Sold {totalItemsSold} pieces of scrap for {totalCreditsGained}!");
			// TODO: Advance quota if we meet/exceed it
		}

		[ClientRpc]
		public void SellQuotaScrapClientRpc() {
			int totalItemsSold = 0;
			int totalCreditsGained = 0;

			foreach (GrabbableObject scrap in ScrapHelpers.GetScrapForQuota()) {
				float actualSellValue = scrap.scrapValue * StartOfRound.Instance.companyBuyingRate;
				int creditsToGain = (int)Math.Round(actualSellValue);

				totalItemsSold++;
				totalCreditsGained += creditsToGain;

				HUDManager.Instance.terminalScript.groupCredits += creditsToGain;
				StartOfRound.Instance.gameStats.scrapValueCollected += creditsToGain;
				TimeOfDay.Instance.quotaFulfilled += creditsToGain;
				TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();

				if (NetworkManager.Singleton.IsServer) {
					scrap.GetComponent<NetworkObject>().Despawn();
				}

				if (scrap.radarIcon != null) {
					Destroy(scrap.radarIcon.gameObject);
				}
			}

			HUDManager.Instance.DisplayGlobalNotification($"Sold {totalItemsSold} pieces of scrap for {totalCreditsGained}!");
			// TODO: Advance quota if we meet it
		}

		[ServerRpc(RequireOwnership = false)]
		public void SellAllScrapServerRpc() {
			SellAllScrapClientRpc();
		}

		[ServerRpc(RequireOwnership = false)]
		public void SellQuotaScrapServerRpc() {
			SellQuotaScrapClientRpc();
		}

		public override void OnNetworkSpawn() {
			if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) {
				Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
			}

			Instance = this;

			base.OnNetworkSpawn();
		}
	}
}