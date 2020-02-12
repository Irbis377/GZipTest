using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    /// <summary>
    /// This class need to control Queue<Buffer> with using locking
    /// </summary>
    class QueueManager<Buffer>
    {
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private Queue<Buffer> queue = new Queue<Buffer>();

        private ManualResetEvent empty = new ManualResetEvent(false);
        private ManualResetEvent full = new ManualResetEvent(true);
        private ManualResetEvent exit = new ManualResetEvent(false);

        private WaitHandle[] emptyAndExit;

        private object dequeueLocker = new object();
        private object enqueueLocker = new object();

        private readonly int size;

        public QueueManager(int _size)
        {
            size = _size;
            emptyAndExit = new WaitHandle[] { empty, exit };
        }
        public int Count
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    return queue.Count;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }

        public void Exit()
        {
            exit.Set();
        }

        public Buffer Dequeue()
        {
            lock (dequeueLocker)
            {
                if (exit.WaitOne(0) && !empty.WaitOne(0))
                {
                    return default(Buffer);
                }
                WaitHandle.WaitAny(emptyAndExit);
                locker.EnterWriteLock();
                try
                {
                    Buffer ret = default(Buffer);
                    if (queue.Count > 0)
                    {
                        full.Set();
                        ret = queue.Dequeue();
                        if (queue.Count == 0)
                        {
                            empty.Reset();
                        }
                    }

                    return ret;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }

        public void Enqueue(Buffer buff)
        {
            lock (enqueueLocker)
            {
                locker.EnterWriteLock();
                try
                {
                    if (queue.Count > size)
                    {
                        full.Reset();
                        locker.ExitWriteLock();
                        full.WaitOne();
                        locker.EnterWriteLock();
                    }
                    queue.Enqueue(buff);
                }
                finally
                {
                    locker.ExitWriteLock();
                }
                empty.Set();
            }
        }
    }
}
