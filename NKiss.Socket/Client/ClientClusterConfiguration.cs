

using System;
using System.Collections.Generic;
using Adhesive.Common;

namespace NKiss.Socket
{
    public class ClientClusterConfiguration
    {
        public string Name { get; set; }

        public SerializableTimeSpan MaintenanceInterval { get; set; }

        public List<ClientNodeConfiguration> ClientNodeConfigurations { get; set; }

        public ClientClusterConfiguration()
        {
            MaintenanceInterval = TimeSpan.FromSeconds(5);
        }
    }
}
