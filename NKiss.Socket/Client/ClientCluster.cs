
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NKiss.Common;

namespace NKiss.Socket
{
    internal class ClientCluster
    {
        private readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private readonly ClientNodeLocator nodeLocator = new ClientNodeLocator();
        private readonly Dictionary<string, ClientNode> clientNodes = new Dictionary<string, ClientNode>();
        private readonly Dictionary<string, ClientNode> deadClientNodes = new Dictionary<string, ClientNode>();
        private readonly ClientClusterConfiguration config;
        private readonly ClientClusterState state;
        private readonly Thread tryRecoverNodeThread;

        internal ClientClusterState State
        {
            get
            {
                return state;
            }
        }

        internal ClientClusterConfiguration Configuration
        {
            get
            {
                return config;
            }
        }

        private void NodeErrorCallback(ClientNode node)
        {
            try
            {
                if (!deadClientNodes.ContainsKey(node.Name))
                {
                    deadClientNodes.Add(node.Name, node);
                    if (clientNodes.ContainsKey(node.Name))
                    {
                        clientNodes.Remove(node.Name);
                    }
                    InitNodeLocator();
                    Interlocked.Increment(ref node.State.totalFailedCount);
                    LocalLoggingService.Warning(string.Format("集群 {0} 中节点 {1} 出现错误，已经从集群中移除", config.Name, node.Name));
                }
            }
            catch (Exception ex)
            {
                LocalLoggingService.Error(ex.ToString());
            }

        }

        private void MaintainThreadAction()
        {
            while (true)
            {
                Thread.Sleep(Math.Max((int)(((TimeSpan)config.MaintenanceInterval).TotalMilliseconds), 1000));

                if (locker.TryEnterUpgradeableReadLock(TimeSpan.FromSeconds(10)))
                {
                    try
                    {
                        var deadClientNodesCopy = deadClientNodes.Select(i => i.Value).ToArray();
                        foreach (var node in deadClientNodesCopy)
                        {
                            LocalLoggingService.Debug(string.Format("集群 {0} 开始检查节点 {1} 是否已经恢复...", config.Name, node.Name));

                            ClientSocket socket = null;
                            using (socket = node.GetDirectClientSocket())
                            {
                            }

                            if (socket != null)
                            {
                                LocalLoggingService.Info(string.Format("集群 {0} 节点 {1} 正在恢复...", config.Name, node.Name));
                                node.TryRecover();
                                if (node.ClientNodeStatus == ClientNodeStatus.Alive)
                                {
                                    if (locker.TryEnterWriteLock(TimeSpan.FromSeconds(10)))
                                    {
                                        try
                                        {
                                            if (deadClientNodes.ContainsKey(node.Name))
                                            {
                                                deadClientNodes.Remove(node.Name);
                                                clientNodes.Add(node.Name, node);
                                                InitNodeLocator();
                                                Interlocked.Increment(ref node.State.totalRecoverCount);
                                                LocalLoggingService.Info(string.Format("集群 {0} 节点 {1} 已经恢复", config.Name, node.Name));
                                            }                                           
                                        }
                                        finally
                                        {
                                            locker.ExitWriteLock();
                                        }                                        
                                    }
                                }
                                else
                                {
                                    LocalLoggingService.Warning(string.Format("集群 {0} 节点 {1} 恢复失败", config.Name, node.Name));
                                }
                            }
                            else
                            {
                                LocalLoggingService.Info(string.Format("集群 {0} 节点 {1} 并没有恢复", config.Name, node.Name));
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        LocalLoggingService.Error(ex.ToString());
                    }
                    finally
                    {
                        locker.ExitUpgradeableReadLock();
                    }

                    if (clientNodes.Count == 0)
                        LocalLoggingService.Error(string.Format("严重错误，集群 {0} 中的所有节点都已经失效！", config.Name));
                }
            }
        }

        private void InitNodeLocator()
        {
            nodeLocator.Initialize(clientNodes.Select(item => item.Value).ToList());
            LocalLoggingService.Info(string.Format("集群 {0} 构建分配方案完成", config.Name));
        }

        internal ClientCluster(ClientClusterConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException("传入的集群配置为空！");
            if (string.IsNullOrEmpty(config.Name))
                throw new ArgumentException("集群名为空！");

            tryRecoverNodeThread = new Thread(MaintainThreadAction)
            {
                Name = string.Format("{0}_{1}", "NKiss.Socket.Client_TryRecoverNodeThreadAction", config.Name),
                IsBackground = true,
            };
            tryRecoverNodeThread.Start();

            this.config = config;
            this.state = new ClientClusterState(config);

            foreach (var nodeConfig in config.ClientNodeConfigurations)
            {
                if (clientNodes.ContainsKey(nodeConfig.Name))
                    throw new Exception(string.Format("在集群 {0} 中已经存在名为 {1} 的节点，请检查配置!", config.Name, nodeConfig.Name));
                var node = new ClientNode(nodeConfig, state.ClientNodeStates[nodeConfig.Name], this.NodeErrorCallback);
                if (node.ClientNodeStatus == ClientNodeStatus.Alive)
                {
                    clientNodes.Add(nodeConfig.Name, node);
                }
            }

            InitNodeLocator();

            LocalLoggingService.Info(string.Format("初始化集群 {0} 完成", config.Name));

        }

        internal ClientSocket AcquireSocket()
        {
            ClientNode node = nodeLocator.LocateNode();
            if (node == null)
                return null;
            if (node.ClientNodeStatus == ClientNodeStatus.Dead)
                NodeErrorCallback(node);
            return node.GetClientSocket();
        }
    }
}
