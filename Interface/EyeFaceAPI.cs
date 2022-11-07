using HarmonyLib;
using NeosModLoader;
using FrooxEngine;

namespace NeosEyeFaceAPI
{
    public class NeosEyeFaceAPI : NeosMod
	{
		public override string Name => "EyeFaceAPI";
		public override string Author => "dfgHiatus";
		public override string Version => "1.2.0";
		public override string Link => "https://github.com/dfgHiatus/Neos-Eye-Face-API/";
		public override void OnEngineInit()
		{
			// Harmony.DEBUG = true;
			new Harmony("net.dfgHiatus.EyeFaceAPI").PatchAll();
			Engine.Current.InputInterface.RegisterInputDriver(new EyeDevice());
			Engine.Current.OnReady += () =>
				Engine.Current.InputInterface.RegisterInputDriver(new MouthDevice());
        }
	}
}
