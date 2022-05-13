using System;
using System.IO;
using System.IO.Compression;

namespace Ovation.FasterQC.Net
{
    public class FastqReader : ISequenceReader
    {
        private readonly FileStream inputStream;

        private readonly GZipStream gzipStream;

        private readonly BufferedStream bufferedStream;

        private readonly BinaryReader binaryReader;

        private bool disposedValue;

        public FastqReader(string fastq, bool gzipped = true)
        {
            var bufferSize = 128 * 1024;

            var fileStreamOptions = new FileStreamOptions()
            {
                Mode = FileMode.Open,
                BufferSize = bufferSize,
            };

            if (gzipped == true)
            {
                inputStream = File.Open(fastq, fileStreamOptions);
                gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                bufferedStream = new BufferedStream(gzipStream, bufferSize);
                binaryReader = new BinaryReader(bufferedStream);
            }
            else
            {
                inputStream = File.Open(fastq, fileStreamOptions);
                bufferedStream = new BufferedStream(inputStream, bufferSize);
                binaryReader = new BinaryReader(bufferedStream);
            }
        }

        public bool ReadSequence(out Sequence sequence)
        {
            // this is clearly dangerous, instead read a large chunk of the file
            // and then walk through it returning only the consumed portion while
            // keeping track of the last byte consumed on the stream
            byte[] bytes = new byte[1024];

            int offset = 0;
            int line = 0;
            int[] endOfLines = new int[4];

            try
            {
                while (line < 4)
                {
                    var b = binaryReader.ReadByte();
                    if (b == (byte)'\n')
                    {
                        endOfLines[line++] = offset;
                    }
                    else
                    {
                        bytes[offset++] = b;
                    }
                }

                for (var read = endOfLines[1]; read < endOfLines[2]; read++)
                {
                    bytes[read] &= 0xdf;
                }

                sequence = new Sequence(bytes, endOfLines);
                return true;
            }
            catch (EndOfStreamException)
            {
                Console.Error.WriteLine("End of stream");
                sequence = null;
                return false;
            }
        }

        public int ApproximateCompletion()
        {
            return (int)((double)inputStream.Position / (double)inputStream.Length * 100.0);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    binaryReader?.Dispose();
                    bufferedStream?.Dispose();
                    gzipStream?.Dispose();
                    inputStream?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}