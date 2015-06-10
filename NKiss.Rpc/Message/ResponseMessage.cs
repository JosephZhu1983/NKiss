using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NKiss.Rpc
{
    [Serializable]
    public class ResponseMessage
    {
        public object ReturnValue { get; set; }

        public Exception RemoteException { get; set; }

        public override string ToString()
        {
            return string.Format("ResponseMessage: 返回 {0}, 异常 = {1}", ReturnValue, RemoteException);
        }
    }
}
