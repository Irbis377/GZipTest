using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    class Decompressor: GZipper
    {
        public Decompressor(string input, string output) : base(input, output) { }
        private int ReadBufSize = 4096;
        protected override void Read(FileStream originalFileStream, CommonData data)
        {
            int buffOrder = 0;
            for (; ; )
            {
                int numRead;
                //read length of block
                byte[] abLength = new byte[4];
                numRead = originalFileStream.Read(abLength, 0, 4);
                if (numRead == 0)
                    break;
              
                int blockLength = BitConverter.ToInt32(abLength, 0);

                //read data
                byte[] buffer = new byte[blockLength];
                numRead = originalFileStream.Read(buffer, 0, blockLength);
                if (numRead == 0)
                    break;
                
                Buffer buff = new Buffer(buffOrder++, buffer, numRead);
                data.dataQueue.Enqueue(buff);

                Thread.Sleep(0);
            }
        }
      
        protected override void Write(FileStream compressedFileStream, Buffer buff)
        {
            compressedFileStream.Write(buff.Data, 0, buff.Data.Length);
        }
        protected override void Process(CommonData data, Buffer buff)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                using (MemoryStream memStream = new MemoryStream(buff.Data))
                {
                    using (GZipStream gzipStream = new GZipStream(memStream, data.CompressionMode))
                    {
                        byte[] buffer = new byte[ReadBufSize];
                        int numRead;
                        while ((numRead = gzipStream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            outStream.Write(buffer, 0, numRead);
                        }
                    }
                }
                byte[] outBuffer = outStream.ToArray();
                Buffer outData = new Buffer(buff.Order, outBuffer, outBuffer.Length);
                
                //add this block to list 
                data.processedDataList.Add(buff.Order, outData);
            }
        }
    }
}
