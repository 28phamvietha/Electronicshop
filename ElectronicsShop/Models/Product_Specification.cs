using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ElectronicsShop.Models
{
    [Table("Product_Specifications")]
    public class Product_Specification
    {
        [Key]  // 🔥 QUAN TRỌNG NHẤT
        public int spec_id { get; set; }
        public int product_id { get; set; }
        public string spec_group { get; set; }
        public string spec_name { get; set; }
        public string spec_value { get; set; }
        [ForeignKey("product_id")]
        public virtual Product Product { get; set; }

    }
}