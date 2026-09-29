using BepInEx;

namespace Aryx_F22E_StrikeRaptor.FCS.Dev
{
	// Dev/debug unlock for the F-22E Strike Raptor FCS. This plugin has no code of its own: when it is loaded, the FCS
	// binds every setting other than the player ones (envelope, gains, allocator, airframe, asymmetric load, ground,
	// visuals, telemetry and logs) to this plugin's config file, BepInEx/config/Aryx_F22E_StrikeRaptor.FCS.Dev.cfg,
	// and they show in the ConfigurationManager (F1) under this entry. Remove the DLL and they go back to the built-in
	// defaults (the file is left alone and read again when the DLL comes back).
	[BepInPlugin("Aryx_F22E_StrikeRaptor.FCS.Dev", "F-22E Strike Raptor FCS - Dev/Debug settings", "1.9.7")]
	public class FcsDevPlugin : BaseUnityPlugin
	{
		private void Awake()
		{
			base.Logger.LogInfo("F-22E FCS dev/debug settings unlocked.");
		}
	}
}
