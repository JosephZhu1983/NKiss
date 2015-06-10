using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKiss.Socket;

namespace NKiss.Rpc
{
    public class ServiceHost : IDisposable
    {
        public AsyncSocketServerState SocketState
        {
            get
            {
                if (server == null) throw new Exception("服务未启动！");
                return server.State;
            }
        }

        public AsyncSocketServerConfiguration SocketConfiguration
        {
            get
            {
                if (server == null) throw new Exception("服务未启动！");
                return server.Configuration;
            }
        }

        private AsyncSocketServer server;
        private ServerMessageHandler handler;
        private Dictionary<string, ServiceDescription> metaData = new Dictionary<string, ServiceDescription>();

        public void AddService<TServiceInterface, TService>(ServiceInstanceMode mode) where TService : new()
        {
            var interfaceType = typeof(TServiceInterface);
            var serviceType = typeof(TService);

            if (server != null)
                throw new Exception("添加服务失败，服务启动后不允许增加服务！");

            if (!interfaceType.IsInterface)
                throw new Exception(string.Format("添加服务失败，服务接口 {0} 不是接口！", interfaceType.FullName));
            if (!serviceType.IsClass)
                throw new Exception(string.Format("添加服务失败，服务 {0} 不是类！", serviceType.FullName));
            if (!serviceType.GetInterfaces().Contains(interfaceType))
                throw new Exception(string.Format("添加服务失败，服务 {0} 没有实现接口 {1}！", serviceType.FullName, interfaceType.FullName));

            var key = typeof(TServiceInterface).FullName;

            lock (metaData)
            {
                if (metaData.ContainsKey(key))
                {
                    throw new Exception(string.Format("添加服务失败，服务接口 {0} 已经添加过！", interfaceType.FullName));
                }
                else
                {
                    TService obj = default(TService);
                    if (mode == ServiceInstanceMode.Singleton)
                    {
                        try
                        {
                            obj = Activator.CreateInstance<TService>();
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("添加服务失败，无法创建服务 {0} 的实例！", serviceType.FullName));
                        }
                    }

                    var methods = serviceType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                        .ToDictionary(_ => _.Name, _ => _);

                    metaData.Add(key, new ServiceDescription
                    {
                        InterfaceType = interfaceType,
                        ServiceType = serviceType,
                        ServiceInstanceMode = mode,
                        SingletonObject = obj,
                        Methods = methods,
                    });
                }
            }
        }

        public void Start()
        {
            if (metaData.Count == 0)
                throw new Exception("在启动服务之前，需要至少调用AddSerive()方法注册一个服务！");

            server = new AsyncSocketServer();
            handler = new ServerMessageHandler(metaData);
            try
            {
                server.Start(handler.GenerateResponse);
            }
            catch(Exception ex)
            {
                throw new Exception("服务启动时出现异常！", ex);
            }
        }

        public void Dispose()
        {
            server.Dispose();
        }
    }
}
