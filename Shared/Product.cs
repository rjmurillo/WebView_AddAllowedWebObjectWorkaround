using System;

namespace WebViewAddAllowedWebObjectWorkaround.Shared
{
    // A sample plain old CLR object (POCO)
    /// <summary>
    /// A sample plain old CLR object (POCO) for product information
    /// </summary>
    public class Product
    {

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the expiry date.
        /// </summary>
        /// <value>The expiry date.</value>
        public DateTime ExpiryDate { get; set; }
        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        /// <value>The price.</value>
        public decimal Price { get; set; }
        /// <summary>
        /// Gets or sets the sizes.
        /// </summary>
        /// <value>The sizes.</value>
        public string[] Sizes { get; set; }
    }
}