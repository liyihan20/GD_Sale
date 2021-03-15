using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    public class K3Emp
    {
        public string account { get; set; }
        public string emp_name { get; set; }
        public string emp_card_number { get; set; }
        public string emp_dep { get; set; }
    }

    public class K3Customer
    {
        public string account { get; set; }
        public string customer_name { get; set; }
        public string customer_number { get; set; }
    }
    public class K3CustomerInfo
    {
        public string account { get; set; }
        public string customer_name { get; set; }
        public string customer_number { get; set; }
        public string customer_attn { get; set; }
        public string customer_addr { get; set; }
        public string customer_tel { get; set; }
        public string customer_fax { get; set; }
    }

    public class K3Product
    {
        public string account { get; set; }
        public int item_id { get; set; }
        public string item_no { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public string unit_name { get; set; }
        public string unit_number { get; set; }

    }

    public class K3Items
    {
        public string account { get; set; }
        public string what { get; set; }
        public string fid { get; set; }
        public string fname { get; set; }
    }
}