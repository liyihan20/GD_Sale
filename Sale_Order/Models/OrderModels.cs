using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    //订单临时表列表model
    public class OrderModel
    {
        public int bill_id { get; set; }
        public string sys_no { get; set; }
        public string buy_unit { get; set; }
        public string product_name { get; set; }
        public string product_model { get; set; }
        public decimal? qty { get; set; }
        public decimal? deal_price { get; set; }
        public string apply_status { get; set; }
        public string apply_date { get; set; }
    }

    //附件信息model
    public class AttachmentModel
    {
        public string file_name { get; set; }
        public string file_size { get; set; }
        public string upload_time { get; set; }
    }

    //变更订单信息model
    public class BillChangeInfoModel {
        public int update_id { get; set; }
        public string bill_no { get; set; }
        public string apply_date { get; set; }
        public string change_content { get; set; }
        public string apply_status { get; set; }
        public string bill_type { get; set; }
    }

    //营业员比例model
    public class SalerPerModel{
        public int salerId { get; set; }
        public string cardNumber { get; set; }
        public string salerName { get; set; }
        public string zt { get; set; }
        public float percent { get; set; }
    }
    
}