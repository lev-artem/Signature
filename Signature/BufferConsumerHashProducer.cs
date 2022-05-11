using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Signature
{
    public class BufferConsumerHashProducer
    {
        private readonly AutoResetEvent _completedEvent;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<(int number,byte[] buffer)> _buffersInput;
        private readonly BlockingCollection<(int number, string hashCode)> _hashCodeOutput;


        public BufferConsumerHashProducer(BlockingCollection<(int number, byte[] buffer)> buffersInput, BlockingCollection<(int number, string hashCode)> hashCodeOutput, CancellationTokenSource cancellationTokenSource, AutoResetEvent completedEvent)
        {
            _buffersInput = buffersInput ?? throw new ArgumentNullException(nameof(buffersInput));
            _completedEvent = completedEvent ?? throw new ArgumentNullException(nameof(completedEvent));
            _hashCodeOutput = hashCodeOutput ?? throw new ArgumentNullException(nameof(hashCodeOutput));
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
                        var hashCode = Encoding.Default.GetString(result);
                        _hashCodeOutput.Add((number, hashCode));
                    }
                    _hashCodeOutput.CompleteAdding();
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