using System.Threading;

namespace QuestProModule
{
  public class SyncCell<T> where T : class, new()
  {
    private T _current = new();
    public void Swap(ref T toSwap)
    {
      var prevCurrent = Interlocked.Exchange(ref _current, toSwap);
      toSwap = prevCurrent;
    }
  }
}
