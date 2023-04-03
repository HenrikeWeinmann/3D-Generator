using System;
using System.Collections.Generic;

namespace DefaultNamespace {
    public class PriorityQueue<T> where T : IComparable<T> {
        private LinkedList<T> items;
        public PriorityQueue()
        {
            this.items = new LinkedList<T>();
        }

        public void Enqueue(T item)
        {
            if (items.Count == 0) {
                items.AddLast(item);
            } else {
                var current = items.First;

                while (current != null && current.Value.CompareTo(item) > 0) {
                    current = current.Next;
                }

                if (current == null) {
                    items.AddLast(item);
                }else {
                    items.AddBefore(current, item);
                }
            }
            
        }

        public T Dequeue()
        {
            if (items.Count == 0) {
                throw new InvalidOperationException("Queue is empty");
            }

            T value = items.First.Value;
            items.RemoveFirst();
            return value;
        }


        public int Count()
        {
            return items.Count;
        }

        
    }
}


