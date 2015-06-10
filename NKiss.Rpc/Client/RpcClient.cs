using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKiss.Socket;
using NKiss.Common;

namespace NKiss.Rpc
{
    internal class RpcClient : AbstractClient<RpcClient>
    {
        public ResponseMessage RequestAndGetResponse(RequestMessage request)
        {
            var requestBytes = BinarySerialization.SerializeMessage(request);
            LocalLoggingService.Debug(() => "发送消息：" + BinarySerialization.FormatData(requestBytes));

            var responseBytes = SendAndReceiveData(requestBytes);
            LocalLoggingService.Debug(() => "接受消息：" + BinarySerialization.FormatData(responseBytes));

            var responseMessage = BinarySerialization.DeserializeMessage(responseBytes) as ResponseMessage;

            if (responseMessage == null)
                throw new Exception("未知错误，没有得到服务端的返回");

            return responseMessage;
        }
    }
}
