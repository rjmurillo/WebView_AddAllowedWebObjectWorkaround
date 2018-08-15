using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WebViewAddAllowedWebObjectWorkaround.Shared
{
    // A sample data provider, providing instances of class Product to be used by JaavaScriptInteropMethods class
    public class ProductRepository
    {
        private static readonly Dictionary<string, Product> Products = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase)
        {
            {"Apple", new Product()
                {
                    Name = "Apple",
                    ExpiryDate = DateTime.Today.AddDays(3d),
                    Price = 3.99M,
                    Sizes = new []{"Small", "Medium", "Large"}
                }
            },
            {"Pear", new Product()
                {
                    Name = "Pear",
                    ExpiryDate = DateTime.Today.AddDays(7d),
                    Price = 2.29M,
                    Sizes = new []{"Small", "Large"}
                }
            }
        };

        public IEnumerable<Product> GetProducts()
        {
            Trace.WriteLine($"Entering {GetType().FullName}.{nameof(GetProducts)}");
            return Products.Values;
        }

        public Product GetProductByName(string name)
        {
            Trace.WriteLine($"Entering {GetType().FullName}.{nameof(GetProductByName)}");
            Products.TryGetValue(name, out var retval);
            return retval;
        }
    }
}