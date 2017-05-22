using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ao3TrackReader
{
    static public class SyncronizationExtensions
    {
        public struct ReadLockStruct : IDisposable
        {
            private ReaderWriterLockSlim rwlock;

            public ReadLockStruct(ReaderWriterLockSlim rwlock)
            {
                this.rwlock = rwlock;
            }

            public void Dispose()
            {
                rwlock.ExitReadLock();
            }
        }

        public struct WriteLockStruct : IDisposable
        {
            private ReaderWriterLockSlim rwlock;

            public WriteLockStruct(ReaderWriterLockSlim rwlock)
            {
                this.rwlock = rwlock;
            }

            public void Dispose()
            {
                rwlock.ExitWriteLock();
            }
        }

        public struct UpgradeableReadLockStruct : IDisposable
        {
            private ReaderWriterLockSlim rwlock;

            public UpgradeableReadLockStruct(ReaderWriterLockSlim rwlock)
            {
                this.rwlock = rwlock;
            }

            public void Dispose()
            {
                rwlock.ExitUpgradeableReadLock();
            }
        }

        public static ReadLockStruct ReadLock(this ReaderWriterLockSlim rwlock)
        {
            rwlock.EnterReadLock();
            return new ReadLockStruct(rwlock);
        }

        public static WriteLockStruct WriteLock(this ReaderWriterLockSlim rwlock)
        {
            rwlock.EnterWriteLock();
            return new WriteLockStruct(rwlock);
        }

        public static UpgradeableReadLockStruct UpgradeableReadLock(this ReaderWriterLockSlim rwlock)
        {
            rwlock.EnterUpgradeableReadLock();
            return new UpgradeableReadLockStruct(rwlock);
        }


        public struct SemaphoreLockStruct : IDisposable
        {
            private SemaphoreSlim sem;

            public SemaphoreLockStruct(SemaphoreSlim sem)
            {
                this.sem = sem;
            }

            public void Dispose()
            {
                sem?.Release();
            }
        }

        public static SemaphoreLockStruct Lock(this SemaphoreSlim sem)
        {
            sem.Wait();
            return new SemaphoreLockStruct(sem);
        }

        public async static Task<SemaphoreLockStruct> LockAsync(this SemaphoreSlim sem)
        {
            await sem.WaitAsync();
            return new SemaphoreLockStruct(sem);
        }
    }
}
