using System.Threading;

namespace Sukun.Windows;

// One lock for installed, portable, Debug and preview builds in the Windows session.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex;
    private readonly EventWaitHandle activation;
    private RegisteredWaitHandle? listener;
    private bool disposed;
    public bool IsPrimary { get; }

    public SingleInstance(string name = "Local\\Sukun.Desktop")
    {
        mutex = new Mutex(false, name);
        try { IsPrimary = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { IsPrimary = true; }
        activation = new EventWaitHandle(false, EventResetMode.AutoReset, name + ".Activate");
    }
    public void RequestActivation() => activation.Set();
    public void Listen(Action activate)
    {
        if (!IsPrimary) throw new InvalidOperationException("Only the primary instance can listen.");
        listener = ThreadPool.RegisterWaitForSingleObject(activation, (_, _) =>
        { if (!Volatile.Read(ref disposed)) activate(); }, null, Timeout.Infinite, false);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        listener?.Unregister(null);
        activation.Dispose();
        if (IsPrimary) mutex.ReleaseMutex();
        mutex.Dispose();
    }
}
