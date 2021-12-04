using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;
using Newtonsoft.Json;
using org.in2bits.MyXls;
using CrystalDecisions.CrystalReports.Engine;
using Sale_Order.Models.SODTTableAdapters;

namespace Sale_Order.Services
{
    public class SOSv:BillSv
    {
        private Sale_SO bill;
        public SOSv() { }                

        public SOSv(string sysNo)
        {
            bill = db.Sale_SO.Where(s => s.sys_no == sysNo).FirstOrDefault();
        }

        public override string BillType
        {
            get { return "SO"; }
        }

        public override string BillTypeName
        {
            get { return "销售订单"; }
        }

        public override string CreateViewName
        {
            get { return "CreateNSO"; }
        }

        public override string CheckViewName
        {
            get { return "CheckNSO"; }
        }

        public override string CheckListViewName
        {
            get { return "CheckNBillList"; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            bill = new Sale_SO();

            bill.user_name = currentUser.realName;
            bill.user_id = currentUser.userId;
            bill.sys_no = GetNextSysNo(BillType);
            bill.order_date = DateTime.Now;
            bill.percent1 = 100;
            bill.step_version = 0;
            bill.account = account;

            var dep = new K3ItemSv(account).GetK3Items("agency").Where(k => k.fname == currentUser.departmentName).FirstOrDefault();
            if (dep != null) {
                bill.department_name = dep.fname;
                bill.department_no = dep.fid;
            }

            return new SOModel() { head = bill, entrys = new List<Sale_SO_details>() };
        }

        public override object GetBill(int stepVersion, int userId)
        {
            bill.step_version = stepVersion;
            return new SOModel() { head = bill, entrys = db.Sale_SO_details.Where(s => s.order_id == bill.id).ToList() };
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            Sale_SO h = JsonConvert.DeserializeObject<Sale_SO>(fc.Get("head"));
            List<Sale_SO_details>  ds = JsonConvert.DeserializeObject<List<Sale_SO_details>>(fc.Get("details"));

            //如已提交，则不能再保存
            if (h.step_version == 0) {
                if (new ApplySv().ApplyHasBegan(h.sys_no)) {
                    throw new Exception("已提交的单据不能再次保存！");
                }
            }

            //订单号判断是否有重复
            if (!string.IsNullOrEmpty(h.order_no)) {
                if (db.Sale_SO.Where(s => s.order_no == h.order_no && s.sys_no != h.sys_no).Count() > 0) {
                    throw new Exception("此订单号之前已使用！");
                }
            }

            //2020-07-10 伟忠要求判断采购订单号是否重复，有重复的不能再下
            if (!string.IsNullOrWhiteSpace(h.po_number)) {
                var existedPoNumberBill = from s in db.Sale_SO
                                          join a in db.Apply on s.sys_no equals a.sys_no
                                          where s.po_number == h.po_number && s.sys_no != h.sys_no
                                          && (a.success == true || a.success == null)
                                          select s.sys_no;
                if (existedPoNumberBill.Count() > 0) {
                    throw new Exception("此采购订单号之前已经使用过，流水号：" + existedPoNumberBill.First());
                }
            }

            //验证客户编码与客户名称是否匹配
            if (!new K3ItemSv(h.account).IsCustomerNameAndNoMath(h.buy_unit_name, h.buy_unit_no)) {
                throw new Exception("购货单位请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(h.account).IsCustomerNameAndNoMath(h.plan_firm_name, h.plan_firm_no)) {
                throw new Exception("方案公司请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(h.account).IsCustomerNameAndNoMath(h.oversea_client_name, h.oversea_client_no)) {
                throw new Exception("海外客户请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(h.account).IsCustomerNameAndNoMath(h.final_client_name, h.final_client_no)) {
                throw new Exception("最终客户请输入后按回车键搜索后在列表中选择");
            }


            #region 验证业务员和主管
            if (string.IsNullOrEmpty(h.clerk_no)) {
                throw new Exception("业务员1请输入后按回车键搜索后在列表中选择");
            }
            if (string.IsNullOrEmpty(h.charger_no)) {
                throw new Exception("主管请输入后按回车键搜索后在列表中选择");
            }
            if (string.IsNullOrEmpty(h.clerk2_name) && !string.IsNullOrEmpty(h.clerk2_no)) {
                h.clerk2_no = "";
            }
            if (string.IsNullOrEmpty(h.clerk3_name) && !string.IsNullOrEmpty(h.clerk3_no)) {
                h.clerk3_no = "";
            }
            string account = h.account ?? "光电总部";
            var c1 = new K3ItemSv(account).GetK3Emp(h.clerk_no);
            if (c1.Count() != 1) {
                throw new Exception("业务员1不可用，请重新选择");
            }
            else if (!c1.First().emp_name.Equals(h.clerk_name)) {
                throw new Exception("业务员1请输入后按回车键搜索后在列表中选择");
            }
            if (h.percent2 != null && h.percent2 > 0) {
                var c2 = new K3ItemSv(account).GetK3Emp(h.clerk2_no);
                if (c2.Count() != 1) {
                    throw new Exception("业务员2不可用，请重新选择");
                }
                else if (!c2.First().emp_name.Equals(h.clerk2_name)) {
                    throw new Exception("业务员2请输入后按回车键搜索后在列表中选择");
                }
            }
            if (h.percent3 != null && h.percent3 > 0) {
                var c3 = new K3ItemSv(account).GetK3Emp(h.clerk3_no);
                if (c3.Count() != 1) {
                    throw new Exception("业务员3不可用，请重新选择");
                }
                else if (!c3.First().emp_name.Equals(h.clerk3_name)) {
                    throw new Exception("业务员3请输入后按回车键搜索后在列表中选择");
                }
            }
            var c4 = new K3ItemSv(account).GetK3Emp(h.charger_no);
            if (c4.Count() != 1) {
                throw new Exception("主管不可用，请重新选择");
            }
            else if (!c4.First().emp_name.Equals(h.charger_name)) {
                throw new Exception("主管请输入后按回车键搜索后在列表中选择");
            }
            #endregion

            #region 验证营业员比例的合法性,并保存营业员比例
            string salerPercent = h.salePs.Trim();
            string[] salers = salerPercent.Split(new char[] { ';', '；' });
            List<SalerPerModel> spList = new List<SalerPerModel>();
            for (int i = 0; i < salers.Count(); i++) {
                string[] salerSplit = salers[i].Split(new char[] { ':', '：' });
                if (salerSplit.Count() != 2) {
                    if (string.IsNullOrWhiteSpace(salerSplit[0])) {
                        continue;
                    }
                    throw new Exception("保存失败：以下营业员比例输入不合法：" + salerSplit[0]);
                }
                string tpName = salerSplit[0].Trim();
                string tpPercent = salerSplit[1].Trim();
                float percent;
                //2013-7-23 增加一种格式：周启校（林伟源）：50%
                if (tpName.Contains("(") || tpName.Contains("（")) {
                    string[] tpNameSplit = tpName.Split(new char[] { '(', ')', '（', '）' });
                    foreach (string tpNamest in tpNameSplit) {
                        if (!string.IsNullOrWhiteSpace(tpNamest)) {
                            if (new K3ItemSv(account).GetK3Emp(tpNamest).Count() < 1)
                            {
                                throw new Exception("保存失败：以下营业员不可用：" + tpNamest);
                            }
                        }
                    }
                }
                else {
                    if (new K3ItemSv(account).GetK3Emp(tpName).Count() < 1)
                    //if (utl.getSalerId(tpName) == null)
                    {
                        throw new Exception("保存失败：以下营业员不可用：" + tpName);
                    }
                }
                if (tpPercent.Contains("%")) {
                    tpPercent = tpPercent.Substring(0, tpPercent.IndexOf('%'));
                }
                if (!float.TryParse(tpPercent, out percent)) {
                    throw new Exception("保存失败：以下比例不合法：" + tpPercent);
                }
                if (percent < 0 || percent > 100) {
                    throw new Exception("保存失败：以下比例超出范围：" + tpPercent);
                }
                SalerPerModel spm = new SalerPerModel();
                if (tpName.Contains("(") || tpName.Contains("（")) {
                    spm.salerName = tpName;
                    spm.percent = (float)Math.Round(percent, 1);
                    spm.salerId = 0;
                    spm.cardNumber = "0";
                    spm.zt = "gd";
                }
                else {
                    var thisSaler = db.getSaler(tpName, 1).First();
                    spm.salerId = thisSaler.id;
                    spm.salerName = thisSaler.name;
                    spm.cardNumber = thisSaler.cardNum;
                    spm.zt = thisSaler.zt;
                    spm.percent = (float)Math.Round(percent, 1);
                }
                spList.Add(spm);
            }
            if (spList.Sum(l => l.percent) != 100) {
                throw new Exception("保存失败：营业员比例之和不等以100%");
            }
            #endregion

            #region 表体验证
            int taxRate = 13;
            List<int> projectNos = new List<int> { 467 };
            try {
                projectNos = db.VwProjectNumber.Where(v => v.client_number == h.buy_unit_no || v.client_number == h.oversea_client_no || v.id == 467).Select(v => v.id).ToList();
            }
            catch {

            }
            int currentIndex = 0;
            foreach (var d in ds) {
                currentIndex++;
                d.entry_id = currentIndex;
                if (d.project_no == null) {
                    throw new Exception("保存失败：项目编号不能为空");
                }
                else if (!projectNos.Contains((int)d.project_no)) {
                    throw new Exception("保存失败：项目名称[" + d.project_name + "]不属于当前客户");
                }

                if (h.currency_no == "RMB" && d.tax_rate != taxRate) {
                    throw new Exception("保存失败：第" + currentIndex + "行：币别为人民币的税率必须是" + taxRate);
                }
                else if (h.currency_no != "RMB" && d.tax_rate != 0) {
                    throw new Exception("保存失败：第" + currentIndex + "行：币别为非人民币的税率必须是0");
                }

                d.unit_price = d.unit_price ?? 0;
                d.aux_tax_price = d.aux_tax_price ?? 0;
                if (Math.Abs((decimal)(d.unit_price * (1 + d.tax_rate / 100) - d.aux_tax_price)) > 0.0001m) {
                    throw new Exception("保存失败：第" + currentIndex + "行：不含税单价 * (1+税率%）不等于含税单价");
                }

                if (d.deal_price > 0) {
                    d.MU = 100 * (1 - ((d.cost * (1 + d.tax_rate / 100)) / (d.deal_price * (decimal)h.exchange_rate))) - d.fee_rate;
                    if (d.MU <= 0) {
                        d.commission_rate = 0;
                    }
                    else {
                        d.commission_rate = new K3ItemSv(h.account).GetK3CommissionRate(h.product_type_name, (double)d.MU,h.department_name);
                    }
                    //2018-10-1开始，佣金计算公式修改，将佣金再除（1+税率%）
                    if (h.product_type_name == "CCM" && d.MU < -6) {
                        d.commission = (d.deal_price / (1 + d.tax_rate / 100)) * d.cost * 0.002m * (decimal)h.exchange_rate;
                    }
                    else {
                        d.commission = (d.deal_price / (1 + d.tax_rate / 100)) * d.qty * (decimal)h.exchange_rate * d.commission_rate / 100;
                    }
                    d.commission = Decimal.Round((decimal)d.commission, 2);
                }

                d.suggested_delivery_date = d.suggested_delivery_date ?? d.delivery_date;
                d.confirm_date = d.confirm_date ?? d.target_date;

            }

            #endregion


            //如果存在2张以上相同流水号的订单，表示该订单时修改而成，此时要将旧的订单删除。
            var existedBills = db.Sale_SO.Where(s => s.sys_no == h.sys_no).ToList();
            if (existedBills.Count() >= 1) {
                var existedDetails = db.Sale_SO_details.Where(d => existedBills.Select(s => s.id).Contains(d.order_id));

                BackupData bd = new BackupData();
                bd.op_date = DateTime.Now;
                bd.sys_no = h.sys_no;
                bd.user_id = user.userId;
                bd.main_data = JsonConvert.SerializeObject(existedBills);
                bd.secondary_data = JsonConvert.SerializeObject(existedDetails);
                db.BackupData.InsertOnSubmit(bd);

                db.Sale_SO_details.DeleteAllOnSubmit(existedDetails);
                db.Sale_SO.DeleteAllOnSubmit(existedBills);
            }
            try {
                db.Sale_SO.InsertOnSubmit(h);
                db.Sale_SO_details.InsertAllOnSubmit(ds);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                throw new Exception("订单保存失败：" + ex.Message);
            }

            //最后保存表头与表体的关系
            foreach (var d in ds) {
                d.order_id = h.id;
            }
            db.SubmitChanges();

            return "";
        }

        public override List<object> GetBillList(SalerSearchParamModel pm, int userId)
        {
            pm.toDate = pm.toDate.AddDays(1);
            pm.sysNo = pm.sysNo ?? "";            

            var result = (from o in db.Sale_SO
                          join e in db.Sale_SO_details on o.id equals e.order_id
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.user_id == userId
                          && (o.sys_no.Contains(pm.sysNo) || e.item_modual.Contains(pm.sysNo))
                          && o.order_date >= pm.fromDate
                          && o.order_date <= pm.toDate
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          select new OrderModel()
                          {
                              bill_id = o.id,
                              apply_status = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批当中"),
                              buy_unit = o.buy_unit_name,
                              deal_price = e.deal_price,
                              product_model = e.item_modual,
                              product_name = e.item_name,
                              qty = e.qty,
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
            List<Sale_SO_details> details = db.Sale_SO_details.Where(s => s.order_id == bill.id).ToList();

            bill.step_version = 0;
            bill.sys_no = GetNextSysNo(BillType);
            bill.order_date = DateTime.Now;
            bill.account = bill.account ?? "光电总部";

            return new SOModel() { head = bill, entrys = details };
        }

        public override string GetProcessNo()
        {
            if (bill.department_name.Equals("总裁办")) {
                return "SO_2";
            }
            return BillType;
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            Dictionary<string, int?> dic = new Dictionary<string, int?>();
            dic.Add("部门NO", db.User.Single(u => u.id == bill.user_id).Department1.dep_no);

            return dic;
        }

        public override string GetProductModel()
        {
            return db.Sale_SO_details.Where(s => s.order_id == bill.id).Select(s => s.item_modual).FirstOrDefault();
        }

        public override string GetCustomerName()
        {
            return bill.buy_unit_name;
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.Sale_SO.Where(s => s.sys_no == sysNo).Count() > 0;
        }

        public override string GetSpecificBillTypeName()
        {
            return BillTypeName + "_" + bill.order_type_name;
        }

        public override void DoWhenBeforeApply()
        {
            
        }

        public override void DoWhenAfterApply()
        {

        }

        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            if (stepName.Contains("下单组") && isPass) {
                if (string.IsNullOrEmpty(bill.order_no)) {
                    throw new Exception("订单编号必须填写保存后才能审批通过");
                }
            }
        }

        public override void DoWhenFinishAudit(bool isPass)
        {
            if (isPass) {
                MoveToFormalDir(bill.sys_no); //成功结束的申请，将附件移动到正式目录
            }
        }

        /// <summary>
        /// 导出excel数据的模型
        /// </summary>
        private class ExcelData
        {
            public Sale_SO h { get; set; }
            public Sale_SO_details e { get; set; }
            public string auditStatus { get; set; }
        }

        /// <summary>
        /// 导出SO的excel通用方法
        /// </summary>
        /// <param name="myData">导出的数据</param>
        private void ExportExcel(List<ExcelData> myData)
        {
            ushort[] colWidth = new ushort[] {12,16,16,14,18,28,28,28,28,20,28,
                                            14,14,14,14,14,14,14,14,14,10,
                                            12,20,14,14,14,14,60,60,60};

            string[] colName = new string[] {"状态", "流水号","订单号","下单日期","办事处","购货单位","海外客户","终极客户","方案公司","产品名称","型号规格",
                                            "数量","成交价","成交金额","不含税单价","合同价","合同金额","成本RMB","MU","佣金RMB","币别",
                                            "汇率","结算方式","营业员1","比例1","营业员2","比例2","说明","包装参数","摘要"};

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

                cells.Add(++rowIndex, colIndex, d.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.h.sys_no);
                cells.Add(rowIndex, ++colIndex, d.h.order_no);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.h.order_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.h.department_name);
                cells.Add(rowIndex, ++colIndex, d.h.buy_unit_name);
                cells.Add(rowIndex, ++colIndex, d.h.oversea_client_name);
                cells.Add(rowIndex, ++colIndex, d.h.final_client_name);
                cells.Add(rowIndex, ++colIndex, d.h.plan_firm_name);
                cells.Add(rowIndex, ++colIndex, d.e.item_name);
                cells.Add(rowIndex, ++colIndex, d.e.item_modual);

                cells.Add(rowIndex, ++colIndex, d.e.qty);
                cells.Add(rowIndex, ++colIndex, d.e.deal_price);
                cells.Add(rowIndex, ++colIndex, d.e.deal_price * d.e.qty);
                cells.Add(rowIndex, ++colIndex, d.e.unit_price);
                cells.Add(rowIndex, ++colIndex, d.e.aux_tax_price);
                cells.Add(rowIndex, ++colIndex, d.e.aux_tax_price * d.e.qty);
                cells.Add(rowIndex, ++colIndex, d.e.cost);
                cells.Add(rowIndex, ++colIndex, d.e.MU);
                cells.Add(rowIndex, ++colIndex, d.e.commission);
                cells.Add(rowIndex, ++colIndex, d.h.currency_name);

                cells.Add(rowIndex, ++colIndex, d.h.exchange_rate);
                cells.Add(rowIndex, ++colIndex, d.h.clearing_way_name);
                cells.Add(rowIndex, ++colIndex, d.h.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.h.percent1);
                cells.Add(rowIndex, ++colIndex, d.h.clerk2_name);
                cells.Add(rowIndex, ++colIndex, d.h.percent2);
                cells.Add(rowIndex, ++colIndex, d.h.description);
                cells.Add(rowIndex, ++colIndex, d.h.further_info);
                cells.Add(rowIndex, ++colIndex, d.h.salePs);
            }

            xls.Send();
        }

        public override void ExportSalerExcle(Models.SalerSearchParamModel pm, int userId)
        {
            pm.toDate = pm.toDate.AddDays(1);
            pm.sysNo = pm.sysNo ?? "";

            var myData = (from o in db.Sale_SO
                          join e in db.Sale_SO_details on o.id equals e.order_id
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.user_id == userId
                          && (o.sys_no.Contains(pm.sysNo) || e.item_modual.Contains(pm.sysNo))
                          && o.order_date >= pm.fromDate
                          && o.order_date <= pm.toDate
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          orderby o.order_date
                          select new ExcelData()
                          {
                              h = o,
                              e = e,
                              auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批当中"),
                          }).ToList();

            ExportExcel(myData);
        }

        public override void ExportAuditorExcle(Models.AuditSearchParamModel pm, int userId)
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
                          join o in db.Sale_SO on a.sys_no equals o.sys_no
                          join e in db.Sale_SO_details on o.id equals e.order_id
                          where ad.user_id == userId
                          && a.order_type == BillType
                          && a.sys_no.Contains(pm.sysNo)
                          && e.item_modual.Contains(pm.proModel)
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
                          select new ExcelData()
                          {
                              h = o,
                              e = e,
                              auditStatus = (a.success == true ? "申请成功" : a.success == false ? "申请失败" : "审批当中"),
                          }).ToList();

            ExportExcel(myData);

        }

        public override void BeforeRollBack(int step)
        {
            
        }

        public override System.IO.Stream PrintReport(string fileFolder)
        {
            throw new Exception("暂不支持此类单据的打印服务");
            //if (bill == null) {
            //    throw new Exception("单据不存在");
            //}

            //string crystalFile = "";
            //switch (bill.order_type_name) {
            //    case "生产单":
            //        crystalFile = "SO_A5_Report.rpt";//正常订单
            //        break;
            //    case "物料处理":
            //        crystalFile = "SO_mat_A5_Report.rpt";//物料处理单
            //        break;
            //    case "样品单":
            //        crystalFile = "SO_free_A5_Report.rpt";//免费订单
            //        break;
            //    default:
            //        throw new Exception("打印模版不存在");
            //}

            //System.IO.Stream stream = null;
            //using (ReportClass rptH = new ReportClass()) {
            //    using (SODT normalSoDt = new SODT()) {
            //        using (SOReportTableAdapter SoTa = new SOReportTableAdapter()) {
            //            SoTa.Fill(normalSoDt.SOReport, bill.sys_no);
            //        }
            //        rptH.FileName = fileFolder + crystalFile;
            //        rptH.Load();
            //        rptH.SetDataSource(normalSoDt);
            //    }
            //    stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            //}
            //return stream;
        }

        public override string GetOrderNumber()
        {
            return bill.order_no;
        }
    }
}