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
        private readonly static ModConfigurationKey<float> EyeOpennessExponent = new ModConfigurationKey<float>("quest_pro_eye_open_exponent", "Exponent to apply to eye openness.  Can be updated at runtime.  Useful for applying different curves for how open your eyes are.", () => 1.0f);

        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<float> EyeWideMultiplier = new ModConfigurationKey<float>("quest_pro_eye_wide_multiplier", "Multiplier to apply to eye wideness.  Can be updated at runtime.  Useful for multiplying the amount your eyes can widen by.", () => 1.0f);

        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<float> EyeMovementMultiplier = new ModConfigurationKey<float>("quest_pro_eye_movement_multiplier", "Multiplier to adjust the movement range of the user's eyes.  Can be updated at runtime.", () => 1.0f);

        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<float> EyeExpressionMultiplier = new ModConfigurationKey<float>("quest_pro_eye_expression_multiplier", "Multiplier to adjust the range of the user's eye expressions.  Can be updated at runtime.", () => 1.0f);
        [AutoRegisterConfigKey]
        private readonly static ModConfigurationKey<bool> AutoStartALXRClient = new ModConfigurationKey<bool>("quest_pro_alxr_auto_start", "Auto-starts built-in ALXR if it's not presently running.  Defaults to off.", () => false);


        public static ALXRModule qpm;

        static ModConfiguration _config;

        public static float EyeOpenExponent = 1.0f;
        public static float EyeWideMult = 1.0f;
        public static float EyeMoveMult = 1.0f;
        public static float EyeExpressionMult = 1.0f;
        public static bool AutoStartALXR = false;

        public override string Name => "QuestPro4Neos";
		public override string Author => "dfgHiatus & Geenz";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/QuestPro4Neos";

		public override void OnEngineInit()
		{
            _config = GetConfiguration();

            _config.OnThisConfigurationChanged += OnConfigurationChanged;

            new Harmony("net.dfgHiatus.QuestPro4Neos").PatchAll();
		}

        [HarmonyPatch(typeof(InputInterface), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(Engine) })]
        public class InputInterfaceCtorPatch
        {
            public static void Postfix(InputInterface __instance)
            {
                qpm = new ALXRModule();

                qpm.Initialize(_config.GetValue(QuestProIP));

                __instance.RegisterInputDriver(qpm);

                Engine.Current.OnShutdown += () => qpm.Teardown();
            }
        }

        private void OnConfigurationChanged(ConfigurationChangedEvent @event)
        {
            if (@event.Key == EyeOpennessExponent)
            {
                float openExp = 1.0f;
                if (@event.Config.TryGetValue(EyeOpennessExponent, out openExp))
                {
                    EyeOpenExponent = openExp;
                }
            }

            if (@event.Key == EyeWideMultiplier)
            {
                float wideMult = 1.0f;
                if (@event.Config.TryGetValue(EyeWideMultiplier, out wideMult))
                {
                    EyeWideMult = wideMult;
                }
            }

            if (@event.Key == QuestProIP)
            {
                // Should probably re-initialize the device if this changes for whatever reason.
            }

            if (@event.Key == EyeMovementMultiplier)
            {
                float moveMult = 1.0f;
                if (@event.Config.TryGetValue(EyeMovementMultiplier, out moveMult))
                {
                    EyeMoveMult = moveMult;
                }
            }

            if (@event.Key == AutoStartALXRClient)
            {
                bool autostart = false;
                if (@event.Config.TryGetValue(AutoStartALXRClient, out autostart))
                {
                    AutoStartALXR = autostart;
                }
            }
        }
    }
}
