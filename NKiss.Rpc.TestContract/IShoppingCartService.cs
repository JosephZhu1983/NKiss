using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NKiss.Rpc.TestContract
{
    public interface IShoppingCartService
    {
        void AddItem(string name, ShoppingCartItem item);

        List<ShoppingCartItem> GetItems(string name);

        void ClearItems(string name);
    }
}
