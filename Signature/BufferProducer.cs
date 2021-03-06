using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace Signature
{
    public class BufferProducer
    {
        private readonly string _filePath;
        private readonly int _bufferLenght;

        private readonly ArrayPool<byte> _bufferPool;
        private readonly AutoResetEvent _completedEvent;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<(int number, byte[] buffer)> _fileBlocksOutput;

        public BufferProducer(BlockingCollection<(int number, byte[] buffer)> fileBlocksOutput, ArrayPool<byte> bufferPool, string filePath, int bufferLength, AutoResetEvent completedEvent, CancellationTokenSource cancellationTokenSource)
        {
            if(bufferLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferLength), bufferLength, "cannot be less or equal to 0");
            }
            _bufferLenght = bufferLength;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentOutOfRangeException(nameof(filePath), filePath, "cannot be null, empty or whitespace");
            }
            _filePath = filePath;

            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            _completedEvent = completedEvent ?? throw new ArgumentNullException(nameof(completedEvent));
            _fileBlocksOutput = fileBlocksOutput ?? throw new ArgumentNullException(nameof(fileBlocksOutput));
            _cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
        }


        public void Run()
        {
            try
            {
                using (var fileStream = File.OpenRead(_filePath))
                {
                    var i = 0;
                    var buffer = _bufferPool.Rent(_bufferLenght);
                    while (fileStream.Read(buffer, 0, _bufferLenght) > 0)
                    {
                        _fileBlocksOutput.Add((i, buffer), _cancellationTokenSource.Token);
                        buffer = _bufferPool.Rent(_bufferLenght);
                        i++;
                    }
                    _fileBlocksOutput.CompleteAdding();                  
                }
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}, {ex.StackTrace}");
                _cancellationTokenSource.Cancel();
            }
            finally
            {
                _completedEvent.Set();
            }
        }
    }
}