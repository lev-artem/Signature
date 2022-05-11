using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Signature
{
    class Program
    {
        const int BOUNDED_QUEUE_CAPACITY = 4096;
        const int BUFFER_PRODUCER_AND_HASH_CONSUMER_THREAD_COUNT = 2;

        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
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

                var fileBlockCollection = new BlockingCollection<(int Number, byte[] Buffer)>(BOUNDED_QUEUE_CAPACITY);
                var producer = new BufferProducer(fileBlockCollection, filePath, blockSize, cancellationTokenSource);
                var producerThread = new Thread(producer.Run);

                var threads = new List<Thread>()
                {
                    producerThread
                };

                var hashCodeCollection = new BlockingCollection<(int Number, string HashCode)>(BOUNDED_QUEUE_CAPACITY);
                var consumerProducerThreadCount = Math.Max(1, Environment.ProcessorCount - BUFFER_PRODUCER_AND_HASH_CONSUMER_THREAD_COUNT);
                
                for(int i = 0; i < consumerProducerThreadCount; i++)
                {
                    var consumerProducer = new BufferConsumerHashProducer(fileBlockCollection, hashCodeCollection, cancellationTokenSource);
                    var consumerProducerThread = new Thread(consumerProducer.Run);
                    threads.Add(consumerProducerThread);
                }
                
                var hashConsumer = new HashConsumer(hashCodeCollection, fileBlockCount, cancellationTokenSource);
                var hashConsumerThread = new Thread(hashConsumer.Run);
                threads.Add(hashConsumerThread);

                threads.ForEach(thread => thread.Start());
                threads.ForEach(thread => thread.Join());
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex}, {ex.StackTrace}");
                cancellationTokenSource.Cancel();       
            }
        }
    }
}