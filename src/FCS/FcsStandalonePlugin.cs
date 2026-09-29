using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	[BepInPlugin("Aryx_F22E_StrikeRaptor.FCS", "F-22E Strike Raptor FCS", Version)]
	[BepInDependency("Aryx_F22E_StrikeRaptor", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(DevGuid, BepInDependency.DependencyFlags.SoftDependency)]
	public class FcsStandalonePlugin : BaseUnityPlugin
	{
		public const string Version = "1.9.7";

		// the dev/debug unlock: a separate, empty plugin DLL. When it is loaded (soft dependency, so before this one),
		// every setting other than the player ones is bound to its config file and shows in the ConfigurationManager
		// under it; without it those settings are held at their defaults in memory and never written anywhere.
		public const string DevGuid = "Aryx_F22E_StrikeRaptor.FCS.Dev";

		private void Awake()
		{
			ConfigFile dev = null;
			if (Chainloader.PluginInfos.TryGetValue(DevGuid, out PluginInfo devInfo) && devInfo != null && devInfo.Instance != null)
			{
				dev = devInfo.Instance.Config;
			}
			bool devUnlocked = dev != null;
			bool devFresh = devUnlocked && !File.Exists(dev.ConfigFilePath);
			if (!devUnlocked)
			{
				dev = new ConfigFile(Path.Combine(Paths.CachePath, "Aryx_F22E_StrikeRaptor.FCS.builtin-defaults.cfg"), saveOnInit: false);
				dev.SaveOnConfigSet = false;
			}
			Plugin.Init(base.Config, dev, devUnlocked, devFresh, base.Logger);
			Plugin.ApplyPatches(new Harmony("com.aryx.strikeraptor.fcs"));
		}
	}
}
