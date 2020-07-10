using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    public class UserInfo
    {
        public int userId { get; set; }
        public string userName { get; set; }
        public string realName { get; set; }
        public string email { get; set; }
        public string departmentName { get; set; }
    }
}