using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKiss.Common;

namespace NKiss.Rpc
{
    public class RpcClientConfiguration
    {
        public List<RpcClientConfigurationItem> RpcClientConfigurationItems { get; set; }

        internal static string GetRpcClientClusterName(string interfaceName)
        {
            var config = LocalConfigService.GetConfig(new RpcClientConfiguration
            {
                RpcClientConfigurationItems = new List<RpcClientConfigurationItem>
                {
                    new RpcClientConfigurationItem
                    {
                        ClusterName = "TestCluster",
                        InterfaceName = "NKiss.Rpc.TestContract.IMathService",
                    },
                        new RpcClientConfigurationItem
                    {
                        ClusterName = "TestCluster",
                        InterfaceName = "NKiss.Rpc.TestContract.IShoppingCartService",
                    },
                }
            });

            var items = config.RpcClientConfigurationItems.Where(item => item.InterfaceName == interfaceName).ToList();
            if (items.Count > 1)
                throw new Exception(string.Format("接口 {0} 找到两个对应的集群，请检查配置文件！", interfaceName));
            else if (items.Count == 0)
                throw new Exception(string.Format("接口 {0} 并没有找到配置的集群，请检查配置文件！", interfaceName));
            return items.First().ClusterName;
        }
    }

    public class RpcClientConfigurationItem
    {
        public string InterfaceName { get; set; }

        public string ClusterName { get; set; }
    }
}
