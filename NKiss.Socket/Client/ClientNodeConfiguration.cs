
using System;
using Adhesive.Common;

namespace NKiss.Socket
{
    public enum ClientNodeWeight
    {
        Low = 100,
        Medium = 200,
        High = 400,
    }

    public class ClientNodeConfiguration
    {
        public string Name { get; set; }

        public string Address { get; set; }

        public ClientNodeWeight Weight { get; set; }

        public int MaxConnections { get; set; }

        public int MinConnections { get; set; }

        public SerializableTimeSpan MaxIdleTime { get; set; }

        public SerializableTimeSpan MaxBusyTime { get; set; }

        public SerializableTimeSpan ConnectTimeout { get; set; }

        public SerializableTimeSpan SendTimeout { get; set; }

        public SerializableTimeSpan ReceiveTimeout { get; set; }

        public SerializableTimeSpan MaintenanceInterval { get; set; }

        public ClientNodeConfiguration()
        {
            MaxConnections = 50;
            MinConnections = 5;
            MaxIdleTime = TimeSpan.FromSeconds(10);
            MaxBusyTime = TimeSpan.FromSeconds(10);
            SendTimeout = TimeSpan.FromSeconds(30);
            ReceiveTimeout = TimeSpan.FromSeconds(30);
            ConnectTimeout = TimeSpan.FromSeconds(5);
            MaintenanceInterval = TimeSpan.FromSeconds(30);
            Weight = ClientNodeWeight.Medium;
        }
    }
}
