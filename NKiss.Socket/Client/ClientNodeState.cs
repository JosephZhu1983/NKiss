using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NKiss.Socket
{
    public class ClientNodeState
    {
        internal int totalFailedCount;
        public int TotalFailedCount
        {
            get
            {

                return totalFailedCount;
            }
        }

        internal int totalRecoverCount;
        public int TotalRecoverCount
        {
            get
            {

                return totalRecoverCount;
            }
        }

        internal long totalSentCount;
        public long TotalSentCount
        {
            get
            {
                return totalSentCount;
            }
        }

        internal long lastSecondSentCount;
        internal int perSecondSentConut;
        public int PerSecondSentConut
        {
            get
            {
                return perSecondSentConut;
            }
        }

        internal long totalReceiveCount;
        public long TotalReceiveCount
        {
            get
            {
                return totalReceiveCount;
            }
        }

        internal long lastSecondReceiveCount;
        internal int perSecondReceiveConut;
        public int PerSecondReceiveConut
        {
            get
            {
                return perSecondReceiveConut;
            }
        }

        internal long totalBytesReceived;
        public long TotalBytesReceived
        {
            get
            {
                return totalBytesReceived;
            }
        }

        internal long lastSecondBytesReceived;
        internal int perSecondBytesReceived;
        public int PerSecondBytesReceived
        {
            get
            {
                return perSecondBytesReceived;
            }
        }

        internal long totalBytesSent;
        public long TotalBytesSent
        {
            get
            {
                return totalBytesSent;
            }
        }

        internal long lastSecondBytesSent;
        internal int perSecondBytesSent;
        public int PerSecondBytesSent
        {
            get
            {
                return perSecondBytesSent;
            }
        }

        internal int currentSocketCount;
        public int CurrentSocketCount
        {
            get
            {
                return currentSocketCount;
            }
        }

        internal long totalRemovedIdleTimeoutSocketCount;
        public long TotalRemovedIdleTimeoutSocketCount
        {
            get
            {
                return totalRemovedIdleTimeoutSocketCount;
            }
        }

        internal long totalRemovedBusyTimeoutSocketCount;
        public long TotalRemovedBusyTimeoutSocketCount
        {
            get
            {
                return totalRemovedBusyTimeoutSocketCount;
            }
        }
    }
}
