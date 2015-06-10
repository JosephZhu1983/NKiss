using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NKiss.Rpc
{
    [Serializable]
    public class RequestMessage
    {
        public string ServiceInterfaceName { get; set; }

        public string MethodName { get; set; }

        public object[] Parameters { get; set; }

        public override string ToString()
        {
            return string.Format("RequestMessage: {0}.{1}(...)", ServiceInterfaceName, MethodName);
        }
    }
}
