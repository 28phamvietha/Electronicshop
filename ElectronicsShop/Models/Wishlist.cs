using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicsShop.Models
{
    [Table("Wishlist")]
    public partial class Wishlist
    {
        [Key]
        public int wishlist_id { get; set; }

        public int user_id { get; set; }

        public int product_id { get; set; }

        public virtual Product Product { get; set; }

        public virtual User User { get; set; }
    }
}
