using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signature
{
    public class ThreadSafeConsoleWriter
    {
        private readonly object _lock = new object();

        private readonly string[] _hashCodes;
        private readonly long _blockCount;

        private int _currentBlockNumber;

        public ThreadSafeConsoleWriter(int hashCodeArraySize, long blockCount)
        {
            if (hashCodeArraySize <= 0)
            {
                throw new ArgumentOutOfRangeException("bufferArraySize", hashCodeArraySize, "cannot be less or equal to 0");
            }

            if (blockCount <= 0)
            {
                throw new ArgumentOutOfRangeException("blockCount", blockCount, "cannot be less or equal to 0");
            }
            _blockCount = blockCount;

            _currentBlockNumber = 0;
            _hashCodes = new string[hashCodeArraySize];
        }

        public void Write(int blockNumber, string hashCode)
        {
            lock(_lock)
            {
                var blockNumberOverflowingHashCodeArrayIndex = _currentBlockNumber + _hashCodes.Length;

                while(blockNumber >= blockNumberOverflowingHashCodeArrayIndex)
                {
                    Monitor.Wait(_lock);
                    blockNumberOverflowingHashCodeArrayIndex = _currentBlockNumber + _hashCodes.Length;
                }

                _hashCodes[blockNumber - _currentBlockNumber] = hashCode;

                if(AreAllHashCodesIsFilled())
                {
                    Write();
                }

                if (IsLastWriteOperationAndAreAllHashCodesOfLastWriteOperationIsFilled())
                {
                    Write(true);
                }
            }
        }

        private bool AreAllHashCodesIsFilled()
        {
            return !_hashCodes.Any(it => it == null);
        }

        private bool IsLastWriteOperationAndAreAllHashCodesOfLastWriteOperationIsFilled()
        {
            return _blockCount - _hashCodes.Length < _currentBlockNumber && AreAllHashCodesOfLastWriteOperationIsFilled();
        }

        private bool AreAllHashCodesOfLastWriteOperationIsFilled()
        {
            var lastWriteOperationElementCount = _blockCount % _hashCodes.Length;
            for (int i = 0; i < lastWriteOperationElementCount; i++)
            {
                if (string.IsNullOrWhiteSpace(_hashCodes[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private void Write(bool isLastWriteOperation = false)
        {
            var showElementCount = _hashCodes.Length;

            if (isLastWriteOperation)
            {
                showElementCount = (int)_blockCount % _hashCodes.Length;
            }

            for (int i = 0; i < showElementCount; i++)
            {
                Console.WriteLine($"{_currentBlockNumber} {_hashCodes[i]}");
                _currentBlockNumber++;
                _hashCodes[i] = null;
            }

            Monitor.PulseAll(_lock);
        }
    }
}