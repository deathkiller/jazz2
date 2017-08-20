using System.Collections.Generic;

namespace Jazz2
{
    public class VertexPool<T> where T : struct
    {
        private int itemCount, currentIndex;
        private List<T[]> pool;

        public VertexPool(int itemCount)
        {
            this.itemCount = itemCount;
            this.pool = new List<T[]>();
        }

        public T[] Get()
        {
            currentIndex++;

            if (pool.Count < currentIndex) {
                T[] array = new T[itemCount];
                pool.Add(array);
                return array;
            } else {
                return pool[currentIndex - 1];
            }
        }

        public void Reclaim()
        {
            currentIndex = 0;
        }
    }
}