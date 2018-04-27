using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttackShip
{

    /// <summary>
    /// A simple wrapper around the standard Queue.
    /// The queue only accepts a given range of integers, and the set mechanism uses a boolean array of this size.
    /// </summary>
    class distinctIntQueue
    {
        bool[] queuedAlready;
        Queue<int> Q;

        public distinctIntQueue(int initQsize, int numValues)
        {
            Q = new Queue<int>(initQsize);
            queuedAlready = new bool[numValues];
        }

        public int Count()
        {
            return Q.Count;
        }

        public void Enqueue(int v)
        {
            // Don't enqueue it if we already have - like a set.
            if (!queuedAlready[v])
            {
                Q.Enqueue(v);
                queuedAlready[v] = true;
            }
        }

        public int Dequeue()
        {
            return Q.Dequeue();
        }
    }
}
