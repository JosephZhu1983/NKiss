
namespace NKiss.Socket
{
    internal class OperationUserToken
    {
        internal int ReceiveOffsetOnBuffer { get; private set; }
        internal int SendOffsetOnBuffer { get; private set; }
        internal int TotalReceiveHeaderByteCount { get; private set; }
        internal int TotalSendHeaderByteConut { get; private set; }

        internal bool ReceiveHeaderCompleted { get; set; }
        internal int ReceivedHeaderByteCount { get; set; }
        internal int ReceivedBodyByteCount { get; set; }
        internal int TotalReceiveBodyByteCount { get; set; }
        internal byte[] ReceiveHeaderData { get; set; }
        internal byte[] ReceiveBodyData { get; set; }
        
        internal bool SendHeaderCompleted { get; set; }
        internal bool SendBodyCompleted { get; set; }
        internal int SentBodyByteCount { get; set; }
        internal int TotalSendBodyByteCount { get; set; }
        internal byte[] SendHeaderData { get; set; }
        internal byte[] SendBodyData { get; set; }

        internal OperationUserToken(int receiveBufferOffset, int sendBufferOffset)
        {
            TotalReceiveHeaderByteCount = 4;
            TotalSendHeaderByteConut = 4;
            ReceiveOffsetOnBuffer = receiveBufferOffset;
            SendOffsetOnBuffer = sendBufferOffset;
            ReceiveHeaderData = new byte[TotalReceiveHeaderByteCount];
        }

        internal void ResetForReceive()
        {
            ReceivedHeaderByteCount = ReceivedBodyByteCount = TotalReceiveBodyByteCount = 0;
            ReceiveHeaderCompleted = false;
        }

        internal void ResetForSend()
        {
            SendHeaderCompleted = SendBodyCompleted = false;
            SentBodyByteCount = TotalSendBodyByteCount = 0;
        }
    }
}
