using System.Collections.Generic;

namespace BlitzSniffer.Util
{
    public sealed class FixedSizedQueue<T> : Queue<T>
    {
        private readonly int Capacity;

        public FixedSizedQueue(int capacity)
        {
            Capacity = capacity;
        }

        public new void Enqueue(T item)
        {
            base.Enqueue(item);

            if (base.Count > Capacity)
            {
                base.Dequeue();
            }
        }
        
    }
}