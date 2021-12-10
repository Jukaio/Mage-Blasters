using System.Linq;

namespace Alteruna.Trinity.Development
{
    public class HistoryBuffer<T>
    {
        private T[] mBuffer;
        private int mHead = 0;
        private int mTail = 0;
        private int mCapacity = 0;
        private int mSize = 0;

        public HistoryBuffer(int capacity)
        {
            if (capacity > 0)
            {
                mBuffer = new T[capacity];
                mCapacity = capacity;
            }
        }

        public int Size { get { return mSize; } }

        public void PushFront(T item)
        {
            mBuffer[mHead] = item;
            mHead = (mHead + 1) % mCapacity;

            if (mSize < mCapacity)
            {
                mSize++;
            }
            else
            {
                mTail = (mTail + 1) % mCapacity;
            }
        }

        public T[] ToArray()
        {
            T[] array = new T[mSize];
            for (int i = 0; i < mSize; i++)
            {
                array[i] = mBuffer[(mTail + i) % mCapacity];
            }
            return array;
        }

        public T Max()
        {
            return mBuffer.Max();
        }

        public T Min()
        {
            return mBuffer.Min();
        }

        public void Clear()
        {
            mSize = 0;
        }
    }
}
