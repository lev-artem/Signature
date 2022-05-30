using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Signature
{
    class Program
    {
        const int BUFFER_PRODUCER_THREAD_COUNT = 1;
        const int TIMEOUT_TO_COMPLETE_ALL_CHILD_THREADS_MS = 1000;

        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var completedEvents = new List<AutoResetEvent>();
            try
            {
                if (args == null || args.Length != 2)
                {
                    throw new ArgumentException("Not all arguments specified");
                }
                var filePath = args[0];
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(filePath);
                }
                var blockSize = int.Parse(args[1]); 
                if(blockSize <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "block size specified in arg1 cannot be less or equal to 0");
                }
                var fileLength = new FileInfo(filePath).Length;
                var fileBlockCount = fileLength / blockSize;
                if (fileLength % blockSize > 0)
                {
                    fileBlockCount++;
                }

                var producerCompletedEvent = new AutoResetEvent(false);
                completedEvents.Add(producerCompletedEvent);

                var taskCount = Environment.ProcessorCount;
                var bufferPool = ArrayPool<byte>.Create(blockSize, taskCount);
                var fileBlockCollection = new BlockingCollection<(int Number, byte[] Buffer)>(taskCount);
                var producer = new BufferProducer(fileBlockCollection, bufferPool, filePath, blockSize, producerCompletedEvent, cancellationTokenSource);
                var producerThread = new Thread(producer.Run);

                var threads = new List<Thread>()
                {
                    producerThread
                };

                var consumerThreadCount = Math.Max(1, Environment.ProcessorCount - BUFFER_PRODUCER_THREAD_COUNT);
                var writer = new ThreadSafeConsoleWriter(taskCount, fileBlockCount);
                
                for(int i = 0; i < consumerThreadCount; i++)
                {
                    var consumerCompletedEvent = new AutoResetEvent(false);
                    completedEvents.Add(consumerCompletedEvent);
                    var consumer = new BufferConsumer(fileBlockCollection, bufferPool, writer, consumerCompletedEvent, cancellationTokenSource);
                    var consumerThread = new Thread(consumer.Run);
                    threads.Add(consumerThread);
                }

                threads.ForEach(thread => thread.Start());
                threads.ForEach(thread => thread.Join());
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex}, {ex.StackTrace}");
                cancellationTokenSource.Cancel();
                if(completedEvents.Any())
                {
                    WaitHandle.WaitAll(completedEvents.ToArray(), TIMEOUT_TO_COMPLETE_ALL_CHILD_THREADS_MS);
                }
            }
        }
    }
}