using System;
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
				// TODO: Move into a ScrapHelpers.GetAllSellableScrapInShip method, also add some config options for gifts, shotguns and shotgun shells
				if (scrap.itemProperties.itemName == "Gift" || scrap.isHeld || scrap.scrapValue <= 0) {
					continue;
				}

				// TODO: Abstract into a "Sell Item" method we can reuse
				float actualSellValue = scrap.scrapValue * StartOfRound.Instance.companyBuyingRate;
				int creditsToGain = (int)Math.Round(actualSellValue);

				totalItemsSold++;
				totalCreditsGained += creditsToGain;

				HUDManager.Instance.terminalScript.groupCredits += creditsToGain;
				StartOfRound.Instance.gameStats.scrapValueCollected += creditsToGain;
				TimeOfDay.Instance.quotaFulfilled += creditsToGain;
				TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();

				/* TODO: Testing, remove this!
				if (NetworkManager.Singleton.IsServer) {
					scrap.GetComponent<NetworkObject>().Despawn();
				}

				if (scrap.radarIcon != null) {
					Destroy(scrap.radarIcon.gameObject);
				}
				*/
			}

			HUDManager.Instance.DisplayGlobalNotification($"Sold {totalItemsSold} pieces of scrap for {totalCreditsGained}!");
			// TODO: Advance quota if we meet/exceed it
		}

		[ClientRpc]
		public void SellAmountClientRpc(int amount) {
			int totalItemsSold = 0;
			int totalCreditsGained = 0;

			foreach (GrabbableObject scrap in ScrapHelpers.GetScrapForAmount(amount)) {
				float actualSellValue = scrap.scrapValue * StartOfRound.Instance.companyBuyingRate;
				int creditsToGain = (int)Math.Round(actualSellValue);

				totalItemsSold++;
				totalCreditsGained += creditsToGain;

				HUDManager.Instance.terminalScript.groupCredits += creditsToGain;
				StartOfRound.Instance.gameStats.scrapValueCollected += creditsToGain;
				TimeOfDay.Instance.quotaFulfilled += creditsToGain;
				TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();

				/* TODO: Testing, remove this!
				if (NetworkManager.Singleton.IsServer) {
					scrap.GetComponent<NetworkObject>().Despawn();
				}

				if (scrap.radarIcon != null) {
					Destroy(scrap.radarIcon.gameObject);
				}
				*/
			}

			HUDManager.Instance.DisplayGlobalNotification($"Sold {totalItemsSold} pieces of scrap for {totalCreditsGained}!");
			// TODO: Advance quota if we meet it
		}

		[ServerRpc(RequireOwnership = false)]
		public void SellAllScrapServerRpc() {
			SellAllScrapClientRpc();
		}

		[ServerRpc(RequireOwnership = false)]
		public void SellAmountServerRpc(int amount) {
			SellAmountClientRpc(amount);
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
