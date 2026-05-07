using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectronicsShop.Models
{
    public partial class Order
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Order()
        {
            Order_Items = new HashSet<Order_Items>();
        }

        [Key]
        [DisplayName("Mã đơn")]
        public int order_id { get; set; }

        public int shipment_id { get; set; }

        public int user_id { get; set; }

        [DisplayName("Ngày đặt")]
        public DateTime? order_date { get; set; }

        [DisplayName("Tổng tiền")]
        public decimal? total_amount { get; set; }

        [StringLength(50)]
        [DisplayName("Trạng thái")]
        public string status { get; set; }

        [StringLength(50)]
        [DisplayName("Phương thức thanh toán")]
        public string payment_method { get; set; }

        [StringLength(10)]
        [DisplayName("Ghi chú")]
        public string order_note { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Order_Items> Order_Items { get; set; }

        public virtual Shipment Shipment { get; set; }

        public virtual User User { get; set; }
    }
}
