using SellFromTerminal.Patches;
using Unity.Netcode;

namespace SellFromTerminal
{
	public class NetworkHandler : NetworkBehaviour
	{
		public static NetworkHandler Instance { get; private set; }

		private int SellScrap(GrabbableObject scrap) {
			int creditsToGain = scrap.ActualSellValue();

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

			return creditsToGain;
		}

		[ClientRpc]
		public void SellAllScrapClientRpc() {
			int totalItemsSold = 0;
			int totalCreditsGained = 0;

			foreach (GrabbableObject scrap in ScrapHelpers.GetAllScrapInShip()) {
				int creditsGained = SellScrap(scrap);
				totalCreditsGained += creditsGained;
				totalItemsSold++;
			}

			HUDManager.Instance.DisplayGlobalNotification($"Sold {totalItemsSold} pieces of scrap for {totalCreditsGained}!");
			// Hacky fix for showing the actual amount of items sold and credits gained
			TerminalPatch.numScrapSold = totalItemsSold;
			TerminalPatch.sellScrapFor = totalCreditsGained;

			if (TimeOfDay.Instance.quotaFulfilled >= TimeOfDay.Instance.profitQuota && SellFromTerminalBase.ConfigCanAdvanceQuotaWhenFulfilled.Value) {
				StartOfRound.Instance.PassTimeToNextDay(StartOfRound.Instance.connectedPlayersAmount);
				TimeOfDay.Instance.SetNewProfitQuota();
			}
		}

		[ClientRpc]
		public void SellAmountClientRpc(int amount) {
			int totalItemsSold = 0;
			int totalCreditsGained = 0;

			foreach (GrabbableObject scrap in ScrapHelpers.GetScrapForAmount(amount)) {
				int creditsGained = SellScrap(scrap);
				totalCreditsGained += creditsGained;
				totalItemsSold++;
			}

			HUDManager.Instance.DisplayGlobalNotification($"Sold {totalItemsSold} pieces of scrap for {totalCreditsGained}!");
			// Hacky fix for showing the actual amount of items sold and credits gained
			TerminalPatch.numScrapSold = totalItemsSold;
			TerminalPatch.sellScrapFor = totalCreditsGained;

			if (TimeOfDay.Instance.quotaFulfilled >= TimeOfDay.Instance.profitQuota && SellFromTerminalBase.ConfigCanAdvanceQuotaWhenFulfilled.Value) {
				StartOfRound.Instance.PassTimeToNextDay(StartOfRound.Instance.connectedPlayersAmount);
				TimeOfDay.Instance.SetNewProfitQuota();
			}
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
