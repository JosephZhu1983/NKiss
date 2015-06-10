using System.Collections.Generic;
using System.Linq;
using NKiss.Common;

namespace NKiss.Socket
{
    public class ClientConfiguration
    {
        public List<ClientClusterConfiguration> ClientClusters { get; set; }

        internal static ClientClusterConfiguration GetClientClusterConfiguration(string clusterName)
        {
            var clusters = GetConfig().ClientClusters;
            return clusters.FirstOrDefault(c => c.Name == clusterName);
        }

        private static ClientConfiguration GetConfig()
        {
            var config = LocalConfigService.GetConfig(GetDefaultConfig());
            return config;
        }

        private static ClientConfiguration GetDefaultConfig()
        {
            return new ClientConfiguration
            {
                ClientClusters = new List<ClientClusterConfiguration>
                {
                    new ClientClusterConfiguration
                    {
                        Name = "TestCluster",
                        ClientNodeConfigurations = new List<ClientNodeConfiguration>
                        {
                            new ClientNodeConfiguration
                            {
                                Name = "TestNode1",
                                Address = "127.0.0.1:1983",
                            },
                            new ClientNodeConfiguration
                            {
                                Name = "TestNode2",
                                Address = "127.0.0.1:1984",
                            }
                        }
                    }
                }
            };
        }
    }
}
