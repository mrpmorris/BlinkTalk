using System;
using System.Threading;

public static class IndicationProcessor
{
    private static Thread thread;

    public static void Start()
    {
        if (thread != null)
            throw new InvalidOperationException("Already started");
        thread = new Thread(Execute);
        thread.Start();
    }

    public static void Stop()
    {
        if (thread == null)
            throw new InvalidOperationException("Already stopped");
        thread = null;
    }

    private static void Execute()
    {
        while (thread != null)
        {
            Thread.Sleep(1000);
        }
    }
}
