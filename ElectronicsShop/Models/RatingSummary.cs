using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElectronicsShop.Models
{
    public class RatingSummary
    {
        public int total { get; set; }
        public double avg { get; set; }
        public int star5 { get; set; }
        public int star4 { get; set; }
        public int star3 { get; set; }
        public int star2 { get; set; }
        public int star1 { get; set; }
    }
}