using System;
using System.Diagnostics;
using BaseX;

namespace QuestProModule;

internal class AlvrMonitor : IDisposable
{
  private Process _alxrProcess;

  private bool IsAlxrRunning
  {
    get
    {
      Process[] pname = Process.GetProcessesByName("alxr-client");
      UniLog.Log($"ALXR processes: {pname.Length}");
      return pname.Length > 0;
    }
  }

  private bool StartAlxr()
  {
    // Attempt to start ALXR.
    _alxrProcess = new Process();
    string neosPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
    neosPath = System.IO.Path.GetDirectoryName(neosPath);

    _alxrProcess.StartInfo.FileName = neosPath + "/alxr_client_windows/alxr-client.exe";
    _alxrProcess.StartInfo.Arguments = "--no-alvr-server --no-bindings";

    UniLog.Log($"Starting ALXR at: {_alxrProcess.StartInfo.FileName}");

    return _alxrProcess.Start();
  }

  public void Dispose()
  {
    _alxrProcess?.Dispose();
  }
}