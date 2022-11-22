using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.Net;
using QuestProModule.ALXR;
using BaseX;
using System;
using FrooxEngine.UIX;

namespace QuestProModule
{
    public class QuestProMod : NeosMod
	{
		[AutoRegisterConfigKey]
		private readonly static ModConfigurationKey<string> QuestProIP = new ModConfigurationKey<string>("quest_pro_IP", "Quest Pro IP. This can be found in ALXR's settings, requires a restart to take effect", () => "127.0.0.1");

        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<float> EyeOpennessExponent = new ModConfigurationKey<float>("quest_pro_eye_open_exponent", "Exponent to apply to eye openness.  Useful for applying different curves for how open your eyes are.", () => 1.0f);

		public static ALXRModule qpm;

        static ModConfiguration _config;

		public override string Name => "QuestPro4Neos";
		public override string Author => "dfgHiatus & Geenz";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/QuestPro4Neos";
		public override void OnEngineInit()
		{
            _config = GetConfiguration();

            var openness = default(float);

            if (!_config.TryGetValue(EyeOpennessExponent, out openness))
            {
                openness = 1.0f;

                _config.Set(EyeOpennessExponent, openness);
            }

            new Harmony("net.dfgHiatus.QuestPro4Neos").PatchAll();
		}

        [HarmonyPatch(typeof(InputInterface), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(Engine) })]
        public class InputInterfaceCtorPatch
        {
            public static void Postfix(InputInterface __instance)
            {
                try
                {
                    qpm = new ALXRModule();

                    qpm.Initialize(_config.GetValue(QuestProIP));

                    __instance.RegisterInputDriver(new EyeDevice(_config.GetValue(EyeOpennessExponent)));
                    __instance.RegisterInputDriver(new MouthDevice());

                    Engine.Current.OnShutdown += () => qpm.Teardown();
                }
                catch (Exception ex)
                {
                    Warn("Module failed to initiallize.");
                    Warn(ex.ToString());
                }
            }
        }

        private void OnConfigurationChanged(ConfigurationChangedEvent @event)
        {
            if (@event.Label == "NeosModSettings variable change") return;

        }
    }
}
