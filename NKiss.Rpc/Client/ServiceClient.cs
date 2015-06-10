using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NKiss.Rpc
{
    public class ServiceClient
    {
        public static T CreateClient<T>() where T : class
        {
            return (T)(new ServiceProxy<T>().GetTransparentProxy());
        }
    }
}
