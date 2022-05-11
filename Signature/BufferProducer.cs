using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Signature
{
    public class BufferProducer
    {
        private readonly string _filePath;
        private readonly int _bufferLenght;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<(int number, byte[] buffer)> _fileBlocksOutput;

        public BufferProducer(BlockingCollection<(int number, byte[] buffer)> fileBlocksOutput, string filePath, int bufferLength, CancellationTokenSource cancellationTokenSource)
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

            _fileBlocksOutput = fileBlocksOutput ?? throw new ArgumentNullException(nameof(fileBlocksOutput));
            _cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
        }


        public void Run()
        {
            try
            {
                using (var fileStream = File.OpenRead(_filePath))
                {
                    using (var hashCoder = SHA256.Create())
                    {
                        var i = 0;
                        var buffer = new byte[_bufferLenght];
                        while (fileStream.Read(buffer, 0, _bufferLenght) > 0)
                        {
                            _fileBlocksOutput.Add((i, (byte[])buffer.Clone()), _cancellationTokenSource.Token);
                            i++;
                        }
                        _fileBlocksOutput.CompleteAdding();
                    }
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
        }
    }
}