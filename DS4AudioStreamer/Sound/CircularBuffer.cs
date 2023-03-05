using System;

namespace DS4AudioStreamer.Sound
{
    public class CircularBuffer<T> where T: unmanaged
    {
        private readonly object _sync = new();
        
        private readonly T[] _backingBuffer;

        private int _start;
        private int _end;

        public int Capacity => _backingBuffer.Length;

        public int CurrentLength
        {
            get
            {
                lock (_sync)
                {
                    if (_end > _start) return _end - _start;
                    if (_end < _start) return _end + Capacity - _start;
                    return _hasData ? Capacity : 0;                    
                }
            }
        }

        private bool _hasData = false;

        /// <summary>
        /// Creates a circular buffer
        /// </summary>
        /// <param name="size">Size of the buffer in samples</param>
        public CircularBuffer(int size)
        {
            _backingBuffer = new T[size];
            _start = 0;
            _end = 0;
        }

        public unsafe void CopyFrom(T[] arr, int length)
        {
            lock (_sync)
            {
                var startOffset = 0;
                if (CurrentLength + length >= Capacity)
                {
                    startOffset = CurrentLength + length - Capacity;
                }
            
                if (_end + length > Capacity)
                {
                    var newLength = Capacity - _end;
                    var remainder = length - newLength;

                    Buffer.BlockCopy(arr, 0, _backingBuffer, _end * sizeof(T), newLength * sizeof(T));
                    Buffer.BlockCopy(arr, newLength * sizeof(T), _backingBuffer, 0, remainder * sizeof(T));

                    _end = remainder;
                }
                else
                {
                    Buffer.BlockCopy(arr, 0, _backingBuffer, _end * sizeof(T), length * sizeof(T));
                    _end = (_end + length) % Capacity;
                }
            
                _start = (_start + startOffset) % Capacity;

                _hasData = true;
            }
        }

        public unsafe void CopyTo(T[] destination, int offset, int length)
        {
            lock (_sync)
            {
                var zeroFill = 0;
            
                // Zero-fill if the request can't be filled with the current buffer contents
                if (length > CurrentLength)
                {
                    zeroFill = length - CurrentLength;
                    length -= zeroFill;
                }

                if (_start + length > Capacity)
                {
                    var newLength = Capacity - _start;
                    var remainder = length - newLength;
                

                    Buffer.BlockCopy(_backingBuffer, _start * sizeof(T), destination, offset * sizeof(T), newLength * sizeof(T));
                    Buffer.BlockCopy(_backingBuffer, 0, destination, (offset + newLength) * sizeof(T), remainder * sizeof(T));

                    _start = remainder;
                }
                else if (length > 0)
                {
                    Buffer.BlockCopy(_backingBuffer, _start * sizeof(T), destination, offset * sizeof(T), length * sizeof(T));
                    Array.Copy(_backingBuffer, _start, destination, offset, length);

                    _start = (_start + length) % Capacity;
                }

                if (zeroFill > 0)
                {
                    _hasData = false;
                    Array.Fill(destination, new T(), length + offset, zeroFill);
                    Console.WriteLine("Glitch");
                } else if (_start == _end) _hasData = false;
            }
        }

        public unsafe void CopyTo(T[] destination, int length)
        {
            lock (_sync)
            {
                var zeroFill = 0;
                // Zero-fill if the request can't be filled with the current buffer contents
                if (length > CurrentLength)
                {
                    zeroFill = length - CurrentLength;
                    length -= zeroFill;
                }

                if (_start + length >= Capacity)
                {
                    var newLength = Capacity - _start;
                    var remainder = length - newLength;

                    Buffer.BlockCopy(_backingBuffer, _start * sizeof(T), destination, 0, newLength * sizeof(T));
                    Buffer.BlockCopy(_backingBuffer, 0, destination, newLength * sizeof(T), remainder * sizeof(T));

                    _start = remainder;
                }
                else if (length > 0)
                {
                    Buffer.BlockCopy(_backingBuffer, _start * sizeof(T), destination, 0, length * sizeof(T));

                    _start = (_start + length) % Capacity;
                }

                if (zeroFill > 0)
                {
                    _hasData = false;
                    Array.Fill(destination, new T(), length, zeroFill);
                } else if (_start == _end) _hasData = false;
            }
        }
    }
}