using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;
using Sale_Order.Utils;

namespace Sale_Order.Services
{
    public class MXSv:BillSv
    {
        private Sale_MX bill;

        public MXSv() { }
        public MXSv(string sysNo)
        {
            bill = db.Sale_MX.Where(m => m.sys_no == sysNo).First();
        }
        public override string BillType
        {
            get { return "MX"; }
        }

        public override string BillTypeName
        {
            get { return "Total solution"; }
        }

        public override string CreateViewName
        {
            get { return "CreateNMX"; }
        }

        public override string CheckViewName
        {
            get { return "CheckNMX"; }
        }

        public override string CheckListViewName
        {
            get { return "CheckNBillList"; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            bill = new Sale_MX();
            bill.step_version = 0;
            bill.sys_no = GetNextSysNo(BillType);
            bill.user_id = currentUser.userId;
            bill.user_name = currentUser.realName;
            bill.order_date = DateTime.Now;
            bill.percent1 = 100;
            
            return new MXModel() { head = bill, entrys = new List<Sale_MX_details>() };
        }

        public override object GetBill(int stepVersion, int userId)
        {
            bill.step_version = stepVersion;
            return new MXModel() { head = bill, entrys = db.Sale_MX_details.Where(d => d.sys_no == bill.sys_no).ToList() };
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            throw new NotImplementedException();
        }

        public override List<object> GetBillList(SalerSearchParamModel pm, int userId)
        {
            throw new NotImplementedException();
        }

        public override object GetNewBillFromOld()
        {
            throw new NotImplementedException();
        }

        public override string GetProcessNo()
        {
            return "MX";
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            Dictionary<string, int?> dic = new Dictionary<string, int?>();
            dic.Add("部门NO", db.User.Where(u => u.id == bill.user_id).First().Department1.dep_no);

            return dic;
        }

        public override string GetProductModel()
        {
            return db.Sale_MX_details.Where(d => d.sys_no == bill.sys_no).Select(d => d.item_modual).FirstOrDefault();
        }

        public override string GetCustomerName()
        {
            return bill.buy_unit_name;
        }

        public override string GetOrderNumber()
        {
            return "";
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.Sale_MX.Where(m => m.sys_no == sysNo).Count() > 0;
        }

        public override string GetSpecificBillTypeName()
        {
            return BillTypeName;
        }

        public override void DoWhenBeforeApply()
        {
            if (db.Sale_MX_details.Where(m => m.sys_no == bill.sys_no).Select(m => m.account).Distinct().Count() < 2) {
                throw new Exception("单据明细必须至少包含2个公司的订单");
            }
        }

        public override void DoWhenAfterApply()
        {

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
    }
}