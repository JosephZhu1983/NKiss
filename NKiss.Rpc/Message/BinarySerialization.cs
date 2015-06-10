using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using NKiss.Common;

namespace NKiss.Rpc
{
    internal class BinarySerialization
    {
        internal static string FormatData(byte[] data)
        {
            return FormatData(data, 0, data.Length);
        }

        internal static string FormatData(byte[] buffer, int offset, int count)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("数据大小：{0}，数据内容", count);
            for (int i = 0; i < count; i++)
            {
                sb.AppendFormat("{0:x2}", buffer[offset + i]);
                if (i % 2 == 1) sb.Append(" ");
            }
            return sb.ToString();
        }

        internal static byte[] SerializeMessage(object message)
        {
            using (var memoryStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(memoryStream, message);
                return memoryStream.ToArray();
            }
        }

        internal static object DeserializeMessage(byte[] bytes)
        {
            try
            {
                using (var deserializeMemoryStream = new MemoryStream(bytes))
                {
                    deserializeMemoryStream.Position = 0;
                    return new BinaryFormatter().Deserialize(deserializeMemoryStream);
                }
            }
            catch (Exception ex)
            {
                LocalLoggingService.Error(string.Format("反序列化的时候出现错误，异常信息为：{0}", ex.ToString()));
                return null;
            }
        }
    }
}
