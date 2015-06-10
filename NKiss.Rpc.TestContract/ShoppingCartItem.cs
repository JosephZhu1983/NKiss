using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NKiss.Rpc.TestContract
{
    [Serializable]
    public class ShoppingCartItem
    {
        public string Name { get; set; }

        public int Price { get; set; }

        public override string ToString()
        {
            return Name + ":" + Price;
        }
    }
}
