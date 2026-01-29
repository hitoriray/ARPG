using System;
using System.Collections.Generic;

namespace Battle.ECS.Core.Collision.Grid
{
    public sealed class BetterList<T>
    {
        private T[] _buffer;
        public T[] Buffer  => _buffer;

        public int Count { get; private set; }

        public BetterList(int capacity = 16)
        {
            _buffer = new T[capacity];
            Count = 0;
        }

        public void Add(T item)
        {
            if (Count == _buffer.Length)
            {
                var newArr = new T[_buffer.Length << 1];
                Array.Copy(_buffer, newArr, Count);
                _buffer = newArr;
            }
            _buffer[Count++] = item;
        }

        public bool Remove(T item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(_buffer[i], item))
                {
                    _buffer[i] = _buffer[Count - 1];
                    _buffer[Count - 1] = default;
                    Count--;
                    return true;
                }
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (index < Count)
            {
                _buffer[index] = _buffer[Count - 1];
                _buffer[Count - 1] = default;
                Count--;
            }
        }

        public void FastClear()
        {
            Count = 0;
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (index < 0 || arrayIndex < 0 || count < 0)
                throw new ArgumentOutOfRangeException("index/arrayIndex/count 不能为负数");

            if (index + count > Count)
                throw new ArgumentException("源集合中可复制的元素数量不足");

            if (arrayIndex + count > array.Length)
                throw new ArgumentException("目标数组空间不足");

            Array.Copy(_buffer, index, array, arrayIndex, count);
        }

        public T this[int index]
        {
            get => _buffer[index];
            set => _buffer[index] = value;
        }

        public ref T GetRef(int index)
        {
            if (index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref _buffer[index];
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public struct Enumerator
        {
            private readonly BetterList<T> _list;
            private int _index;

            public Enumerator(BetterList<T> list)
            {
                _list = list;
                _index = -1;
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _list.Count;
            }

            public T Current => _list._buffer[_index];
        }
    }
}