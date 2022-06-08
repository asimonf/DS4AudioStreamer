using System;

namespace DS4AudioStreamer.Sound
{
    public class CircularBuffer<T> where T: unmanaged
    {
        private readonly T[] _backingBuffer;

        private int _start;
        private int _end;

        public int Capacity => _backingBuffer.Length;

        public int CurrentLength
        {
            get
            {
                if (_end > _start) return _end - _start;
                if (_end < _start) return _end + Capacity - _start;
                return _hasData ? Capacity : 0;
            }
        }
        public int Glitches { get; set; }

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
            Glitches = 0;
        }

        public void AddSample(T sample)
        {
            if (_end == Capacity)
            {
                _backingBuffer[0] = sample;

                _end = 1;
            }
            else
            {
                
                _end += 1;
            }
        }

        public void CopyFrom(T[] arr, int length)
        {
            int startOffset = 0;
            if (CurrentLength + length >= Capacity)
            {
                startOffset = CurrentLength + length - Capacity;
            }
            
            // Console.WriteLine("capacity: {0}, length: {1}, startOffset {2}", Capacity, CurrentLength, startOffset);
            
            if (_end + length > Capacity)
            {
                var newLength = Capacity - _end;
                var remainder = length - newLength;

                // Buffer.BlockCopy(arr, 0, _backingBuffer, _end, newLength);
                Array.Copy(arr, 0, _backingBuffer, _end, newLength);
                Array.Copy(arr, newLength, _backingBuffer, 0, remainder);

                _end = remainder;
            }
            else
            {
                Array.Copy(arr, 0, _backingBuffer, _end, length);
                _end = (_end + length) % Capacity;
            }
            
            _start = (_start + startOffset) % Capacity;

            _hasData = true;
            
            // Console.WriteLine("start: {0}, end: {1}", _start, _end);
        }

        public void CopyTo(T[] destination, int offset, int length)
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

                Array.Copy(_backingBuffer, _start, destination, offset, newLength);
                Array.Copy(_backingBuffer, 0, destination, offset + newLength, remainder);

                _start = remainder;
            }
            else if (length > 0)
            {
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

        public void CopyTo(T[] destination, int length)
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

                Array.Copy(_backingBuffer, _start, destination, 0, newLength);
                Array.Copy(_backingBuffer, 0, destination, newLength, remainder);

                _start = remainder;
            }
            else if (length > 0)
            {
                Array.Copy(_backingBuffer, _start, destination, 0, length);

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