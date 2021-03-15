using org.in2bits.MyXls;
using Sale_Order.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Services
{
    /// <summary>
    /// CM和CC的包装类，只要用于查询和导出excel时同时读取这2类的数据
    /// </summary>
    public class CXSv:BillSv
    {
        public override string BillType
        {
            get { return "CX"; }
        }

        public override string BillTypeName
        {
            get { return "开模改模单"; }
        }

        public override string CreateViewName
        {
            get { throw new NotImplementedException(); }
        }

        public override string CheckViewName
        {
            get { throw new NotImplementedException(); }
        }

        public override string CheckListViewName
        {
            get { return "CheckNBillList"; }
        }

        public override object GetNewBill(Models.UserInfo currentUser)
        {
            throw new NotImplementedException();
        }

        public override object GetBill(int stepVersion, int userId)
        {
            throw new NotImplementedException();
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, Models.UserInfo user)
        {
            throw new NotImplementedException();
        }

        public override List<object> GetBillList(Models.SalerSearchParamModel pm, int userId)
        {
            var list = new CMSv().GetBillList(pm, userId);
            list.AddRange(new CCSv().GetBillList(pm, userId));

            return list;
        }

        public override object GetNewBillFromOld()
        {
            throw new NotImplementedException();
        }

        public override string GetProcessNo()
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            throw new NotImplementedException();
        }

        public override string GetProductModel()
        {
            throw new NotImplementedException();
        }

        public override string GetCustomerName()
        {
            throw new NotImplementedException();
        }

        public override bool HasOrderSaved(string sysNo)
        {
            throw new NotImplementedException();
        }

        public override string GetSpecificBillTypeName()
        {
            throw new NotImplementedException();
        }

        public override void DoWhenBeforeApply()
        {
            throw new NotImplementedException();
        }

        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            throw new NotImplementedException();
        }

        public override void DoWhenFinishAudit(bool isPass)
        {
            throw new NotImplementedException();
        }

        public override void ExportSalerExcle(Models.SalerSearchParamModel pm, int userId)
        {
            var cms = new CMSv().GetCMSalerExcelData(pm, userId);
            var ccs = new CCSv().GetCCSalerExcelData(pm, userId);

            //列宽：
            ushort[] colWidth = new ushort[] {12, 16,12,16,16,20,18,16,16,16,
                                             14,32,32,32,20,28,14,14,12,
                                             14,14,20,20,20,16,16,16,32};

            //列名：
            string[] colName = new string[] {"状态", "流水号","模单类型","订单编号","下单日期","要求样品完成日期","产品类别","办事处","对应事业部","项目组",
                                            "营业工程师","购货单位","终极客户","方案公司","产品名称","型号规格","样品数量","样品单价","币别",
                                            "收费","免费","产品行业分类","结算方式","交货地点","贸易类型","计入事业部","特殊开模单","备注"};

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("开改模单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("订单信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(colWidth[i] * 256);
                sheet.AddColumnInfo(col);
            }

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 1;

            //设置标题
            foreach (var name in colName) {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var c in ccs) {
                var d = c.h;
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, c.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.sys_no);
                cells.Add(rowIndex, ++colIndex, d.model_type);
                cells.Add(rowIndex, ++colIndex, d.old_bill_no);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.bill_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.fetch_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.product_type);
                cells.Add(rowIndex, ++colIndex, d.agency_name);
                cells.Add(rowIndex, ++colIndex, "CCM");
                cells.Add(rowIndex, ++colIndex, d.project_team);

                cells.Add(rowIndex, ++colIndex, d.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.zz_customer_name);
                cells.Add(rowIndex, ++colIndex, d.plan_firm_name);
                cells.Add(rowIndex, ++colIndex, d.product_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.qty);
                cells.Add(rowIndex, ++colIndex, d.price);
                cells.Add(rowIndex, ++colIndex, d.currency_name);

                cells.Add(rowIndex, ++colIndex, d.charge);
                cells.Add(rowIndex, ++colIndex, d.free);
                cells.Add(rowIndex, ++colIndex, "");
                cells.Add(rowIndex, ++colIndex, d.clear_way);
                cells.Add(rowIndex, ++colIndex, d.fetch_add_name);
                cells.Add(rowIndex, ++colIndex, d.trade_type_name);
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.count_in_bus_dep) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.special_model) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, d.comment);
            }
            foreach (var c in cms) {
                var d = c.h;
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, c.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.sys_no);
                cells.Add(rowIndex, ++colIndex, d.model_type);
                cells.Add(rowIndex, ++colIndex, d.old_bill_no);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.bill_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.fetch_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.product_type);
                cells.Add(rowIndex, ++colIndex, d.agency_name);
                cells.Add(rowIndex, ++colIndex, d.bus_dep);
                cells.Add(rowIndex, ++colIndex, d.project_team);

                cells.Add(rowIndex, ++colIndex, d.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.zz_customer_name);
                cells.Add(rowIndex, ++colIndex, d.plan_firm_name);
                cells.Add(rowIndex, ++colIndex, d.product_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.qty);
                cells.Add(rowIndex, ++colIndex, d.price);
                cells.Add(rowIndex, ++colIndex, d.currency_name);

                cells.Add(rowIndex, ++colIndex, d.charge);
                cells.Add(rowIndex, ++colIndex, d.free);
                cells.Add(rowIndex, ++colIndex, d.product_classification);
                cells.Add(rowIndex, ++colIndex, d.clear_way);
                cells.Add(rowIndex, ++colIndex, d.fetch_add_name);
                cells.Add(rowIndex, ++colIndex, d.trade_type_name);
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.count_in_bus_dep) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.special_model) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, d.comment);
            }

            xls.Send();            
        }

        public override void ExportAuditorExcle(Models.AuditSearchParamModel pm, int userId)
        {
            var cms = new CMSv().GetCMAuditorExcelData(pm, userId);
            var ccs = new CCSv().GetCCAuditorExcelData(pm, userId);

            //列宽：
            ushort[] colWidth = new ushort[] {12, 16,12,16,16,20,18,16,16,16,
                                             14,32,32,32,20,28,14,14,12,
                                             14,14,20,20,20,16,16,16,32};

            //列名：
            string[] colName = new string[] {"状态", "流水号","模单类型","订单编号","下单日期","要求样品完成日期","产品类别","办事处","对应事业部","项目组",
                                            "营业工程师","购货单位","终极客户","方案公司","产品名称","型号规格","样品数量","样品单价","币别",
                                            "收费","免费","产品行业分类","结算方式","交货地点","贸易类型","计入事业部","特殊开模单","备注"};

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("开改模单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("订单信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(colWidth[i] * 256);
                sheet.AddColumnInfo(col);
            }

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 1;

            //设置标题
            foreach (var name in colName) {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var c in cms) {
                var d = c.h;
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, c.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.sys_no);
                cells.Add(rowIndex, ++colIndex, d.model_type);
                cells.Add(rowIndex, ++colIndex, d.old_bill_no);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.bill_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.fetch_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.product_type);
                cells.Add(rowIndex, ++colIndex, d.agency_name);
                cells.Add(rowIndex, ++colIndex, d.bus_dep);
                cells.Add(rowIndex, ++colIndex, d.project_team);

                cells.Add(rowIndex, ++colIndex, d.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.zz_customer_name);
                cells.Add(rowIndex, ++colIndex, d.plan_firm_name);
                cells.Add(rowIndex, ++colIndex, d.product_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.qty);
                cells.Add(rowIndex, ++colIndex, d.price);
                cells.Add(rowIndex, ++colIndex, d.currency_name);

                cells.Add(rowIndex, ++colIndex, d.charge);
                cells.Add(rowIndex, ++colIndex, d.free);
                cells.Add(rowIndex, ++colIndex, d.product_classification);
                cells.Add(rowIndex, ++colIndex, d.clear_way);
                cells.Add(rowIndex, ++colIndex, d.fetch_add_name);
                cells.Add(rowIndex, ++colIndex, d.trade_type_name);
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.count_in_bus_dep) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.special_model) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, d.comment);
            }

            foreach (var c in ccs) {
                var d = c.h;
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, c.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.sys_no);
                cells.Add(rowIndex, ++colIndex, d.model_type);
                cells.Add(rowIndex, ++colIndex, d.old_bill_no);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.bill_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.fetch_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.product_type);
                cells.Add(rowIndex, ++colIndex, d.agency_name);
                cells.Add(rowIndex, ++colIndex, "CCM");
                cells.Add(rowIndex, ++colIndex, d.project_team);

                cells.Add(rowIndex, ++colIndex, d.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.zz_customer_name);
                cells.Add(rowIndex, ++colIndex, d.plan_firm_name);
                cells.Add(rowIndex, ++colIndex, d.product_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.qty);
                cells.Add(rowIndex, ++colIndex, d.price);
                cells.Add(rowIndex, ++colIndex, d.currency_name);

                cells.Add(rowIndex, ++colIndex, d.charge);
                cells.Add(rowIndex, ++colIndex, d.free);
                cells.Add(rowIndex, ++colIndex, "");
                cells.Add(rowIndex, ++colIndex, d.clear_way);
                cells.Add(rowIndex, ++colIndex, d.fetch_add_name);
                cells.Add(rowIndex, ++colIndex, d.trade_type_name);
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.count_in_bus_dep) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.special_model) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, d.comment);
            }

            xls.Send();
        }

        public override void BeforeRollBack(int step)
        {
            throw new NotImplementedException();
        }

        public override System.IO.Stream PrintReport(string fileFolder)
        {
            throw new NotImplementedException();
        }

        public override string GetOrderNumber()
        {
            throw new NotImplementedException();
        }
    }
}