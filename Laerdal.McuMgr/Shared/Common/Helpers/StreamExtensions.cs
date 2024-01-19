using System;
using System.IO;
using System.Threading.Tasks;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class StreamExtensions
    {
        static internal async Task<byte[]> ReadBytesAsync(this Stream stream, int maxBytesToRead = -1, bool disposeStream = false)
        {           
            stream = stream ?? throw new ArgumentNullException(nameof(stream));

            var readToEnd = maxBytesToRead < 0;
            if (readToEnd && stream is MemoryStream { Position: 0 } memoryStream) //optimization
            {
                var result = memoryStream.ToArray();
                
                if (disposeStream)
                    stream.Dispose(); //todo   use await stream.DisposeAsync() here when we upgrade to .net 8
                
                return result;
            }

            using var tempMemoryStream = readToEnd
                ? new MemoryStream()
                : new MemoryStream(maxBytesToRead);

            await (
                readToEnd
                    ? stream.CopyToAsync(tempMemoryStream)
                    : stream.CopyToAsync(tempMemoryStream, bufferSize: maxBytesToRead)
            );

            if (disposeStream)
                stream.Dispose(); //todo   use await stream.DisposeAsync() here when we upgrade to .net 8

            return tempMemoryStream.ToArray();
        }
    }
}