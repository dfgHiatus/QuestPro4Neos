using BaseX;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OscCore;

namespace QuestProModule;

public class AlvrConnection : IDisposable
{
  private readonly SyncCell<FbMessage> _messageTarget;

  /// <summary>
  /// A message owned by this reader that is currently being mutated.
  /// </summary>
  private FbMessage _workingMessage = new();

  private readonly UdpClient _client;
  private readonly CancellationTokenSource _stopToken = new();
  private readonly Task _listenTask;

  public AlvrConnection(int port, SyncCell<FbMessage> target)
  {
    _messageTarget = target;
    _client = new UdpClient(port);
    _listenTask = Task.Run(ListenAsync);
    UniLog.Log($"Opening Alvr connection on {port}");
  }

  private async Task ListenAsync()
  {
    while (!_stopToken.IsCancellationRequested)
    {
      try
      {
        var got = await _client.ReceiveAsync();
        var message = new OscMessageRaw(new ArraySegment<byte>(got.Buffer));
        switch (message.Address)
        {
          case "/tracking/eye/left/Quat":
          case "/tracking/eye/left/Active":
          case "/tracking/eye/right/Quat":
          case "/tracking/eye/right/Active":
            // Ignore these, we do this ourselves.
            break;
          case "/tracking/eye_htc":
          case "/tracking/lip_htc":
            UniLog.Error("Unexpected ALVR message in loading area, please use facebook eye tracking.");
            break;
          case "/tracking/face_fb":
            _workingMessage.ParseOsc(message);
            if (!_stopToken.IsCancellationRequested)
            {
              _messageTarget.Swap(ref _workingMessage);
            }
            break;
        }
      }
      catch (Exception ex)
      {
        UniLog.Error(ex.Message);
      }
    }
  }

  public void Dispose()
  {
    UniLog.Log("Alvr connection closing.");
    _stopToken.Cancel();
    _client.Dispose();
  }
}