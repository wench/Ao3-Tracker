using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ao3TrackReader
{
    public class InjectionSequencer : IDisposable
    {
        CancellationTokenSource cts;
        SemaphoreSlim phase1;
        SemaphoreSlim phase2;

        public InjectionSequencer(Func<InjectionSequencer, Task> action)
        {
            cts = new CancellationTokenSource();
            phase1 = new SemaphoreSlim(0, 1);
            phase2 = new SemaphoreSlim(0, 1);
            Task.Run(async () => { await action(this); Dispose(); }, cts.Token).ConfigureAwait(false);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    phase1.Dispose();
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
        public Task Phase1 => phase1.WaitAsync(cts.Token);
        public Task Phase2 => phase2.WaitAsync(cts.Token);

        public void Cancel()
        {
            if (!disposedValue && !IsCancellationRequested) cts.Cancel();
        }
        public void StartPhase1()
        {
            if (!disposedValue && !IsCancellationRequested) phase1.Release();
        }
        public void StartPhase2()
        {
            if (!disposedValue && !IsCancellationRequested) phase2.Release();
        }
    }
}
