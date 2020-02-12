using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    class Compressor : GZipper
    {
        public Compressor(string input, string output) : base(input, output) { }
        /// <summary>
        /// just read buf size blocks and save into queue with number of order
        /// </summary>
        /// <param name="originalFileStream"></param>
        /// <param name="data"></param>
        protected override void Read(FileStream originalFileStream, CommonData data)
        {
            int buffOrder = 0;
            byte[] buffer = new byte[BufSize];
            int numRead;
            while ((numRead = originalFileStream.Read(buffer, 0, BufSize)) != 0)
            {
                Buffer buff = new Buffer(buffOrder++, buffer, numRead);
                data.dataQueue.Enqueue(buff);
            }
        }

        /// <summary>
        /// Write block (with ordernum and lenght) of bytes into filestream 
        /// </summary>
        /// <param name="compressedFileStream"></param>
        /// <param name="buff"></param>
        protected override void Write(FileStream compressedFileStream, Buffer buff)
        {
            byte[] abLength = BitConverter.GetBytes(buff.Data.Length);
            compressedFileStream.Write(abLength, 0, abLength.Length);

            compressedFileStream.Write(buff.Data, 0, buff.Data.Length);
        }
        protected override void Process(CommonData data, Buffer buff)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memStream, data.CompressionMode))
                {
                    gzipStream.Write(buff.Data, 0, buff.Length); 
                }
                byte[] outBuffer = memStream.ToArray();
                Buffer outData = new Buffer(buff.Order, outBuffer, outBuffer.Length);

                data.processedDataList.Add(buff.Order, outData);
            }
        }
    }
}
