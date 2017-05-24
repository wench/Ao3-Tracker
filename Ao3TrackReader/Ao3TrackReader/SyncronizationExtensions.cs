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
                disposed = false;
            }

            bool disposed;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                rwlock.ExitReadLock();
            }

            public void TaskRun(Action action)
            {
                try
                {
                    var task = Task.Run(action);
                    task.Wait();
                }
                catch (AggregateException ae)
                {
                    ae.Flatten();
                }
                finally
                {
                    Dispose();
                }
            }

            public void TaskRun(Func<Task> action)
            {
                try
                {
                    var task = Task.Run(action);
                    task.Wait();
                }
                catch (AggregateException ae)
                {
                    ae.Flatten();
                }
                finally
                {
                    Dispose();
                }
            }

            public T TaskRun<T>(Func<T> func)
            {
                try
                {
                    var task = Task.Run(func);
                    task.Wait();
                    return task.Result;
                }
                catch (AggregateException ae)
                {
                    ae.Flatten();
                    return default(T);
                }
                finally
                {
                    Dispose();
                }
            }

            public T TaskRun<T>(Func<Task<T>> func)
            {
                try
                {
                    var task = Task.Run(func);
                    task.Wait();
                    return task.Result;
                }
                catch (AggregateException ae)
                {
                    ae.Flatten();
                    return default(T);
                }
                finally
                {
                    Dispose();
                }
            }

        }

        public struct WriteLockStruct : IDisposable
        {
            private ReaderWriterLockSlim rwlock;

            public WriteLockStruct(ReaderWriterLockSlim rwlock)
            {
                this.rwlock = rwlock;
                disposed = false;
            }

            bool disposed;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                rwlock.ExitWriteLock();
            }

            public void TaskRun(Action action)
            {
                try
                {
                    var task = Task.Run(action);
                    task.Wait();
                }
                catch (AggregateException ae)
                {
                    ae.Flatten();
                }
                finally
                {
                    Dispose();
                }
            }

            public void TaskRun(Func<Task> action)
            {
                try
                {
                    var task = Task.Run(action);
                    task.Wait();
                }
                catch (AggregateException ae)
                {
                    ae.Flatten();
                }
                finally
                {
                    Dispose();
                }
            }

            public T TaskRun<T>(Func<T> func)
            {
                try
                {
                    var task = Task.Run(func);
                    task.Wait();
                    return task.Result;
                }
                catch (AggregateException ae)
                {
                    ae.Flatten();
                    return default(T);
                }
                finally
                {
                    Dispose();
                }
            }
            public T TaskRun<T>(Func<Task<T>> func)
            {
                try
                {
                    var task = Task.Run(func);
                    task.Wait();
                    return task.Result;
                }
                catch (AggregateException ae)
                {
                    ae.Flatten();
                    return default(T);
                }
                finally
                {
                    Dispose();
                }
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
