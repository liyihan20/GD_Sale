using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    //部门审核人model
    public class Examiner
    {
        //charger表id
        public int id { get; set; }
        //审核步骤，1：办事处一审；2：办事处二审；3：市场部一审；4：市场部二审
        public int step { get; set; }
        //审核名称，例如：办事处一审
        public string name { get; set; }
        //审核人部门
        public string department { get; set; }
        //审核人名字
        public string examiner { get; set; }
    }

    public class DepList {
        public int id { get; set; }
        public string name { get; set; }
        public int? dep_no { get; set; }
        public string dep_type { get; set; }
    }
}