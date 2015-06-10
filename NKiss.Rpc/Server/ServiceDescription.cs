using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NKiss.Rpc
{
    public enum ServiceInstanceMode
    {
        SingleCall = 1,
        Singleton = 2,
    }

    internal class ServiceDescription
    {
        internal Type InterfaceType { get; set; }

        internal Type ServiceType { get; set; }

        internal ServiceInstanceMode ServiceInstanceMode { get; set; }

        internal object SingletonObject { get; set; }

        internal Dictionary<string, MethodInfo> Methods { get; set; }
    }
}
