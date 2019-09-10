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
        public string extra { get; set; }

        public SimpleResultModel()
        {
            this.suc = true;
        }
        public SimpleResultModel(bool _suc, string _msg = "", string _extra = "")
        {
            this.suc = _suc;
            this.msg = _msg;
            this.extra = _extra;
        }

        public SimpleResultModel(Exception ex)
        {
            this.suc = false;
            this.msg = ex.Message;
        }
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