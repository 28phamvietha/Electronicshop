using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectronicsShop.Models
{
    public partial class User
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public User()
        {
            Carts = new HashSet<Cart>();
            Orders = new HashSet<Order>();
            Shipments = new HashSet<Shipment>();
            Wishlists = new HashSet<Wishlist>();
        }

        [Key]
        [DisplayName("Mã user")]
        public int user_id { get; set; }

        [Required(ErrorMessage ="Bạn cần nhập tên")]
        [DisplayName("Tên")]
        [StringLength(50)]
        public string first_name { get; set; }

        [Required(ErrorMessage = "Bạn cần nhập họ đệm")]
        [DisplayName("Họ đệm")]
        [StringLength(50)]
        public string last_name { get; set; }

        [Required(ErrorMessage = "Bạn cần email")]
        [DisplayName("Email")]
        [StringLength(100)]
        public string email { get; set; }

        [Required(ErrorMessage = "Bạn cần nhập mật khẩu")]
        [DisplayName("Mật khẩu")]
        [StringLength(100)]
        public string password { get; set; }

        [StringLength(20)]
        [DisplayName("Số điện thoại")]
        public string phone { get; set; }

        [StringLength(255)]
        [DisplayName("Địa chỉ")]
        public string address { get; set; }

        [StringLength(50)]
        [DisplayName("Role")]
        public string role { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Cart> Carts { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Order> Orders { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Shipment> Shipments { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Wishlist> Wishlists { get; set; }
    }
}
