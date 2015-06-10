using System.Collections.Generic;
using System.Net.Sockets;

namespace NKiss.Socket
{
    internal class BufferManager
    {
        private byte[] bufferBlock;
        private int currentIndex;
        private int totalBytes;
        private int bufferSize;
        private Stack<int> freeIndexPool;

        internal BufferManager(int totalBytes, int bufferSize)
        {
            this.totalBytes = totalBytes;
            this.bufferSize = bufferSize;
            this.currentIndex = 0;
            this.freeIndexPool = new Stack<int>();
        }

        internal void Init()
        {
            bufferBlock = new byte[totalBytes];
        }

        internal bool SetBuffer(SocketAsyncEventArgs args)
        {
            //首先利用已经释放的块
            if (freeIndexPool.Count > 0)
            {
                args.SetBuffer(bufferBlock, freeIndexPool.Pop(), bufferSize);
            }
            else
            {
                //没有空间了
                if (totalBytes < (currentIndex + bufferSize))
                {
                    return false;
                }
                //利用新的块
                args.SetBuffer(bufferBlock, currentIndex, bufferSize);
                currentIndex += bufferSize;
            }
            return true;
        }

        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            //需要释放的SocketAsyncEventArgs的缓冲位置存入栈
            freeIndexPool.Push(args.Offset);
            //为SocketAsyncEventArgs取消设置缓冲
            args.SetBuffer(null, 0, 0);
        }
    }
}
