using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NKiss.Rpc.TestContract
{
    public interface IFileService
    {
        void UploadFile(string fileName, byte[] fileData);

        byte[] DownloadFile(string fileName);
    }
}
