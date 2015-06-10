
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NKiss.Common;

namespace NKiss.Socket
{
    internal enum ClientNodeStatus
    {
        Alive = 1,
        Dead = 2,
        Recovering = 3,
    }

    internal class ClientNode
    {
        private readonly List<ClientSocket> idleSockets = new List<ClientSocket>();
        private readonly List<ClientSocket> busySockets = new List<ClientSocket>();
        private readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private readonly Thread maintainThread;
        private readonly ClientNodeConfiguration config;
        private readonly ClientNodeState state;
        private Action<ClientNode> nodeErrorCallbackAction;
        internal ClientNodeStatus ClientNodeStatus { get; private set; }

        internal ClientNodeState State 
        {
            get
            {
                return state;
            }
        }
        

        internal ClientNodeWeight Weight
        {
            get
            {
                return config.Weight;
            }
        }

        internal string Name
        {
            get
            {
                return config.Name;
            }
        }

        internal string Address
        {
            get
            {
                return config.Address;
            }
        }

        internal void TryRecover()
        {
            if (ClientNodeStatus == ClientNodeStatus.Recovering) return;
            ClientNodeStatus = ClientNodeStatus.Recovering;
            Init();
        }

        private void MaintainThreadAction()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(Convert.ToInt32(((TimeSpan)config.MaintenanceInterval).TotalMilliseconds));
                }
                catch (Exception ex)
                {
                    LocalLoggingService.Warning("{0} {1}", config.MaintenanceInterval, ex.Message);
                    Thread.Sleep(1000 * 60);
                }

                if (ClientNodeStatus == ClientNodeStatus.Alive)
                {
                    if (locker.TryEnterUpgradeableReadLock(TimeSpan.FromSeconds(10)))
                    {
                        try
                        {
                            var maxIdleTimeoutSocketsRemoveCount = Math.Max(0, busySockets.Count + idleSockets.Count - config.MinConnections);
                            var idleTimeoutSockets = idleSockets.Where(s => s.IdleTime != DateTime.MaxValue && s.IdleTime + config.MaxIdleTime < DateTime.Now)
                                .OrderBy(s => s.IdleTime)
                                .Take(maxIdleTimeoutSocketsRemoveCount).ToList();

                            if (idleTimeoutSockets.Count > 0)
                            {
                                LocalLoggingService.Info(string.Format("准备从节点 {0} 中移除 {1} 个空闲超时的Socket", Name, idleTimeoutSockets.Count));
                                if (locker.TryEnterWriteLock(TimeSpan.FromSeconds(1)))
                                {
                                    try
                                    {
                                        foreach (var socket in idleTimeoutSockets)
                                        {
                                            idleSockets.Remove(socket);
                                            socket.Destroy();
                                        }
                                        Interlocked.Add(ref state.totalRemovedIdleTimeoutSocketCount, idleTimeoutSockets.Count);
                                    }
                                    catch (Exception ex)
                                    {
                                        LocalLoggingService.Error(ex.ToString());
                                    }
                                    finally
                                    {
                                        locker.ExitWriteLock();
                                    }
                                }
                            }
                            var busyTimeoutSockets = busySockets.Where(s => s.BusyTime != DateTime.MaxValue && s.BusyTime + config.MaxBusyTime < DateTime.Now).ToList();
                            if (busyTimeoutSockets.Count > 0)
                            {
                                LocalLoggingService.Info(string.Format("准备从节点 {0} 中移除 {1} 个忙碌超时的Socket", Name, busyTimeoutSockets.Count));

                                if (locker.TryEnterWriteLock(TimeSpan.FromSeconds(1)))
                                {
                                    try
                                    {
                                        foreach (var socket in busyTimeoutSockets)
                                        {
                                            busySockets.Remove(socket);
                                            socket.Destroy();
                                        }
                                        Interlocked.Add(ref state.totalRemovedBusyTimeoutSocketCount, idleTimeoutSockets.Count);
                                    }
                                    catch (Exception ex)
                                    {
                                        LocalLoggingService.Error(ex.ToString());
                                    }
                                    finally
                                    {
                                        locker.ExitWriteLock();
                                    }
                                }
                            }

                            var needAddSocketsCount = Math.Max(0, config.MinConnections - idleSockets.Count - busySockets.Count);
                            if (needAddSocketsCount > 0)
                            {
                                LocalLoggingService.Info(string.Format("准备为节点 {0} 补充 {1} 个新的Socket", Name, needAddSocketsCount));
                                for (int i = 0; i < needAddSocketsCount; i++)
                                {
                                    var socket = CreateClientSocket(false, ReturnClientSocket, CloseClientSocket, CloseClientSocketAndNode);
                                    if (socket != null)
                                    {
                                        if (locker.TryEnterWriteLock(TimeSpan.FromSeconds(1)))
                                        {
                                            try
                                            {
                                                idleSockets.Add(socket);
                                            }
                                            finally
                                            {
                                                locker.ExitWriteLock();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LocalLoggingService.Warning(string.Format("尝试为节点 {0} 补充新的Socket失败", Name));
                                        break;
                                    }
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
                    }
                }
            }
        }

        private void Init()
        {
            idleSockets.Clear();
            busySockets.Clear();

            for (int i = 0; i < config.MinConnections; i++)
            {
                var socket = CreateClientSocket(false, ReturnClientSocket, CloseClientSocket, CloseClientSocketAndNode);
                if (socket != null)
                {
                    idleSockets.Add(socket);
                }
                else
                {
                    LocalLoggingService.Warning(string.Format("初始化节点 {0} 失败，无法创建连接", config.Name));
                    return;
                }
            }

            ClientNodeStatus = ClientNodeStatus.Alive;
            LocalLoggingService.Info(string.Format("节点 {0} 完成初始化，共初始化 {1} 个Socket", config.Name, idleSockets.Count));
        }

        private ClientSocket CreateClientSocket(bool isDirectSocket, Action<ClientSocket> disposeAction, Action<ClientSocket> lowlevelErrorAction, Action<ClientSocket> highLevelErrorAction)
        {
            var ip = config.Address.Split(':')[0];
            var port = Convert.ToInt16(config.Address.Split(':')[1]);
            var socket = new ClientSocket(state, disposeAction, lowlevelErrorAction, highLevelErrorAction,
                        Convert.ToInt16(((TimeSpan)config.SendTimeout).TotalMilliseconds),
                        Convert.ToInt16(((TimeSpan)config.ReceiveTimeout).TotalMilliseconds), ip, port);
            try
            {
                socket.Connect(Convert.ToInt16(((TimeSpan)config.ConnectTimeout).TotalMilliseconds));
                return socket;
            }
            catch (Exception ex)
            {
                LocalLoggingService.Warning(string.Format("节点 {0} 创建Socket出错，异常信息为：{1}", config.Name, ex.ToString()));
                if (!isDirectSocket)
                    CloseClientSocketAndNode(socket);
                else
                    socket.Destroy();
            }

            return null;
        }

        private void CloseClientSocket(ClientSocket socket)
        {
            socket.Destroy();
            busySockets.Remove(socket);
        }

        private void CloseClientSocketAndNode(ClientSocket socket)
        {
            if (ClientNodeStatus == ClientNodeStatus.Dead) return;

            LocalLoggingService.Debug("节点 {0} 关闭整个节点", config.Name);

            Destory();
            ClientNodeStatus = ClientNodeStatus.Dead;
            nodeErrorCallbackAction(this);
        }


        private void Destory()
        {
            if (locker.TryEnterWriteLock(TimeSpan.FromSeconds(1)))
            {
                try
                {
                    foreach (var socket in new List<ClientSocket>(idleSockets))
                    {
                        socket.Destroy();
                    }
                    foreach (var socket in new List<ClientSocket>(busySockets))
                    {
                        socket.Destroy();
                    }
                    idleSockets.Clear();
                    busySockets.Clear();
                    LocalLoggingService.Warning(string.Format("节点 {0} 已经清理完毕", config.Name));
                }
                catch (Exception ex)
                {
                    LocalLoggingService.Warning(string.Format("节点 {0} 清理时候出错，异常信息为：{1}", config.Name, ex.ToString()));
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }

        private void ReturnClientSocket(ClientSocket socket)
        {
            if (locker.TryEnterWriteLock(TimeSpan.FromSeconds(1)))
            {
                socket.Release();
                try
                {
                    busySockets.Remove(socket);
                    if (idleSockets.Count + busySockets.Count < config.MaxConnections)
                    {
                        idleSockets.Add(socket);
                    }
                    else
                    {
                        LocalLoggingService.Warning(string.Format("节点 {0} 的Socket池已达到最大限制 {1}，Socket无须放回池", config.Name, config.MaxConnections));
                        socket.Destroy();
                    }
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
            else
            {
                LocalLoggingService.Warning(string.Format("节点 {0} 无法获得锁，最大连接数为 {1}，Socket不能放回池", config.Name, config.MaxConnections));
                socket.Destroy();
            }

        }

        internal ClientNode(ClientNodeConfiguration config, ClientNodeState state, Action<ClientNode> errorAction)
        {
            if (string.IsNullOrEmpty(config.Address))
                throw new ArgumentNullException(string.Format("未设置节点地址 {0}.Address，请检查配置！", config.Name));
            if (config.Address.Split(':').Length != 2)
                throw new ArgumentException(string.Format("节点 {0}的Address格式必须为IP:Port，请检查配置", config.Name), "Address");
            var port = 0;
            if (!int.TryParse(config.Address.Split(':')[1], out port))
                throw new ArgumentException(string.Format("节点 {0}的Address的端口号不是数字，请检查配置！", config.Name), "Address");
            if (config.MaxConnections < config.MinConnections)
                throw new ArgumentException(string.Format("节点 {0}的MaxConnections不能小于MinConnections，请检查配置！", config.Name), "MaxConnections");

            if (config.ConnectTimeout < TimeSpan.FromMilliseconds(100))
                config.ConnectTimeout = TimeSpan.FromMilliseconds(100);
            if (config.ReceiveTimeout < TimeSpan.FromMilliseconds(100))
                config.ReceiveTimeout = TimeSpan.FromMilliseconds(100);
            if (config.SendTimeout < TimeSpan.FromMilliseconds(100))
                config.SendTimeout = TimeSpan.FromMilliseconds(100);

            if (config.MaxIdleTime < TimeSpan.FromSeconds(1))
                config.MaxIdleTime = TimeSpan.FromSeconds(1);
            if (config.MaxBusyTime < TimeSpan.FromSeconds(1))
                config.MaxBusyTime = TimeSpan.FromSeconds(1);
            if (config.MaintenanceInterval < TimeSpan.FromSeconds(1))
                config.MaintenanceInterval = TimeSpan.FromSeconds(1);

            this.config = config;
            this.state = state;
            this.nodeErrorCallbackAction = errorAction;

            Init();
            maintainThread = new Thread(MaintainThreadAction)
            {
                IsBackground = true,
                Name = string.Format("{0}_{1}", "Adhesive.DistributedComponentClient_NodeMaintainThread", config.Name),
            };
            maintainThread.Start();

            LocalLoggingService.Info(string.Format("初始化节点 {0} 完成", Name));
        }

        internal ClientSocket GetDirectClientSocket()
        {
            LocalLoggingService.Debug(string.Format("节点 {0} 获取一个短连接", config.Name));
            return CreateClientSocket(true, socket =>
            {
                socket.Destroy();
            }, socket =>
            {
                socket.Destroy();
            }, socket =>
            {
                socket.Destroy();
            });
        }

        internal ClientSocket GetClientSocket()
        {
            if (ClientNodeStatus != ClientNodeStatus.Alive)
                return null;

            if (locker.TryEnterUpgradeableReadLock(TimeSpan.FromSeconds(10)))
            {
                try
                {
                    Interlocked.Exchange(ref state.currentSocketCount, idleSockets.Count + busySockets.Count);
                    var socket = idleSockets.OrderByDescending(s => s.IdleTime).FirstOrDefault();
                    if (socket != null)
                    {
                        socket.Acquire();
                        locker.EnterWriteLock();
                        try
                        {
                            idleSockets.Remove(socket);
                            busySockets.Add(socket);
                        }
                        finally
                        {
                            locker.ExitWriteLock();
                        }
                    }

                    else
                    {
                        if (busySockets.Count < config.MaxConnections)
                        {
                            LocalLoggingService.Debug(string.Format("节点 {0} 中没有空闲的Socket，新创建一个Socket并加入池", config.Name));
                            socket = CreateClientSocket(false, ReturnClientSocket, CloseClientSocket, CloseClientSocketAndNode);
                            if (socket != null)
                            {
                                locker.EnterWriteLock();
                                try
                                {
                                    busySockets.Add(socket);
                                }
                                finally
                                {
                                    locker.ExitWriteLock();
                                }

                            }
                            else
                            {
                                LocalLoggingService.Warning(string.Format("节点 {0} 中GetClientSocket尝试创建一个新的Socket失败", config.Name));
                            }
                        }
                        else
                        {
                            LocalLoggingService.Warning(string.Format("节点 {0} 的Socket池已满，无法获得Socket ", config.Name));
                        }
                    }

                    return socket;
                }
                finally
                {
                    locker.ExitUpgradeableReadLock();
                }
            }

            LocalLoggingService.Debug(string.Format("节点 {0} 没有获取到Socket，原因是获得锁失败", config.Name));
            return null;
        }
    }
}
