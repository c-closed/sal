namespace Sboard접속기.Services;

public class SingleInstance : IDisposable
{
    private IntPtr _mutex;
    private bool _disposed;

    public bool TryAcquire(string mutexName)
    {
        _mutex = NativeMethods.CreateMutexW(IntPtr.Zero, true, mutexName);
        if (_mutex == IntPtr.Zero)
            return true;

        uint err = NativeMethods.GetLastError();
        if (err == NativeMethods.ERROR_ALREADY_EXISTS)
        {
            NativeMethods.CloseHandle(_mutex);
            _mutex = IntPtr.Zero;
            return false;
        }
        return true;
    }

    public void Dispose()
    {
        if (!_disposed && _mutex != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_mutex);
            _mutex = IntPtr.Zero;
        }
        _disposed = true;
    }
}
