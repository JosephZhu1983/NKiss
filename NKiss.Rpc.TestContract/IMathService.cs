using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NKiss.Rpc.TestContract
{
    public interface IMathService
    {
        int Add(int a, int b);

        int Devide(int a, int b);
    }
}
