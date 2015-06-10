
using System;
using System.Collections.Generic;
using NKiss.Common;
using System.Text;

namespace NKiss.Socket
{
    public abstract class AbstractClient<T> where T : AbstractClient<T>, new()
    {
        private readonly static Dictionary<string, ClientCluster> clusters = new Dictionary<string, ClientCluster>();
        private readonly static int MAX_BODY_BYTES = 1024 * 1024 * 1024;
        private readonly static T instance = new T();

        public ClientClusterConfiguration Configuration { get; private set; }
        public ClientClusterState State { get; private set; }

        public static T GetClient(string clusterName)
        {
            var config = ClientConfiguration.GetClientClusterConfiguration(clusterName);
            if (config == null)
                throw new Exception(string.Format("没有找到集群 {0} 的配置信息！", clusterName));

            if (!clusters.ContainsKey(clusterName))
            {
                lock (clusters)
                {
                    if (!clusters.ContainsKey(clusterName))
                    {
                        var cluster = new ClientCluster(config);
                        clusters.Add(clusterName, cluster);
                        instance.Configuration = cluster.Configuration;
                        instance.State = cluster.State;
                    }
                }
            }
            return instance;
        }

        protected byte[] SendAndReceiveData(byte[] sendData)
        {
            using (var socket = GetCluster().AcquireSocket())
            {
                if (socket != null)
                {
                    if (sendData.Length > MAX_BODY_BYTES)
                        throw new Exception(string.Format("期望发送的主体过大，字节数为 {0}，超过了 {1}", sendData.Length, MAX_BODY_BYTES));
                    byte[] sendHeader = BitConverter.GetBytes(sendData.Length);
                    byte[] sendBuffer = new byte[4 + sendData.Length];
                    Buffer.BlockCopy(sendHeader, 0, sendBuffer, 0, 4);
                    Buffer.BlockCopy(sendData, 0, sendBuffer, 4, sendData.Length);
                    socket.Write(sendBuffer, 0, sendBuffer.Length);
                    var receiveheader = socket.Read(4);
                    if (receiveheader != null)
                    {
                        var receiveBodyLength = BitConverter.ToInt32(receiveheader, 0);
                        if (receiveBodyLength > MAX_BODY_BYTES)
                            throw new Exception(string.Format("期望接受的主体过大，字节数为 {0}，超过了 {1}", receiveBodyLength, MAX_BODY_BYTES));
                        var receiveBody = socket.Read(receiveBodyLength);
                        return receiveBody;
                    }
                    else
                        throw new Exception(string.Format("从 {0} 读取数据失败！", Configuration.Name));
                }
                else
                    throw new Exception(string.Format("从 {0} 获得连接失败！", Configuration.Name));
            }
        }

        private ClientCluster GetCluster()
        {
            if (!clusters.ContainsKey(Configuration.Name))
                throw new Exception(string.Format("没有找到集群 {0}！", Configuration.Name));
            return clusters[Configuration.Name];
        }
    }
}
