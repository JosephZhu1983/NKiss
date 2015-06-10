using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NKiss.Socket
{
    public class ClientClusterState
    {
        private Timer perSecondCounterTimer;
        public Dictionary<string, ClientNodeState> ClientNodeStates { get; private set; }

        public ClientClusterState(ClientClusterConfiguration config)
        {
            ClientNodeStates = new Dictionary<string, ClientNodeState>();
            foreach (var node in config.ClientNodeConfigurations)
            {
                ClientNodeStates.Add(node.Name, new ClientNodeState());
            }

            perSecondCounterTimer = new Timer(state =>
            {
                foreach (var node in ClientNodeStates.Values)
                {
                    node.perSecondBytesReceived = (int)(node.totalBytesReceived - node.lastSecondBytesReceived);
                    node.perSecondBytesSent = (int)(node.totalBytesSent - node.lastSecondBytesSent);
                    node.perSecondSentConut = (int)(node.totalSentCount - node.lastSecondSentCount);
                    node.perSecondReceiveConut = (int)(node.totalReceiveCount - node.lastSecondReceiveCount);

                    node.lastSecondBytesReceived = node.totalBytesReceived;
                    node.lastSecondBytesSent = node.totalBytesSent;
                    node.lastSecondReceiveCount = node.totalReceiveCount;
                    node.lastSecondSentCount = node.totalSentCount;
                }
                
            }, null, 0, 1000);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("当前时间\t{0}{1}", DateTime.Now, Environment.NewLine);
            sb.AppendLine();

            foreach (var node in ClientNodeStates)
            {
                sb.AppendFormat("节点名\t{0}{1}", node.Key, Environment.NewLine);
                sb.AppendLine();
                sb.AppendFormat("\t每秒接受字节\t{0}{1}", node.Value.PerSecondBytesReceived, Environment.NewLine);
                sb.AppendFormat("\t总共接受字节\t{0}{1}", node.Value.TotalBytesReceived, Environment.NewLine);
                sb.AppendFormat("\t每秒接受次数\t{0}{1}", node.Value.PerSecondReceiveConut, Environment.NewLine);
                sb.AppendFormat("\t总共接受次数\t{0}{1}", node.Value.TotalReceiveCount, Environment.NewLine);
                sb.AppendLine();
                sb.AppendFormat("\t每秒发送字节\t{0}{1}", node.Value.PerSecondBytesSent, Environment.NewLine);
                sb.AppendFormat("\t总共发送字节\t{0}{1}", node.Value.TotalBytesSent, Environment.NewLine);
                sb.AppendFormat("\t每秒发送次数\t{0}{1}", node.Value.PerSecondSentConut, Environment.NewLine);
                sb.AppendFormat("\t总共发送次数\t{0}{1}", node.Value.TotalSentCount, Environment.NewLine);
                sb.AppendLine();
                sb.AppendFormat("\t总共失效次数\t{0}{1}", node.Value.TotalFailedCount, Environment.NewLine);
                sb.AppendFormat("\t总共恢复次数\t{0}{1}", node.Value.TotalRecoverCount, Environment.NewLine);
                sb.AppendLine();
                sb.AppendFormat("\t当前连接\t{0}{1}", node.Value.CurrentSocketCount, Environment.NewLine);
                sb.AppendFormat("\t总共回收闲置超时的连接\t{0}{1}", node.Value.TotalRemovedIdleTimeoutSocketCount, Environment.NewLine);
                sb.AppendFormat("\t总共回收忙碌超时的连接\t{0}{1}", node.Value.TotalRemovedBusyTimeoutSocketCount, Environment.NewLine);
                sb.AppendLine();
            }
            return sb.ToString();
        }
       
    }
}
