using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SellFromTerminal
{
	[BepInPlugin(ModGUID, ModName, ModVersion)]
	public class SellFromTerminalBase : BaseUnityPlugin
	{
		public const string ModGUID = "stormytuna.SellFromTerminal";
		public const string ModName = "SellFromTerminal";
		public const string ModVersion = "1.1.2";

		public static ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(ModGUID);
		public static SellFromTerminalBase Instance;
		public static AssetBundle CustomAssets;

		private readonly Harmony harmony = new Harmony(ModGUID);

		private void Awake() {
			if (Instance is null) {
				Instance = this;
			}

			Log.LogInfo("Sell From Terminal has awoken!");

			// For NetworkPatcher, initialises patched NetworkBehaviours
			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
				foreach (MethodInfo method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
					object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
					if (attributes.Length > 0) {
						method.Invoke(null, null);
					}
				}
			}

			string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			CustomAssets = AssetBundle.LoadFromFile(Path.Combine(assemblyLocation, "sellfromterminalbundle"));
			if (CustomAssets is null) {
				Log.LogError("Failed to load custom assets!");
			}

			LoadConfigs();

			harmony.PatchAll();
		}

		#region Config

		public static ConfigEntry<bool> ConfigCanSellShotgunAndShells;
		public static ConfigEntry<bool> ConfigCanSellGifts;
		public static ConfigEntry<bool> ConfigCanSellPickles;
		public static ConfigEntry<int> ConfigExactAmountAllowance;

		private void LoadConfigs() {
			ConfigCanSellShotgunAndShells = Config.Bind("Can Sell",
														"CanSellShotgunAndShells",
														false,
														"Whether or not to allow the 'Shotgun' and 'Ammo' scrap to be sold");

			ConfigCanSellGifts = Config.Bind("Can Sell",
											 "CanSellGifts",
											 false,
											 "Whether or not to allow the 'Gift' item to be sold");

			ConfigCanSellPickles = Config.Bind("Can Sell",
											   "CanSellPickles",
											   true,
											   "Whether or not to allow the 'Jar of Pickles' item to be sold");

			ConfigExactAmountAllowance = Config.Bind("Misc",
													 "ExactAmountAllowance",
													 0,
													 "The amount of allowance over the specified amount to grant. Ex: Setting this to 5 will make 'sell 50' also sell at 51, 52, 53, 54 and 55");
		}

		#endregion
	}
}
