using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;
using NKiss.Common;

namespace NKiss.Rpc
{
    internal class ServerMessageHandler
    {
        private Dictionary<string, ServiceDescription> metaData = new Dictionary<string, ServiceDescription>();

        internal ServerMessageHandler(Dictionary<string, ServiceDescription> metaData)
        {
            this.metaData = metaData;
        }

        internal byte[] GenerateResponse(byte[] receive)
        {
            LocalLoggingService.Debug(() => "接受消息：" + BinarySerialization.FormatData(receive));

            var requestMessage = BinarySerialization.DeserializeMessage(receive) as RequestMessage;

            ResponseMessage responseMessage = new ResponseMessage
            {
                ReturnValue = null,
                RemoteException = new Exception("无法处理请求，未知错误！"),
            };

            if (requestMessage == null)
            {
                responseMessage = new ResponseMessage
                {
                    ReturnValue = null,
                    RemoteException = new Exception("无法处理请求，原因是无法解析到请求消息！"),
                };
            }
            else
            {
                var key = requestMessage.ServiceInterfaceName;

                if (!metaData.ContainsKey(key))
                {
                    responseMessage = new ResponseMessage
                    {
                        RemoteException = new Exception("无法处理请求，原因是该服务接口在服务端并未注册！"),
                    };
                }
                else
                {
                    var desc = metaData[key];
                    var obj = desc.SingletonObject;
                    if (obj == null)
                    {
                        try
                        {
                            obj = Activator.CreateInstance(desc.ServiceType);
                        }
                        catch(Exception ex)
                        {
                            responseMessage = new ResponseMessage
                            {
                                RemoteException = new Exception("无法处理请求，原因是无法创建服务实例！", ex),
                            };
                        }
                    }

                    if (obj != null)
                    {
                        if (!desc.Methods.ContainsKey(requestMessage.MethodName))
                        {
                            responseMessage = new ResponseMessage
                            {
                                RemoteException = new Exception(string.Format("无法处理请求，原因是无法找到方法 {0}！", requestMessage.MethodName)),
                            };
                        }
                        else
                        {
                            var mi = desc.Methods[requestMessage.MethodName];
                            try
                            {
                                var result = mi.Invoke(obj, requestMessage.Parameters);
                                responseMessage = new ResponseMessage
                                {
                                    ReturnValue = result,
                                };
                            }
                            catch (TargetInvocationException ex)
                            {
                                var innerException = ex.InnerException;
                                responseMessage = new ResponseMessage
                                {
                                    RemoteException = new Exception(string.Format("无法处理请求，原因是调用方法 {0} 出现错误，请查看内部异常获知更多信息！", requestMessage.MethodName), innerException),
                                };
                            }
                            catch (Exception ex)
                            {
                                responseMessage = new ResponseMessage
                                {
                                    RemoteException = new Exception(string.Format("无法处理请求，原因是调用方法 {0} 出现错误，请查看内部异常获知更多信息", requestMessage.MethodName), ex),
                                };
                            }
                        }
                    }
                }
            }

            var send = BinarySerialization.SerializeMessage(responseMessage);
            LocalLoggingService.Debug(() => "发送消息：" + BinarySerialization.FormatData(send));
            return send;
        }
    }
}
