using System.Diagnostics;

namespace API.Functions;

public class PromsScaleStat
{
    private long totalDurationInMs = 0;
    private int alreadyAppended = 0;

    private readonly Stopwatch sw = new();
    public dynamic Value()
    {
        if (totalDurationInMs == 0)
            return "Ingest has not started. Can't provide stat";
        
        return new
        {
            AlreadyAppended = alreadyAppended,
            Duration = TimeSpan.FromMilliseconds(totalDurationInMs),
            IngestRatePerSec = alreadyAppended / (totalDurationInMs / 1000)
        };
    }

    public void Resume()
    {
        sw.Start();
    }
    public void RecordThenSuspend()
    {
        alreadyAppended++;
        totalDurationInMs += sw.ElapsedMilliseconds;
        sw.Reset();
    }
}
