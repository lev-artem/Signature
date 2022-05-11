using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Signature
{
    public class HashConsumer
    {
        private readonly AutoResetEvent _completedEvent;
        private readonly Dictionary<int, string> _hashCodeDict;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<(int number, string hashCode)> _hashCodeInput;

        public HashConsumer(BlockingCollection<(int number, string hashCode)> hashCodeInput, long blockCount, CancellationTokenSource cancellationTokenSource, AutoResetEvent completedEvent)
        {
            if (blockCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockCount), blockCount, "cannot be less or equal to 0");
            }
            _hashCodeDict = new Dictionary<int, string>((int)blockCount);
            _hashCodeInput = hashCodeInput ?? throw new ArgumentNullException(nameof(hashCodeInput));
            _completedEvent = completedEvent ?? throw new ArgumentNullException(nameof(completedEvent));
            _cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
        }

        public void Run()
        {
            try
            {
                foreach (var (number, hashCode) in _hashCodeInput.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    _hashCodeDict[number] = hashCode;
                }
                foreach (var hashCodeBlock in _hashCodeDict)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }
                    Console.WriteLine($"{hashCodeBlock.Key} {hashCodeBlock.Value}");
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