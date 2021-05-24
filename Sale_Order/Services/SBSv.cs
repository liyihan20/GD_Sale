using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;
using Sale_Order.Utils;
using Newtonsoft.Json;
using org.in2bits.MyXls;
using System.IO;
using CrystalDecisions.CrystalReports.Engine;
using Sale_Order.Models.SBDTTableAdapters;
using Sale_Order.Interfaces;

namespace Sale_Order.Services
{
    public class SBSv : BillSv, IFinishEmail
    {
        private SampleBill bill;

        public SBSv() { }
        public SBSv(string sysNo)
        {
            bill = db.SampleBill.SingleOrDefault(s => s.sys_no == sysNo);
        }

        public override string BillType
        {
            get { return "SB"; }
        }

        public override string BillTypeName
        {
            get { return "样品单"; }
        }

        public override string CreateViewName
        {
            get { return "CreateNSB"; }
        }

        public override string CheckViewName
        {
            get { return "CheckNSB"; }
        }

        public override string CheckListViewName
        {
            get { return "CheckNBillList"; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            bill = new SampleBill();
            bill.sys_no = GetNextSysNo(BillType);
            bill.bill_date = DateTime.Now;
            bill.step_version = 0;
            bill.account = account;
            bill.User = db.User.Where(u => u.id == currentUser.userId).First();
            var dep = new K3ItemSv(account).GetK3Items("agency").Where(k => k.fname == currentUser.departmentName).FirstOrDefault();
            if (dep != null) {
                bill.agency_name = dep.fname;
                bill.agency_no = dep.fid;
            }

            return bill;
        }

        public override object GetBill(int stepVersion, int userId)
        {
            bill.step_version = stepVersion;
            return bill;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            SampleBill sb = JsonConvert.DeserializeObject<SampleBill>(fc.Get("head"));

            //如已提交，则不能再保存
            if (sb.step_version == 0) {
                sb.original_user_id = user.userId;
                sb.create_date = DateTime.Now;
                if (new ApplySv().ApplyHasBegan(sb.sys_no)) {
                    throw new Exception("已提交的单据不能再次保存！");
                }
            }

            account = sb.account;

            //验证客户编码与客户名称是否匹配
            if (!new K3ItemSv(account).IsCustomerNameAndNoMath(sb.customer_name, sb.customer_no)) {
                throw new Exception("购货单位请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(account).IsCustomerNameAndNoMath(sb.plan_firm_name, sb.plan_firm_no)) {
                throw new Exception("方案公司请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(account).IsCustomerNameAndNoMath(sb.sea_customer_name, sb.sea_customer_no)) {
                throw new Exception("海外客户请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(account).IsCustomerNameAndNoMath(sb.zz_customer_name, sb.zz_customer_no)) {
                throw new Exception("最终客户请输入后按回车键搜索后在列表中选择");
            }

            if (string.IsNullOrEmpty(sb.clerk_no)) {
                throw new Exception("业务员请输入后按回车键搜索后在列表中选择");
            }
            if (string.IsNullOrEmpty(sb.charger_no)) {
                throw new Exception("主管请输入后按回车键搜索后在列表中选择");
            }

            var c1 = new K3ItemSv(account).GetK3Emp(sb.clerk_no);
            if (c1.Count() != 1) {
                throw new Exception("业务员不可用，请重新选择");
            }
            else if (!c1.First().emp_name.Equals(sb.clerk_name)) {
                throw new Exception("业务员请输入后按回车键搜索后在列表中选择");
            }

            var c4 = new K3ItemSv(account).GetK3Emp(sb.charger_no);
            if (c4.Count() != 1) {
                throw new Exception("主管不可用，请重新选择");
            }
            else if (!c4.First().emp_name.Equals(sb.charger_name)) {
                throw new Exception("主管请输入后按回车键搜索后在列表中选择");
            }

            sb.update_user_id = user.userId;
            sb.clear_way = sb.clear_way_name;//历史遗留问题

            if (sb.is_free.Equals("免费")) {
                sb.total_sum = sb.sample_qty * sb.cost;
                sb.deal_price = 0;
                sb.contract_price = 0;
            }
            else {
                sb.total_sum = sb.sample_qty * sb.contract_price;
            }

            var existedBill = db.SampleBill.Where(s => s.sys_no == sb.sys_no).FirstOrDefault();
            if (existedBill != null) {
                sb.original_user_id = existedBill.original_user_id;

                //备份
                BackupData bd = new BackupData();
                bd.sys_no = sb.sys_no;
                bd.main_data = new SomeUtils().ModelToString(existedBill);
                bd.op_date = DateTime.Now;
                bd.user_id = user.userId;
                db.BackupData.InsertOnSubmit(bd);

                //删除旧数据
                db.SampleBill.DeleteOnSubmit(existedBill);
            }

            var apply = db.Apply.Where(a => a.sys_no == sb.sys_no).FirstOrDefault();
            if (apply != null) {
                apply.p_model = sb.product_model;
            }

            db.SampleBill.InsertOnSubmit(sb);
            try {
                db.SubmitChanges();
            }
            catch (Exception ex) {
                throw new Exception("保存失败：" + ex.Message);
            }

            return "";
        }

        public override List<object> GetBillList(SalerSearchParamModel pm, int userId)
        {
            pm.toDate = pm.toDate.AddDays(1);
            pm.sysNo = pm.sysNo ?? "";

            var result = (from o in db.SampleBill
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.original_user_id == userId
                          && (o.sys_no.Contains(pm.sysNo) || o.product_model.Contains(pm.sysNo))
                          && o.bill_date >= pm.fromDate
                          && o.bill_date <= pm.toDate
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          select new OrderModel()
                          {
                              bill_id = o.id,
                              apply_status = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批当中"),
                              buy_unit = o.customer_name,
                              deal_price = o.deal_price,
                              product_model = o.product_model,
                              product_name = o.product_name,
                              qty = o.sample_qty,
                              sys_no = o.sys_no,
                              apply_date = (Y == null ? "" : Y.start_date.ToString()),
                              account = o.account ?? "光电总部"
                          }).ToList();

            foreach (var re in result.Where(r => !string.IsNullOrEmpty(r.apply_date))) {
                re.apply_date = DateTime.Parse(re.apply_date).ToString("yyyy-MM-dd HH:mm");
            }

            return result.ToList<Object>();
        }

        public override object GetNewBillFromOld()
        {
            bill.step_version = 0;
            bill.sys_no = GetNextSysNo(BillType);
            bill.bill_date = DateTime.Now;
            bill.account = bill.account ?? "光电总部";

            return bill;
        }

        public override string GetProcessNo()
        {
            string processType = BillType;
            if (bill.project_team.StartsWith("OLED")) {
                processType += "_OLED";
            }
            else if (bill.is_free.Equals("免费")) {
                processType += "_Free";
            }
            else {
                processType += "_Charge";
            }
            return processType;
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            Dictionary<string, int?> dic = new Dictionary<string, int?>();
            dic.Add("部门NO", db.User.Single(u => u.id == bill.original_user_id).Department1.dep_no);
            dic.Add("研发项目组NO", db.Department.Single(d => d.name == bill.project_team && d.dep_type == "研发项目组").dep_no);

            return dic;
        }

        public override string GetProductModel()
        {
            return bill.product_model;
        }

        public override string GetOrderNumber()
        {
            return bill.bill_no;
        }

        public override string GetCustomerName()
        {
            return bill.customer_name;
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.SampleBill.Where(s => s.sys_no == sysNo).Count() > 0;
        }

        public override string GetSpecificBillTypeName()
        {
            return bill.is_free + BillTypeName;
        }

        public override void DoWhenBeforeApply()
        {

        }

        public override void DoWhenAfterApply()
        {

        }

        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {

        }

        public override void DoWhenFinishAudit(bool isPass)
        {
            if (isPass) {
                string dateStr = "";
                if (!"RMB".Equals(bill.currency_no)) {
                    dateStr = "H";
                }

                if ("光电总部".Equals(bill.account)) {
                    dateStr += "G";
                }
                else if ("光电仁寿".Equals(bill.account)) {
                    dateStr += "R";
                }
                else if ("光电科技".Equals(bill.account)) {
                    dateStr += "K";
                }

                if ("免费".Equals(bill.is_free)) {
                    dateStr += "YPMF";
                }
                else {
                    dateStr += "SWYP";
                }
                if ("RMB".Equals(bill.currency_no)) {
                    dateStr += "-";
                }
                dateStr += DateTime.Now.ToString("yy");

                string billNo = GetNextNo("YP", dateStr, 4);
                bill.bill_no = billNo.Substring(2); //将前缀YP去掉。

                db.SubmitChanges();
            }
        }

        private class SBExcelData
        {
            public SampleBill h { get; set; }
            public string auditStatus { get; set; }
        }

        public override void ExportSalerExcle(SalerSearchParamModel pm, int userId)
        {
            pm.toDate = pm.toDate.AddDays(1);
            pm.sysNo = pm.sysNo ?? "";

            var myData = (from o in db.SampleBill
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.original_user_id == userId
                          && (o.sys_no.Contains(pm.sysNo) || o.product_model.Contains(pm.sysNo))
                          && o.bill_date >= pm.fromDate
                          && o.bill_date <= pm.toDate
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          orderby o.bill_date
                          select new SBExcelData()
                          {
                              h = o,
                              auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批当中"),
                          }).ToList();

            ExportExcel(myData);
        }

        public override void ExportAuditorExcle(AuditSearchParamModel pm, int userId)
        {
            DateTime fromDate, toDate;
            if (!DateTime.TryParse(pm.from_date, out fromDate)) {
                fromDate = DateTime.Parse("2010-01-01");
            }
            if (!DateTime.TryParse(pm.to_date, out toDate)) {
                toDate = DateTime.Parse("2099-01-01");
            }
            else {
                toDate = toDate.AddDays(1);
            }

            pm.sysNo = pm.sysNo ?? "";
            pm.proModel = pm.proModel ?? "";

            var myData = (from a in db.Apply
                          from ad in a.ApplyDetails
                          join o in db.SampleBill on a.sys_no equals o.sys_no
                          where ad.user_id == userId
                          && a.order_type == BillType
                          && a.sys_no.Contains(pm.sysNo)
                          && o.product_model.Contains(pm.proModel)
                          && a.start_date >= fromDate
                          && a.start_date <= toDate
                          && (pm.isFinish == 10
                          || (pm.isFinish == 1 && a.success == true)
                          || (pm.isFinish == 0 && a.success == null)
                          || (pm.isFinish == -1 && a.success == false))
                          && (pm.auditResult == 10
                          || (pm.auditResult == 1 && ad.pass == true)
                          || (pm.auditResult == 0 && ad.pass == null
                               && ((ad.countersign == true && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == false).Count() == 0)
                                   || ((ad.countersign == false || ad.countersign == null) && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == true).Count() == 0)
                               )
                             )
                          || (pm.auditResult == -1 && ad.pass == false)
                          )
                          && (ad.step == 1 || a.ApplyDetails.Where(ads => ads.step == ad.step - 1 && ads.pass == true).Count() > 0)
                          orderby a.start_date descending
                          select new SBExcelData()
                          {
                              h = o,
                              auditStatus = (a.success == true ? "申请成功" : a.success == false ? "申请失败" : "审批当中"),
                          }).Distinct().ToList();

            ExportExcel(myData);
        }

        private void ExportExcel(List<SBExcelData> myData)
        {
            //列宽：
            ushort[] colWidth = new ushort[] { 12, 12, 12, 16, 24, 30, 12, 12, 12, 12, 16, 16 };

            //列名：
            string[] colName = new string[] { "公司", "样品单种类", "下单日期", "订单编号", "客户名称", "型号", "数量", "成交价", "合同价", "成本", "办事处", "营业员" };

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

            foreach (var data in myData) {
                var d = data.h;
                colIndex = 1;

                cells.Add(++rowIndex, colIndex, d.account);
                cells.Add(rowIndex, ++colIndex, d.is_free);
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

        public override void BeforeRollBack(int step)
        {
            if (!string.IsNullOrEmpty(bill.bill_no)) {
                throw new Exception("单号已生成，不能收回");
            }
        }

        public override System.IO.Stream PrintReport(string fileFolder)
        {
            Stream stream = null;
            using (ReportClass rptH = new ReportClass()) {
                using (SBDT sbDt = new SBDT()) {
                    using (Sale_sample_billTableAdapter cmTa = new Sale_sample_billTableAdapter()) {
                        cmTa.Fill(sbDt.Sale_sample_bill, bill.sys_no);
                    }
                    //设置办事处1、总裁办3，市场部2审核人名字
                    string agencyAuditor = "", ceoAuditor = "", marketAuditor = "", yfAdmin = "", yfManager = "",
                        quotationAuditor = "", yfTopLevel = "", marketManager = "";

                    var auditDetails = (from a in db.Apply
                                        join d in db.ApplyDetails on a.id equals d.apply_id
                                        join u in db.User on d.user_id equals u.id
                                        where a.sys_no == bill.sys_no && d.pass == true
                                        select new
                                        {
                                            d.step,
                                            d.step_name,
                                            u.real_name
                                        }).ToList();

                    agencyAuditor = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("办事处")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    marketManager = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("总经理")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    yfManager = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("项目经理")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    yfAdmin = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("项目管理员")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    yfTopLevel = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("项目组上级")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    quotationAuditor = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("报价员")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    marketAuditor = auditDetails.Where(ad => ad.step == 2).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    ceoAuditor = auditDetails.Where(ad => ad.step == 3).Select(ad => ad.real_name).FirstOrDefault() ?? "";

                    sbDt.Sample_Bill_Auditor.AddSample_Bill_AuditorRow(agencyAuditor, yfManager, yfTopLevel, yfAdmin, quotationAuditor, marketAuditor, ceoAuditor, marketManager);

                    rptH.FileName = fileFolder + "SBYF_A4_Report.rpt";
                    rptH.Load();
                    rptH.SetDataSource(sbDt);
                }
                stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            }
            return stream;
        }

        public string ccToOthers(string sysNo, bool isPass)
        {
            var emailList = (from a in db.Apply
                             join d in db.ApplyDetails on a.id equals d.apply_id
                             join u in db.User on d.user_id equals u.id
                             where a.sys_no == sysNo
                             && d.step == 1
                             && d.step_name.Contains("项目") //项目经理、项目管理员与项目组上级
                             && u.email != null
                             && u.email != ""
                             select u.email).ToList();
            return string.Join(",", emailList);
        }

        public bool needReport(bool isPass)
        {
            return true;
        }
    }
}