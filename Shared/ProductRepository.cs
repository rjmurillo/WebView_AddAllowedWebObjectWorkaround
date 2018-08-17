using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WebViewAddAllowedWebObjectWorkaround.Shared
{
    /// <summary>
    /// A sample data provider, providing instancess of <see cref="Product"/>.
    /// </summary>
    public class ProductRepository
    {
        private static readonly Dictionary<string, Product> Products = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase)
        {
            {"Apple", new Product
                {
                    Name = "Apple",
                    ExpiryDate = DateTime.Today.AddDays(3d),
                    Price = 3.99M,
                    Sizes = new []{"Small", "Medium", "Large"}
                }
            },
            {"Pear", new Product
                {
                    Name = "Pear",
                    ExpiryDate = DateTime.Today.AddDays(7d),
                    Price = 2.29M,
                    Sizes = new []{"Small", "Large"}
                }
            }
        };

        /// <summary>
        /// Gets the products.
        /// </summary>
        /// <returns><seealso cref="IEnumerable{Product}"/></returns>
        public IEnumerable<Product> GetProducts()
        {
            Trace.WriteLine($"Entering {GetType().FullName}.{nameof(GetProducts)}");
            return Products.Values;
        }

        /// <summary>
        /// Gets the <see cref="Product"/> for a given name.
        /// </summary>
        /// <param name="name">The name of the product.</param>
        /// <returns>If the repository contains a product with the given name, then an instance of <see cref="Product"/>; otherwise, null.</returns>
        public Product GetProductByName(string name)
        {
            Trace.WriteLine($"Entering {GetType().FullName}.{nameof(GetProductByName)}");
            Products.TryGetValue(name, out var retval);
            return retval;
        }
    }
}