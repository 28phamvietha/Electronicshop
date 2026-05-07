using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectronicsShop.Models
{
    public partial class Category
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Category()
        {
            Products = new HashSet<Product>();
        }

        [Key]
        [DisplayName("Mã Danh Mục")]
        public int category_id { get; set; }

        [Required(ErrorMessage ="Bạn cần nhập tên danh mục")]
        [StringLength(100)]
        [DisplayName("Danh Mục")]
        public string category_name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Product> Products { get; set; }
    }
}
