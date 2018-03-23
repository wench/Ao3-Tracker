using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ao3TrackReader
{
    public class InjectionSequencer : IDisposable
    {
        object locker = new object();
        CancellationTokenSource cts;
        SemaphoreSlim phase2;
        Func<InjectionSequencer, Task> action;

        public InjectionSequencer(Func<InjectionSequencer, Task> action)
        {
            cts = new CancellationTokenSource();
            phase2 = new SemaphoreSlim(0, 1);
            this.action = action;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    phase2.Dispose();
                    cts.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~InjectionSequencer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public bool IsCancellationRequested => cts.IsCancellationRequested;
        public CancellationToken Token => cts.Token;
        public Task Phase1 => Task.CompletedTask;
        public Task Phase2 => phase2.WaitAsync(cts.Token);
        int running = 0;

        public void Cancel()
        {
            if (!disposedValue && !IsCancellationRequested) cts.Cancel();
        }
        public async void StartPhase1()
        {
            if (!disposedValue && !IsCancellationRequested)
            {
                if (Interlocked.Increment(ref running) == 1)
                {
                    await action(this);
                    Dispose();
                }
            }
        }
        public void StartPhase2()
        {
            if (!disposedValue && !IsCancellationRequested)
            {
                lock (locker)
                {
                    if (phase2.CurrentCount == 0)
                        phase2.Release();
                }
            }
        }
    }
}
