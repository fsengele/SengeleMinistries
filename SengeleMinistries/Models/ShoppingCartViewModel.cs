using System.Collections.Generic;
using System.Linq;

namespace SengeleMinistries.Models
{
    public class ShoppingCartViewModel
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal Subtotal
        {
            get
            {
                return Items.Sum(item => item.Total);
            }
        }

        public int TotalItems
        {
            get
            {
                return Items.Sum(item => item.Quantity);
            }
        }
    }
}