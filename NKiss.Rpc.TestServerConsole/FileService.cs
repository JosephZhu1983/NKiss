using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKiss.Rpc.TestContract;
using System.IO;

namespace NKiss.Rpc.TestServerConsole
{
    class FileService : IFileService
    {
        private readonly string FILE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");

        public FileService()
        {
            if (!Directory.Exists(FILE_PATH))
                Directory.CreateDirectory(FILE_PATH);
        }

        public void UploadFile(string fileName, byte[] fileData)
        {
            File.WriteAllBytes(Path.Combine(FILE_PATH, fileName), fileData);
        }

        public byte[] DownloadFile(string fileName)
        {
            return File.ReadAllBytes(Path.Combine(FILE_PATH, fileName));
        }
    }
}
