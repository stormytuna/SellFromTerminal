using SellFromTerminal.Patches;
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
				// TODO: Abstract into a "Sell Item" method we can reuse
				int creditsToGain = scrap.ActualSellValue();
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
			// Hacky fix for showing the actual amount of items sold and credits gained
			TerminalPatch.numScrapSold = totalItemsSold;
			TerminalPatch.sellScrapFor = totalCreditsGained;
			// TODO: Advance quota if we meet/exceed it
		}

		[ClientRpc]
		public void SellAmountClientRpc(int amount) {
			int totalItemsSold = 0;
			int totalCreditsGained = 0;

			foreach (GrabbableObject scrap in ScrapHelpers.GetScrapForAmount(amount)) {
				int creditsToGain = scrap.ActualSellValue();
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
			// Hacky fix for showing the actual amount of items sold and credits gained
			TerminalPatch.numScrapSold = totalItemsSold;
			TerminalPatch.sellScrapFor = totalCreditsGained;
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
