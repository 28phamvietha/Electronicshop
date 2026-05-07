using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElectronicsShop.Models
{
    public class Review
    {
        public int review_id { get; set; }
        public int product_id { get; set; }
        public int user_id { get; set; }
        public int rating { get; set; }
        public string comment { get; set; }
        public DateTime created_at { get; set; }

        public User User { get; set; }
    }
}