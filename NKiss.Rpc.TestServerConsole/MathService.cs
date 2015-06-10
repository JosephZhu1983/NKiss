using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKiss.Rpc.TestContract;

namespace NKiss.Rpc.TestServerConsole
{
    class MathService : IMathService
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public int Devide(int a, int b)
        {
            return a / b;
        }
    }
}
