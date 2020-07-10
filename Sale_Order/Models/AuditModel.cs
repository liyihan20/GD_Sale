using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    //审核人查看自己待审核或审核过的单据列表
    public class AuditListModel
    {
        public AuditListModel()
        {
            this.account = "光电总部";
        }

        public int step { get; set; }
        public string stepName { get; set; }
        public int applyId { get; set; }
        public string sysNum { get; set; }
        public string depName { get; set; }
        public string salerName { get; set; }
        public string previousStepTime { get; set; }
        public string status { get; set; }
        public string orderNo { get; set; }
        public string hasImportK3 { get; set; }
        public string finalStatus { get; set; }
        public string encryptNo { get; set; }
        public string orderType { get; set; }
        public string model { get; set; }
        public string account { get; set; }
    }

    //审核人查看自己待审核或审核过的变更单据列表
    public class AuditUpdateListModel
    {
        public int step { get; set; }
        public string bill_no { get; set; }
        public string bill_type { get; set; }
        public int update_id { get; set; }
        public string depName { get; set; }
        public string salerName { get; set; }
        public string previousStepTime { get; set; }
        public string status { get; set; }

       
    }

    //查看审核状态
    public class AuditStatusModel {

        public int step { get; set; }
        public string stepName { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string auditor { get; set; }
        public string department { get; set; }
        public string comment { get; set; }
        public bool? pass { get; set; }
    }

    //后台查看订单model
    public class backBills { 
        public int applyId { get; set; }
        public string apply_date { get; set; }
        public string orderType { get; set; }
        public string sysNum { get; set; }
        public string depName { get; set; }
        public string salerName { get; set; }
        public string status { get; set; }
        public string encryptNo { get; set; }
        public string model { get; set; }
    }

    public class ResultModel
    {
        public string value { get; set; }
        public string text { get; set; }
    }

    public class CeoAuditListModel
    {
        public int? step { get; set; }
        public string stepName { get; set; }
        public int applyId { get; set; }
        public int applyDetailId { get; set; }
        public string sysNum { get; set; }
        public string depName { get; set; }
        public string salerName { get; set; }
        public DateTime? applyTime { get; set; }
        public string applyTimeStr { get; set; }
        public string orderType { get; set; }
        public string model { get; set; }
    }

    ////导出SO excel 报表
    //public class AuditorSoExcel {
    //    public string audit_status { get; set; }
    //    public string sys_no { get; set; }
    //    public string order_no { get; set; }
    //    public string contract_no { get; set; }
    //    public string trade_type { get; set; }
    //    public string order_type { get; set; }
    //    public string product_type { get; set; }
    //    public string product_use { get; set; }
    //    public string agency { get; set; }
    //    public string order_date { get; set; }
    //    public string customer { get; set; }
    //    public string oversea_customer { get; set; }
    //    public string final_customer { get; set; }
    //    public string plan_customer { get; set; }
    //    public string back_paper { get; set; }
    //    public string produce_way { get; set; }
    //    public string print_truly { get; set; }
    //    public string client_logo { get; set; }
    //    public string delivery_place { get; set; }
    //    public string department { get; set; }
    //    public string employee { get; set; }
    //    public string clear_way { get; set; }
    //    public string exchange_rate { get; set; }
    //    public string currency { get; set; }
    //    public string sale_way { get; set; }
    //    public string mat_number { get; set; }
    //    public string mat_name { get; set; }
    //    public string mat_model { get; set; }
    //    public string tax_rate { get; set; }
    //    public string qty { get; set; }
    //    public string deal_price { get; set; }
    //    public string deal_sum { get; set; }
    //    public string cost { get; set; }
    //    public string discount_rate { get; set; }
    //    public string delivery_date { get; set; }
    //    public string quote_no { get; set; }
    //    public string comment { get; set; }
    //    public string description { get; set; }
    //    public string further_info { get; set; }
    //}

}