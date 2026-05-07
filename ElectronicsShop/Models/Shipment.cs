using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectronicsShop.Models
{
    public partial class Shipment
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Shipment()
        {
            Orders = new HashSet<Order>();
        }

        [Key]
        public int shipment_id { get; set; }

        public int user_id { get; set; }

        public string recipient_first_name { get; set; }
        public string recipient_last_name { get; set; }
        public string recipient_phone { get; set; }

        [StringLength(255)]
        [DisplayName("Địa chỉ")]
        public string shipment_address { get; set; }

        [StringLength(50)]
        public string shipment_city { get; set; }

        [StringLength(50)]
        public string shipment_country { get; set; }

        [StringLength(20)]
        public string shipment_zip_code { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Order> Orders { get; set; }

        public virtual User User { get; set; }
    }
}
