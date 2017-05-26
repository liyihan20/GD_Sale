using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    //流程时间监控的Model
    public class ProcTimeModel
    {
        public int applyId { get; set; }
        public string orderType { get; set; }
        public string ProduceDep { get; set; }
        public string sysNo { get; set; }
        public string agency { get; set; }
        public string applier { get; set; }
        public string applyDate { get; set; }
        public string applyTime { get; set; }
        public List<AuditTimeModel> auditList { get; set; }
    }
    public class BomProductModel
    {
        public string levels { get; set; } //层级
        public string fnumber { get; set; }
        public string fmodel { get; set; }
        public string fname { get; set; }
        public decimal fqty { get; set; } //单位用量
        public string unitname { get; set; } //单位
        public string perName { get; set; } //外购或自制
        public string code_s { get; set; } //主料或替料    
        public decimal total_qty { get; set; }
        public string source
        {
            get { return "BOM"; }
            set { value = "BOM"; }
        }
    }
    public class AuditTimeModel {
        public int step { get; set; }
        public string stepName { get; set; }
        public string auditor { get; set; }
        public string auditTime { get; set; }
        public string timeCost { get; set; }
        public bool? pass { get; set; }
        public bool? blocking { get; set; }
        public bool timeCostTooLong { get; set; }
    }
}