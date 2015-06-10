using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;

namespace NKiss.Rpc
{
    internal class ServiceProxy<T> : RealProxy where T : class
    {
        private static Dictionary<string, RpcClient> clients = new Dictionary<string, RpcClient>();

        public ServiceProxy()
            : base(typeof(T))
        {
            
        }


        public override IMessage Invoke(IMessage msg)
        {
            var message = msg as IMethodCallMessage;
            if (message == null)
            {
                return null;
            }

            var interfaceName = typeof(T).FullName;

            RpcClient client = null;

            if (!clients.ContainsKey(interfaceName))
            {
                lock (clients)
                {
                    if (!clients.ContainsKey(interfaceName))
                    {
                        try
                        {
                            var clusterName = RpcClientConfiguration.GetRpcClientClusterName(interfaceName);
                            client = RpcClient.GetClient(clusterName);
                        }
                        catch (Exception ex)
                        {
                            return new ReturnMessage(ex, message);
                        }
                    }
                }
            }
            else
            {
                client = clients[interfaceName];
            }

            var requestMessage = new RequestMessage
            {
                ServiceInterfaceName = interfaceName,
                MethodName = message.MethodName,
                Parameters = message.InArgs
            };

            try
            {
                var responseMessage = client.RequestAndGetResponse(requestMessage);

                return responseMessage.RemoteException != null
                       ? new ReturnMessage(responseMessage.RemoteException, message)
                       : new ReturnMessage(responseMessage.ReturnValue, null, 0, message.LogicalCallContext, message);
            }
            catch (Exception ex)
            {
                return new ReturnMessage(ex, message);
            }
        }
    }
}
