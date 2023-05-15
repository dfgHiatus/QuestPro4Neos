using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.Threading;

namespace QuestProModule;

public class QuestProMod : NeosMod
{
  [AutoRegisterConfigKey] private static readonly ModConfigurationKey<int> AlvrListenPort =
    new("quest_pro_alvr_port",
      "The port alvr will communicate with, this should be VrcFaceTracking Osc mode in alvr.", () => DefaultPort);

  [AutoRegisterConfigKey] private static readonly ModConfigurationKey<float> EyeOpennessExponent =
    new("quest_pro_eye_open_exponent",
      "Exponent to apply to eye openness.  Can be updated at runtime.  Useful for applying different curves for how open your eyes are.",
      () => 1.0f);

  [AutoRegisterConfigKey] private static readonly ModConfigurationKey<float> EyeWideMultiplier =
    new("quest_pro_eye_wide_multiplier",
      "Multiplier to apply to eye wideness.  Can be updated at runtime.  Useful for multiplying the amount your eyes can widen by.",
      () => 1.0f);

  [AutoRegisterConfigKey] private static readonly ModConfigurationKey<float> EyeMovementMultiplier =
    new("quest_pro_eye_movement_multiplier",
      "Multiplier to adjust the movement range of the user's eyes.  Can be updated at runtime.", () => 1.0f);

  [AutoRegisterConfigKey] private static readonly ModConfigurationKey<float> EyeExpressionMultiplier =
    new("quest_pro_eye_expression_multiplier",
      "Multiplier to adjust the range of the user's eye expressions.  Can be updated at runtime.", () => 1.0f);

  [AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> AutoStartAlvrClient =
    new("quest_pro_alxr_auto_start",
      "Auto-starts built-in ALXR if it's not presently running.  Defaults to off.", () => false);

  [AutoRegisterConfigKey]
  private static readonly ModConfigurationKey<string> AlvrClientName =
    new("quest_pro_alxr_client_name",
      "The name of the server to look for in the system process pool. Defaults to alxr-client",
      () => "alxr-client");

  [AutoRegisterConfigKey] private static readonly ModConfigurationKey<string> AlvrClientPath =
    new("quest_pro_alxr_client_path",
      "The path to the alvr server itself.  Defaults to {neos root}/alxr_client_windows/alxr-client.exe, this path is relative to the neos root.",
      () => "./alxr_client_windows/alxr-client.exe");

  [AutoRegisterConfigKey] private static readonly ModConfigurationKey<string> AlvrArguments =
    new("quest_pro_alxr_arguments",
      "Arguments to start alvr with.  Defaults to --no-alvr-server --no-bindings",
      () => "--no-alvr-server --no-bindings");

  private static ModConfiguration _config;

  private static SyncCell<FbMessage> _store;
  private static AlvrConnection _connection;
  private static FbInputDriver _driver;
  private static AlvrMonitor _monitor;

  /// <summary>
  /// The default port from ALVRs VRCFaceTracking OCS connection
  /// </summary>
  private const int DefaultPort = 9620;

  public override string Name => "QuestPro4Neos";
  public override string Author => "dfgHiatus & Geenz & Earthmark";
  public override string Version => "2.0.0";
  public override string Link => "https://github.com/dfgHiatus/QuestPro4Neos";

  public override void OnEngineInit()
  {
    _config = GetConfiguration();
    _config.OnThisConfigurationChanged += OnConfigurationChanged;

    new Harmony("net.dfgHiatus.QuestPro4Neos").PatchAll();
  }

  [HarmonyPatch(typeof(InputInterface), MethodType.Constructor)]
  [HarmonyPatch(new[] { typeof(Engine) })]
  public class InputInterfaceCtorPatch
  {
    public static void Postfix(InputInterface __instance)
    {
      _store = new SyncCell<FbMessage>();
      _connection = new AlvrConnection(_config.TryGetValue(AlvrListenPort, out var port) ? port : DefaultPort, _store);
      _driver = new FbInputDriver(_store);
      __instance.RegisterInputDriver(_driver);

      if (_config.TryGetValue(AutoStartAlvrClient, out var enable) && enable)
      {
        StartMonitor();
      }

      Engine.Current.OnShutdown += () =>
      {
        _connection?.Dispose();
        _monitor?.Dispose();
      };
    }
  }

  private static void StartMonitor()
  {
    _monitor?.Dispose();
    _monitor = new AlvrMonitor
    {
      ClientName = _config.TryGetValue(AlvrClientName, out var name) ? name : null,
      ClientPath = _config.TryGetValue(AlvrClientPath, out var path) ? path : null,
      Arguments = _config.TryGetValue(AlvrArguments, out var args) ? args : null,
    };
  }

  private void OnConfigurationChanged(ConfigurationChangedEvent @event)
  {
    if (@event.Key == EyeOpennessExponent)
    {
      if (@event.Config.TryGetValue(EyeOpennessExponent, out var openExp))
      {
        _driver.EyeOpenExponent = openExp;
      }
    }

    if (@event.Key == EyeWideMultiplier)
    {
      if (@event.Config.TryGetValue(EyeWideMultiplier, out var wideMulti))
      {
        _driver.EyeWideMulti = wideMulti;
      }
    }

    if (@event.Key == EyeMovementMultiplier)
    {
      if (@event.Config.TryGetValue(EyeMovementMultiplier, out var moveMulti))
      {
        _driver.EyeMoveMulti = moveMulti;
      }
    }

    if (@event.Key == EyeExpressionMultiplier)
    {
      if (@event.Config.TryGetValue(EyeExpressionMultiplier, out var eyeExpressionMulti))
      {
        _driver.EyeExpressionMulti = eyeExpressionMulti;
      }
    }

    if (@event.Key == AutoStartAlvrClient)
    {
      if (@event.Config.TryGetValue(AutoStartAlvrClient, out var autoStart) && autoStart)
      {
        StartMonitor();
      }
      else
      {
        _monitor?.Dispose();
        _monitor = null;
      }
    }

    if (@event.Key == AlvrClientName)
    {
      if (@event.Config.TryGetValue(AlvrClientName, out var clientName))
      {
        if (_monitor != null)
        {
          _monitor.ClientName = clientName;
        }
      }
    }

    if (@event.Key == AlvrClientPath)
    {
      if (@event.Config.TryGetValue(AlvrClientPath, out var clientPath))
      {
        if (_monitor != null)
        {
          _monitor.ClientPath = clientPath;
        }
      }
    }

    if (@event.Key == AlvrArguments)
    {
      if (@event.Config.TryGetValue(AlvrArguments, out var arguments))
      {
        if (_monitor != null)
        {
          _monitor.Arguments = arguments;
        }
      }
    }

    if (@event.Key == AlvrListenPort)
    {
      if (@event.Config.TryGetValue(AlvrListenPort, out var listenPort))
      {
        _connection.Dispose();
        _connection = new AlvrConnection(listenPort, _store);
      }
    }
  }
}