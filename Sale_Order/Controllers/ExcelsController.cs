using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using org.in2bits.MyXls;
using Sale_Order.Models;
using Sale_Order.Utils;
using Sale_Order.Filter;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace Sale_Order.Controllers
{
    public class ExcelsController : Controller
    {
        SaleDBDataContext db = new SaleDBDataContext();
        SomeUtils utl = new SomeUtils();
        // GET: /Excels/
        //营业员导出数据
        [SessionTimeOutFilter()]
        public void exportExcel(FormCollection fcl)
        {
            string sysNo = fcl.Get("sys_no");
            string fromDateStr = fcl.Get("fromDate");
            string toDatestr = fcl.Get("toDate");
            string billType = fcl.Get("billType");
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            DateTime fromDate = DateTime.Parse("1990-1-1");
            DateTime toDate = DateTime.Parse("2099-9-9");
            if (!string.IsNullOrWhiteSpace(fromDateStr))
                fromDate = DateTime.Parse(fromDateStr);
            if (!string.IsNullOrWhiteSpace(toDatestr))
                toDate = DateTime.Parse(toDatestr);

            switch (billType)
            {
                case "SO":
                case "1":
                    utl.writeEventLog("导出Excel", "SO:" + fromDateStr + "~" + toDatestr, sysNo, Request);
                    exportSOExcel(sysNo, fromDate, toDate, userId);
                    break;
                case "3":
                    //包含CCM开改模和其它开改模
                    utl.writeEventLog("导出Excel", "CM:" + fromDateStr + "~" + toDatestr, sysNo, Request);
                    exportCMExcel(sysNo, fromDate, toDate, userId);
                    break;
                case "4":
                case "SB":
                    utl.writeEventLog("导出Excel", "MB:" + fromDateStr + "~" + toDatestr, sysNo, Request);
                    exportSBExcel(sysNo, fromDate, toDate, userId);
                    break;
                case "5":
                case "BL":
                    utl.writeEventLog("导出Excel", "BL:" + fromDateStr + "~" + toDatestr, sysNo, Request);
                    exportBLExcel(sysNo, fromDate, toDate, userId);
                    break;
                case "7":
                case "HC":
                    utl.writeEventLog("导出Excel", "HC:" + fromDateStr + "~" + toDatestr, sysNo, Request);
                    exportHCExcel(sysNo, fromDate, toDate, userId);
                    break;
            }

        }

        public void exportSOExcel(string sysNo, DateTime fromDate, DateTime toDate, int userId)
        {
            var myData = (from o in db.vwExcelSaleOrder
                          where
                           o.user_id == userId &&
                          (o.sys_no.Contains(sysNo) || o.product_model.Contains(sysNo))
                          && o.order_date >= fromDate && o.order_date <= toDate
                          orderby o.order_date
                          select o).ToList();

            ushort[] colWidth = new ushort[] {16,16,14,18,28,28,28,28,20,28,
                                            14,14,14,14,14,14,14,14,14,10,
                                            12,20,14,14,14,14,14,14,60,60};

            string[] colName = new string[] { "流水号","订单号","下单日期","办事处","购货单位","海外客户","终极客户","方案公司","产品名称","型号规格",
                                            "数量","成交价","成交金额","不含税单价","合同价","合同金额","成本RMB","MU","佣金RMB","币别",
                                            "汇率","结算方式","营业员1","组别1","比例1","营业员2","组别2","比例2","说明","摘要"};

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("销售订单_{0}.xls", DateTime.Now.ToShortDateString());
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
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, d.sys_no);
                cells.Add(rowIndex, ++colIndex, d.order_no);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.order_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.department_name);
                cells.Add(rowIndex, ++colIndex, d.buy_unit_name);
                cells.Add(rowIndex, ++colIndex, d.oversea_client_name);
                cells.Add(rowIndex, ++colIndex, d.final_client_name);
                cells.Add(rowIndex, ++colIndex, d.plan_firm_name);
                cells.Add(rowIndex, ++colIndex, d.product_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);

                cells.Add(rowIndex, ++colIndex, d.qty);
                cells.Add(rowIndex, ++colIndex, d.deal_price);
                cells.Add(rowIndex, ++colIndex, d.deal_sum);
                cells.Add(rowIndex, ++colIndex, d.unit_price);
                cells.Add(rowIndex, ++colIndex, d.aux_tax_price);
                cells.Add(rowIndex, ++colIndex, d.aux_sum);
                cells.Add(rowIndex, ++colIndex, d.cost);
                cells.Add(rowIndex, ++colIndex, d.MU);
                cells.Add(rowIndex, ++colIndex, d.commission);
                cells.Add(rowIndex, ++colIndex, d.currency_name);

                cells.Add(rowIndex, ++colIndex, d.exchange_rate);
                cells.Add(rowIndex, ++colIndex, d.clearing_way_name);
                cells.Add(rowIndex, ++colIndex, d.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.group1);
                cells.Add(rowIndex, ++colIndex, d.percent1);
                cells.Add(rowIndex, ++colIndex, d.clerk2_name);
                cells.Add(rowIndex, ++colIndex, d.group2);
                cells.Add(rowIndex, ++colIndex, d.percent2);
                cells.Add(rowIndex, ++colIndex, d.description);
                cells.Add(rowIndex, ++colIndex, d.summary);
            }

            xls.Send();
        }

        public void exportSBExcel(string sysNo, DateTime fromDate, DateTime toDate, int userId)
        {
            var myData = (from sb in db.SampleBill
                          where (sb.sys_no.Contains(sysNo) || sb.product_model.Contains(sysNo))
                          && sb.bill_date >= fromDate
                          && sb.bill_date <= toDate
                          && sb.original_user_id == userId
                          select sb).ToList();

            //列宽：
            ushort[] colWidth = new ushort[] { 12, 12, 16, 24, 30, 12, 12, 12, 12, 16, 16 };

            //列名：
            string[] colName = new string[] { "样品单种类", "下单日期", "订单编号", "客户名称", "型号", "数量", "成交价", "合同价", "成本", "办事处", "营业员" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("样品单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("单据信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, d.is_free);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.bill_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.bill_no);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.sample_qty);
                cells.Add(rowIndex, ++colIndex, d.deal_price);
                cells.Add(rowIndex, ++colIndex, d.contract_price);
                cells.Add(rowIndex, ++colIndex, d.cost);
                cells.Add(rowIndex, ++colIndex, d.agency_name);
                cells.Add(rowIndex, ++colIndex, d.clerk_name);
            }

            xls.Send();
        }

        public void exportCMExcel(string sysNo, DateTime fromDate, DateTime toDate, int userId)
        {
            var myData = (from mc in db.CcmModelContract
                          where (mc.sys_no.Contains(sysNo) || mc.product_model.Contains(sysNo))
                          && mc.bill_date >= fromDate
                          && mc.bill_date <= toDate
                          && mc.user_id == userId
                          select mc).ToList();
            var myData2 = (from mc in db.ModelContract
                          where (mc.sys_no.Contains(sysNo) || mc.product_model.Contains(sysNo))
                          && mc.bill_date >= fromDate
                          && mc.bill_date <= toDate
                          && mc.original_user_id == userId
                          select mc).ToList();

            //列宽：
            ushort[] colWidth = new ushort[] {16,12,16,16,20,18,16,16,16,
                                             14,32,32,32,20,28,14,14,12,
                                             14,14,20,20,20,16,16,16,32};

            //列名：
            string[] colName = new string[] { "流水号","模单类型","订单编号","下单日期","要求样品完成日期","产品类别","办事处","对应事业部","项目组",
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
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, d.sys_no);
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
            foreach (var d in myData2)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, d.sys_no);
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

        public void exportBLExcel(string sysNo, DateTime fromDate, DateTime toDate, int userId)
        {
            var myData = (from sb in db.Sale_BL
                          join detail in db.Sale_BL_details on sb.id equals detail.bl_id into dtemp
                          from bd in dtemp.DefaultIfEmpty()
                          where (sb.sys_no.Contains(sysNo) || sb.product_model.Contains(sysNo))
                          && sb.bl_date >= fromDate
                          && sb.bl_date <= toDate
                          && sb.original_user_id == userId
                          select new
                          {
                              bl = sb,
                              bd = bd
                          }).ToList();

            //列宽：
            ushort[] colWidth = new ushort[] { 12, 16, 32, 12, 24, 18, 16, 32, 32,
                18,16,24,24,16,64,32,24,12,16,16,16,24,12 };

            //列名：
            string[] colName = new string[] { "备料日期", "备料编号", "产品型号", "数量", "客户名称", "计划下订单日期", "营业员","TFT型号","TP型号",
            "成交价（不含税）","事业部","产品用途","对应项目组","办事处","摘要","物料型号","物料名称","单位","单位用量","数量","最高采购价","备注","来源"};

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("备料单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("单据信息列表");

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

            foreach (var d in myData) {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, ((DateTime)d.bl.bl_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.bl.bill_no);
                cells.Add(rowIndex, ++colIndex, d.bl.product_model);
                cells.Add(rowIndex, ++colIndex, d.bl.qty);
                cells.Add(rowIndex, ++colIndex, d.bl.customer_name);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.bl.plan_order_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.bl.User.real_name);
                cells.Add(rowIndex, ++colIndex, d.bl.TFT_model);
                cells.Add(rowIndex, ++colIndex, d.bl.TP_model);

                cells.Add(rowIndex, ++colIndex, d.bl.deal_price);
                cells.Add(rowIndex, ++colIndex, d.bl.bus_dep);
                cells.Add(rowIndex, ++colIndex, d.bl.product_use);
                cells.Add(rowIndex, ++colIndex, d.bl.project_group);
                cells.Add(rowIndex, ++colIndex, d.bl.User.Department1.name);
                //cells.Add(rowIndex, ++colIndex, d.bl.bl_project);
                cells.Add(rowIndex, ++colIndex, d.bl.comment);

                if (d.bd != null) {
                    cells.Add(rowIndex, ++colIndex, d.bd.fmodel);
                    cells.Add(rowIndex, ++colIndex, d.bd.fname);
                    cells.Add(rowIndex, ++colIndex, d.bd.unitname);
                    cells.Add(rowIndex, ++colIndex, d.bd.fqty);
                    cells.Add(rowIndex, ++colIndex, d.bd.total_qty);
                    cells.Add(rowIndex, ++colIndex, d.bd.highest_price);
                    cells.Add(rowIndex, ++colIndex, d.bd.comment);
                    cells.Add(rowIndex, ++colIndex, d.bd.source);
                }
            }

            xls.Send();
        }

        public void exportHCExcel(string sysNo, DateTime fromDate, DateTime toDate, int userId)
        {
            var myData = (from hc in db.Sale_HC
                          where (hc.sys_no.Contains(sysNo) || hc.item_model.Contains(sysNo))
                          && hc.bill_date >= fromDate
                          && hc.bill_date <= toDate
                          && hc.user_id == userId
                          select hc).ToList();

            //列宽：
            ushort[] colWidth = new ushort[] { 12, 12, 16, 24, 24, 36, 24, 24, 12, 12 };

            //列名：
            string[] colName = new string[] { "日期", "流水号", "事业部", "信利物料编码", "信利物料名称", "信利物料型号", "华为物料编码", "华为PO单号", "出货数量", "制单人" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("华为出货报告处理单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("单据信息列表");

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
            //"日期", "流水号", "事业部", "信利物料编码", "信利物料名称", "信利物料型号", "华为物料编码", "华为PO单号", "出货数量", "制单人"
            foreach (var d in myData) {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, ((DateTime)d.bill_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.sys_no);
                cells.Add(rowIndex, ++colIndex, d.dep_name);
                cells.Add(rowIndex, ++colIndex, d.item_no);
                cells.Add(rowIndex, ++colIndex, d.item_name);
                cells.Add(rowIndex, ++colIndex, d.item_model);
                cells.Add(rowIndex, ++colIndex, d.hw_item_no);
                cells.Add(rowIndex, ++colIndex, d.hw_po_no);
                cells.Add(rowIndex, ++colIndex, d.item_qty);
                cells.Add(rowIndex, ++colIndex, d.user_name);
            }

            xls.Send();
        }

        //总裁办导出数据
        [SessionTimeOutFilter()]
        public ActionResult exportCeoExcel(string beginDate, string toDate)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            if (!utl.hasGotPower(userId, Powers.ceo_excel.ToString()))
            {
                utl.writeEventLog("导出总裁办Excel", "没有权限", "", Request, 1000);
                ViewBag.tip = "没有权限";
                return View("tip");
            }
            DateTime bDate = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
            DateTime tDate = bDate;
            if (!string.IsNullOrEmpty(beginDate))
            {
                bDate = DateTime.Parse(beginDate);
            }
            if (!string.IsNullOrEmpty(toDate))
            {
                tDate = DateTime.Parse(toDate);
            }
            if (bDate > tDate)
            {
                utl.writeEventLog("导出总裁办Excel", "日期期间不正确", "", Request, 10);
                ViewBag.tip = "日期期间不正确";
                return View("tip");
            }
            utl.writeEventLog("导出总裁办Excel", bDate.ToShortDateString() + "~" + tDate.ToShortDateString(), "", Request);
            BeginExportCeoExcel(bDate, tDate);
            return null;
        }

        public void BeginExportCeoExcel(DateTime fromDate, DateTime toDate)
        {
            var myData = (from v in db.VWCeoExcel
                          where v.order_date >= fromDate
                          && v.order_date <= toDate
                          orderby v.order_date
                          select v).ToList();

            ushort[] colWidth = new ushort[] { 12, 12, 20, 18, 18, 18, 28, 18, 14, 100 };

            //下单日期，交货日期，办事处，产品类别，订单号，规格型号，成交金额，币别，说明
            string[] colName = new string[] { "下单日期", "交货日期", "办事处", "产品类别", "产品用途", "订单号", "规格型号", "成交金额", "币别", "说明" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("总裁办报表_{0}.xls", DateTime.Now.ToShortDateString());
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
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in myData)
            {
                colIndex = 1;

                //下单日期，交货日期，办事处，产品类别，订单号，规格型号，成交金额，币别，说明
                cells.Add(++rowIndex, colIndex, ((DateTime)d.order_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.delivery_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.department_name);                
                cells.Add(rowIndex, ++colIndex, d.product_type_name);
                cells.Add(rowIndex, ++colIndex, d.product_use);
                cells.Add(rowIndex, ++colIndex, d.order_no);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.deal_sum);
                cells.Add(rowIndex, ++colIndex, d.currency_name);
                cells.Add(rowIndex, ++colIndex, d.description);
            }

            xls.Send();
        }

        //办事处样品单Excel
        public ActionResult exportAgencyExcel(string beginDate, string toDate) {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            if (!utl.hasGotPower(userId, Powers.agency_sb_excel.ToString()))
            {
                utl.writeEventLog("导出样品单办事处Excel", "没有权限", "", Request, 1000);
                ViewBag.tip = "没有权限";
                return View("tip");
            }
            DateTime bDate = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
            DateTime tDate = bDate;
            if (!string.IsNullOrEmpty(beginDate))
            {
                bDate = DateTime.Parse(beginDate);
            }
            if (!string.IsNullOrEmpty(toDate))
            {
                tDate = DateTime.Parse(toDate);
            }
            if (bDate > tDate)
            {
                utl.writeEventLog("导出样品单办事处Excel", "日期期间不正确", "", Request, 10);
                ViewBag.tip = "日期期间不正确";
                return View("tip");
            }
            utl.writeEventLog("导出样品单办事处Excel", bDate.ToShortDateString() + "~" + tDate.ToShortDateString(), "", Request);
            BeginExportAgencyExcel(bDate, tDate);
            return null;
        }

        public void BeginExportAgencyExcel(DateTime fromDate, DateTime toDate) {            
            var myData = (from s in db.SampleBill
                          from a in db.Apply
                          where a.sys_no == s.sys_no
                          && a.success == true
                          && s.bill_date >= fromDate
                          && s.bill_date <= toDate
                          select new
                          {
                              s.is_free,
                              s.bill_date,
                              s.bill_no,
                              s.customer_name,
                              s.product_model,
                              s.sample_qty,
                              s.deal_price,
                              s.contract_price,
                              s.cost,
                              s.agency_name,
                              s.clerk_name
                          }).ToList();
            //列宽：
            ushort[] colWidth = new ushort[] { 12, 12, 16, 24, 30, 12, 12, 12, 12, 16, 16 };

            //列名：
            string[] colName = new string[] { "样品单种类", "下单日期", "订单编号", "客户名称", "型号", "数量", "成交价", "合同价", "成本", "办事处", "营业员" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("样品单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("单据信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, d.is_free);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.bill_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.bill_no);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.sample_qty);
                cells.Add(rowIndex, ++colIndex, d.deal_price);
                cells.Add(rowIndex, ++colIndex, d.contract_price);
                cells.Add(rowIndex, ++colIndex, d.cost);
                cells.Add(rowIndex, ++colIndex, d.agency_name);
                cells.Add(rowIndex, ++colIndex, d.clerk_name);
            }

            xls.Send();
        }

        //审核人导出的excel,templateID=2是默认模板
        [SessionTimeOutFilter()]
        public void exportAuditorSOExcel(string ids, int templateId = 2)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            //将id列的最后一个逗号去掉
            if (ids.LastIndexOf(',') == ids.Length - 1)
            {
                ids = ids.Substring(0, ids.Length - 1);
            }
            bool canSeePrice = true;
            if (utl.hasGotPower(userId, Powers.not_all_price.ToString()))
            {
                utl.writeEventLog("导出无价格报表Excel", "成功导出记录数:" + ids.Split(',').Count(), "模板ID:" + templateId, Request, 0);
                canSeePrice = false;
            }
            else
            {
                utl.writeEventLog("导出有价格报表Excel", "成功导出记录数:" + ids.Split(',').Count(), "模板ID:" + templateId, Request, 0);
            }

            BeginExportAuditorSOExcel(ids, canSeePrice, templateId);
        }

        public void BeginExportAuditorSOExcel(string ids, bool canSeePrice)
        {

            var datas = db.getAuditorSOExcels(ids, canSeePrice);
            ushort[] colWidth = new ushort[] {12,14,16,16,16,16,16,16,18,18,
                                            18,16,28,28,28,28,18,18,18,18,
                                            18,20,28,14,14,14,14,14,14,14,
                                            12,14,16,40,60,60};

            string[] colName = new string[] { "审核状态","下单日期","流水号","订单号","贸易类型","订单类型","产品类型","产品用途","结算方式","办事处",
                                            "对应项目组","业务员","购货单位","海外客户","终极客户","方案公司","回头纸是否确认","生产方式(冒险做货)", "外包装印TRULY", "是否印有客户LOGO",
                                            "交货地点","产品名称","型号规格","数量","成交价","成交金额","成本RMB","税率","折扣率(%)","币别",
                                            "汇率","交货日期", "报价编号","摘要","说明","补充说明"};

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("销售订单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("订单信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //内容居中宋体
            XF dataXF = xls.NewXF();
            dataXF.Font.FontName = "宋体";
            dataXF.HorizontalAlignment = HorizontalAlignments.Centered;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in datas)
            {
                colIndex = 1;
                //"审核状态","下单日期","流水号","订单号","贸易类型","订单类型","产品类型","产品用途","结算方式","办事处",                
                cells.Add(++rowIndex, colIndex, d.audit_status, dataXF);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.order_date).ToShortDateString(), dataXF);
                cells.Add(rowIndex, ++colIndex, d.sys_no, dataXF);
                cells.Add(rowIndex, ++colIndex, d.order_no, dataXF);
                cells.Add(rowIndex, ++colIndex, d.trade_type, dataXF);
                cells.Add(rowIndex, ++colIndex, d.order_type, dataXF);
                cells.Add(rowIndex, ++colIndex, d.product_type, dataXF);
                cells.Add(rowIndex, ++colIndex, d.product_use, dataXF);
                cells.Add(rowIndex, ++colIndex, d.clear_way, dataXF);
                cells.Add(rowIndex, ++colIndex, d.department, dataXF);

                //"对应项目组","业务员","购货单位","海外客户","终极客户","方案公司","回头纸是否确认","生产方式(冒险做货)", "外包装印TRULY", "是否印有客户LOGO",                
                cells.Add(rowIndex, ++colIndex, d.project_team, dataXF);
                cells.Add(rowIndex, ++colIndex, d.employee, dataXF);
                cells.Add(rowIndex, ++colIndex, d.customer, dataXF);
                cells.Add(rowIndex, ++colIndex, d.oversea_customer, dataXF);
                cells.Add(rowIndex, ++colIndex, d.final_customer, dataXF);
                cells.Add(rowIndex, ++colIndex, d.plan_customer, dataXF);
                cells.Add(rowIndex, ++colIndex, d.back_paper, dataXF);
                cells.Add(rowIndex, ++colIndex, d.produce_way, dataXF);
                cells.Add(rowIndex, ++colIndex, d.print_truly, dataXF);
                cells.Add(rowIndex, ++colIndex, d.client_logo, dataXF);

                //交货地点","产品名称","型号规格","数量","成交价","成交金额","成本RMB","税率","折扣率(%)","币别",                
                cells.Add(rowIndex, ++colIndex, d.delivery_place, dataXF);
                cells.Add(rowIndex, ++colIndex, d.mat_name, dataXF);
                cells.Add(rowIndex, ++colIndex, d.mat_model, dataXF);
                cells.Add(rowIndex, ++colIndex, d.qty, dataXF);
                cells.Add(rowIndex, ++colIndex, d.deal_price, dataXF);
                cells.Add(rowIndex, ++colIndex, d.deal_sum, dataXF);
                cells.Add(rowIndex, ++colIndex, d.cost, dataXF);
                cells.Add(rowIndex, ++colIndex, d.tax_rate, dataXF);
                cells.Add(rowIndex, ++colIndex, d.discount_rate, dataXF);
                cells.Add(rowIndex, ++colIndex, d.currency, dataXF);

                // "汇率","交货日期", "报价编号","摘要","说明","补充说明"
                cells.Add(rowIndex, ++colIndex, d.exchange_rate, dataXF);
                cells.Add(rowIndex, ++colIndex, d.delivery_date, dataXF);
                cells.Add(rowIndex, ++colIndex, d.quote_no, dataXF);
                cells.Add(rowIndex, ++colIndex, d.comment);
                cells.Add(rowIndex, ++colIndex, d.description);
                cells.Add(rowIndex, ++colIndex, d.further_info);
            }

            xls.Send();
        }

        public void BeginExportAuditorSOExcel(string ids, bool canSeePrice, int tempalteId)
        {
            //根据用户模板获取需要导出的字段
            string segments = db.UserExcelTemplate.Single(u => u.id == tempalteId).seg_info;
            string[] segmentsArr = segments.Split(',');

            //不能看价格的字段
            string[] priceSegs = new string[] { "deal_price", "deal_sum", "cost", };

            //缓存各个字段的中文、英文名和列宽
            var segInfoList = db.ExcelSegments.Where(s => s.excel_type == "auditor_SO").ToList();

            SqlConnection conn = null;
            SqlDataAdapter adap = null;
            DataSet ds = null;
            string contentSql = @"select * from vw_auditor_excel where id in(" + ids + ") order by id";
            string connectionStr = ConfigurationManager.ConnectionStrings["SaleOrder_platformConnectionString"].ConnectionString;
            using (conn = new SqlConnection(connectionStr))
            {
                try
                {
                    conn.Open();
                    ds = new DataSet();
                    using (adap = new SqlDataAdapter(contentSql, conn))
                    {
                        adap.Fill(ds, "contentInfo");
                    }
                }
                catch (Exception ex)
                {
                    utl.writeEventLog("导出Excel", "出现异常：" + ex.Message, "", Request, 1000);
                    return;
                }
            }

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("销售订单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("订单信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 0;
            string enName = "";

            //设置列宽
            ColumnInfo col;
            foreach (var colName in segmentsArr)
            {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = (ushort)colIndex;
                col.ColumnIndexEnd = (ushort)colIndex;
                col.Width = (ushort)(segInfoList.Where(s => s.cn_name == colName).First().col_width * 256);
                sheet.AddColumnInfo(col);
                colIndex++;
            }

            colIndex = 1;

            //设置标题
            foreach (var colName in segmentsArr)
            {
                cells.Add(rowIndex, colIndex++, colName, boldXF);
            }
            
            //设置内容
            foreach (DataRow contentR in ds.Tables["contentInfo"].Rows)
            {
                rowIndex++;
                colIndex = 1;
                foreach (var colName in segmentsArr)
                {
                    enName = segInfoList.Where(s => s.cn_name == colName).First().en_name;
                    if (!canSeePrice && priceSegs.Contains(enName))
                    {
                        cells.Add(rowIndex, colIndex++, ""); //不能看价格
                    }
                    else
                    {
                        cells.Add(rowIndex, colIndex++, contentR[enName] == DBNull.Value ? "" : contentR[enName]);
                    }
                }
            }

            //释放资源            
            ds.Dispose();
            xls.Send();
        }

        public void exportAuditorSBExcel(string ids)
        {
            var myData = db.getAuditorSBExcels(ids);

            //列宽：
            ushort[] colWidth = new ushort[] { 12, 12, 16, 24, 30, 12, 12, 12, 12, 16, 16 };

            //列名：
            string[] colName = new string[] { "样品单种类", "下单日期", "订单编号", "客户名称", "型号", "数量", "成交价", "合同价", "成本", "办事处", "营业员" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("样品单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("单据信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, d.is_free);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.bill_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.bill_no);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.sample_qty);
                cells.Add(rowIndex, ++colIndex, d.deal_price);
                cells.Add(rowIndex, ++colIndex, d.contract_price);
                cells.Add(rowIndex, ++colIndex, d.cost);
                cells.Add(rowIndex, ++colIndex, d.agency_name);
                cells.Add(rowIndex, ++colIndex, d.clerk_name);
            }

            xls.Send();
        }

        public void exportAuditorCMExcel(string ids) {

            //string[] idStrArr = ids.Split(',').Distinct().ToArray();
            //int[] idArr = new int[idStrArr.Count()];
            //for (var i = 0; i < idStrArr.Count(); i++) {
            //    idArr[i] = int.Parse(idStrArr[i]);
            //}
            //var myData = from a in db.Apply
            //              from c in db.CcmModelContract
            //              where a.order_type == "CC"
            //              && idArr.Contains(a.id)
            //              && a.sys_no == c.sys_no
            //              select c;

            var myData = db.getAuditorCMExcels(ids);
            //列宽：
            ushort[] colWidth = new ushort[] {16,12,16,16,20,18,16,16,16,
                                             14,32,32,32,20,28,14,14,12,
                                             14,14,20,20,20,16,16,16,32};

            //列名：
            string[] colName = new string[] {"流水号","模单类型","订单编号","下单日期","要求样品完成日期","产品类别","办事处","对应事业部","项目组",
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
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, d.sys_no);
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
                cells.Add(rowIndex, ++colIndex, d.classification);
                cells.Add(rowIndex, ++colIndex, d.clear_way);
                cells.Add(rowIndex, ++colIndex, d.fetch_add_name);
                cells.Add(rowIndex, ++colIndex, d.trade_type_name);
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.count_in_bus_dep) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, !string.IsNullOrEmpty(d.special_model) ? "是" : "否");
                cells.Add(rowIndex, ++colIndex, d.comment);
            }
            xls.Send();
        }

        public void exportTHExcel(FormCollection fc)
        {

            string customerNumber = fc.Get("cust_no");
            string stockNo = fc.Get("stock_no");
            string model = fc.Get("pro_model");
            string fromDateStr = fc.Get("fromDate");
            string toDateStr = fc.Get("toDate");
            string auditResultStr = fc.Get("auditResult");

            //处理一下参数
            DateTime fromDate, toDate;
            int auditResult;
            if (!DateTime.TryParse(fromDateStr, out fromDate)) fromDate = DateTime.Parse("1980-1-1");
            if (!DateTime.TryParse(toDateStr, out toDate)) toDate = DateTime.Parse("2099-9-9");
            if (!Int32.TryParse(auditResultStr, out auditResult)) auditResult = 10;

            BeginExportTHExcel(customerNumber, fromDate, toDate, stockNo, model, auditResult);
            utl.writeEventLog("Excel导出", "导出退换货列表", "", Request);
        }

        public void BeginExportTHExcel(string customerInfo, DateTime fromDate, DateTime toDate, string stockNo, string model, int auditResult)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            var myData = (from v in db.VwReturnBill
                          where v.user_id == userId
                          && (v.customer_number.Contains(customerInfo) || v.customer_name.Contains(customerInfo))
                          && (v.stock_no.Contains(stockNo) || v.seorder_no.Contains(stockNo))
                          && (v.product_model.Contains(model) || v.sys_no.Contains(model))
                          && v.fdate >= fromDate
                          && v.fdate <= toDate
                          && (auditResult == 10 || v.audit_result == auditResult || (v.audit_result == null && auditResult == 0))
                          orderby v.fdate, v.sys_no, v.entry_no
                          select new
                          {
                              FDate = v.fdate,
                              FSysNo = v.sys_no,
                              FStockNo = v.stock_no,
                              FOrderBillNo = v.seorder_no,
                              FCustomerNo = v.customer_number,
                              FCustomerName = v.customer_name,
                              FProductNo = v.product_number,
                              FProductName = v.product_name,
                              FProductModel = v.product_model,
                              Fauxqty = v.aux_qty,
                              FReturnQty = v.return_qty,
                              FRealReturnQty = v.real_return_qty,
                              FOnlineStatus = v.is_online==true?"已上线":"未上线",
                              FReturnDep = v.return_dept,
                              FCHDepName=v.ch_dep_name,
                              //FHasRedQty=v.has_red_qty,
                              //FHasReturnQty=v.has_replace_qty,
                              FHasInvoice = v.has_invoice == true ? "已开" : "未开",
                              //FNeedResend = v.need_resend == true ? "换货" : "退红字",
                              apply_status = v.audit_result == null ? "未提交" : v.audit_result == 1 ? "申请成功" : v.audit_result == -1 ? "申请失败" : "审批中"
                          }).ToList();

            ushort[] colWidth = new ushort[] { 12, 16, 20, 20, 12, 28, 18, 24, 
                                               28, 14, 14, 14, 14, 14, 14, 14, 14 };

            string[] colName = new string[] { "下单日期", "退货编号", "出库单号", "订单单号", "客户代码", "客户名称", "产品编码", "产品名称",
                                              "规格型号", "已发数量", "退货数量","实退数量", "上线状态", "退货部门", "出货组", "蓝字发票", "审核状态" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("退换货申请_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("退换货申请信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            //设置内容
            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex++, ((DateTime)d.FDate).ToShortDateString());
                cells.Add(rowIndex, colIndex++, d.FSysNo);
                cells.Add(rowIndex, colIndex++, d.FStockNo);
                cells.Add(rowIndex, colIndex++, d.FOrderBillNo);
                cells.Add(rowIndex, colIndex++, d.FCustomerNo);
                cells.Add(rowIndex, colIndex++, d.FCustomerName);
                cells.Add(rowIndex, colIndex++, d.FProductNo);
                cells.Add(rowIndex, colIndex++, d.FProductName);

                cells.Add(rowIndex, colIndex++, d.FProductModel);
                cells.Add(rowIndex, colIndex++, d.Fauxqty);
                cells.Add(rowIndex, colIndex++, d.FReturnQty);
                cells.Add(rowIndex, colIndex++, d.FRealReturnQty);
                cells.Add(rowIndex, colIndex++, d.FOnlineStatus);
                cells.Add(rowIndex, colIndex++, d.FReturnDep);
                cells.Add(rowIndex, colIndex++, d.FCHDepName);
                //cells.Add(rowIndex, colIndex++, d.FHasReturnQty);
                //cells.Add(rowIndex, colIndex++, d.FHasRedQty);
                cells.Add(rowIndex, colIndex++, d.FHasInvoice);
                //cells.Add(rowIndex, colIndex++, d.FNeedResend);
                cells.Add(rowIndex, colIndex++, d.apply_status);
            }

            xls.Send();
        }

        //物控导出红字退货报表
        public void exportTHExcelByCon(FormCollection fc)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            string customerNumber = fc.Get("cust_no");
            string billNo = fc.Get("bill_no");
            string sysNo = fc.Get("sys_no");
            string fromDateStr = fc.Get("fromDate");
            string toDateStr = fc.Get("toDate");
            string procDep = fc.Get("proc_dep");

            //处理一下参数
            DateTime fromDate, toDate;
            if (!DateTime.TryParse(fromDateStr, out fromDate)) fromDate = DateTime.Parse("1980-1-1");
            if (!DateTime.TryParse(toDateStr, out toDate)) toDate = DateTime.Parse("2099-9-9");

            string[] depArr;
            if ("all".Equals(procDep)) {
                string userCanCheckDeps = db.User.Single(u => u.id == userId).can_check_deps;
                if (userCanCheckDeps.Equals("*")) {
                    depArr = db.Department.Where(d => d.dep_type == "退货事业部").Select(d => d.name).ToArray();
                }
                else {
                    depArr = userCanCheckDeps.Split(new Char[] { ',', '，' });
                }
            }
            else {
                depArr = procDep.Split(new Char[] { ',', '，' });
            }

            BeginExportTHExcelByCon(customerNumber, fromDate, toDate, billNo, sysNo, depArr);
            utl.writeEventLog("Excel导出", "物控导出退换货列表", "", Request);
        }

        public void BeginExportTHExcelByCon(string customerInfo, DateTime fromDate, DateTime toDate, string billNo, string sysNo, string[] depArr)
        {
            var myData = (from v in db.VwReturnBill
                          where (v.customer_number.Contains(customerInfo) || v.customer_name.Contains(customerInfo))
                          && (v.stock_no.Contains(billNo) || v.seorder_no.Contains(billNo))
                          && v.sys_no.Contains(sysNo)
                          && v.fdate >= fromDate
                          && v.fdate <= toDate
                          && v.audit_result == 1
                          && depArr.Contains(v.return_dept)
                          orderby v.fdate, v.sys_no, v.entry_no
                          select new
                          {
                              FUserName = v.user_name,
                              FDepartmentName = v.department_name,
                              FDate = v.fdate,
                              FSysNo = v.sys_no,
                              FStockNo = v.stock_no,
                              FOrderBillNo = v.seorder_no,
                              FCustomerNo = v.customer_number,
                              FCustomerName = v.customer_name,
                              FProductNo = v.product_number,
                              FProductName = v.product_name,
                              FProductModel = v.product_model,
                              FReturnDep = v.return_dept,
                              Fauxqty = v.aux_qty,
                              FReturnQty = v.return_qty,
                              FOnlineStatus = v.is_online == true ? "已上线" : "未上线",
                              FCHDepName = v.ch_dep_name,
                              FRealReturnQty = v.real_return_qty,
                              //FHasRedQty = v.has_red_qty,
                              //FHasReturnQty = v.has_replace_qty,
                              FHasInvoice = v.has_invoice == true ? "已开" : "未开",
                              //FNeedResend = v.need_resend == true ? "换货" : "退红字",
                              apply_status = v.audit_result == null ? "未提交" : v.audit_result == 1 ? "申请成功" : v.audit_result == -1 ? "申请失败" : "审批中"
                          }).ToList();

            ushort[] colWidth = new ushort[] { 12, 16,16,16, 20, 20, 12, 28, 18, 24, 
                                               28, 14, 14, 14, 14, 14, 14, 14, 14 };

            string[] colName = new string[] { "下单日期", "退货编号","营业员","办事处", "出库单号", "订单单号", "客户代码", "客户名称", "产品编码", "产品名称",
                                              "规格型号", "已发数量", "退货数量","实退数量", "上线状态","退货部门","出货组", "蓝字发票", "审核状态" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("退换货申请_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("退换货申请信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            //设置内容
            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex++, ((DateTime)d.FDate).ToShortDateString());
                cells.Add(rowIndex, colIndex++, d.FSysNo);
                cells.Add(rowIndex, colIndex++, d.FUserName);
                cells.Add(rowIndex, colIndex++, d.FDepartmentName);
                cells.Add(rowIndex, colIndex++, d.FStockNo);
                cells.Add(rowIndex, colIndex++, d.FOrderBillNo);
                cells.Add(rowIndex, colIndex++, d.FCustomerNo);
                cells.Add(rowIndex, colIndex++, d.FCustomerName);
                cells.Add(rowIndex, colIndex++, d.FProductNo);
                cells.Add(rowIndex, colIndex++, d.FProductName);

                cells.Add(rowIndex, colIndex++, d.FProductModel);
                cells.Add(rowIndex, colIndex++, d.Fauxqty);
                cells.Add(rowIndex, colIndex++, d.FReturnQty);
                cells.Add(rowIndex, colIndex++, d.FRealReturnQty);
                cells.Add(rowIndex, colIndex++, d.FOnlineStatus);
                cells.Add(rowIndex, colIndex++, d.FReturnDep);
                cells.Add(rowIndex, colIndex++, d.FCHDepName);
                //cells.Add(rowIndex, colIndex++, d.FHasReturnQty);
                //cells.Add(rowIndex, colIndex++, d.FHasRedQty);
                cells.Add(rowIndex, colIndex++, d.FHasInvoice);
                //cells.Add(rowIndex, colIndex++, d.FNeedResend);
                cells.Add(rowIndex, colIndex++, d.apply_status);
            }

            xls.Send();
        }

        public void exportTHExcelByAuditor(string ids)
        {                        
            //将id列的最后一个逗号去掉
            if (ids.LastIndexOf(',') == ids.Length - 1)
            {
                ids = ids.Substring(0, ids.Length - 1);
            }

            BeginExportTHExcelByAuditor(ids);
            utl.writeEventLog("Excel导出", "导出退换货列表:"+ids, "", Request);
        }

        public void BeginExportTHExcelByAuditor(string ids)
        {
            var myData = (from v in db.getAuditorTHExcels(ids)
                          select new
                          {
                              FDate = v.fdate,
                              FSysNo = v.sys_no,
                              FStockNo = v.stock_no,
                              FOrderBillNo = v.seorder_no,
                              FCustomerNo = v.customer_number,
                              FCustomerName = v.customer_name,
                              FProductNo = v.product_number,
                              FProductName = v.product_name,
                              FProductModel = v.product_model,
                              Fauxqty = v.aux_qty,
                              FReturnQty = v.return_qty,
                              FRealReturnQty = v.real_return_qty,
                              FOnlineStatus = v.is_online == true ? "已上线" : "未上线",
                              FReturnDep = v.return_dept,
                              FCHDepName = v.ch_dep_name,
                              //FHasRedQty=v.has_red_qty,
                              //FHasReturnQty=v.has_replace_qty,
                              FHasInvoice = v.has_invoice == true ? "已开" : "未开",
                              //FNeedResend = v.need_resend == true ? "换货" : "退红字",
                              apply_status = v.audit_result == null ? "未提交" : v.audit_result == 1 ? "申请成功" : v.audit_result == -1 ? "申请失败" : "审批中"
                          }).ToList();

            ushort[] colWidth = new ushort[] { 12, 16, 20, 20, 12, 28, 18, 24, 
                                               28, 14, 14, 14, 14, 14, 14, 14, 14 };

            string[] colName = new string[] { "下单日期", "退货编号", "出库单号", "订单单号", "客户代码", "客户名称", "产品编码", "产品名称",
                                              "规格型号", "已发数量", "退货数量","实退数量", "上线状态", "退货部门", "出货组", "蓝字发票", "审核状态" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("退换货申请_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("退换货申请信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            //设置内容
            foreach (var d in myData)
            {
                colIndex = 1;

                cells.Add(++rowIndex, colIndex++, ((DateTime)d.FDate).ToShortDateString());
                cells.Add(rowIndex, colIndex++, d.FSysNo);
                cells.Add(rowIndex, colIndex++, d.FStockNo);
                cells.Add(rowIndex, colIndex++, d.FOrderBillNo);
                cells.Add(rowIndex, colIndex++, d.FCustomerNo);
                cells.Add(rowIndex, colIndex++, d.FCustomerName);
                cells.Add(rowIndex, colIndex++, d.FProductNo);
                cells.Add(rowIndex, colIndex++, d.FProductName);

                cells.Add(rowIndex, colIndex++, d.FProductModel);
                cells.Add(rowIndex, colIndex++, d.Fauxqty);
                cells.Add(rowIndex, colIndex++, d.FReturnQty);
                cells.Add(rowIndex, colIndex++, d.FRealReturnQty);
                cells.Add(rowIndex, colIndex++, d.FOnlineStatus);
                cells.Add(rowIndex, colIndex++, d.FReturnDep);
                cells.Add(rowIndex, colIndex++, d.FCHDepName);
                //cells.Add(rowIndex, colIndex++, d.FHasReturnQty);
                //cells.Add(rowIndex, colIndex++, d.FHasRedQty);
                cells.Add(rowIndex, colIndex++, d.FHasInvoice);
                //cells.Add(rowIndex, colIndex++, d.FNeedResend);
                cells.Add(rowIndex, colIndex++, d.apply_status);
            }

            xls.Send();
        }

        //审核员导出备料单报表，要求按照计划经理审批OK和规格型号筛选
        //public ActionResult exportAuditorBLExcel(string beginDate, string toDate, string proModel)
        //{
        //    DateTime bDate = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
        //    DateTime tDate = bDate;
        //    if (!string.IsNullOrEmpty(beginDate)) {
        //        bDate = DateTime.Parse(beginDate);
        //    }
        //    if (!string.IsNullOrEmpty(toDate)) {
        //        tDate = DateTime.Parse(toDate);
        //    }
        //    if (bDate > tDate) {
        //        utl.writeEventLog("导出审核员备料单Excel", "日期期间不正确", "", Request, 10);
        //        ViewBag.tip = "日期期间不正确";
        //        return View("tip");
        //    }
        //    utl.writeEventLog("导出审核员备料单Excel", bDate.ToShortDateString() + "~" + tDate.ToShortDateString() + ";model:" + proModel, "", Request);
        //    BeginExportAuditorBLExcel(bDate, tDate.AddDays(1), proModel);
        //    return null;
        //}
        //public void BeginExportAuditorBLExcel(DateTime bDate, DateTime tDate, string proModel)
        //{
        //    var myData = (from b in db.Sale_BL
        //                  from bd in b.Sale_BL_details
        //                  from a in db.ApplyDetails
        //                  where a.Apply.sys_no == b.sys_no
        //                  && a.Apply.success == true
        //                  && a.step_name.Contains("计划经理")
        //                  && a.finish_date >= bDate
        //                  && a.finish_date <= tDate
        //                  && b.product_model.Contains(proModel)
        //                  select new
        //                  {
        //                      bl = b,
        //                      bd = bd
        //                  }).Distinct().ToList();

        //    //列宽：
        //    ushort[] colWidth = new ushort[] { 12, 16, 32, 12, 24, 18, 16, 32, 32,
        //        18,16,24,24,16,64,32,24,12,16,16,16,24,12 };

        //    //列名：
        //    string[] colName = new string[] { "备料日期", "备料编号", "产品型号", "数量", "客户名称", "计划下订单日期", "营业员","TFT型号","TP型号",
        //    "成交价（不含税）","事业部","产品用途","对应项目组","办事处","摘要","物料型号","物料名称","单位","单位用量","数量","最高采购价","备注","来源"};

        //    //設置excel文件名和sheet名
        //    XlsDocument xls = new XlsDocument();
        //    xls.FileName = string.Format("备料单_{0}.xls", DateTime.Now.ToShortDateString());
        //    Worksheet sheet = xls.Workbook.Worksheets.Add("单据信息列表");

        //    //设置各种样式

        //    //标题样式
        //    XF boldXF = xls.NewXF();
        //    boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
        //    boldXF.Font.Height = 12 * 20;
        //    boldXF.Font.FontName = "宋体";
        //    boldXF.Font.Bold = true;

        //    //设置列宽
        //    ColumnInfo col;
        //    for (ushort i = 0; i < colWidth.Length; i++) {
        //        col = new ColumnInfo(xls, sheet);
        //        col.ColumnIndexStart = i;
        //        col.ColumnIndexEnd = i;
        //        col.Width = (ushort)(colWidth[i] * 256);
        //        sheet.AddColumnInfo(col);
        //    }

        //    Cells cells = sheet.Cells;
        //    int rowIndex = 1;
        //    int colIndex = 1;

        //    //设置标题
        //    foreach (var name in colName) {
        //        cells.Add(rowIndex, colIndex++, name, boldXF);
        //    }

        //    foreach (var d in myData) {
        //        colIndex = 1;

        //        cells.Add(++rowIndex, colIndex, ((DateTime)d.bl.bl_date).ToString("yyyy-MM-dd"));
        //        cells.Add(rowIndex, ++colIndex, d.bl.bill_no);
        //        cells.Add(rowIndex, ++colIndex, d.bl.product_model);
        //        cells.Add(rowIndex, ++colIndex, d.bl.qty);
        //        cells.Add(rowIndex, ++colIndex, d.bl.customer_name);
        //        cells.Add(rowIndex, ++colIndex, ((DateTime)d.bl.plan_order_date).ToString("yyyy-MM-dd"));
        //        cells.Add(rowIndex, ++colIndex, d.bl.User.real_name);
        //        cells.Add(rowIndex, ++colIndex, d.bl.TFT_model);
        //        cells.Add(rowIndex, ++colIndex, d.bl.TP_model);

        //        cells.Add(rowIndex, ++colIndex, d.bl.deal_price);
        //        cells.Add(rowIndex, ++colIndex, d.bl.bus_dep);
        //        cells.Add(rowIndex, ++colIndex, d.bl.product_use);
        //        cells.Add(rowIndex, ++colIndex, d.bl.project_group);
        //        cells.Add(rowIndex, ++colIndex, d.bl.User.Department1.name);
        //        //cells.Add(rowIndex, ++colIndex, d.bl_project);
        //        cells.Add(rowIndex, ++colIndex, d.bl.comment);

        //        cells.Add(rowIndex, ++colIndex, d.bd.fmodel);
        //        cells.Add(rowIndex, ++colIndex, d.bd.fname);
        //        cells.Add(rowIndex, ++colIndex, d.bd.unitname);
        //        cells.Add(rowIndex, ++colIndex, d.bd.fqty);
        //        cells.Add(rowIndex, ++colIndex, d.bd.total_qty);
        //        cells.Add(rowIndex, ++colIndex, d.bd.highest_price);
        //        cells.Add(rowIndex, ++colIndex, d.bd.comment);
        //        cells.Add(rowIndex, ++colIndex, d.bd.source);
        //    }

        //    xls.Send();
        //}

        //导出事业部已审核但是还未导入K3的客退数据

        public void exportAuditorBLExcel(string ids)
        {
            var myData = db.getAuditorBLExcels(ids);

            //列宽：
            ushort[] colWidth = new ushort[] { 12, 16, 12, 16, 32, 12, 24, 18, 16,32,
                    16, 18,18,18,32,32,28,16,18,18,
                    18,18,16,18};

            //列名：
            string[] colName = new string[] { "审核结果", "流水号","备料日期", "备料编号", "产品型号", "数量", "客户名称", "计划下订单日期", "营业员","备料项目",
                "成交价（不含税）","事业部","产品用途","办事处","摘要","物料型号","物料名称","单位","单位用量","标准数量",
                "订料数量","K3数量","来源","订料员"};

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("备料单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("单据信息列表");

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

            foreach (var d in myData) {
                colIndex = 1;
                //"流水号","备料日期", "备料编号", "产品型号", "数量", "客户名称", "计划下订单日期", "营业员",
                //"成交价（不含税）","事业部","产品用途","办事处","摘要","物料型号","物料名称","单位","单位用量","标准数量",
                //"订料数量","K3数量","来源","订料员"

                cells.Add(++rowIndex, colIndex, d.audit_result == null ? "未提交" : (d.audit_result == 0 ? "审核中" : (d.audit_result == 1 ? "成功" : "失败")));
                cells.Add(rowIndex, ++colIndex, d.sys_no);
                cells.Add(rowIndex, ++colIndex, d.bl_date);
                cells.Add(rowIndex, ++colIndex, d.bill_no);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.qty);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.plan_order_date);
                cells.Add(rowIndex, ++colIndex, d.real_name);
                cells.Add(rowIndex, ++colIndex, d.bl_project);

                cells.Add(rowIndex, ++colIndex, d.deal_price);
                cells.Add(rowIndex, ++colIndex, d.bus_dep);
                cells.Add(rowIndex, ++colIndex, d.product_use);
                cells.Add(rowIndex, ++colIndex, d.agency_name);
                cells.Add(rowIndex, ++colIndex, d.comment);
                cells.Add(rowIndex, ++colIndex, d.fmodel);
                cells.Add(rowIndex, ++colIndex, d.fname);
                cells.Add(rowIndex, ++colIndex, d.unitname);
                cells.Add(rowIndex, ++colIndex, d.fqty);
                cells.Add(rowIndex, ++colIndex, d.total_qty);

                cells.Add(rowIndex, ++colIndex, d.order_qty);
                cells.Add(rowIndex, ++colIndex, d.k3_qty);
                cells.Add(rowIndex, ++colIndex, d.source);
                cells.Add(rowIndex, ++colIndex, d.order_name);
            }

            xls.Send();
        }

        public void exportAuditNotInK3Excel() {
            var myData = (from v in db.VwDepHasAuditNoInK3
                          orderby v.dep_name, v.sys_no
                          select v).ToList();

            ushort[] colWidth = new ushort[] { 14, 16, 16, 20, 28, 18, 18, 18 };
            
            string[] colName = new string[] { "部门", "客退编号", "订单号", "品名", "型号", "数量", "事业部审核时间", "完结时间" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("已审未导入K3_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("客退数据列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++)
            {
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
            foreach (var name in colName)
            {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in myData)
            {
                colIndex = 1;

                //"部门", "客退编号", "订单号", "品名", "型号", "数量", "事业部审核时间", "完结时间"
                cells.Add(++rowIndex, colIndex, d.dep_name);
                cells.Add(rowIndex, ++colIndex, d.sys_no );
                cells.Add(rowIndex, ++colIndex, d.seorder_no);
                cells.Add(rowIndex, ++colIndex, d.product_name);
                cells.Add(rowIndex, ++colIndex, d.product_model);
                cells.Add(rowIndex, ++colIndex, d.real_return_qty);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.dept_last_audit_time).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.finish_date == null ? "未完结" : ((DateTime)d.finish_date).ToShortDateString());
            }

            xls.Send();
        }
    }
}
