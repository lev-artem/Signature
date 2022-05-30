using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Signature
{
    public class BufferConsumer
    {
        private readonly ArrayPool<byte> _bufferPool;
        private readonly ThreadSafeConsoleWriter _writer;
        private readonly AutoResetEvent _completedEvent;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<(int number,byte[] buffer)> _buffersInput;

        public BufferConsumer(BlockingCollection<(int number, byte[] buffer)> buffersInput, ArrayPool<byte> bufferPool, ThreadSafeConsoleWriter writer, AutoResetEvent completedEvent, CancellationTokenSource cancellationTokenSource)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            _buffersInput = buffersInput ?? throw new ArgumentNullException(nameof(buffersInput));
            _completedEvent = completedEvent ?? throw new ArgumentNullException(nameof(completedEvent));
            _cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
        }


        public void Run()
        {
            try
            {
                using (var hashCoder = SHA256.Create())
                {
                    foreach (var (number, buffer) in _buffersInput.GetConsumingEnumerable(_cancellationTokenSource.Token))
                    {
                        byte[] result = hashCoder.ComputeHash(buffer);
                        _bufferPool.Return(buffer);
                        var hashCode = Encoding.Default.GetString(result);
                        _writer.Write(number, hashCode);
                    }
                }
            }
            catch(OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex.StackTrace}");
                _cancellationTokenSource.Cancel();
            }
            finally
            {
                _completedEvent.Set();
            }
        }
    }
}