using System.ComponentModel.DataAnnotations;

namespace ElectronicsShop.Models
{
    public partial class Order_Items
    {
        [Key]
        public int order_item_id { get; set; }

        public int order_id { get; set; }

        public int product_id { get; set; }

        public int? quantity { get; set; }

        public decimal? price { get; set; }

        public virtual Order Order { get; set; }

        public virtual Product Product { get; set; }
    }
}
