using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    abstract class GZipper
    {
        string fileIn;
        string fileOut;
        
        public GZipper(string input, string output)
        {
            this.fileIn = input;
            this.fileOut = output;
        }

        protected int BufSize = 1024 * 1024; //1Mb
        protected int MaxQueueCount = 10;

      
        public void Process(CompressionMode CompressionMode)
        {
            //init part 
            FileInfo fiIn = new FileInfo(fileIn);
            FileInfo fiOut = new FileInfo(fileOut);

            CommonData data = new CommonData();
            data.CompressionMode = CompressionMode;
            data.FileIn = fiIn;
            data.FileOut = fiOut;
            
            //starting threads for reading 
            for (int i = 0; i < CommonData.ThreadCount; i++)
            {
                data.FinishProcessingEvent[i] = new ManualResetEvent(false);
                Thread thread = new Thread(processDataThread); 
                thread.Start(new ThreadStartInfo(i, data));
            }

            //starting thread for writing 
            Thread threadWrite = new Thread(WriteProcessedDataThread);
            threadWrite.Start(data);

            try
            {
                using (FileStream originalFileStream = fiIn.OpenRead())
                {
                    Read(originalFileStream, data);
                }
            }
            finally
            {
                data.FinishReadingEvent.Set();
                data.dataQueue.Exit();
                data.WaitProcessingFinished();
                data.processedDataList.Exit();
                data.FinishWritingEvent.WaitOne();
            }
        }

        private void processDataThread(object objData)
        {
            ThreadStartInfo tsi = (ThreadStartInfo)objData;
            CommonData data = tsi.CompressData;
            try
            {
                while (!data.FinishReadingEvent.WaitOne(0) || data.dataQueue.Count > 0)
                {
                    Buffer buff = data.dataQueue.Dequeue();

                    if (buff != null && buff.Length > 0)
                    {
                        Process(data, buff); //main process that realized in Compressor and Decompressor
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in (De)Compression: {0}", ex.Message);
            }
            finally
            {
                data.FinishProcessingEvent[tsi.ID].Set();
            }
        }

        private void WriteProcessedDataThread(object objData)
        {
            CommonData data = (CommonData)objData;
            try
            {
                int buffOrder = 0;

                using (FileStream compressedFileStream = File.Create(data.FileOut.FullName))
                {
                    while (!data.IsProcessingFinished() || data.processedDataList.Count > 0)
                    {
                        Buffer buff = null;

                        if (!data.processedDataList.TryRetrieveValue(buffOrder, out buff))
                        {
                            Thread.Sleep(0);
                            continue;
                        }

                        if (buff != null && buff.Data.Length > 0)
                        {
                            Write(compressedFileStream, buff);
                        }
                        buffOrder++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in writing: {0}", ex.Message);
            }
            finally
            {
                data.FinishWritingEvent.Set();
            }
        }

        protected abstract void Read(FileStream stream, CommonData data);
        protected abstract void Write(FileStream stream, Buffer buff);
        protected abstract void Process(CommonData data, Buffer buff);

    }
}
