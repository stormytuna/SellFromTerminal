using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace SellFromTerminal.Patches
{
	[HarmonyPatch]
	public class NetworkObjectManager
	{
		private static GameObject networkPrefab;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
		public static void LoadNetworkPrefab() {
			if (networkPrefab != null) {
				return;
			}

			networkPrefab = SellFromTerminalBase.CustomAssets.LoadAsset<GameObject>("SellFromTerminalNetworkHandler");
			networkPrefab.AddComponent<NetworkHandler>();

			NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
		public static void SpawnNetworkHandlerObject() {
			if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) {
				GameObject networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
				networkHandlerHost.GetComponent<NetworkObject>().Spawn();
			}
		}
	}
}
