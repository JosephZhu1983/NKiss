
using System;
using System.Collections.Generic;
using System.Threading;

namespace NKiss.Socket
{
    internal class ClientNodeLocator
    {
        private List<ClientNode> nodes;
        private Random random;
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        internal void Initialize(List<ClientNode> rawNodes)
        {
            this.random = new Random(Environment.TickCount);
            var nodesCandidate = new List<ClientNode>();

            nodesCandidate = new List<ClientNode>();
            foreach (var node in rawNodes)
            {
                for (int i = 0; i < (int)node.Weight; i++)
                {
                    nodesCandidate.Add(node);
                }
            }

            if (locker.TryEnterWriteLock(TimeSpan.FromSeconds(1)))
            {
                try
                {
                    this.nodes = nodesCandidate;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }

        internal ClientNode LocateNode()
        {
            if (nodes == null)
                throw new InvalidOperationException("ClientNodeLocator还没有初始化");

            if (nodes.Count == 0) return null;

            if (locker.TryEnterReadLock(TimeSpan.FromSeconds(1)))
            {
                try
                {
                    return nodes[random.Next(nodes.Count)];
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }

            return null;
        }
    }
}
