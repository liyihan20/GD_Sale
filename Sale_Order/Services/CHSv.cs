using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;
using Sale_Order.Utils;
using Newtonsoft.Json;
using org.in2bits.MyXls;

namespace Sale_Order.Services
{
    public class CHSv : BillSv
    {
        private CH_bill bill;

        public CHSv() { }
        public CHSv(string sysNo)
        {
            bill = db.CH_bill.Single(c => c.sys_no == sysNo);
        }

        public override string BillType
        {
            get { return "CH"; }
        }

        public override string BillTypeName
        {
            get { return "出货申请单"; }
        }

        public override string CreateViewName
        {
            get { return "CreateNCH"; }
        }

        public override string CheckViewName
        {
            get { return "CheckNCH"; }
        }

        public override string CheckListViewName
        {
            get { return "CheckNBillList"; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            bill = new CH_bill();
            bill.user_name = currentUser.realName;
            bill.user_id = currentUser.userId;
            bill.step_version = 0;
            bill.sys_no = GetNextSysNo(BillType);
            bill.account = account;
            bill.log_out_date = DateTime.Now;
            bill.user_tel = db.CH_bill.Where(c => c.user_id == currentUser.userId).OrderByDescending(c => c.id).Select(c => c.user_tel).FirstOrDefault();

            return new CHModel() { head = bill, entrys = new List<CHEntrys>(), packages = new List<CH_package>() };
        }

        public override object GetBill(int stepVersion, int userId)
        {
            bill.step_version = stepVersion;

            var details = db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList();
            if (stepVersion == -1) {
                //-1表示进入查询界面和审批只读界面，这时不需要保存价钱，所以可以设置价钱为0
                //这里要做权限控制，根据userid，除了会计部，其他人不能查看价钱
                if (!new SomeUtils().hasGotPower(userId, "CanCheckCHPrice")) {
                    details.ForEach(d => d.unit_price = 0);
                }
            }
            List<CHEntrys> entrys = GetTranslateEntrys(bill, details);

            return new CHModel() { head = bill, entrys = entrys, packages = db.CH_package.Where(c => c.sys_no == bill.sys_no).ToList() };
        }

        //将数据表格式的出货明细转化为自定义类明细，主要区别是加了关联数量、库存数量和可申请数量
        private List<CHEntrys> GetTranslateEntrys(CH_bill bill, List<CH_bill_detail> details)
        {
            List<CHEntrys> entrys = JsonConvert.DeserializeObject<List<CHEntrys>>(JsonConvert.SerializeObject(details));
            foreach (var e in entrys) {
                var result = new K3ItemSv(bill.account).GetK3OrderQtys(bill.bill_type, e.order_id, e.order_entry_no);

                e.order_qty = result.order_qty;
                e.relate_qty = result.relate_qty;
                e.inv_qty = result.inv_qty;
                e.can_apply_qty = result.can_apply_qty;
                e.total_price = (decimal)(e.unit_price * (e.real_qty ?? e.apply_qty));
            }

            return entrys;
        }

        private string DecideProcessNo(CH_bill bill, List<CH_bill_detail> details)
        {
            string no = "CH_other";
            if (bill.bill_type.Equals("正单")) {
                if (details.Where(d =>
                    d.department_name.Contains("总裁")
                    //|| d.department_name.Contains("香港")
                    || d.order_no.Contains("YP") //样品单
                    || d.order_no.StartsWith("R") //仁寿单
                    || d.item_number.StartsWith("1") //原料单
                    || bill.customer_name.Contains("信利光电仁寿")
                    || bill.account == "光电仁寿"
                    || bill.user_name.Contains("黄伟忠")
                    ).Count() == 0) {
                    no = "CH";
                }
            }

            return no;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            bill = JsonConvert.DeserializeObject<CH_bill>(fc.Get("head"));
            var details = JsonConvert.DeserializeObject<List<CH_bill_detail>>(fc.Get("details"), new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Local
            });
            string stepName = fc.Get("step_name") ?? "";

            if ("申请人".Equals(stepName)) {
                bill.user_name = user.realName;
                bill.user_id = user.userId;
                bill.create_date = DateTime.Now;
                bill.status = "已保存未提交";
                bill.process_no = DecideProcessNo(bill, details);

                if ("CH".Equals(bill.process_no)) {
                    if (string.IsNullOrEmpty(bill.bus_dep)) {
                        throw new Exception("出货事业部必须选择");
                    }
                    if (bill.planner_id == null || string.IsNullOrEmpty(bill.planner_name)) {
                        throw new Exception("计划员必须选择");
                    }
                }

                int entryNo = 1;
                foreach (var d in details.OrderBy(d => d.order_no).ThenBy(d => d.order_entry_no)) {
                    d.sys_no = bill.sys_no;
                    d.entry_no = entryNo++;
                }

                var existedBill = db.CH_bill.Where(c => c.sys_no == bill.sys_no).FirstOrDefault();
                var existedDetails = db.CH_bill_detail.Where(d => d.sys_no == bill.sys_no).ToList();
                if (existedBill != null) {
                    db.CH_bill.DeleteOnSubmit(existedBill);
                    db.CH_bill_detail.DeleteAllOnSubmit(existedDetails);
                }

                db.CH_bill.InsertOnSubmit(bill);
                db.CH_bill_detail.InsertAllOnSubmit(details.OrderBy(d => d.entry_no));
            }
            else if ("营业员确认".Equals(stepName)) {
                //更新送货地址等信息
                var head = db.CH_bill.Where(c => c.sys_no == bill.sys_no).First();
                head.delivery_addr = bill.delivery_addr;
                head.delivery_attn = bill.delivery_attn;
                head.delivery_tel = bill.delivery_tel;
                head.delivery_unit = bill.delivery_unit;

                //更新实发数量
                var entrys = db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList();
                foreach (var e in entrys) {
                    e.real_qty = details.Where(d => d.entry_no == e.entry_no).FirstOrDefault().real_qty;
                }
            }
            else if ("计划审批".Equals(stepName)) {
                var head = db.CH_bill.Where(c => c.sys_no == bill.sys_no).First();
                head.out_group_id = bill.out_group_id;
                head.out_group_name = bill.out_group_name;
            }
            else if ("出货组审批".Equals(stepName)) {
                var pks = JsonConvert.DeserializeObject<List<CH_package>>(fc.Get("pks"));
                if (pks.Count() < 1) {
                    throw new Exception("请先录入包装参数后再保存");
                }
                pks.ForEach(p => p.create_date = DateTime.Now);
                var exsitedPks = db.CH_package.Where(p => p.sys_no == bill.sys_no).ToList();
                if (exsitedPks.Count() > 0) {
                    db.CH_package.DeleteAllOnSubmit(exsitedPks);
                }
                db.CH_package.InsertAllOnSubmit(pks.OrderBy(p => p.order_no).ThenBy(p => p.order_entry_no));

                //更新出货明细的实发数量
                var entrys = db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList();
                foreach (var en in entrys) {
                    en.real_qty = pks.Where(p => p.order_no == en.order_no && p.order_entry_no == en.order_entry_no).Sum(p => p.pack_num * p.every_qty);
                }

            }
            db.SubmitChanges();

            if (new string[] { "申请人", "营业员确认" }.Contains(stepName)) {
                UpdateDeliveryInfo(new CH_delivery_info()
                {
                    create_name = user.realName,
                    customer_name = bill.customer_name,
                    customer_no = bill.customer_no,
                    delivery_addr = bill.delivery_addr,
                    delivery_attn = bill.delivery_attn,
                    delivery_tel = bill.delivery_tel,
                    delivery_unit = bill.delivery_unit
                });
            }

            return "";
        }

        //更新送货客户信息
        public void UpdateDeliveryInfo(CH_delivery_info info)
        {
            var existedInfo = db.CH_delivery_info.Where(c => c.customer_no == info.customer_no && c.delivery_unit == info.delivery_unit && c.delivery_attn == info.delivery_attn && c.delivery_addr == info.delivery_addr).FirstOrDefault();
            if (existedInfo == null) {
                info.create_time = DateTime.Now;
                info.last_use_time = DateTime.Now;

                db.CH_delivery_info.InsertOnSubmit(info);
            }
            else {
                existedInfo.last_use_time = DateTime.Now;
                existedInfo.delivery_tel = info.delivery_tel;
            }

            db.SubmitChanges();
        }


        public override List<object> GetBillList(SalerSearchParamModel pm, int userId)
        {
            pm.toDate = pm.toDate.AddDays(1);
            pm.sysNo = pm.sysNo ?? "";

            var result = (from o in db.CH_bill
                          join e in db.CH_bill_detail on o.sys_no equals e.sys_no
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.user_id == userId
                          && (o.sys_no.Contains(pm.sysNo) || e.item_model.Contains(pm.sysNo))
                          && o.create_date >= pm.fromDate
                          && o.create_date <= pm.toDate
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          select new OrderModel()
                          {
                              bill_id = o.id,
                              apply_status = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批当中"),
                              buy_unit = o.customer_name,
                              product_model = e.item_model,
                              product_name = e.item_name,
                              qty = e.real_qty ?? e.apply_qty,
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
            var entrys = GetTranslateEntrys(bill, db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList());
            bill.step_version = 0;
            bill.log_out_date = DateTime.Now;
            bill.sys_no = GetNextSysNo(BillType);

            return new CHModel() { head = bill, entrys = entrys, packages = new List<CH_package>() };
        }

        public override string GetProcessNo()
        {
            return bill.process_no;
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            var dic = new Dictionary<string, int?>();

            dic.Add("部门NO", db.User.Single(u => u.id == bill.user_id).Department1.dep_no);
            dic.Add("出货事业部NO", db.Department.Where(d => d.name == bill.bus_dep && d.dep_type == "出货事业部").Select(d => d.dep_no).FirstOrDefault());

            return dic;
        }

        public override string GetProductModel()
        {
            var details = db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList();
            if (details.Count() == 1) {
                return details.First().item_model;
            }
            else if (details.Count() > 1) {
                return details.First().item_model + "...等" + details.Count() + "个";
            }
            else {
                return "";
            }
        }

        public override string GetOrderNumber()
        {
            return bill.k3_stock_no;
        }

        public override string GetCustomerName()
        {
            return bill.customer_name;
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.CH_bill.Where(c => c.sys_no == sysNo).Count() > 0;
        }

        public override string GetSpecificBillTypeName()
        {
            return bill.bill_type + "出货";
        }

        public override void DoWhenBeforeApply()
        {
            //提交之前查可申请数量
            var entrys = db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList();
            foreach (var e in GetTranslateEntrys(bill, entrys)) {
                if (e.can_apply_qty < e.apply_qty) {
                    throw new Exception(string.Format("可申请数量不足：订单【{0}】；型号【{1}】；可申请数量【{2}】;申请数量【{3}】", e.order_no, e.item_model, e.can_apply_qty, e.apply_qty));
                }
            }
        }

        public override void DoWhenAfterApply()
        {
            bill.status = "审批中";
            db.SubmitChanges();
        }

        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            if (isPass) {
                if (stepName.Contains("计划")) {
                    if (bill.out_group_id == null || string.IsNullOrEmpty(bill.out_group_name)) {
                        throw new Exception("请先选择出货组人员并点击保存按钮");
                    }
                    else {
                        new ApplySv(bill.sys_no).UpdateStepAuditor("出货组审批", (int)bill.out_group_id);
                    }
                }

                if (stepName.Contains("出货组") || stepName.Contains("营业员确认")) {
                    //检查实出数量
                    var entrys = db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList();

                    if (stepName.Contains("营业员确认")) {
                        if (bill.log_out_date < DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"))) {
                            throw new Exception("市场部与物流规则：当前时间已超过申请时设置的物流发货时间，请NG后再申请。");
                        }
                        if (entrys.Where(e => e.real_qty == null).Count() > 0) {
                            throw new Exception("存在实发数量未填写的行，请填写保存后再通过");
                        }
                        if (entrys.Where(e => e.real_qty == 0).Count() == entrys.Count()) {
                            throw new Exception("实出数量不能都为0，如果不出货，请NG此申请");
                        }
                    }
                    else if (stepName.Contains("出货组")) {
                        if (db.CH_package.Where(c => c.sys_no == bill.sys_no).Count() == 0) {
                            throw new Exception("必须至少录入一行包装参数并点击保存按钮");
                        }
                    }

                    foreach (var en in entrys) {
                        var existedNotFinishBill = (from c in db.CH_bill
                                                    join e in db.CH_bill_detail on c.sys_no equals e.sys_no
                                                    join a in db.Apply on c.sys_no equals a.sys_no
                                                    where c.sys_no != en.sys_no
                                                    && e.order_no == en.order_no && e.order_entry_no == en.order_entry_no
                                                    && c.k3_out_time == null
                                                    && a.success == true
                                                    select c.sys_no).ToList();
                        if (existedNotFinishBill.Count() > 0) {
                            throw new Exception("存在已审批完成但还未出库的申请单，单号为：" + existedNotFinishBill.First());
                        }
                    }
                    foreach (var e in GetTranslateEntrys(bill, entrys)) {
                        if (e.can_apply_qty < e.real_qty) {
                            throw new Exception(string.Format("可申请数量不足：订单【{0}】；型号【{1}】；可申请数量【{2}】;实出数量【{3}】", e.order_no, e.item_model, e.can_apply_qty, e.real_qty));
                        }
                        if (e.inv_qty < e.real_qty) {
                            throw new Exception(string.Format("库存数量不足：订单【{0}】；型号【{1}】；库存数量【{2}】;实出数量【{3}】", e.order_no, e.item_model, e.inv_qty, e.real_qty));
                        }
                    }
                }

            }
        }

        public override void DoWhenFinishAudit(bool isPass)
        {
            if (isPass) {                
                //正单需要在这里直接插入K3数据库
                if (bill.bill_type == "正单") {
                    var res = new K3ItemSv(bill.account).GenStockbill(bill.sys_no);
                    if (!res.suc) {
                        throw new Exception("导入K3时发生错误，原因：" + res.msg);
                    }
                    else {
                        bill.status = "审批完成";
                    }
                }
                else {
                    bill.status = "审批完成";
                }
            }
            else {
                bill.status = "审批结束";
            }
            db.SubmitChanges();
        }

        private class ExcelData
        {
            public CH_bill h { get; set; }
            public CH_bill_detail e { get; set; }
            public CH_package p { get; set; }
            public string auditStatus { get; set; }
        }
        private void ExportExcel(List<ExcelData> data)
        {
            string[] colName = new string[] {"状态", "公司", "流水号","出库单号", "出货类型","下单日期","物流发货日期","客户名称","出货事业部","计划员","制单人","营业员电话",
                                            "备注","收货单位","ATTN","收货电话","收货地址","订单编号","产品名称","产品型号","订单数量","申请数量","实出数量",
                                            "单位","单价","销售额","客户PO","客户PN","行备注","订单日期","订单行号",
                                            "件数","每件数量","尺寸（CM）","每件净重","每件毛重","总净重","总毛重"};
            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("出货申请单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("出货信息列表");
            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;
            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colName.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(14 * 256);
                sheet.AddColumnInfo(col);
            }

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 1;
            //设置标题
            foreach (var name in colName) {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in data) {
                colIndex = 1;
                //"状态", "流水号","出库单号", "出货类型","下单日期","物流发货日期","客户名称","出货事业部","计划员","制单人","营业员电话",
                //"备注","收货单位","ATTN","收货电话","收货地址","订单编号","产品名称","产品型号","订单数量","申请数量","实出数量",
                //"单位","单价","销售额","客户PO","客户PN","行备注","订单日期","订单行号"
                cells.Add(++rowIndex, colIndex, d.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.h.account);
                cells.Add(rowIndex, ++colIndex, d.h.sys_no);
                cells.Add(rowIndex, ++colIndex, d.h.k3_stock_no);
                cells.Add(rowIndex, ++colIndex, d.h.bill_type);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.h.create_date).ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.h.log_out_date.ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.h.customer_name);
                cells.Add(rowIndex, ++colIndex, d.h.bus_dep);
                cells.Add(rowIndex, ++colIndex, d.h.planner_name);
                cells.Add(rowIndex, ++colIndex, d.h.user_name);
                cells.Add(rowIndex, ++colIndex, d.h.user_tel);
                cells.Add(rowIndex, ++colIndex, d.h.comment);
                cells.Add(rowIndex, ++colIndex, d.h.delivery_unit);
                cells.Add(rowIndex, ++colIndex, d.h.delivery_attn);
                cells.Add(rowIndex, ++colIndex, d.h.delivery_tel);
                cells.Add(rowIndex, ++colIndex, d.h.delivery_addr);
                cells.Add(rowIndex, ++colIndex, d.e.order_no);
                cells.Add(rowIndex, ++colIndex, d.e.item_name);
                cells.Add(rowIndex, ++colIndex, d.e.item_model);
                cells.Add(rowIndex, ++colIndex, d.e.order_qty);
                cells.Add(rowIndex, ++colIndex, d.e.apply_qty);
                if (d.p == null) {
                    cells.Add(rowIndex, ++colIndex, d.e.real_qty);
                }
                else {
                    cells.Add(rowIndex, ++colIndex, d.p.every_qty * d.p.pack_num);
                }
                cells.Add(rowIndex, ++colIndex, d.e.unit_name);
                cells.Add(rowIndex, ++colIndex, d.e.unit_price);
                cells.Add(rowIndex, ++colIndex, d.e.unit_price * d.e.real_qty ?? d.e.apply_qty);
                cells.Add(rowIndex, ++colIndex, d.e.customer_po);
                cells.Add(rowIndex, ++colIndex, d.e.customer_pn);
                cells.Add(rowIndex, ++colIndex, d.e.entry_comment);
                cells.Add(rowIndex, ++colIndex, d.e.order_date.ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.e.order_entry_no);
                //包装参数："件数","每件数量","尺寸（CM）","每件净重","每件毛重","总净重","总毛重"
                if (d.p != null) {
                    cells.Add(rowIndex, ++colIndex, d.p.pack_num);
                    cells.Add(rowIndex, ++colIndex, d.p.every_qty);
                    cells.Add(rowIndex, ++colIndex, d.p.pack_size);
                    cells.Add(rowIndex, ++colIndex, d.p.every_net_weight);
                    cells.Add(rowIndex, ++colIndex, d.p.every_gross_weight);
                    cells.Add(rowIndex, ++colIndex, d.p.every_net_weight * d.p.pack_num);
                    cells.Add(rowIndex, ++colIndex, d.p.every_gross_weight * d.p.pack_num);
                }
            }

            xls.Send();
        }
        public override void ExportSalerExcle(SalerSearchParamModel pm, int userId)
        {
            pm.toDate = pm.toDate.AddDays(1);
            pm.sysNo = pm.sysNo ?? "";

            var myData = (from o in db.CH_bill
                          join e in db.CH_bill_detail on o.sys_no equals e.sys_no
                          join p in db.CH_package on new { e.sys_no, e.order_no, e.order_entry_no } equals new { p.sys_no, p.order_no, p.order_entry_no } into pe
                          from pa in pe.DefaultIfEmpty()
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.user_id == userId
                          && (o.sys_no.Contains(pm.sysNo) || e.item_model.Contains(pm.sysNo))
                          && o.create_date >= pm.fromDate
                          && o.create_date <= pm.toDate
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          orderby o.create_date
                          select new ExcelData()
                          {
                              h = o,
                              e = e,
                              p = pa,
                              auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批当中"),
                          }).ToList();
            //不看价钱
            myData.ForEach(m => m.e.unit_price = 0);

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
                          join o in db.CH_bill on a.sys_no equals o.sys_no
                          join e in db.CH_bill_detail on o.sys_no equals e.sys_no
                          join p in db.CH_package on new { e.sys_no, e.order_no, e.order_entry_no } equals new { p.sys_no, p.order_no, p.order_entry_no } into pe
                          from pa in pe.DefaultIfEmpty()
                          where ad.user_id == userId
                          && a.order_type == BillType
                          && a.sys_no.Contains(pm.sysNo)
                          && e.item_model.Contains(pm.proModel)
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
                              p = pa,
                              auditStatus = (a.success == true ? "申请成功" : a.success == false ? "申请失败" : "审批当中"),
                          }).ToList();
            //没权限的不能查看价钱
            if (!new SomeUtils().hasGotPower(userId, "CanCheckCHPrice")) {
                myData.ForEach(d => d.e.unit_price = 0);
            }

            ExportExcel(myData);
        }

        public override void BeforeRollBack(int step)
        {
            if (!string.IsNullOrEmpty(bill.k3_stock_no)) {
                throw new Exception("已导入K3出库单的申请不能收回，请先作废后再操作");
            }
        }

        public override System.IO.Stream PrintReport(string fileFolder)
        {
            throw new NotImplementedException();
        }

        public List<vw_clerkAndCustomer> GetCleckAndCustomerList(string searchValue)
        {
            var result = (from v in db.vw_clerkAndCustomer
                          where v.customerName.Contains(searchValue)
                          || v.customerNumber.Contains(searchValue)
                          || v.clerkName.Contains(searchValue)
                          || v.clerkNumber.Contains(searchValue)
                          || v.agency.Contains(searchValue)
                          || v.fromSystem == searchValue
                          orderby v.agency, v.clerkId
                          select v).ToList();

            return result;
        }

        public void SaveClerkAndCustomer(System.Web.Mvc.FormCollection fc)
        {
            int id = Int32.Parse(fc.Get("id"));
            int clerkId = Int32.Parse(fc.Get("clerk_id"));
            string customerNumber = fc.Get("customer_number");
            string customerName = fc.Get("customer_name");

            if (!new K3ItemSv("光电总部").IsCustomerNameAndNoMath(customerName, customerNumber)) {
                throw new Exception("客户名称与编码不符合");
            }
            if (id == 0) throw new Exception("来自K3的对应信息不能修改");

            if (db.Sale_ClerkAndCustomer.Where(s => s.id != id && s.clerk_id == clerkId && s.customer_number == customerNumber).Count() > 0) {
                throw new Exception("对应关系已存在，不能重复添加");
            }

            Sale_ClerkAndCustomer m;
            if (id == -1) {
                //新增
                m = new Sale_ClerkAndCustomer();
                db.Sale_ClerkAndCustomer.InsertOnSubmit(m);
            }
            else {
                m = db.Sale_ClerkAndCustomer.Single(s => s.id == id);
            }

            m.customer_name = customerName;
            m.customer_number = customerNumber;
            m.clerk_id = clerkId;

            db.SubmitChanges();
        }

        public void RemoveClerkAndCustomer(int id)
        {
            if (id < 1) throw new Exception("id 不存在");

            var m = db.Sale_ClerkAndCustomer.Single(s => s.id == id);
            db.Sale_ClerkAndCustomer.DeleteOnSubmit(m);
            db.SubmitChanges();

        }

        public List<ResultModel> GetMyCustomers(int userId)
        {
            return db.vw_clerkAndCustomer.Where(v => v.clerkId == userId).Select(v => new ResultModel() { text = v.customerName, value = v.customerNumber }).ToList();
        }

        public List<CH_delivery_info> GetSavedDeliveryInfo(string customerNo)
        {
            return db.CH_delivery_info.Where(c => c.customer_no == customerNo).OrderByDescending(c => c.last_use_time).Take(6).ToList();
        }

        public List<CHPakcageModel> GetPackageForLabel()
        {
            var list = (from e in db.CH_bill_detail
                        join p in db.CH_package on new { e.sys_no, e.order_no, e.order_entry_no } equals new { p.sys_no, p.order_no, p.order_entry_no }
                        where e.sys_no == bill.sys_no
                        select new CHPakcageModel()
                        {
                            comanyName = bill.account,
                            depName = bill.bus_dep,
                            order_no = e.order_no,
                            order_entry_no = e.order_entry_no,
                            sysNo = e.sys_no,
                            customerName = bill.customer_name,
                            item_model = e.item_model,
                            item_name = e.item_name,
                            packId = p.id,
                            pack_num = p.pack_num,
                            pack_size = p.pack_size,
                            every_qty = p.every_qty,
                            every_gross_weight = p.every_gross_weight,
                            every_net_weight = p.every_net_weight
                        }).ToList();

            //转换公司名
            var companys = db.Sale_companies.ToList();
            list.ForEach(l => l.comanyName = companys.Where(c => c.short_name == l.comanyName).Select(c => c.account_name).FirstOrDefault());

            return list;
        }

        public List<CHList4Log> GetCHList4Log(CHList4LogParam param)
        {
            var result = (from h in db.CH_bill
                          join e in db.CH_bill_detail on h.sys_no equals e.sys_no
                          join p in db.CH_package on new { e.sys_no, e.order_no, e.order_entry_no } equals new { p.sys_no, p.order_no, p.order_entry_no } into pe
                          from pa in pe.DefaultIfEmpty()
                          where (h.status == "等待物流处理" || h.status == "已出库")
                          //&& h.bill_type == "正单"
                          && h.k3_import_time >= param.beginDate && h.k3_import_time < param.toDate
                          && e.real_qty > 0
                          orderby h.k3_import_time
                          select new CHList4Log()
                          {
                              account = h.account,
                              bill_type = h.bill_type,
                              stock_addr = h.stock_addr,
                              ch_id = h.id,
                              sysNo = h.sys_no,
                              entry_no = e.entry_no,
                              every_gross_weight = pa.every_gross_weight,
                              every_net_weight = pa.every_net_weight,
                              every_qty = pa.every_qty,
                              ex_comment = h.ex_comment,
                              ex_fee = h.ex_fee,
                              ex_name = h.ex_name,
                              ex_no = h.ex_no,
                              ex_type = h.ex_type,
                              is_print = h.is_print ? "Y" : "N",
                              item_model = e.item_model,
                              k3_import_date = h.k3_import_time,
                              order_no = e.order_no,
                              order_qty = e.order_qty,
                              pack_num = pa.pack_num,
                              pack_size = pa.pack_size,
                              real_qty = pa == null ? e.real_qty : (pa.every_qty * pa.pack_num),
                              stock_no = h.k3_stock_no,
                              log_out_date = h.log_out_date,
                              log_out_span = h.log_out_span,
                              unit_name = e.unit_name,
                              delivery_addr = h.delivery_addr,
                              delivery_unit = h.delivery_unit,
                              item_name = e.item_name,
                              customer_name = h.customer_name
                          });

            if (!string.IsNullOrWhiteSpace(param.stockNo)) {
                result = result.Where(r => r.stock_no.Contains(param.stockNo));
            }
            if (!string.IsNullOrWhiteSpace(param.orderNo)) {
                result = result.Where(r => r.order_no.Contains(param.orderNo));
            }
            if (!string.IsNullOrWhiteSpace(param.itemModel)) {
                result = result.Where(r => r.item_model.Contains(param.itemModel));
            }
            if (!string.IsNullOrWhiteSpace(param.customerName)) {
                result = result.Where(r => r.customer_name.Contains(param.customerName));
            }
            if (!string.IsNullOrWhiteSpace(param.exName)) {
                result = result.Where(r => r.ex_name.Contains(param.exName));
            }
            if (!string.IsNullOrWhiteSpace(param.exNo)) {
                result = result.Where(r => r.ex_no.Contains(param.exNo));
            }
            if (!string.IsNullOrWhiteSpace(param.deliveryAddr)) {
                result = result.Where(r => r.delivery_addr.Contains(param.deliveryAddr));
            }
            if (!"所有".Equals(param.isPrinted)) {
                result = result.Where(r => (param.isPrinted == "未打印" && r.is_print == "N") || (param.isPrinted == "已打印" && r.is_print == "Y"));
            }
            if (!"所有".Equals(param.exSeted)) {
                result = result.Where(r => (param.exSeted == "未录入" && (r.ex_fee == null || r.ex_fee == 0)) || (param.exSeted == "已录入" && r.ex_fee > 0));
            }

            return result.Take(1000).ToList();
        }

        //获取短地址，只包括省份和城市
        public string GetShortAddr(string ids)
        {
            var list = db.ExecuteQuery<string>("exec [dbo].[T_Delivery_SetAdr] @sId = {0}", ids).Distinct().ToList();
            if (list.Count() > 1) throw new Exception("不同城市的送货单不能合并快递");
            return list.FirstOrDefault();
        }

        public List<CHExInfo> GetChExInfo(string ids, string stock_addr, string addr, int size_l, int size_w, int size_h, int cards_num)
        {
            var list = db.ExecuteQuery<CHExInfo>("exec [dbo].[T_Delivery_SelDelyNo] @sId = {0},@Adr = {1},@FLong = {2},@FWith = {3},@FTall = {4},@FQty = {5},@stockAddr = {6}", ids, addr, size_l, size_w, size_h, cards_num, stock_addr).ToList();
            return list.Where(l => l.FReed != null).ToList();
        }

        public void UpdateCHEx(string stockNos, string exName, string exNo, string exType, string exComment, decimal exFee)
        {
            var stockNoList = stockNos.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            var chs = db.CH_bill.Where(c => stockNoList.Contains(c.k3_stock_no)).ToList();
            foreach (var c in chs) {
                c.ex_name = exName;
                c.ex_no = exNo;
                c.ex_type = exType;
                c.ex_fee = exFee;
                c.ex_comment = exComment;
            }
            db.SubmitChanges();
        }

        public void UpdatePrintStatus()
        {
            bill.print_date = DateTime.Now;
            bill.is_print = true;

            db.SubmitChanges();
        }

        public void UpdateSignBack(DateTime day)
        {
            bill.back_sig_acc_date = DateTime.Now;
            bill.back_sig_date = day;
            bill.is_back_sig = true;

            db.SubmitChanges();
        }

        //会签扫描时检查出库单号是否存在，是的话返回流水号
        public CH_bill ValidateStockNo(string stockNo)
        {
            var h = db.CH_bill.Where(c => c.k3_stock_no == stockNo).FirstOrDefault();
            if (h == null) throw new Exception("此送货单号不存在，请检查");
            if (h.is_back_sig) throw new Exception("此送货单之前已被回签，不能再次操作");
            if (h.k3_out_time == null) throw new Exception("此送货单还未出库，不能回签");
            if (!h.is_print) throw new Exception("此送货单未打印，不能回签");

            return h;
        }

        public List<CHBackSignInfo> GetSignInfoList(DateTime fromDate, DateTime toDate, string stockNo, string customerName, string signStatus)
        {
            toDate = toDate.AddDays(1);
            var list = (from h in db.CH_bill
                        join e in db.CH_bill_detail on h.sys_no equals e.sys_no
                        where h.k3_import_time >= fromDate && h.k3_import_time < toDate
                        && h.k3_stock_no.Contains(stockNo)
                        && h.customer_name.Contains(customerName)
                        && h.k3_out_time != null
                        && e.real_qty > 0
                        orderby h.k3_import_time
                        select new CHBackSignInfo()
                        {
                            account = h.account,
                            back_sig_acc_date = h.back_sig_acc_date,
                            back_sig_date = h.back_sig_date,
                            customer_name = h.customer_name,
                            is_back_sig = h.is_back_sig ? "Y" : "N",
                            item_model = e.item_model,
                            item_name = e.item_name,
                            k3_import_date = h.k3_import_time,
                            k3_stock_no = h.k3_stock_no,
                            order_no = e.order_no,
                            order_entry_no = e.order_entry_no,
                            price = e.unit_price,
                            order_qty = e.order_qty,
                            real_qty = e.real_qty,
                            sys_no = h.sys_no,
                            total_price = e.unit_price * e.real_qty
                        });

            if (!"所有".Equals(signStatus)) {
                list = list.Where(r => (signStatus == "未回签" && r.is_back_sig == "N") || (signStatus == "已回签" && r.is_back_sig == "Y"));
            }

            return list.Take(1000).ToList();
        }

        public void ExportSignInfoExcel(List<CHBackSignInfo> data)
        {
            string[] colName = new string[] { "是否回签", "公司", "出库单号", "回签日期", "回签确认日期", "订单号", "订单行号", "客户名称", "规格型号", "数量", "销售单价", "销售金额" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("回签送货单_{0}.xls", DateTime.Now.ToShortDateString());
            Worksheet sheet = xls.Workbook.Worksheets.Add("回签信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colName.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(14 * 256);
                sheet.AddColumnInfo(col);
            }

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 1;

            //设置标题
            foreach (var name in colName) {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }

            foreach (var d in data) {
                colIndex = 1;

                //"是否回签", "出库单号", "回签日期","回签确认日期", "订单号","订单行号","客户名称","规格型号","数量","销售单价","销售金额"

                cells.Add(++rowIndex, colIndex, d.is_back_sig);
                cells.Add(rowIndex, ++colIndex, d.account);
                cells.Add(rowIndex, ++colIndex, d.k3_stock_no);
                cells.Add(rowIndex, ++colIndex, d.back_sig_date == null ? "" : ((DateTime)d.back_sig_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.back_sig_acc_date == null ? "" : ((DateTime)d.back_sig_acc_date).ToString("yyyy-MM-dd HH:mm"));
                cells.Add(rowIndex, ++colIndex, d.order_no);
                cells.Add(rowIndex, ++colIndex, d.order_entry_no);
                cells.Add(rowIndex, ++colIndex, d.customer_name);
                cells.Add(rowIndex, ++colIndex, d.item_model);
                cells.Add(rowIndex, ++colIndex, d.real_qty);
                cells.Add(rowIndex, ++colIndex, d.price);
                cells.Add(rowIndex, ++colIndex, d.total_price);
            }

            xls.Send();
        }

    }
}