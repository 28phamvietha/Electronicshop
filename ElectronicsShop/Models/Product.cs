using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectronicsShop.Models
{
    public partial class Product
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Product()
        {
            Carts = new HashSet<Cart>();
            Order_Items = new HashSet<Order_Items>();
            Product_Images = new HashSet<Product_Images>();
            Wishlists = new HashSet<Wishlist>();
        }

        [Key]
        [DisplayName("Mã sản phẩm")]
        public int product_id { get; set; }

        public int category_id { get; set; }

        [Required(ErrorMessage = "Bạn cần nhập tên sản phẩm")]
        [StringLength(100)]
        [DisplayName("Tên sản phẩm")]
        public string product_name { get; set; }

        [StringLength(2000)]
        [DisplayName("Mô tả")]
        public string description { get; set; }

        [DisplayName("Giá sản phẩm")]
        public decimal? price { get; set; }

        [DisplayName("Giá bán")]
        public decimal? discount_price { get; set; }

        [DisplayName("Stok")]
        public int? stock { get; set; }

        [StringLength(255)]
        [DisplayName("Hãng sản xuất")]
        public string brand { get; set; }

        [DisplayName("New")]
        public bool? is_new { get; set; }

        //public string image_url { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Cart> Carts { get; set; }

        public virtual Category Category { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Order_Items> Order_Items { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Product_Images> Product_Images { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Wishlist> Wishlists { get; set; }

        public virtual ICollection<Product_Specification> Specifications { get; set; }

    }
}
