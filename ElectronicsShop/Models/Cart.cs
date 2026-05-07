using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicsShop.Models
{
    [Table("Cart")]
    public partial class Cart
    {
        [Key]
        public int cart_id { get; set; }

        public int user_id { get; set; }

        public int product_id { get; set; }

        public int? quantity { get; set; }

        public virtual Product Product { get; set; }

        public virtual User User { get; set; }
    }
}
