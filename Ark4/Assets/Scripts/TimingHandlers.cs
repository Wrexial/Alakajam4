using MEC;

public class TimingHandlers
{
    public static void CleanlyKillCoroutine(ref CoroutineHandle? handler)
    {
        if (handler.HasValue)
        {
            Timing.KillCoroutines(handler.Value);
            handler = null;
        }
    }
}