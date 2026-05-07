using System.ComponentModel.DataAnnotations;

namespace ElectronicsShop.Models
{
    public partial class Product_Images
    {
        [Key]
        public int image_id { get; set; }

        [Required]
        [StringLength(255)]
        public string image_url { get; set; }

        public int product_id { get; set; }

        public virtual Product Product { get; set; }
    }
}
