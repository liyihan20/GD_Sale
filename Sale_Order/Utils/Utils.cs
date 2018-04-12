using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;
using System.IO;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Sale_Order.Utils
{
    public class SomeUtils
    {
        SaleDBDataContext db = new SaleDBDataContext();
        string fromStr = "SO0123456789MBFSC";
        string toStr = "_[QWERTYUIOP-)(];";

        //分配系统流水号
        //billType:单据类型；销售订单：SO；开模单：CM；免费样品：FS；收费样品：CS；修理单：MB；
        public string getSystemNo(string billType)
        {
            string result = billType;
            DateTime dt = DateTime.Now;
            string date_str = dt.Year + string.Format("{0:00}", dt.Month) + string.Format("{0:00}", dt.Day);
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == billType && sn.date_string == date_str);
            if (maxRecord.Count() == 0)
            {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = billType,
                    date_string = date_str,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                result += date_str + "001";
            }
            else
            {
                var firstRecord = maxRecord.First();
                firstRecord.max_num = firstRecord.max_num + 1;
                result += date_str + string.Format("{0:000}", firstRecord.max_num);
            }
            db.SubmitChanges();
            return result;
        }

        //获得退货流水号
        public string getReturnSystemNo(string area)
        {
            string billType = area + "TH";
            string result = billType;
            DateTime dt = DateTime.Now;
            string date_str = dt.ToString("yyMMdd");
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == billType && sn.date_string == date_str);
            if (maxRecord.Count() == 0)
            {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = billType,
                    date_string = date_str,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                result += date_str + "01";
            }
            else
            {
                var firstRecord = maxRecord.First();
                firstRecord.max_num = firstRecord.max_num + 1;
                result += date_str + string.Format("{0:00}", firstRecord.max_num);
            }
            db.SubmitChanges();
            return result;
        }
        
        //获得换货流水号
        public string getResendSystemNo(string sys_no) {
            var count = db.ResendBill.Where(r => r.sys_no.StartsWith(sys_no)).Count();
            return sys_no + "_" + (count + 1).ToString();
        }

        //获得备料单单号
        public string getBLbillNo(string marketName,string busDepName,int userId)
        {
            //2018年后编码新规则
            if (DateTime.Now >= DateTime.Parse("2018-1-1")) {
                return getBLbillNo2008(marketName, busDepName, userId);
            }

            string prefix = "G";
            switch (marketName) {
                case "MDS市场部":
                    prefix += "MDS";
                    break;
                case "IDS市场部":
                    prefix += "IDS";
                    break;
                case "CDS市场部":
                    prefix += "CDS";
                    break;
                case "AUT市场部":
                    prefix += "AUT";
                    break;
                case "新加坡市场部":
                    prefix += "XJP";
                    break;
                case "CCM车载市场部":
                    prefix += "VCCM";
                    break;
            }
            prefix += "BL" + DateTime.Now.ToString("yy") + "-";
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == "BL" && sn.date_string == prefix);
            if (maxRecord.Count() == 0) {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = "BL",
                    date_string = prefix,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                prefix += "0001";
            }
            else {
                var firstRecord = maxRecord.First();
                firstRecord.max_num = firstRecord.max_num + 1;
                prefix += string.Format("{0:0000}", firstRecord.max_num);
            }
            db.SubmitChanges();


            return busDepName.Contains("客服") ? prefix + "KF" : prefix;
        }
        public string getBLbillNo2008(string marketName, string busDepName, int userId)
        {
            string prefix = "G";
            string agencyValue = "", marketValue = "/";
            string agencyName = db.User.Single(u => u.id == userId).Department1.name;
            string[] agencyNameArr = new string[] { "汕尾本部", "深圳", "上海", "北京", "光能", "杭州","新加坡" };
            string[] agencyValueArr = new string[] { "SZ", "SZ", "SH", "BJ", "GN", "HZ", "XJP" };
            string[] marketNameArr = new string[] { "汕尾本部", "MDS", "IDS", "CDS", "AUT", "新加坡", "CCM","SDB" };
            string[] marketValueArr = new string[] { "MDS", "MDS", "IDS", "CDS", "AUT", "", "VCCM","SDB" };

            for (var i =0;i<agencyNameArr.Length;i++) {
                if (agencyName.Contains(agencyNameArr[i])) {
                    agencyValue = agencyValueArr[i];
                    break;
                }
            }
            prefix += agencyValue;

            for (var i = 0; i < marketNameArr.Length; i++) {
                if (agencyName.Contains(marketNameArr[i])) {
                    marketValue = marketValueArr[i];
                    break;
                }
            }
            if (marketValue.Equals("/")) {
                switch (marketName) {
                    case "MDS市场部":
                        marketValue = "MDS";
                        break;
                    case "IDS市场部":
                        marketValue = "IDS";
                        break;
                    case "CDS市场部":
                        marketValue = "CDS";
                        break;
                    case "AUT市场部":
                        marketValue = "AUT";
                        break;
                    case "新加坡市场部":
                        marketValue = "XJP";
                        break;
                    case "CCM车载市场部":
                        marketValue = "VCCM";
                        break;
                    case "SDB市场部":
                        marketValue = "SDB";
                        break;
                }
            }
            prefix += marketValue;    

            prefix += "BL" + DateTime.Now.ToString("yy") + "-";
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == "BL" && sn.date_string == prefix);
            if (maxRecord.Count() == 0) {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = "BL",
                    date_string = prefix,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                prefix += "0001";
            }
            else {
                var firstRecord = maxRecord.First();
                firstRecord.max_num = firstRecord.max_num + 1;
                prefix += string.Format("{0:0000}", firstRecord.max_num);
            }
            db.SubmitChanges();


            return busDepName.Contains("客服") ? prefix + "KF" : prefix;
        }

        //样品单编号
        public string getYPBillNo(string currencyNo, bool isFree)
        {
            string prefix = "";
            int shortYear = int.Parse(DateTime.Now.ToString("yy"));
            if (!"RMB".Equals(currencyNo)) {
                prefix = "H";
            }
            if (isFree) {
                prefix += "GYPMF";
            }
            else {
                prefix += "GSWYP";
            }
            if ("RMB".Equals(currencyNo)) {
                prefix += "-" + shortYear;
            }
            else {
                //2018年为j，2019年为k，以此类推
                prefix += (char)(((int)'J' + (shortYear - 18)) > 'Z' ? 'A' : ((int)'J' + (shortYear - 18)));
            }
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == "YP" && sn.date_string == prefix);
            if (maxRecord.Count() == 0) {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = "YP",
                    date_string = prefix,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                prefix += "0001";
            }
            else {
                var firstRecord = maxRecord.First();
                firstRecord.max_num = firstRecord.max_num + 1;
                prefix += string.Format("{0:0000}", firstRecord.max_num);
            }
            db.SubmitChanges();

            return prefix;
        }

        //取得步骤名称
        public string getStepName(int step)
        {

            string stepName = "";
            switch (step)
            {
                case 1:
                    stepName = "办事处一审";
                    break;
                case 2:
                    stepName = "办事处二审";
                    break;
                case 3:
                    stepName = "市场部一审";
                    break;
                case 4:
                    stepName = "市场部二审";
                    break;
                default:
                    stepName = "出错";
                    break;
            }
            return stepName;
        }
        public string getStepName(int step, string tpy)
        {
            var pro = db.Process.Where(p => p.bill_type == tpy && p.is_finish == true);
            if (pro.Count() < 1)
            {
                return "";
            }
            return pro.First().ProcessDetail.Where(pd => pd.step == step).First().step_name;
        }
        
        public string getOrderType(int? typ) {            
            return getBillType(typ.ToString());
        }
        public string getBillType(string typ)
        {
            string orderType = "";
            if (typ.Length < 2) return "";
            switch (typ.Substring(0,2))
            {
                case "SO":
                case "1":
                    orderType = "销售订单";
                    break;
                case "CC":
                    orderType = "CCM开改模单";
                    break;
                case "CM":
                case "3":
                    orderType = "开模改模单";
                    break;
                case "SB":
                case "4":
                    orderType = "样品单";
                    break;
                case "BL":
                case "5":
                    orderType = "备料单";
                    break;
                case "TH":
                case "6":
                    orderType = "退换货申请";
                    break;
                case "HH":
                case "7":
                    orderType = "换货申请";
                    break;
                case "CH":
                case "8":
                    orderType = "出货申请";
                    break;
                case "PJ":
                case "9":
                    orderType = "客户立项";
                    break;
            }
            return orderType;
        }

        //获取佣金率
        public double? GetCommissionRate(string proType, double? MU)
        {
            double? result = 0;
            db.getCommissionRate(proType, MU, ref result);
            return result;
        }


        //通过系统流水号获得已经转移到正式文件夹的附件路径，退换货
        public static string getTHOrderPath(string sysNum)
        {
            string p = ConfigurationManager.AppSettings["AttachmentPath2"];
            string p1 = sysNum.Substring(2, 2);
            string p2 = sysNum.Substring(0, 2);
            string p3 = sysNum.Substring(4, 2);
            string p4 = sysNum.Substring(6, 2);
            string p5 = sysNum.Substring(8, 2);
            string path = Path.Combine(p, p1, p2, p3, p4, p5);
            return path;
        }

        //通过系统流水号获得已经转移到正式文件夹的附件路径，SO单
        public static string getOrderPath(string sysNum)
        {
            string p = ConfigurationManager.AppSettings["AttachmentPath2"];
            string p1 = sysNum.Substring(0, 2);
            string p2 = sysNum.Substring(2, 4);
            string p3 = sysNum.Substring(6, 2);
            string p4 = sysNum.Substring(8, 2);
            string path = Path.Combine(p, p1, p2, p3, p4);
            return path;
        }

        //将附件移动到正式文件夹
        public void moveToFormalDir(string sysNum) {
            string fileName = sysNum + ".rar";
            string oldPath = Path.Combine(ConfigurationManager.AppSettings["AttachmentPath1"], fileName);
            if (System.IO.File.Exists(oldPath))
            {
                //1. 移动附件到正式目录
                FileInfo info = new FileInfo(oldPath);
                string newPath = SomeUtils.getOrderPath(sysNum);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                string newFile=Path.Combine(newPath, fileName);
                //如果正式目录已存在，则先删除
                if (File.Exists(newFile)) {
                    File.Delete(newFile);
                }
                info.MoveTo(newFile);

                //2. 将附件信息带到附件表，表明此流水号带附件。 2015-9-24
                var att = new HasAttachment();
                att.sys_no = sysNum;
                db.HasAttachment.InsertOnSubmit(att);
                db.SubmitChanges();
            }
        }

        //发送邮件通知下一审批环节负责人
        public bool emailToNextAuditor(int applyId) {
            bool sendEmail = bool.Parse(ConfigurationManager.AppSettings["SendEmail"]);
            if (!sendEmail)
                return true;

            Apply app = db.Apply.Single(ap => ap.id == applyId);
            User saler = app.User;
            string sysNo = app.sys_no;
            string operateType = "新增";
            string orderType = getBillType(app.order_type);
            string orderNo = null;

            if (app.success != null)
            {
                string ccEmail = null;
                List<User> prAuditors = null;

                if (app.order_type.Equals("SB")) {
                    var order = db.SampleBill.Where(m => m.sys_no == app.sys_no).First();
                    orderNo = order.bill_no;
                    prAuditors = app.ApplyDetails.Where(ad => ad.step == 1 && (ad.step_name.Contains("项目管理员") || ad.step_name.Contains("项目经理") || ad.step_name.Contains("项目组上级"))).Select(ad => ad.User).ToList();
                }
                if (app.order_type.Equals("CC")) {
                    var order = db.CcmModelContract.Where(m => m.sys_no == app.sys_no).First();
                    orderNo = order.old_bill_no;
                    prAuditors = app.ApplyDetails.Where(ad => ad.step == 1 && (ad.step_name.Contains("项目管理员") || ad.step_name.Contains("项目经理") || ad.step_name.Contains("项目组上级"))).Select(ad => ad.User).ToList();
                }
                if (app.order_type.Equals("CM")) {
                    var order = db.ModelContract.Where(m => m.sys_no == app.sys_no).First();
                    orderNo = order.old_bill_no;
                    prAuditors = app.ApplyDetails.Where(ad => ad.step == 1 && (ad.step_name.Contains("项目管理员") || ad.step_name.Contains("项目经理") || ad.step_name.Contains("项目组上级"))).Select(ad => ad.User).ToList();
                }
                if (app.order_type.Equals("BL")) {
                    var order = db.Sale_BL.Where(s => s.sys_no == app.sys_no).First();
                    orderNo = order.bill_no;
                    prAuditors = app.ApplyDetails.Where(ad => ad.step_name.Contains("计划审批") || ad.step_name.Contains("订料") || ad.step_name.Contains("运作中心")).Select(ad => ad.User).Distinct().ToList();
                    prAuditors.AddRange((from v in db.vw_auditor_relations
                                         join u in db.User on v.auditor_id equals u.id
                                         where v.step_name == "BL_事业部接单员"
                                         && v.department_name == order.bus_dep
                                         select u).ToList());
                    prAuditors.Add(db.User.Single(u => u.real_name == "林莲杏"));
                }
                if (app.order_type.Equals("TH")) {
                    prAuditors = app.ApplyDetails.Where(ad => ad.step_name.Contains("客服")).Select(ad => ad.User).Distinct().ToList();
                }

                if (prAuditors != null)
                {
                    foreach (var prAuditor in prAuditors)
                    {
                        var prEmail = prAuditor.email;
                        if (ccEmail == null)
                            ccEmail = prEmail;
                        else
                            ccEmail += "," + prEmail;
                    }
                }
                return MyEmail.SendBackToSaler((bool)app.success, sysNo, saler.email, orderType, operateType, ccEmail, orderNo, app.p_model);
            }
            else
            {
                var apdetails = app.ApplyDetails.Where(ad => ad.pass != null);
                int step = 1;
                if (apdetails.Count() > 0) {
                    step = (int)apdetails.OrderByDescending(ad => ad.step).First().step + 1;
                }
                var nextDetails = app.ApplyDetails.Where(ad => ad.step == step);
                var stepName = nextDetails.First().step_name;
                string nextEmails = "";
                string returnUrl = MyUrlEncoder("Audit/BeginAudit?step=" + step + "&applyId=" + applyId);
                foreach (var dt in nextDetails) {
                    if (!string.IsNullOrEmpty(nextEmails))
                        nextEmails += ",";  //多收件人要用逗号隔开，MSDN的文档写的是分号...
                    nextEmails += dt.User.email;
                }
                
                return MyEmail.SendToNext(sysNo, saler.real_name,saler.Department1.name, nextEmails, orderType, operateType, stepName,returnUrl);
            }
        }
        //public bool EmailToNextPeople(int applyId) {
        //    Apply app = db.Apply.Single(ap => ap.id == applyId);
        //    User saler = app.User;
        //    string sysNo = "";
        //    string orderType = "";
        //    string operateType = "新增";
        //    switch (app.order_type) { 
        //        case "SO": //销售订单 OrderTp->Order
        //            sysNo = app.Order.sys_no;
        //            orderType = "销售订单";
        //            break;
        //        case "CS": //收费样品单
        //            sysNo = app.Order.sys_no;
        //            orderType = "收费样品单";
        //            break;
        //        case "MB": //维修单
        //            sysNo = app.ContractTp.sys_no;
        //            orderType = "维修单";
        //            break;
        //        case "CM": //开模改模单
        //            sysNo = app.ModelContract.sys_no;
        //            orderType = "开模改模单";
        //            break;
        //        case "FS": //免费样品单
        //            sysNo = app.ModelContract.sys_no;
        //            orderType = "免费样品单";
        //            break;
        //    }
        //    if (app.success == false)
        //    {
        //        return MyEmail.SendBackToSaler(false, sysNo, saler.email,orderType,operateType);
        //    }
        //    else if (app.success == true) {
        //        return MyEmail.SendBackToSaler(true, sysNo, saler.email, orderType, operateType);
        //    }
        //    else
        //    {
        //        int detailCount = app.ApplyDetails.Count();
        //        int step = detailCount;
        //        if (step > 4)
        //            return false;
        //        var chars = saler.Department1.Charger.Where(c => c.step == step);
        //        if (chars.Count() < 1)
        //            return false;
        //        string nextEmails = "";
        //        foreach (Charger ch in chars) {
        //            if (!string.IsNullOrEmpty(nextEmails))
        //                nextEmails += ",";//多收件人要用逗号隔开，MSDN的文档写的是分号...
        //            nextEmails += ch.User.email;
        //        }
        //        return MyEmail.SendToNext(sysNo, saler.real_name,"", nextEmails,orderType,operateType,"","");
        //    }
        //}

        //发送邮件通知下一审批环节负责人
        //public bool EmailToNextPeopleUpdate(int updateId,string operate_type)
        //{
        //    SaleUpdateApply app = db.SaleUpdateApply.Single(s => s.id == updateId);
        //    string billNo = app.bill_no;
        //    User saler = app.User;
        //    string orderType="";
        //    string operateType = operate_type.Equals("M") ? "变更" : "取消";
        //    switch (app.bill_type) { 
        //        case "SO":
        //            orderType = "销售订单";
        //            break;
        //        case "MB":
        //            orderType = "维修单";
        //            break;
        //        case "CM":
        //            orderType = "开模改模单";
        //            break;
        //        case "FS":
        //            orderType = "免费样品单";
        //            break;
        //        case "CS":
        //            orderType = "收费样品单";
        //            break;
        //    }
        //    if (app.UpdateAudit.Where(ad => ad.pass == false).Count() > 0)
        //    {
        //        return MyEmail.SendBackToSaler(false, billNo, saler.email, orderType, operateType);
        //    }
        //    else if (app.success == true)
        //    {
        //        return MyEmail.SendBackToSaler(true, billNo, saler.email, orderType, operateType);
        //    }
        //    else
        //    {
        //        int detailCount = app.UpdateAudit.Count();
        //        int step = detailCount;
        //        if (step > 4)
        //            return false;
        //        var chars = saler.Department1.Charger.Where(c => c.step == step);
        //        if (chars.Count() < 1)
        //            return false;
        //        string nextEmails = "";
        //        foreach (Charger ch in chars)
        //        {
        //            if (!string.IsNullOrEmpty(nextEmails))
        //                nextEmails += ",";//多收件人要用逗号隔开，MSDN的文档写的是分号...
        //            nextEmails += ch.User.email;
        //        }
        //        return MyEmail.SendToNextUpdate(billNo, saler.real_name, nextEmails,orderType,operateType);
        //    }
        //}

        //发送挂起的通知邮件
        public bool emailForBlock(int applyId, int auditor_id, string reason) {
            bool sendEmail = bool.Parse(ConfigurationManager.AppSettings["SendEmail"]);
            if (!sendEmail)
                return true;
            Apply app = db.Apply.Single(ap => ap.id == applyId);
            User saler = app.User;
            string sysNo = app.sys_no;
            string operateType = "新增";
            string orderType = getBillType(app.order_type);
            User auditor = db.User.Single(a => a.id == auditor_id);
            return MyEmail.SendBackToSalerForBlock(sysNo, saler.email, orderType, operateType, auditor.real_name, reason,app.p_model);
        }

        //发送审核失败邮件给前一位审核者，重新审核
        public bool emailToPrevious(int applyId, int previousStep, string reason, string auditorName)
        {
            bool sendEmail = bool.Parse(ConfigurationManager.AppSettings["SendEmail"]);
            if (!sendEmail)
                return true;


            Apply app = db.Apply.Single(ap => ap.id == applyId);

            var previousAuditors = app.ApplyDetails.Where(p => p.step == previousStep);
            string sysNo = app.sys_no;
            string operateType = "新增";
            string orderType = getBillType(app.order_type);

            string nextEmails = "";
            foreach (ApplyDetails ad in previousAuditors)
            {
                if (!string.IsNullOrEmpty(nextEmails))
                    nextEmails += ",";//多收件人要用逗号隔开，MSDN的文档写的是分号...
                nextEmails += ad.User.email;
            }
            return MyEmail.SendBackToPreviousAuditor(sysNo, nextEmails, orderType, operateType, auditorName, reason);
        }

        //获取业务员ID
        public int? getEmployeeId(string name)
        {
            var emp = db.getClerk(name, 1).ToList();
            if (emp.Count() > 0)
                return emp.First().id;
            else
                return null;
        }

        //获取客户ID
        public int? getCustomerId(string name)
        {
            var cus = db.getCostomer(name, 1).ToList();
            if (cus.Count() > 0)
                return cus.First().id;
            else
                return null;
        }

        //获取saler ID
        public int? getSalerId(string name) {
            var saler = db.getSaler(name, 1).ToList();
            if (saler.Count() > 0)
                return saler.First().id;
            else
                return null;
        }

        //获取Items ID
        public int? getItemsId(string what, string name)
        {
            var item = db.vwItems.Where(i => i.what == what && i.fname == name).ToList();
            if (item.Count() > 0)
                return item.First().interid;
            else
                return null;
        }

        //获取产品ID
        public int? getProductId(string model, string name)
        {
            var pro = db.vwProductInfo.Where(p => p.item_model == model && p.item_name == name).ToList();
            if (pro.Count() > 0)
            {
                return pro.First().item_id;
            }
            else
                return null;
        }
        
        //获取K3用户ID
        public int? getK3UserId(string name) {
            var users = db.vwK3User.Where(u => u.name == name);
            if (users.Count() > 0)
                return users.First().id;
            else
                return null;
        }

        //生成随机数列
        public string CreateValidateNumber(int length)
        {
            //去掉数字0和字母o，因为不容易区分
            string Vchar = "0,1,2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p" +
            ",q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q" +
            ",R,S,T,U,V,W,X,Y,Z";

            string[] VcArray = Vchar.Split(new Char[] { ',' });//拆分成数组
            string num = "";

            int temp = -1;//记录上次随机数值，尽量避避免生产几个一样的随机数

            Random rand = new Random();
            //采用一个简单的算法以保证生成随机数的不同
            for (int i = 1; i < length + 1; i++)
            {
                if (temp != -1)
                {
                    rand = new Random(i * temp * unchecked((int)DateTime.Now.Ticks));
                }

                int t = rand.Next(61);
                if (temp != -1 && temp == t)
                {
                    return CreateValidateNumber(length);

                }
                temp = t;
                num += VcArray[t];
            }
            return num;
        }

        //生成验证码图片
        public byte[] CreateValidateGraphic(string validateCode)
        {
            Bitmap image = new Bitmap((int)Math.Ceiling(validateCode.Length * 18.0), 26);
            Graphics g = Graphics.FromImage(image);
            try
            {
                //生成随机生成器
                Random random = new Random();
                //清空图片背景色
                g.Clear(Color.White);
                //画图片的干扰线
                for (int i = 0; i < 25; i++)
                {
                    int x1 = random.Next(image.Width);
                    int x2 = random.Next(image.Width);
                    int y1 = random.Next(image.Height);
                    int y2 = random.Next(image.Height);
                    g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
                }
                Font font = new Font("Arial", 16, (FontStyle.Bold | FontStyle.Italic));
                LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height),
                 Color.Blue, Color.DarkRed, 1.2f, true);
                g.DrawString(validateCode, font, brush, 3, 2);
                //画图片的前景干扰点
                for (int i = 0; i < 100; i++)
                {
                    int x = random.Next(image.Width);
                    int y = random.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                //画图片的边框线
                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);
                //保存图片数据
                MemoryStream stream = new MemoryStream();
                image.Save(stream, ImageFormat.Jpeg);
                //输出图片流
                return stream.ToArray();
            }
            finally
            {
                g.Dispose();
                image.Dispose();
            }
        }

        //查找对应的K3销售订单字段
        public string getK3Segement(string key,string type) {
            return (string)HttpContext.GetGlobalResourceObject(type + "_Segement", key);
        }

        //update so 订单，字段名称转化方法，返回字符串
        public string parseSoSegment(List<UpdateInfos> uis,string bill_no) {
            string result = "";
            StringBuilder sb = null;
            var uis_0 = uis.Where(u => u.entry_id == null).ToList();
            var uis_1 = uis.Where(u => u.entry_id != null).ToList();
            //var exchange_rate = db.vwK3SaleOrder.Where(v => v.bill_no == bill_no).First().exchange_rate;
            //表体拼凑语句
            if (uis_1.Count() > 0)
            {
                sb = new StringBuilder("update ");
                sb.Append(ConfigurationManager.AppSettings["DbPath"]);
                sb.Append("SEOrderEntry set ");
                for (var i = 0; i < uis_1.Count(); i++)
                {
                    if (i == 0)
                    {
                        sb.Append(translateSoValue(uis_1[i].ename, uis_1[i].after_value));
                    }
                    else
                    {
                        sb.Append("," + translateSoValue(uis_1[i].ename, uis_1[i].after_value));
                    }
                }
                sb.Append(" where FEntryId =" + uis_1.First().entry_id
                    + " and FInterId in (select FInterId from " + ConfigurationManager.AppSettings["DbPath"]
                    + "SEOrder where FBIllNo='" + bill_no + "');");

                result += sb.ToString();
                sb = null;
            }
            //表头update拼凑语句            
            if (uis_0.Count() > 0)
            {
                sb = new StringBuilder("update ");
                sb.Append(ConfigurationManager.AppSettings["DbPath"]);
                sb.Append("SEOrder set ");
                for (var i = 0; i < uis_0.Count(); i++)
                {
                    if (i == 0)
                    {
                        sb.Append(translateSoValue(uis_0[i].ename, uis_0[i].after_value));
                    }
                    else
                    {
                        sb.Append("," + translateSoValue(uis_0[i].ename, uis_0[i].after_value));
                    }
                }
                sb.Append(" where FBillNo = '" + bill_no + "';");
                result += sb.ToString();
                sb = null;
            }
            return result;
        }

        //转换so字段的值
        public string translateSoValue(string key,string value) {
            string result = getK3Segement(key,"SO");
            switch (key) { 
                case "trade_type":
                case "order_type":
                case "clearing_way":
                case "product_type":
                case "project_group":
                case "agency":
                    result += " = " + getItemsId(key, value).ToString();
                    break;
                case "sale_way":
                    result += " = " + getItemsId("sale_type", value).ToString();
                    break;
                case "buy_unit":
                case "oversea_client":
                case "final_client":
                case "plan_firm":
                    result += " = " + getCustomerId(value);
                    break;
                case "backpaper_confirm":
                case "produce_way":
                case "print_truly":
                case "client_logo":
                    result += " = " + getItemsId("YesOrNo", value).ToString();
                    break;
                case "charger":
                case "clerk":
                    result += " = " + getEmployeeId(value);
                    break;
                case "qty":
                    result += " = " + value + ",fauxqty = " + value;
                    break;
                case "unit_Price":
                    result += " = " + value + ",fauxprice = " + value;
                    break;
                case "aux_tax_price":
                    result += " = " + value + ",FAuxPriceDiscount = " + value;
                    break;
                default:
                    result += " = '" + value +"'";
                    break;
            }
            return result;
        }

        //update 修理单，字段名称转化方法，返回字符串
        public string parseMBSegment(List<UpdateInfos> uis, string bill_no)
        {
            string result = "";
            string dbName = ConfigurationManager.AppSettings["DbPath"];
            StringBuilder sb = null;
            var uis_0 = uis.Where(u => u.entry_id == null).ToList();
            var uis_1 = uis.Where(u => u.entry_id != null).ToList();
            //var exchange_rate = db.vwK3SaleOrder.Where(v => v.bill_no == bill_no).First().exchange_rate;
            //表头update拼凑语句            
            if (uis_0.Count() > 0)
            {
                sb = new StringBuilder("update ");
                sb.Append(dbName);
                sb.Append("t_RPContract set ");
                for (var i = 0; i < uis_0.Count(); i++)
                {
                    if (i == 0)
                    {
                        sb.Append(translateMBValue(uis_0[i].ename, uis_0[i].after_value));
                    }
                    else
                    {
                        sb.Append("," + translateMBValue(uis_0[i].ename, uis_0[i].after_value));
                    }
                }
                sb.Append(" where FContractNo = '" + bill_no + "';");
                result += sb.ToString();
                sb = null;
            }
            //表体拼凑语句
            if (uis_1.Count() > 0)
            {
                result += "update t1 set t1.FAmount = t2.FTotalAmount,t1.FAmountFor = t2.FTotalAmountFor from " +
                    dbName + "t_RPContractScheme t1 left join " + dbName +
                    "t_RPContract t2 on t1.FContractID = t2.FContractID where t2.FContractNo = '" + bill_no + "'; ";
                sb = new StringBuilder("update ");
                sb.Append(dbName);
                sb.Append("t_RPContractEntry set ");
                for (var i = 0; i < uis_1.Count(); i++)
                {
                    if (i == 0)
                    {
                        sb.Append(translateMBValue(uis_1[i].ename, uis_1[i].after_value));
                    }
                    else
                    {
                        sb.Append("," + translateMBValue(uis_1[i].ename, uis_1[i].after_value));
                    }
                }
                sb.Append(" where FIndex = " + uis_1.First().entry_id
                    + " and FContractID in (select FContractID from " + dbName
                    + "t_RPContract where FContractNo='" + bill_no + "');");

                result += sb.ToString();
                sb = null;
            }
            
            return result;
        }

        //update 开模改模单，字段名称转化方法，返回字符串
        public string parseCMSegment(List<UpdateInfos> uis, string bill_no)
        {
            string result = "";
            string dbName = ConfigurationManager.AppSettings["DbPath"];
            StringBuilder sb = null;
            var uis_0 = uis.Where(u => u.entry_id == null).ToList();
            var uis_1 = uis.Where(u => u.entry_id != null).ToList();
            //var exchange_rate = db.vwK3SaleOrder.Where(v => v.bill_no == bill_no).First().exchange_rate;
            //表头update拼凑语句            
            if (uis_0.Count() > 0)
            {
                sb = new StringBuilder("update ");
                sb.Append(dbName);
                sb.Append("t_BOSContract set ");
                for (var i = 0; i < uis_0.Count(); i++)
                {
                    if (i == 0)
                    {
                        sb.Append(translateCMValue(uis_0[i].ename, uis_0[i].after_value));
                    }
                    else
                    {
                        sb.Append("," + translateCMValue(uis_0[i].ename, uis_0[i].after_value));
                    }
                }
                sb.Append(" where FContractNo = '" + bill_no + "';");
                result += sb.ToString();
                sb = null;
            }
            //表体拼凑语句
            if (uis_1.Count() > 0)
            {
                sb = new StringBuilder("update ");
                sb.Append(dbName);
                sb.Append("t_BOSContractEntry1 set ");
                for (var i = 0; i < uis_1.Count(); i++)
                {
                    if (i == 0)
                    {
                        sb.Append(translateCMValue(uis_1[i].ename, uis_1[i].after_value));
                    }
                    else
                    {
                        sb.Append("," + translateCMValue(uis_1[i].ename, uis_1[i].after_value));
                    }
                }
                sb.Append(" where FIndex = " + uis_1.First().entry_id
                    + " and FID in (select FID from " + dbName
                    + "t_BOSContract where FContractNo='" + bill_no + "');");

                result += sb.ToString();
                sb = null;
            }

            return result;
        }

        //转换修理单字段的值
        public string translateMBValue(string key, string value)
        {
            string result = getK3Segement(key, "MB");
            switch (key)
            {
                case "department":
                    result += " = " + getItemsId("agency", value).ToString();
                    break;
                case "customer":
                    result += " = " + getCustomerId(value);
                    break;
                case "employee":
                    result += " = " + getEmployeeId(value);
                    break;
                default:
                    result += " = '" + value + "'";
                    break;
            }
            return result;
        }

        //转换开模单字段的值
        public string translateCMValue(string key, string value)
        {
            string result = getK3Segement(key, "CM");
            switch (key)
            {
                case "trade_type":
                case "product_type":
                    result += " = " + getItemsId(key, value).ToString();
                    break;
                case "currency_id":
                    result += " = " + getItemsId("currency", value).ToString();
                    break;
                case "clear_way":
                    result += " = " + getItemsId("clearing_way", value).ToString();
                    break;
                case "sale_way":
                    result += " = " + getItemsId("sale_type", value).ToString();
                    break;
                case "customer":
                case "zj_customer":
                case "zz_customer":
                case "plan_firm":
                    result += " = " + getCustomerId(value);
                    break;
                case "department_id":
                    result += " = " + getItemsId("agency", value).ToString();
                    break;
                case "employee_id":
                    result += " = " + getEmployeeId(value);
                    break;
                default:
                    result += " = '" + value + "'";
                    break;
            }
            return result;
        }

        public string getMD5(string str)
        {
            //HashAlgorithm hash = HashAlgorithm.Create("MD5");
            //byte[] result = hash.ComputeHash(Encoding.Default.GetBytes(str));
            //return BitConverter.ToString(result);
            if (str.Length > 2)
            {
                str = "Who" + str.Substring(2) + "Are" + str.Substring(0, 2) + "You";
            }
            else
            {
                str = "Who" + str + "Are" + str + "You";
            }
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = Encoding.Default.GetBytes(str);
            byte[] result = md5.ComputeHash(data);
            String ret = "";
            for (int i = 0; i < result.Length; i++)
            {
                ret += result[i].ToString("x").PadLeft(2, '0');
            }
            return ret;

        }

        //记录系统产生的一些错误
        public void writeDownErrors(string username, string err) {
            if (err.Length > 1000) {
                err = err.Substring(0, 1000);
            }
            db.SystemErrors.InsertOnSubmit(new SystemErrors() { 
                user_name=username,
                op_time=DateTime.Now,
                exception=err
            });
            db.SubmitChanges();
        }

        //验证登录密码，2013-5-27后开启复杂性密码，强制用户修改。
        public string validatePassword(string str)
        {
            string alph = @"[A-Za-z]+";
            string num = @"\d+";
            string cha = @"[\-`=\\\[\];',\./~!@#\$%\^&\*\(\)_\+\|\{\}:""<>\?]+";
            if (!new Regex(alph).IsMatch(str))
            {
                return "新密码必须包含英文字母，保存失败。英文字母有：A~Z，a~z";
            }
            if (!new Regex(num).IsMatch(str))
            {
                return "新密码必须包含阿拉伯数字，保存失败。数字有：0~9";
            }
            if (!new Regex(cha).IsMatch(str))
            {
                return @"新密码必须包含特殊字符，保存失败。特殊字符有：-`=\[];',./~!@#$%^&*()_+|{}:""<>?";
            }
            return "";
        }

        //将unicode编码为GBK
        public string EncodeToGBK(string url)
        {
            string result = System.Web.HttpUtility.UrlEncode(url, System.Text.Encoding.GetEncoding("GB2312"));
            return result;
        }

        //将中文编码为utf-8
        public string EncodeToUTF8(string str)
        {
            string result = System.Web.HttpUtility.UrlEncode(str, System.Text.Encoding.GetEncoding("UTF-8"));
            return result;
        }

        //将utf-8解码
        public string DecodeToUTF8(string str)
        {
            string result = System.Web.HttpUtility.UrlDecode(str, System.Text.Encoding.GetEncoding("UTF-8"));
            return result;
        }

        //生成随机不重复字符串
        public string getRandString(int strLen) {
            string strs = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string result = "";
            char tempStr;
            Random rand= new Random(Environment.TickCount);            
            while (result.Length < strLen) {
                tempStr = strs.ToArray()[rand.Next(strs.Length)];
                if (result.IndexOf(tempStr) < 0) {
                    result += tempStr;
                }
            }
            return result;
        }

        //模拟双色球
        public string getColorBalls(int num) {
            string result = "";
            string tempStr;
            
            Random rand = new Random(Environment.TickCount);
            for (int j = 0; j < num; j++)
            {
                for (int i = 0; i < 7; i++)
                {
                    tempStr = rand.Next(36).ToString();
                    if (result.IndexOf(tempStr) < 0)
                    {
                        if (i == 5)
                        {
                            result += tempStr + "|";
                        }
                        else
                        {
                            result += tempStr + " ";
                        }
                    }
                    else
                    {
                        i--;
                    }
                }                
                result += "<br/>";
            }            
            return result;
        }

        //记录操作日志
        public void writeEventLog(string model, string log, string sysNum, HttpRequestBase request, int unusual = 0)
        {
            if (request == null || request.Cookies["order_cookie"] == null) return;
            db.EventLog.InsertOnSubmit(new EventLog()
            {
                @event = log,
                ip = request.UserHostAddress,
                op_time = DateTime.Now,
                username = DecodeToUTF8(request.Cookies["order_cookie"]["username"]),
                model = model,
                sysNum = sysNum,
                unusual = unusual
            });
            db.SubmitChanges();
        }

        //某人是否有某权限
        public bool hasGotPower(int userId, string powerName) {
            var power = from a in db.Authority
                        from u in db.Group
                        from ga in a.GroupAndAuth
                        from gu in u.GroupAndUser
                        where ga.group_id == u.id
                        && gu.user_id == userId
                        && a.sname == powerName
                        select a;
            if (power.Count() > 0)
                return true;
            else
                return false;

        }

        //简单加密算法，加密流水号
        public string myEncript(string str)
        {
            string resultStr = "";
            int index;

            foreach (char s in str.ToCharArray())
            {
                index = fromStr.IndexOf(s);
                if (index < 0)
                {
                    resultStr += s;
                }
                else
                {
                    resultStr += toStr.Substring(index, 1);
                }
            }
            return resultStr;
        }

        //简单解密算法，解密流水号
        public string myDecript(string str)
        {
            string resultStr = "";
            int index;

            foreach (char s in str.ToCharArray())
            {
                index = toStr.IndexOf(s);
                if (index < 0)
                {
                    resultStr += s;
                }
                else
                {
                    resultStr += fromStr.Substring(index, 1);
                }
            }
            return resultStr;
        }

        //获取订单的内容类型
        public string getContantType(string sysNo) {
            string result = "";
            db.getOrderTypeBySysNo(sysNo, ref result);
            return result;
        }

        //根据流程代码以及其他信息获得审核序列
        public List<ApplyDetails> getApplySequence(Apply ap, string processType, int? applierId = null, int? departmentId = null, int? produceDepartmentId = null) {

            List<ApplyDetails> ads = new List<ApplyDetails>();
            
            var pros = db.Process.Where(p => p.bill_type == processType && p.is_finish == true && p.begin_time <= DateTime.Now && p.end_time >= DateTime.Now);
            if (pros.Count() < 1) {
                throw new Exception("流程不存在或过期，请联系管理员");
            }
            var pro = pros.First();
            int minusStep = 0;
            foreach (var det in pro.ProcessDetail.OrderBy(p=>p.step)) {
                var auditorRel = db.AuditorsRelation.Where(a => a.step_value == det.step_type);
                string relateType = auditorRel.First().relate_type;
                List<int?> auditors = new List<int?>();

                switch (relateType) {
                    case "固定人员":
                        auditors.Add(det.user_id);
                        break;
                    case "申请者":
                        auditors.Add(applierId);
                        break;
                    case "部门":
                        auditors = auditorRel.Where(a => a.relate_value == departmentId).Select(a => a.auditor_id).ToList();
                        break;
                    default:
                        //其它所有的都是以生产部门的id来查询
                        auditors = auditorRel.Where(a => a.relate_value == produceDepartmentId).Select(a => a.auditor_id).ToList();
                        break;
                }

                if (auditors.Count() < 1) {
                    if (det.can_be_null == false)
                    {
                        throw new Exception("步骤【" + det.step_name + "】审核人员不存在");
                    }
                    else if (det.countersign == null || det.countersign == false)
                    {
                        //该步骤的审核人为空且被允许，所以要将后续步骤-1
                        minusStep++;
                    }
                    continue;
                }

                foreach (int auditor in auditors) {
                    ads.Add(new ApplyDetails()
                    {
                        Apply = ap,
                        can_modify = det.can_modify,
                        can_select_next = det.can_select_next,
                        step = det.step - minusStep,
                        step_name = det.step_name,
                        user_id = auditor,
                        countersign = det.countersign
                    });
                }
            }
            
            return ads;
        }

        //升级版的流程引擎，需有事业部、项目组、办事处、报价员等等的审批
        public List<ApplyDetails> getApplySequence(Apply ap, string processType, int? applierId = null, Dictionary<string, int?> auditorsDic = null)
        {
            List<ApplyDetails> ads = new List<ApplyDetails>();

            var pros = db.Process.Where(p => p.bill_type == processType && p.is_finish == true && p.begin_time <= DateTime.Now && p.end_time >= DateTime.Now);
            if (pros.Count() < 1)
            {
                throw new Exception("流程不存在或过期，请联系管理员");
            }
            var pro = pros.First();     
            foreach (var det in pro.ProcessDetail.OrderBy(p => p.step))
            {
                var auditorRel = db.AuditorsRelation.Where(a => a.step_value == det.step_type);
                string relateType = auditorRel.First().relate_type;
                string stepName = auditorRel.First().step_name;
                List<int?> auditors = new List<int?>();
                int? dicValue = 0;

                switch (relateType)
                {
                    case "固定人员":
                        auditors.Add(det.user_id);
                        break;
                    case "申请者":
                        auditors.Add(applierId);
                        break;
                    default:
                        //其它除了从表单直接传过来的审核人，全部通过关联关系进行查询审核人。统一使用关联类型+“ID"这个变量名，值从上一级传进来
                        string realteTypeID = relateType + "ID";
                        if (auditorsDic.TryGetValue(realteTypeID, out dicValue))
                        {
                            if (relateType.StartsWith("表单"))
                            {
                                //如果以表单开头，表示是从表单直接选择审核人员，那么不用关联直接加入审核人
                                auditors.Add(dicValue);
                            }
                            else
                            {
                                auditors = auditorRel.Where(a => a.relate_value == dicValue).Select(a => a.auditor_id).ToList();
                            }
                        }
                        break;
                }

                if (auditors.Count() < 1)
                {
                    if (det.can_be_null == false)
                    {
                        throw new Exception("步骤【" + det.step_name + "】审核人员不存在");
                    }
                    continue;
                }                

                foreach (int auditor in auditors)
                {
                    ads.Add(new ApplyDetails()
                    {
                        Apply = ap,
                        can_modify = det.can_modify,
                        step = det.step,
                        step_name = det.countersign == true ? det.step_name + "(" + stepName.Substring(stepName.IndexOf("_") + 1).Split(new string[] { "审", "负" }, StringSplitOptions.None)[0] + ")" : det.step_name,
                        user_id = auditor,
                        countersign = det.countersign
                    });
                }
            }

            //2017-9-9 对缺失的step重新排序，比如现在是124457，确认缺失step3和6，将3以后的step-1，再将6以后的step也减1
            int maxStep = (int)ads.Max(a => a.step);
            List<int> lostSteps = new List<int>();
            for (int st = 1; st < maxStep; st++) {
                if (ads.Where(a => a.step == st).Count() == 0) {
                    lostSteps.Add(st);
                }
            }
            if (lostSteps.Count() > 0) {
                foreach (int s in lostSteps) {
                    foreach (var ad in ads.Where(a => a.step > s)) {
                        ad.step = ad.step - 1;
                    }
                }
            }

            return ads;
        }


        //获得测试的流程
        public List<ApplyDetails> getTestApplySequence(Apply ap, string processType, int applierId)
        {
            List<ApplyDetails> list = new List<ApplyDetails>();
            var pro = db.Process.Where(p => p.bill_type == processType && p.is_finish == true && p.begin_time <= DateTime.Now && p.end_time >= DateTime.Now).First();
            foreach (var det in pro.ProcessDetail.Select(a => new { step = a.step, stepName = a.step_name, canModify = a.can_modify, countersign = a.countersign }).Distinct())
            {
                list.Add(new ApplyDetails()
                    {
                        Apply = ap,
                        can_modify = det.canModify,
                        step = det.step,
                        step_name = det.stepName,
                        user_id = applierId,
                        countersign = det.countersign
                    });
            }
            return list;
        }

        //在流程中某步骤之后插入步骤
        public bool InsertStepAfterStep(int applyId, int beforeStep, string stepName, int?[] stepAuditor, bool canModify = false, bool canSelectNext = false) {

            if (stepAuditor.Count() == 0)
                return true;

            //将后续步骤+1
            foreach (var ad in db.ApplyDetails.Where(a => a.apply_id == applyId && a.step > beforeStep)) {
                ad.step++;
            }

            //插入新步骤
            ApplyDetails newAd;
            foreach (var auditId in stepAuditor)
            {
                if (auditId != null)
                {
                    newAd = new ApplyDetails();
                    newAd.apply_id = applyId;
                    newAd.step = beforeStep + 1;
                    newAd.step_name = stepName;
                    newAd.user_id = auditId;
                    newAd.can_modify = canModify;
                    newAd.can_select_next = canSelectNext;
                    newAd.parent_step = beforeStep;//增加母步骤，表明是这个母步骤生成的。2015-3-6

                    db.ApplyDetails.InsertOnSubmit(newAd);
                }
            }

            try
            {
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                writeDownErrors("系统，插入步骤", "失败；apply_id:" + applyId.ToString() + "error:" + ex.ToString());
                return false;
            }

            return true;
        }

        //在流程最后添加步骤
        public bool AppendStepAtLast(int applyId, string stepName, int?[] stepAuditor, int parentStep = 0, bool canModify = false, bool canSelectNext = false, bool countersign = false)
        {
            int maxStep = (int)db.ApplyDetails.Where(a => a.apply_id == applyId).Max(a => a.step);
            //没有人即退出
            if (stepAuditor.Count() == 0)
                return true;

            ApplyDetails newAd;
            foreach (var auditId in stepAuditor) {
                if (auditId != null) {
                    newAd = new ApplyDetails();
                    newAd.apply_id = applyId;
                    newAd.step = maxStep + 1;
                    newAd.step_name = stepName;
                    newAd.user_id = auditId;
                    newAd.can_modify = canModify;
                    newAd.can_select_next = canSelectNext;
                    newAd.parent_step = parentStep;
                    newAd.countersign = countersign;

                    db.ApplyDetails.InsertOnSubmit(newAd);
                }
            }

            try {
                db.SubmitChanges();
            }
            catch (Exception ex) {
                writeDownErrors("系统，添加步骤到末尾", "失败；apply_id:" + applyId.ToString() + "error:" + ex.ToString());
                return false;
            }

            return true;
        }

        //删除某母步骤生成的子步骤
        public bool RemoveChildrenStep(int applyId, int parentStep)
        {
            //计算子步骤的数量，去除重复子步骤（则同一步骤多人审核的情况）
            int childrenCount = db.ApplyDetails.Where(a => a.apply_id == applyId && a.parent_step == parentStep).Select(a=>a.step).Distinct().Count();

            if (childrenCount < 1) 
                return true;

            //将后续步骤减去子步骤的数量
            foreach (var det in db.ApplyDetails.Where(a => a.apply_id == applyId && a.step > parentStep && (a.parent_step == null || a.parent_step != parentStep))) {
                det.step -= childrenCount;
            }

            //删除子步骤
            db.ApplyDetails.DeleteAllOnSubmit(db.ApplyDetails.Where(a => a.apply_id == applyId && a.parent_step == parentStep));

            try
            {
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                writeDownErrors("系统，删除子步骤", "失败；apply_id:" + applyId.ToString()+";母步骤：" + parentStep.ToString() + "error:" + ex.ToString());
                return false;
            }
            return true;
        }

        public string ModelToString<T>(T t)
        {
            string result = "";
            foreach (var p in t.GetType().GetProperties())
            {
                var name = p.Name;
                var value = p.GetValue(t, null);
                string strType = p.PropertyType.FullName;
                //id和user_id不需要，因为要验证是否修改过
                if (name.Equals("id")
                    || name.Equals("user_id")
                    || name.Equals("step_version")
                    || name.Equals("original_user_id")
                    || name.Equals("update_user_id")
                    || name.Equals("create_date"))
                {
                    continue;
                }
                if (strType.Contains("String") || strType.Contains("Int32") || strType.Contains("Decimal") || strType.Contains("DateTime"))
                {
                    if (value != null)
                    {
                        result += name + ":" + value + ";";
                    }
                }
            }

            return result;
        }

        public string ModelsToString<T>(List<T> ts)
        {
            string result = "[";
            foreach (var t in ts) {
                result += "{";
                foreach (var p in t.GetType().GetProperties()) {
                    var name = p.Name;
                    var value = p.GetValue(t, null);
                    string strType = p.PropertyType.FullName;
                    //id和user_id不需要，因为要验证是否修改过
                    if (name.Equals("id")
                        || name.Equals("bl_id")
                        || name.Equals("user_id")
                        || name.Equals("step_version")
                        || name.Equals("original_user_id")
                        || name.Equals("update_user_id")
                        || name.Equals("create_date")) {
                        continue;
                    }
                    if (strType.Contains("String") || strType.Contains("Int32") || strType.Contains("Decimal") || strType.Contains("DateTime")) {
                        if (value != null) {
                            result += name + ":" + value + ";";
                        }
                    }
                }
                result += "},";
            }
            result += "]";
            return result;
        }

        /// <summary>
        /// 使用反射将表单的值设置到数据库对象中，根据字段名
        /// </summary>
        /// <param name="col">表单</param>
        /// <param name="obj">数据库对象</param>
        public void SetFieldValueToModel(FormCollection col, object obj)
        {

            foreach (var p in obj.GetType().GetProperties())
            {
                string val = col.Get(p.Name);//字段值
                string pType = p.PropertyType.FullName;//数据类型
                if (string.IsNullOrEmpty(val) || val.Equals("null")) continue;
                if (pType.Contains("DateTime"))
                {
                    DateTime dt;
                    if (DateTime.TryParse(val, out dt))
                    {
                        p.SetValue(obj, dt, null);
                    }
                }
                else if (pType.Contains("Int32"))
                {
                    int i;
                    if (int.TryParse(val, out i))
                    {
                        p.SetValue(obj, i, null);
                    }
                }
                else if (pType.Contains("Decimal"))
                {
                    decimal dm;
                    if (decimal.TryParse(val, out dm))
                    {
                        p.SetValue(obj, dm, null);
                    }
                }
                else if (pType.Contains("String"))
                {
                    p.SetValue(obj, val.Trim(), null);
                }
                else if (pType.Contains("Bool"))
                {
                    bool bl;
                    if (bool.TryParse(val, out bl))
                    {
                        p.SetValue(obj, bl, null);
                    }
                }
            }
        }

        //保存ccm开改模单
        public string saveCCMModelContract(FormCollection col, int stepVersion, int userId)
        {
            CcmModelContract mc = new CcmModelContract();
            mc.step_version = stepVersion;

            SetFieldValueToModel(col, mc);

            //下单组审批，检查订单号的合法性
            if (stepVersion == 4)
            {
                if (string.IsNullOrWhiteSpace(mc.old_bill_no))
                {
                    return "下单组审核必须填写订单号，保存失败。";
                }
                else if (db.CcmModelContract.Where(m => m.sys_no != mc.sys_no && m.old_bill_no == mc.old_bill_no).Count() > 0)
                {
                    return "订单号在下单系统已存在，保存失败。";
                }
                else
                {
                    bool? isExistedInK3 = null;
                    db.isDublicatedBillNo(mc.old_bill_no, "CM", ref isExistedInK3);
                    if (isExistedInK3 == true)
                    {
                        return "订单编号在K3已经存在，保存失败。";
                    }
                }
                //合法性检查
                //检查物料编码
                if (string.IsNullOrEmpty(mc.product_number))
                {
                    var prod = db.vwProductInfo.Where(v => v.item_model == mc.product_model);
                    if (prod.Count() != 1)
                    {
                        return "物料型号在K3不存在或者不唯一，请重新选择";
                    }
                    else
                    {
                        mc.product_number = prod.First().item_no;
                        mc.product_name = prod.First().item_name;
                        mc.product_unit = prod.First().unit_number;
                    }
                }
            }
            if (db.Department.Where(d => d.name == mc.project_team && d.dep_type == "研发项目组").Count() != 1) {
                return "研发项目组不存在或不唯一";
            }
            if (db.vwItems.Where(v => v.fname == mc.clear_way && v.what == "clearing_way").Count() < 1)
            {
                return "支付方式[" + mc.clear_way + "]不存在，保存失败。";
            }
            if (db.getClerk(mc.clerk_no, 1).Count() < 1)
            {
                return "业务员[" + mc.clerk_no + "]不存在，保存失败。";
            }
            {
                //对几个客户的名称和编号进行判断
                try
                {
                    if (db.customerNameAndNoIsFit(mc.customer_no, mc.customer_name).Count() < 1)
                    {
                        var customer = db.getCostomer(mc.customer_name, 1).ToList();
                        if (customer.Count() == 1)
                        {
                            mc.customer_no = customer.First().number;
                        }
                        else
                        {
                            return "购货单位不存在或不唯一，保存失败。";
                        }
                    }
                    if (db.customerNameAndNoIsFit(mc.plan_firm_no, mc.plan_firm_name).Count() < 1)
                    {
                        var customer = db.getCostomer(mc.plan_firm_name, 1).ToList();
                        if (customer.Count() == 1)
                        {
                            mc.plan_firm_no = customer.First().number;
                        }
                        else
                        {
                            return "方案公司不存在或不唯一，保存失败。";
                        }
                    }
                    if (db.customerNameAndNoIsFit(mc.zz_customer_no, mc.zz_customer_name).Count() < 1)
                    {
                        var customer = db.getCostomer(mc.zz_customer_name, 1).ToList();
                        if (customer.Count() == 1)
                        {
                            mc.zz_customer_no = customer.First().number;
                        }
                        else
                        {
                            return "最终客户不存在或不唯一，保存失败。";
                        }
                    }
                    if (db.customerNameAndNoIsFit(mc.oversea_customer_no, mc.oversea_customer_name).Count() < 1)
                    {
                        var customer = db.getCostomer(mc.oversea_customer_name, 1).ToList();
                        if (customer.Count() == 1)
                        {
                            mc.oversea_customer_no = customer.First().number;
                        }
                        else
                        {
                            return "海外客户不存在或不唯一，保存失败。";
                        }
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            {
                if ("GN".Equals(mc.fetch_add_no))
                {
                    //内销  
                    if (!mc.customer_no.StartsWith("01."))
                    {
                        return "内销单的购货单位必须是01开头的，保存失败。";
                    }
                    if (!mc.customer_no.Equals(mc.oversea_customer_no))
                    {
                        return "内销单的购货单位和海外客户不一致，保存失败。";
                    }
                    if (!"03".Equals(mc.trade_type_no))
                    {
                        return "交货汕尾的贸易类型必须为国内贸易，保存失败。";
                    }
                }
                else if ("HK".Equals(mc.fetch_add_no))
                {
                    //外销                    
                    if (!"04.204".Equals(mc.customer_no))
                    {
                        return "外销单的购货单位必须是香港信利光电有限公司（04.204），保存失败。";
                    }
                    if (!mc.oversea_customer_no.StartsWith("05."))
                    {
                        return "外销单的海外客户必须是05开头的，保存失败。";
                    }
                    if (!"02".Equals(mc.trade_type_no))
                    {
                        return "交货香港的贸易类型必须为一般贸易，保存失败。";
                    }
                }
                else
                {
                    return "交货地点必须是汕尾货仓或者香港货仓";
                }                
            }
            if ("*".Equals(mc.clear_way)) {
                return "支付方式不能设置为【*】，保存失败。";
            }
            //设置一些值
            try
            {
                //如果还未提交，表示是营业员保存的，那么user_id就是当前用户id，否则就要取Apply表的用户id
                var app = db.Apply.Where(a => a.sys_no == mc.sys_no);
                if (app.Count() < 1)
                {
                    mc.user_id = userId;
                }
                else
                {
                    mc.user_id = app.First().user_id;
                }
                
                mc.clerk_name = db.getClerk(mc.clerk_no, 1).First().name;
                mc.agency_name = db.vwItems.Where(v => v.what == "agency" && v.fid == mc.agency_no).First().fname;
                mc.currency_name = db.vwItems.Where(v => v.what == "currency" && v.fid == mc.currency_no).First().fname;
                mc.fetch_add_name = db.vwItems.Where(v => v.what == "delivery_place" && v.fid == mc.fetch_add_no).First().fname;
                mc.trade_type_name = db.vwItems.Where(v => v.what == "trade_type" && v.fid == mc.trade_type_no).First().fname;
                mc.quotation_clerk_name = db.User.Where(u => u.id == mc.quotation_clerk_id).First().real_name;
                mc.clear_way_no = db.vwItems.Where(v => v.fname == mc.clear_way && v.what == "clearing_way").First().fid;
            }
            catch (Exception ex)
            {
                return "表头的其他名称值设置发生错误" + ex.Message.ToString();
            }

            db.CcmModelContract.InsertOnSubmit(mc);

            var exitedInDb = db.CcmModelContract.Where(m => m.sys_no == mc.sys_no);
            if (exitedInDb.Count() > 0)
            {
                var exitedMc = exitedInDb.First();
                //先备份
                BackupData bd = new BackupData();
                bd.user_id = exitedMc.user_id;
                bd.op_date = DateTime.Now;
                bd.sys_no = exitedMc.sys_no;
                bd.main_data = ModelToString(exitedMc);
                db.BackupData.InsertOnSubmit(bd);

                //然后删除   
                db.CcmModelContract.DeleteAllOnSubmit(exitedInDb);

                //将规格型号更新到申请表
                if (stepVersion > 0)
                {
                    var ap = db.Apply.Single(a => a.sys_no == mc.sys_no);
                    ap.p_model = mc.product_model;
                }
            }

            //提交到数据库
            try
            {
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                
                return ex.Message.ToString();
            }

            return "";
        }

        //保存开改模单
        public string saveModelContract(FormCollection col, int stepVersion, int userId)
        {
            ModelContract mc = new ModelContract();
            mc.step_version = stepVersion;

            SetFieldValueToModel(col, mc);

            int? salerId; //营业ID，即下单者ID
            if (stepVersion == 0)
            {
                salerId = userId;
            }
            else
            {
                salerId = db.Apply.Where(a => a.sys_no == mc.sys_no).First().user_id;
            }

            //下单组审批，检查订单号的合法性
            if (stepVersion == 4)
            {
                if (string.IsNullOrWhiteSpace(mc.old_bill_no))
                {
                    return "下单组审核必须填写订单号，保存失败。";
                }
                else if (db.CcmModelContract.Where(m => m.sys_no != mc.sys_no && m.old_bill_no == mc.old_bill_no).Count() > 0)
                {
                    return "订单号在下单系统已存在，保存失败。";
                }
                else
                {
                    bool? isExistedInK3 = null;
                    db.isDublicatedBillNo(mc.old_bill_no, "CM", ref isExistedInK3);
                    if (isExistedInK3 == true)
                    {
                        return "订单编号在K3已经存在，保存失败。";
                    }
                }
                //合法性检查
                //检查物料编码
                if (string.IsNullOrEmpty(mc.product_number))
                {
                    var prod = db.vwProductInfo.Where(v => v.item_model == mc.product_model);
                    if (prod.Count() != 1)
                    {
                        return "物料型号在K3不存在或者不唯一，请重新选择";
                    }
                    else
                    {
                        mc.product_number = prod.First().item_no;
                        mc.product_name = prod.First().item_name;
                        mc.product_unit = prod.First().unit_number;
                    }
                }
            }
            if (db.vwItems.Where(v => v.fname == mc.clear_way && v.what == "clearing_way").Count() < 1)
            {
                return "支付方式[" + mc.clear_way + "]不存在，保存失败。";
            }
            if (db.getClerk(mc.clerk_no, 1).Count() < 1)
            {
                return "业务员[" + mc.clerk_no + "]不存在，保存失败。";
            }
            {
                //对几个客户的名称和编号进行判断
                try
                {
                    if (db.customerNameAndNoIsFit(mc.customer_no, mc.customer_name).Count() < 1)
                    {
                        var customer = db.getCostomer(mc.customer_name, 1).ToList();
                        if (customer.Count() == 1)
                        {
                            mc.customer_no = customer.First().number;
                        }
                        else
                        {
                            return "购货单位不存在或不唯一，保存失败。";
                        }
                    }
                    if (db.customerNameAndNoIsFit(mc.plan_firm_no, mc.plan_firm_name).Count() < 1)
                    {
                        var customer = db.getCostomer(mc.plan_firm_name, 1).ToList();
                        if (customer.Count() == 1)
                        {
                            mc.plan_firm_no = customer.First().number;
                        }
                        else
                        {
                            return "方案公司不存在或不唯一，保存失败。";
                        }
                    }
                    if (db.customerNameAndNoIsFit(mc.zz_customer_no, mc.zz_customer_name).Count() < 1)
                    {
                        var customer = db.getCostomer(mc.zz_customer_name, 1).ToList();
                        if (customer.Count() == 1)
                        {
                            mc.zz_customer_no = customer.First().number;
                        }
                        else
                        {
                            return "最终客户不存在或不唯一，保存失败。";
                        }
                    }
                    if (db.customerNameAndNoIsFit(mc.oversea_customer_no, mc.oversea_customer_name).Count() < 1)
                    {
                        var customer = db.getCostomer(mc.oversea_customer_name, 1).ToList();
                        if (customer.Count() == 1)
                        {
                            mc.oversea_customer_no = customer.First().number;
                        }
                        else
                        {
                            return "海外客户不存在或不唯一，保存失败。";
                        }
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            {
                if ("RMB".Equals(mc.currency_no))
                {
                    //内销
                    if (!"GN".Equals(mc.fetch_add_no)) {
                        return "内销单的送货地点必须是汕尾";
                    }
                    if (!mc.customer_no.StartsWith("01."))
                    {
                        return "内销单的购货单位必须是01开头的，保存失败。";
                    }
                    if (!mc.customer_no.Equals(mc.oversea_customer_no))
                    {
                        return "内销单的购货单位和海外客户不一致，保存失败。";
                    }
                    if (!"03".Equals(mc.trade_type_no))
                    {
                        return "内销单的贸易类型必须为国内贸易，保存失败。";
                    }
                }
                else if ("USD".Equals(mc.currency_no) || "HKD".Equals(mc.currency_no)) 
                {
                    //外销                    
                    if(!"HK".Equals(mc.fetch_add_no)){
                        return "外销单的送货地点必须是香港";
                    }
                    if (!"04.204".Equals(mc.customer_no))
                    {
                        return "外销单的购货单位必须是香港信利光电有限公司（04.204），保存失败。";
                    }
                    if (!mc.oversea_customer_no.StartsWith("05."))
                    {
                        return "外销单的海外客户必须是05开头的，保存失败。";
                    }
                    if (!"02".Equals(mc.trade_type_no))
                    {
                        return "外销单的贸易类型必须为一般贸易，保存失败。";
                    }
                }
                else
                {
                    return "币别必须为人民币、港币或美元";
                }
            }
            if ("*".Equals(mc.clear_way))
            {
                return "支付方式不能设置为【*】，保存失败。";
            }
            //设置一些值
            try
            {
                //如果还未提交，表示是营业员保存的，那么user_id就是当前用户id，否则就要取Apply表的用户id                
                mc.original_user_id = salerId;
                mc.update_user_id = userId;                

                mc.clerk_name = db.getClerk(mc.clerk_no, 1).First().name;
                mc.agency_name = db.vwItems.Where(v => v.what == "agency" && v.fid == mc.agency_no).First().fname;
                mc.currency_name = db.vwItems.Where(v => v.what == "currency" && v.fid == mc.currency_no).First().fname;
                mc.fetch_add_name = db.vwItems.Where(v => v.what == "delivery_place" && v.fid == mc.fetch_add_no).First().fname;
                mc.trade_type_name = db.vwItems.Where(v => v.what == "trade_type" && v.fid == mc.trade_type_no).First().fname;
                mc.quotation_clerk_name = db.User.Where(u => u.id == mc.quotation_clerk_id).First().real_name;
                mc.clear_way_no = db.vwItems.Where(v => v.fname == mc.clear_way && v.what == "clearing_way").First().fid;
            }
            catch (Exception ex)
            {
                return "表头的其他名称值设置发生错误" + ex.Message.ToString();
            }

            db.ModelContract.InsertOnSubmit(mc);

            var exitedInDb = db.ModelContract.Where(m => m.sys_no == mc.sys_no);
            if (exitedInDb.Count() > 0)
            {
                var exitedMc = exitedInDb.First();
                //先备份
                BackupData bd = new BackupData();
                bd.user_id = exitedMc.update_user_id ;
                bd.op_date = DateTime.Now;
                bd.sys_no = exitedMc.sys_no;
                bd.main_data = ModelToString(exitedMc);
                db.BackupData.InsertOnSubmit(bd);

                //然后删除   
                db.ModelContract.DeleteAllOnSubmit(exitedInDb);

                //将规格型号更新到申请表
                if (stepVersion > 0) {
                    var ap = db.Apply.Single(a => a.sys_no == mc.sys_no);
                    ap.p_model = mc.product_model;
                }

            }

            //提交到数据库
            try
            {
                db.SubmitChanges();
            }
            catch (Exception ex)
            {

                return ex.Message.ToString();
            }

            return "";
        }

        //保存样品单
        public string saveSampleBill(FormCollection col, int stepVersion, int userId)
        {
            SampleBill sb = new SampleBill();
            sb.step_version = stepVersion;
            SetFieldValueToModel(col, sb);//将表单值设置到对象

            int? salerId; //营业ID，即下单者ID
            if (stepVersion == 0)
            {
                salerId = userId;
            }
            else
            {
                salerId = db.Apply.Where(a => a.sys_no == sb.sys_no).First().user_id;
            }

            //下单组审批，检查订单号的合法性,2018年开始自动生成编号
            //if (stepVersion == 4)
            //{
            //    if (string.IsNullOrWhiteSpace(sb.bill_no))
            //    {
            //        return "下单组审核必须填写订单号，保存失败。";
            //    }
            //    else if (db.SampleBill.Where(m => m.sys_no != sb.sys_no && m.bill_no == sb.bill_no).Count() > 0)
            //    {
            //        return "订单号在下单系统已存在，保存失败。";
            //    }
            //    else
            //    {
            //        bool? isExistedInK3 = null;
            //        db.isDublicatedBillNo(sb.bill_no, "SB", ref isExistedInK3);
            //        if (isExistedInK3 == true)
            //        {
            //            return "订单编号在K3已经存在，保存失败。";
            //        }
            //    }
            //}

            //检查字段合法性
            //方案公司、终端公司、海外客户
            if (string.IsNullOrEmpty(sb.plan_firm_no))
            {
                var tmpCustomer = db.getCostomer(sb.plan_firm_name, 1).ToList();
                if (tmpCustomer.Count() > 0)
                {
                    sb.plan_firm_no = tmpCustomer.First().number;
                }
            }
            if (string.IsNullOrEmpty(sb.zz_customer_no))
            {
                var tmpCustomer = db.getCostomer(sb.zz_customer_name, 1).ToList();
                if (tmpCustomer.Count() > 0)
                {
                    sb.zz_customer_no = tmpCustomer.First().number;
                }
            }
            if (string.IsNullOrEmpty(sb.sea_customer_no))
            {
                var tmpCustomer = db.getCostomer(sb.sea_customer_name, 1).ToList();
                if (tmpCustomer.Count() > 0)
                {
                    sb.sea_customer_no = tmpCustomer.First().number;
                }
            }
            if (sb.is_free.Equals("收费"))
            {
                if (sb.deal_price == null)
                {
                    return "收费样品必须填写[成交价]";
                }
                if (sb.contract_price == null)
                {
                    return "收费样品必须填写[合同价]";
                }
                if (sb.quotation_clerk_id == null)
                {
                    return "收费样品必须选择[报价员]";
                }
                if (string.IsNullOrEmpty(sb.quote_num))
                {
                    return "收费样品必须填写[报价编号]";
                }
                if (string.IsNullOrEmpty(sb.is_special_sample))
                {
                    return "收费样品必须填写[是否为特殊样品单]";
                }
            }

            if (string.IsNullOrEmpty(sb.product_model))
            {
                return "规格型号不能为空";
            }
            if (db.vwItems.Where(v => v.fname == sb.clear_way && v.what == "clearing_way").Count() < 1)
            {
                return "支付方式[" + sb.clear_way + "]不存在，保存失败。";
            }
            if (db.getCostomer(sb.customer_no, 1).Count() < 1)
            {
                return "购货单位[" + sb.customer_no + "]不存在，保存失败。";
            }
            if (string.IsNullOrEmpty(sb.plan_firm_no) || db.getCostomer(sb.plan_firm_no, 1).Count() < 1)
            {
                return "方案公司[" + sb.plan_firm_name + "]不存在，保存失败。";
            }
            if (string.IsNullOrEmpty(sb.zz_customer_no) || db.getCostomer(sb.zz_customer_no, 1).Count() < 1)
            {
                return "终端客户[" + sb.zz_customer_name + "]不存在，保存失败。";
            }
            if (string.IsNullOrEmpty(sb.sea_customer_no) || db.getCostomer(sb.sea_customer_no, 1).Count() < 1)
            {
                return "海外客户[" + sb.sea_customer_name + "]不存在，保存失败。";
            }
            if (db.getClerk(sb.clerk_no, 1).Count() < 1)
            {
                return "业务员[" + sb.clerk_no + "]不存在，保存失败。";
            }
            {                
                if (sb.currency_no.Equals("RMB"))
                {
                    if (sb.is_free.Equals("收费") && !sb.sale_type_no.Equals("FXF03"))
                    {
                        return "币别为RMB的收费样品，[销售方式]必须是分期收款销售，保存失败";
                    }
                    if (sb.is_free.Equals("免费") && !sb.sale_type_no.Equals("FXF01"))
                    {
                        return "币别为RMB的免费样品，[销售方式]必须是现销，保存失败";
                    }
                    if (!sb.trade_type_no.Equals("03"))
                    {
                        return "币别为RMB的[贸易类型]必须是国内贸易";
                    }
                    if (!sb.customer_no.StartsWith("01."))
                    {
                        return "币别为RMB的[购货单位]必须是01开头的";
                    }
                    if (!sb.sea_customer_no.StartsWith("01."))
                    {
                        return "币别为RMB的[海外客户]必须是01开头的";
                    }
                }
                else
                {
                    if (!sb.sale_type_no.Equals("FXF02"))
                    {
                        return "币别为非RMB的[销售方式]必须是赊销，保存失败";
                    }
                    if (!sb.trade_type_no.Equals("02"))
                    {
                        return "币别为非RMB的[贸易类型]必须是一般贸易";
                    }
                    if (!sb.customer_no.Equals("04.204"))
                    {
                        return "币别为非RMB的[购货单位]必须是香港信利光电有限公司";
                    }
                    if (DateTime.Now <= DateTime.Parse("2017-01-01"))
                    {
                        if (!sb.sea_customer_no.StartsWith("05."))
                        {
                            return "币别为非RMB的[海外客户]必须是05开头的";
                        }
                    }
                    else {
                        //2017年启用外销单，办事处与海外客户之间的约束
                        var user = db.User.Single(u => u.id == salerId);                        
                        string errorMsg = ValidateOverSearCustomerAndAgency(user.Department1.name, sb.sea_customer_no);
                        if (!string.IsNullOrEmpty(errorMsg))
                        {
                            return errorMsg;
                        }
                    }
                }
            }
            if (db.Department.Where(d => d.name == sb.project_team && d.dep_type == "研发项目组").Count() != 1) {
                return "研发项目组不存在或不唯一";
            }
            if (string.IsNullOrEmpty(sb.agency_no)) {
                return "办事处不存在";
            }
            //设置一些值
            try
            {
                //如果还未提交，表示是营业员保存的，那么user_id就是当前用户id，否则就要取Apply表的用户id                
                sb.original_user_id = salerId;
                sb.update_user_id = userId;
                sb.create_date = DateTime.Now;
                sb.customer_name = db.getCostomer(sb.customer_no, 1).First().name;
                sb.clerk_name = db.getClerk(sb.clerk_no, 1).First().name;
                sb.agency_name = db.vwItems.Where(v => v.what == "agency" && v.fid == sb.agency_no).First().fname;
                sb.currency_name = db.vwItems.Where(v => v.what == "currency" && v.fid == sb.currency_no).First().fname;
                sb.fetch_add_name = db.vwItems.Where(v => v.what == "delivery_place" && v.fid == sb.fetch_add_no).First().fname;
                sb.trade_type_name = db.vwItems.Where(v => v.what == "trade_type" && v.fid == sb.trade_type_no).First().fname;
                sb.sale_type_name = db.vwItems.Where(v => v.what == "sale_type" && v.fid == sb.sale_type_no).First().fname;
                sb.quotation_clerk_name = sb.quotation_clerk_id == null ? "" : db.User.Where(u => u.id == sb.quotation_clerk_id).First().real_name;
                if (sb.is_free.Equals("免费"))
                {
                    sb.total_sum = sb.sample_qty * sb.cost;
                    sb.deal_price = 0;
                    sb.contract_price = 0;
                }
                else
                {
                    sb.total_sum = sb.sample_qty * sb.contract_price;
                }

            }
            catch (Exception ex)
            {
                return "表头的其他名称值设置发生错误" + ex.Message.ToString();
            }

            var existedSBs = db.SampleBill.Where(s => s.sys_no == sb.sys_no);
            if (existedSBs.Count() > 0)
            {
                var existedSB = existedSBs.First();
                if (ModelToString(sb) == ModelToString(existedSB))
                {
                    return "";//表明保存的内容和之前的是一样的，不需要再保存一次
                }

                //备份
                BackupData bd = new BackupData();
                bd.sys_no = sb.sys_no;
                bd.main_data = ModelToString(existedSB);
                bd.op_date = DateTime.Now;
                bd.user_id = existedSB.update_user_id;
                db.BackupData.InsertOnSubmit(bd);

                //删除旧数据
                db.SampleBill.DeleteAllOnSubmit(existedSBs);

                //将规格型号更新到申请表
                if (stepVersion > 0)
                {
                    var ap = db.Apply.Single(a => a.sys_no == sb.sys_no);
                    ap.p_model = sb.product_model;
                }

            }

            //提交到数据库
            try
            {
                db.SampleBill.InsertOnSubmit(sb);
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }

            return "";
        }

        //保存备料单
        public string saveBLBill(FormCollection col, int stepVersion, int userId)
        {
            Sale_BL bl = new Sale_BL();
            bl.step_version = stepVersion;
            SetFieldValueToModel(col, bl);//将表单值设置到对象

            string details = col.Get("Sale_BL_details");
            if (!string.IsNullOrEmpty(details)) {
                List<Sale_BL_details> detailList = JsonConvert.DeserializeObject<List<Sale_BL_details>>(details); //将表体反序列化
                int entryNo = 1;
                foreach (var detail in detailList) {
                    if (detail.order_qty == null || detail.order_qty == 0) {
                        return "第" + entryNo + "行的订料数量必须填写且不能为0";
                    }
                    detail.entry_no = entryNo++;
                }
                bl.Sale_BL_details.AddRange(detailList);
            }

            int? salerId; //营业ID，即下单者ID
            if (stepVersion == 0) {
                salerId = userId;
            }
            else {
                salerId = db.Apply.Where(a => a.sys_no == bl.sys_no).First().user_id;
            }

            //验证字段合法性            
            if (string.IsNullOrEmpty(bl.product_model)) {
                return "产品型号不能为空";
            }
            if (string.IsNullOrEmpty(bl.bl_type)) {
                return "备料类型必须选择";
            }
            if ("有合同协议".Equals(bl.bl_type) && string.IsNullOrWhiteSpace(bl.bl_contract_no)) {
                return "有合同协议的协议号不能为空";
            }
            if (string.IsNullOrEmpty(bl.bl_project)) {
                return "备料项目至少应选择一项";
            }
            if (bl.bl_project.Contains("其它") && string.IsNullOrEmpty(bl.bl_project_other)) {
                return "备料项目选择了其它，请在相邻文本框中说明其它的内容";
            }
            if (string.IsNullOrEmpty(bl.clerk_no)) {
                return "营业员必须在列表中输入姓名或厂牌之后按回车键选择";
            }

            try {
                bl.original_user_id = salerId;
                bl.update_user_id = userId;
                bl.trade_type_name = db.vwItems.Where(v => v.what == "trade_type" && v.fid == bl.trade_type_no).First().fname;
                bl.clerk_name = db.getClerk(bl.clerk_no, 1).First().name;

                var existedBills = db.Sale_BL.Where(s => s.sys_no == bl.sys_no);
                if (existedBills.Count() > 0) {
                    var existed = existedBills.First();
                    //if (ModelToString(bl) == ModelToString(existed)) {
                    //    return "";//表明保存的内容和之前的是一样的，不需要再保存一次
                    //}

                    //备份
                    BackupData bd = new BackupData();
                    bd.sys_no = bl.sys_no;
                    bd.main_data = ModelToString(existed);
                    bd.secondary_data = ModelsToString<Sale_BL_details>(existed.Sale_BL_details.ToList());
                    bd.op_date = DateTime.Now;
                    bd.user_id = existed.update_user_id;
                    db.BackupData.InsertOnSubmit(bd);

                    //删除旧数据
                    db.Sale_BL_details.DeleteAllOnSubmit(existed.Sale_BL_details);
                    db.Sale_BL.DeleteAllOnSubmit(existedBills);

                    //将规格型号更新到申请表
                    if (stepVersion > 0) {
                        var ap = db.Apply.Single(a => a.sys_no == bl.sys_no);
                        ap.p_model = bl.product_model;
                    }
                }

                //提交到数据库                
                db.Sale_BL.InsertOnSubmit(bl);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        //外销单验证海外客户与办事处的关系
        public string ValidateOverSearCustomerAndAgency(string agencyName, string overseaNumber) {
            string[] agencyNameArr = new string[] { "上海", "深圳", "北京", "光能", "厦门", "新加坡", "中国市场部", "汕尾本部", "韩国", "杭州" };
            string[] customerNumberArr = new string[] { "05.21.", "05.22.", "05.23.", "05.24.", "05.25.", "05.26.", "05.27.", "05.28.", "05.29.", "05.30." };          
            //上海办：05.21.****
            //深圳办：05.22.****
            //北京办：05.23.****
            //光能办：05.24.****
            //厦门办：05.25.****
            //新加坡办：05.26.****
            //中国市场部：05.27.****   
            //汕尾本部：05.28.****
            //韩国办：05.29.****
            //杭州办：05.30.****
            for (var i = 0; i < agencyNameArr.Count(); i++) {
                if (agencyName.Contains(agencyNameArr[i]) && !overseaNumber.StartsWith(customerNumberArr[i])) {
                    return "外销单【" + agencyName + "】的海外客户必须是【" + customerNumberArr[i] + "】开头的";
                }
            }
            return "";
        }

        //验证勾稽状态，也就是是否已开蓝字是否已初始申请时的一样。有可能是在审核过程中发生改变，这种情况应该提示。
        public string ValidateHasInvoiceFlag(string sys_no)
        {
            var bill = db.ReturnBill.Where(r => r.sys_no == sys_no).First();
            var hasInvoice = (bool)bill.has_invoice;
            var entryFlag=false;
            foreach (var entry in bill.ReturnBillDetail)
            {
                var entrys = db.VWBlueStockBill.Where(v => v.FInterID == entry.stock_inter_id && v.FEntryID == entry.stock_entry_id);
                if(entrys.Count()<1){
                    return "行号是【" + entry.entry_no + "】的蓝字出库单已经推过红字单，操作失败！";
                }else{
                    entryFlag=entrys.First().FHookStatus == 2 ? true : false;
                }
                if (entryFlag != hasInvoice)
                {
                    return "行号是【" + entry.entry_no + "】的K3最新【勾稽状态】与【已开蓝字发票】状态不一致，操作失败！";
                }
                if (entry.real_return_qty > entrys.First().FcanApplyQty) {
                    return "行号是【" + entry.entry_no + "】的申请数量比K3的可操作数量大，可能的原因之一是申请过程中部分数量推了发票，操作失败！";
                }
            }
            return "";
        }

        public string MyUrlEncoder(string url)
        {
            return url.Replace("=", "(equal)").Replace("&", "(and)").Replace("/", "(slash)").Replace("?", "(ask)");
        }

        public string MyUrlDecoder(string url)
        {
            return url.Replace("(equal)", "=").Replace("(and)", "&").Replace("(slash)", "/").Replace("(ask)", "?");
        } 

    }
}