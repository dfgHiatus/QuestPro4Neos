using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.Net;
using QuestProModule.ALXR;

namespace QuestProModule
{
    public class QuestProMod: NeosMod
	{
		public static ModConfiguration config;

		[AutoRegisterConfigKey]
		public static ModConfigurationKey<IPAddress> QuestProIP = new ModConfigurationKey<IPAddress>("quest_pro_IP", "Quest Pro IP. This can be found in ALXR's settings, requires a restart to take effect", () => IPAddress.Parse("192.168.1.163"));

		public static ALXRModule qpm;

		public override string Name => "QuestPro4Neos";
		public override string Author => "dfgHiatus";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/QuestPro4Neos";
		public override void OnEngineInit()
		{
			qpm = new ALXRModule();
			qpm.Initialize();

			config = GetConfiguration();
			new Harmony("net.dfgHiatus.QuestPro4Neos").PatchAll();
			Engine.Current.InputInterface.RegisterInputDriver(new EyeDevice());
			Engine.Current.OnReady += () =>
				Engine.Current.InputInterface.RegisterInputDriver(new MouthDevice());

			Engine.Current.OnShutdown += () => qpm.Teardown();
		}
	}
}
