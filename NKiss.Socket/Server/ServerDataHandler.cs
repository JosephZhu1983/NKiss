using System;
using System.Net.Sockets;
using NKiss.Common;

namespace NKiss.Socket
{
    internal class ServerDataHandler
    {
        private readonly int MAX_BODY_BYTES = 1024 * 1024 * 1024;

        internal bool ReceiveData(SocketAsyncEventArgs arg, int receivedByteCount, int headerByteCountOffset)
        {
            var token = arg.UserToken as OperationUserToken;

            if (!token.ReceiveHeaderCompleted)
            {
                var remainingHeaderByteCount = token.TotalReceiveHeaderByteCount - token.ReceivedHeaderByteCount;
                if (receivedByteCount < remainingHeaderByteCount)
                {
                    Buffer.BlockCopy(arg.Buffer, token.ReceiveOffsetOnBuffer, token.ReceiveHeaderData, token.ReceivedHeaderByteCount, receivedByteCount);
                    LocalLoggingService.Debug("收到字节数为 {0} < 尚未接受的头的字节数为 {1}，继续接受头", receivedByteCount, remainingHeaderByteCount);
                    token.ReceivedHeaderByteCount += receivedByteCount;
                    return false;
                }
                else
                {
                    Buffer.BlockCopy(arg.Buffer, token.ReceiveOffsetOnBuffer, token.ReceiveHeaderData, token.ReceivedHeaderByteCount, remainingHeaderByteCount);
                    LocalLoggingService.Debug("收到字节数为 {0} > 尚未接受的头的字节数为 {1}，头接受完毕", receivedByteCount, remainingHeaderByteCount);
                    token.ReceivedHeaderByteCount += remainingHeaderByteCount;
                    token.ReceiveHeaderCompleted = true;
                    token.TotalReceiveBodyByteCount = BitConverter.ToInt32(token.ReceiveHeaderData, 0);
                    if (token.TotalReceiveBodyByteCount > MAX_BODY_BYTES)
                    {
                        throw new Exception(string.Format("期望接受的主体过大，字节数为 {0}，超过了 {1}", token.TotalSendBodyByteCount, MAX_BODY_BYTES));
                    }
                    token.ReceiveBodyData = new byte[token.TotalReceiveBodyByteCount];
                    var remainingByteCountForBody = receivedByteCount - remainingHeaderByteCount;
                    if (remainingByteCountForBody == 0)
                        return false;
                    else
                        return ReceiveData(arg, remainingByteCountForBody, remainingHeaderByteCount);
                }
            }
            else
            {
                var remainingBodyByteCount = token.TotalReceiveBodyByteCount - token.ReceivedBodyByteCount;
                if (receivedByteCount < remainingBodyByteCount)
                {
                    Buffer.BlockCopy(arg.Buffer, token.ReceiveOffsetOnBuffer + headerByteCountOffset, token.ReceiveBodyData, token.ReceivedBodyByteCount, receivedByteCount);
                    token.ReceivedBodyByteCount += receivedByteCount;
                    LocalLoggingService.Debug("收到的字节数为 {0} < 剩余的主体字节数为 {1}，还需要继续接受主体", receivedByteCount, remainingBodyByteCount);
                    return false;
                }
                else if (receivedByteCount == remainingBodyByteCount)
                {
                    Buffer.BlockCopy(arg.Buffer, token.ReceiveOffsetOnBuffer + headerByteCountOffset, token.ReceiveBodyData, token.ReceivedBodyByteCount, remainingBodyByteCount);
                    token.ReceivedBodyByteCount += remainingBodyByteCount;
                    LocalLoggingService.Debug("接收到一条完整的消息，其中头长 {0} 字节，主体长 {1} 字节", token.ReceivedHeaderByteCount, token.ReceivedBodyByteCount);
                    return true;
                }
                else
                {
                    throw new Exception(string.Format("收到的字节数为 {0} > 剩余的主体字节数为 {1}，这是一个不合法的请求", receivedByteCount, remainingBodyByteCount));
                }
            }
        }

        internal void PrepareSendData(SocketAsyncEventArgs arg, int bufferByteCount, int headerByteCountOffset)
        {
            var token = arg.UserToken as OperationUserToken;

            if (!token.SendHeaderCompleted)
            {
                token.TotalSendBodyByteCount = token.SendBodyData.Length;
                if (token.TotalSendBodyByteCount > MAX_BODY_BYTES)
                    throw new Exception(string.Format("期望发送的主体过大，字节数为 {0}，超过了 {1}", token.TotalSendBodyByteCount, MAX_BODY_BYTES));
                token.SendHeaderData = BitConverter.GetBytes(token.TotalSendBodyByteCount);
                Buffer.BlockCopy(token.SendHeaderData, 0, arg.Buffer, token.SendOffsetOnBuffer, token.TotalSendHeaderByteConut);
                token.SendHeaderCompleted = true;
                PrepareSendData(arg, bufferByteCount, token.TotalSendHeaderByteConut);
            }
            else
            {
                var totalSendBufferSize = bufferByteCount - headerByteCountOffset;
                var remainingSendBodyByteCount = token.TotalSendBodyByteCount - token.SentBodyByteCount;
                if (totalSendBufferSize > remainingSendBodyByteCount)
                {
                    LocalLoggingService.Debug("发送缓冲区字节数为 {0} > 剩余的主体字节数 {1}，主体可以发送完毕", totalSendBufferSize, remainingSendBodyByteCount);
                    Buffer.BlockCopy(token.SendBodyData, token.SentBodyByteCount, arg.Buffer, token.SendOffsetOnBuffer + headerByteCountOffset, remainingSendBodyByteCount);
                    arg.SetBuffer(token.SendOffsetOnBuffer, headerByteCountOffset + remainingSendBodyByteCount);
                    token.SendBodyCompleted = true;
                    token.SentBodyByteCount += remainingSendBodyByteCount;
                }
                else
                {
                    LocalLoggingService.Debug("发送缓冲区字节数为 {0} <= 剩余的主体字节数 {1}，主体还需要继续发送", totalSendBufferSize, remainingSendBodyByteCount);
                    Buffer.BlockCopy(token.SendBodyData, token.SentBodyByteCount, arg.Buffer, token.SendOffsetOnBuffer + headerByteCountOffset, totalSendBufferSize);
                    arg.SetBuffer(token.SendOffsetOnBuffer, headerByteCountOffset + totalSendBufferSize);
                    token.SentBodyByteCount += totalSendBufferSize;
                }
            }
        }
    }
}
