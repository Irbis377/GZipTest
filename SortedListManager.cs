using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    class SortedListManager<Int32, Buffer>
    {
            private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
            private SortedList<Int32, Buffer> list = new SortedList<Int32, Buffer>();

            private ManualResetEvent empty = new ManualResetEvent(false);
            private ManualResetEvent exit = new ManualResetEvent(false);
            private WaitHandle[] emptyAndExit;
            private object retrieveLocker = new object();

            public SortedListManager()
            {
                emptyAndExit = new WaitHandle[] { empty, exit };
            }

            public int Count
            {
                get
                {
                    locker.EnterReadLock();
                    try
                    {
                        return list.Count;
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

            public void Add(Int32 key, Buffer value)
            {
                locker.EnterWriteLock();
                try
                {
                    list.Add(key, value);
                    empty.Set();
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }

            public bool TryRetrieveValue(Int32 key, out Buffer value)
            {
                lock (retrieveLocker)
                {
                    value = default(Buffer);
                    if (exit.WaitOne(0) && !empty.WaitOne(0))
                    {
                        return false;
                    }
                    WaitHandle.WaitAny(emptyAndExit);
                    locker.EnterWriteLock();
                    try
                    {
                        bool bRet = list.TryGetValue(key, out value);
                        if (bRet)
                        {
                            list.Remove(key);
                            if (list.Count == 0)
                            {
                                empty.Reset();
                            }
                        }

                        return bRet;
                    }
                    finally
                    {
                        locker.ExitWriteLock();
                    }
                }
            }
        }
    }
