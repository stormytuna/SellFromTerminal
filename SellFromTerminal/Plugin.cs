using System;
using System.IO;
using System.Reflection;
using BepInEx;
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
		public const string ModVersion = "1.0.0";

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

			harmony.PatchAll();
		}
	}
}
