using BaseX;
using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace QuestProModule;

internal class AlvrMonitor : IDisposable
{
  public string ClientName { get; set; }
  public string ClientPath { get; set; }
  public string Arguments { get; set; }

  private readonly Timer _timer;

  private Process _foundProcess;

  public AlvrMonitor()
  {
    _timer = new(1000);
    _timer.Elapsed += (o, e) => CheckProcess();
  }

  void CheckProcess()
  {
    if (!CheckIfRunning())
    {
      StartClient();
    }
  }

  private bool CheckIfRunning()
  {
    Process[] alvrClients = Process.GetProcessesByName("alxr-client");
    foreach (Process client in alvrClients)
    {
      if (_foundProcess is { HasExited: false })
      {
        client.Dispose();
        continue;
      }

      WatchProcess(client);
    }

    return !(_foundProcess?.HasExited ?? true);
  }

  private void WatchProcess(Process process)
  {
    _foundProcess?.Dispose();
    _foundProcess = null;

    if (process.HasExited)
    {
      return;
    }

    _foundProcess = process;

    _foundProcess.Exited += (o, e) => _timer.Start();
  }

  private void StartClient()
  {
    try
    {
      string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      string clientPath = Path.Combine(directory, ClientPath);
      var process = Process.Start(clientPath, Arguments);
      
      UniLog.Log($"Starting ALXR at: {process.StartInfo.FileName}");
      WatchProcess(process);
    }
    catch
    {
      UniLog.Log($"Failed to start process at {ClientPath}");
    }
  }

  public void Dispose()
  {
    _timer.Dispose();
    _foundProcess?.Dispose();
  }
}