using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ao3TrackReader
{
    static public class ReaderWriterLockSlimExtensions
    {
        public struct ReadLockStruct : IDisposable
        {
            private ReaderWriterLockSlim rwlock;

            public ReadLockStruct(ReaderWriterLockSlim rwlock)
            {
                this.rwlock = rwlock;
                rwlock.EnterReadLock();
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
                rwlock.EnterWriteLock();
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
                rwlock.EnterUpgradeableReadLock();
            }

            public void Dispose()
            {
                rwlock.ExitUpgradeableReadLock();
            }
        }

        public static ReadLockStruct ReadLock(this ReaderWriterLockSlim rwlock)
        {
            return new ReadLockStruct(rwlock);
        }

        public static WriteLockStruct WriteLock(this ReaderWriterLockSlim rwlock)
        {
            return new WriteLockStruct(rwlock);
        }

        public static UpgradeableReadLockStruct UpgradeableReadLock(this ReaderWriterLockSlim rwlock)
        {
            return new UpgradeableReadLockStruct(rwlock);
        }

    }
}
