using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    class CommonData
    {
        public const int MaxQueueCount = 10;
        public static int ThreadCount =  Environment.ProcessorCount;
       
        public QueueManager<Buffer> dataQueue = new QueueManager<Buffer>(MaxQueueCount); //data that was read there
        public SortedListManager<int, Buffer> processedDataList = new SortedListManager<int, Buffer>(); //data to write in output file will be there 
        
        public ManualResetEvent FinishReadingEvent = new ManualResetEvent(false);
        public ManualResetEvent FinishWritingEvent = new ManualResetEvent(false);
        public ManualResetEvent[] FinishProcessingEvent = new ManualResetEvent[ThreadCount];

        public CompressionMode CompressionMode; 

        public FileInfo FileIn;
        public FileInfo FileOut;

        public bool IsProcessingFinished()
        {
            bool ret = true;
            for (int i = 0; i < FinishProcessingEvent.Length; i++)
            {
                ret &= FinishProcessingEvent[i].WaitOne(0);
            }
            return ret;
        }
        public void WaitProcessingFinished()
        {
            WaitHandle[] finishEvents = new WaitHandle[FinishProcessingEvent.Length];
            for (int i = 0; i < FinishProcessingEvent.Length; i++)
            {
                finishEvents[i] = FinishProcessingEvent[i];
            }
            WaitHandle.WaitAll(finishEvents);
        }
    }

    class ThreadStartInfo
    {
        public int ID;
        public CommonData CompressData;
        public ThreadStartInfo(int id, CommonData data)
        {
            ID = id;
            CompressData = data;
        }
    }

    class Buffer
    {
        public byte[] Data { get; private set; }   
        public int Order { get; private set; } //this need to decompress in right order 
        public int Length { get; private set; } //length of block data
        

        public Buffer(int order, byte[] d, int l)
        {
            Order = order; 
            Data = new byte[l];
            Array.Copy(d, Data, l);
            Length = l;
        }
    }
}
