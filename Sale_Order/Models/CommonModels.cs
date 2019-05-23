using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    public class SimpleResultModel
    {
        public bool suc { get; set; }
        public string msg { get; set; }
    }

    public class AttachmentModelNew
    {
        public string file_id { get; set; }
        public string file_name { get; set; }
        public string file_size { get; set; }
        public string uploader { get; set; }
        public string file_status { get; set; }
    }
}