using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKiss.Rpc.TestContract;

namespace NKiss.Rpc.TestServerConsole
{
    class ShoppingCartService : IShoppingCartService
    {
        private Dictionary<string, List<ShoppingCartItem>> data = new Dictionary<string, List<ShoppingCartItem>>();

        public void AddItem(string name, ShoppingCartItem item)
        {
            if (!data.ContainsKey(name))
            {
                lock (data)
                {
                    if (!data.ContainsKey(name))
                    {
                        data.Add(name, new List<ShoppingCartItem>
                        {
                            { item }
                        });
                        return;
                    }
                }
            }

            var mycart = data[name];
            lock (mycart)
                mycart.Add(item);
        }

        public List<ShoppingCartItem> GetItems(string name)
        {
            if (!data.ContainsKey(name)) return new List<ShoppingCartItem>();

            lock (data[name])
                return data[name];
        }

        public void ClearItems(string name)
        {
            if (!data.ContainsKey(name)) return;

            lock (data[name])
            {
                data[name] = new List<ShoppingCartItem>();
            }
        }
    }
}
