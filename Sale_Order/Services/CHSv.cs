using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;
using Sale_Order.Utils;

namespace Sale_Order.Services
{
    public class CHSv:BillSv
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
            bill.step_version = 0;
            bill.sys_no = GetNextSysNo(BillType);
            bill.account = account;


            return new CHModel() { head = bill };
        }

        public override object GetBill(int stepVersion, int userId)
        {
            bill.step_version = stepVersion;

            var details = db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList();
            //这里要做权限控制，根据userid，除了会计部，其他人不能查看价钱
            if (!new SomeUtils().hasGotPower(userId, "CanCheckCHPrice")) {
                details.ForEach(d => d.unit_price = 0);
            }

            return new CHModel() { head = bill, entrys = details, packages = db.CH_package.Where(c => c.sys_no == bill.sys_no).ToList() };
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            throw new NotImplementedException();
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
                              deal_price = 0,
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
            bill.step_version = 0;
            bill.sys_no = GetNextSysNo(BillType);

            return new CHModel() { head = bill, entrys = db.CH_bill_detail.Where(c => c.sys_no == bill.sys_no).ToList() };
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

        public override void ExportSalerExcle(SalerSearchParamModel pm, int userId)
        {
            throw new NotImplementedException();
        }

        public override void ExportAuditorExcle(AuditSearchParamModel pm, int userId)
        {
            throw new NotImplementedException();
        }

        public override void BeforeRollBack(int step)
        {
            throw new NotImplementedException();
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

    }
}