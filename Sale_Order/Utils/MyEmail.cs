using System;
using System.Web.Helpers;

namespace Sale_Order.Utils
{
    public class MyEmail
    {
        static string server = "smtp.truly.com.cn";
        //static string semiServer = "smtp.truly.cn";
        static string webAddress = "http://192.168.90.100/SaleOrder";
        static string outAddress = "http://59.37.42.23/SaleOrder";
        //static string fsrMail = "fangsr.sale@truly.com.cn";
        static string accountParam = "&accountset=op";
        static string isInnerFrame = "&isInnerFrame=true";
        static string notInnerFrame = "&isInnerFrame=false";
        static string urlPrefix = "/Account/Login?url=";
        static string copName = "信利光电股份有限公司";
        static string username;
        static string bcc = "crm@truly.com.cn";
        //static string semiBcc = "crm@truly.cn";
        //static string adminEmail = "liyihan.ic@truly.com.cn";

        //发送邮件的包装方法，因为集团邮箱经常发送不出邮件，所以以后默认使用半导体邮箱。2013-6-7
        public static bool SendEmail(string content, string emailAddress, string ccEmail = null, string subject = "订单申请审批")
        {
            return TrulyEmail.EmailUtil.SemiSend("CRM客户关系管理系统", subject + "（光电）", content, emailAddress, ccEmail);
            //try
            //{
            //    WebMail.SmtpServer = semiServer;
            //    WebMail.SmtpPort = 25;
            //    WebMail.UserName = "crm";
            //    WebMail.From = "\"订单平台\"<crm@truly.cn>";
            //    WebMail.Password = "tic3006";
            //    WebMail.Send(
            //    to: emailAddress,
            //    cc: ccEmail,
            //    //bcc: semiBcc,
            //    subject: subject + "（光电）",
            //    body: content + "<br /><div style='clear:both'><hr />来自:移动销售平台订单系统<br />注意:此邮件是系统自动发送，请不要直接回复此邮件</div>",
            //    isBodyHtml: true
            //    );
            //}
            //catch (Exception ex)
            //{
            //    //如果发送失败，使用集团的邮箱发送。
            //    SomeUtils utl = new SomeUtils();
            //    utl.writeDownErrors(username, ex.Message.ToString());
            //    return UseSemiEmail(content, emailAddress);
            //}
            //return true;
        }

        //发送回订单申请者
        public static bool SendBackToSaler(bool isSuc, string sys_no, string salerEmailAdd, string orderType, string operateType, string ccEmail = null, string orderNo = null, string model = null)
        {
            SomeUtils utl = new SomeUtils();
            string content = "<div>你好:</div>";
            content += string.Format("<div style='margin-left:30px;'><div>你申请的流水号为{0}的单据已经处理完毕，结果如下。</div>", sys_no);
            string table = "<table style='width:400px;font-size:14px;' border='0' cellpadding='0' cellspacing='3'>";
            table += string.Format("<tr><td style='width:100px'>公司:</td><td style='width:300px'>{0}</td></tr>", copName);
            table += string.Format("<tr><td>订单类型:</td><td>{0}</td></tr>", orderType);
            if (!string.IsNullOrEmpty(orderNo))
            {
                table += string.Format("<tr><td>单据编号:</td><td>{0}</td></tr>", orderNo);
            }
            if (!string.IsNullOrEmpty(model))
            {
                table += string.Format("<tr><td>规格型号:</td><td>{0}</td></tr>", model);
            }
            table += string.Format("<tr><td>申请操作:</td><td>{0}</td></tr>", operateType);
            table += string.Format("<tr><td>申请结果:</td><td>{0}</td></tr>", isSuc ? "申请成功" : "申请失败");
            if (orderType.Equals("开模改模单"))
            {
                string url = sys_no.StartsWith("CM") ? utl.MyUrlEncoder("Files/printCMYFReport?sysNo=" + sys_no) : utl.MyUrlEncoder("Files/printCCYFReport?sysNo=" + sys_no);
                table += string.Format("<tr><td>PDF:</td><td><a href='{0}{1}{2}{3}{4}'>内网点此</a> <a href='{5}{1}{2}{3}{4}'>外网点此</a></td></tr>", webAddress, urlPrefix, url, accountParam, notInnerFrame, outAddress);
            }
            else if (orderType.Equals("样品单"))
            {
                string url = utl.MyUrlEncoder("Files/printSBYFReport?sysNo=" + sys_no);
                table += string.Format("<tr><td>PDF:</td><td><a href='{0}{1}{2}{3}{4}'>内网点此</a> <a href='{5}{1}{2}{3}{4}'>外网点此</a></td></tr>", webAddress, urlPrefix, url, accountParam, notInnerFrame, outAddress);
            }
            else if (orderType.Equals("备料单")) {
                string url = utl.MyUrlEncoder("Files/printBLReport?sysNo=" + sys_no);
                table += string.Format("<tr><td>PDF:</td><td><a href='{0}{1}{2}{3}{4}'>内网点此</a> <a href='{5}{1}{2}{3}{4}'>外网点此</a></td></tr>", webAddress, urlPrefix, url, accountParam, notInnerFrame, outAddress);
            }

            table += "</table>";

            if (SendEmail(content + table, salerEmailAdd, ccEmail, orderType + "申请完成"))
                return true;
            else
                return false;
        }

        //挂起操作通知订单申请者
        public static bool SendBackToSalerForBlock(string sys_no, string salerEmailAdd, string orderType, string operateType, string auditor, string reason,string model="")
        {
            string content = "<div>你好:</div>";
            content += string.Format("<div style='margin-left:30px;'><div>你申请的流水号为{0}的单据被挂起，详细如下。</div>", sys_no);
            content += String.Format("<div style='float:left;width:100px'>公司:<br/>订单类型:<br/>申请操作:<br/>规格型号:<br/>挂起操作人:<br/>挂起原因:<br/></div><div style='float:left;width:500px'>{3}<br/>{0}<br/>{5}<br/>{1}<br/>{2}<br/>{4}<br/></div>", orderType, operateType, auditor, copName, reason,model);

            if (SendEmail(content, salerEmailAdd))
                return true;
            else
                return false;
        }

        //退回到上一步，通知上一审核者
        //挂起操作通知订单申请者
        public static bool SendBackToPreviousAuditor(string sys_no, string previousEmailAdd, string orderType, string operateType, string auditor, string reason)
        {
            string content = "<div>你好:</div>";
            content += string.Format("<div style='margin-left:30px;'><div>你审批的流水号为{0}的单据被退回，请重新审核，详细如下。</div>", sys_no);
            content += String.Format("<div style='float:left;width:100px'>公司:<br/>订单类型:<br/>申请操作:<br/>退回操作人:<br/>退回原因:<br/></div><div style='float:left;width:500px'>{3}</br>{0}</br>{1}<br/>{2}<br/>{4}<br/></div>", orderType, operateType, auditor, copName, reason);

            if (SendEmail(content, previousEmailAdd, null, orderType + "被退回通知"))
                return true;
            else
                return false;
        }

        //发送到下一环节审批人,下一环节审批人可以有多个
        public static bool SendToNext(string sys_no, string salerName, string depName, string nextEmailAdds, string orderType, string operateType, string stepName, string returnUrl)
        {
            username = salerName;
            string content = "<div>你好:</div>";
            content += string.Format("<div style='margin-left:30px;'><div>你有一张待审核的流水号为{0}的单据，请尽快处理。</div>", sys_no);
            content += String.Format("<div style='float:left;width:100px'>公司:<br/>申请人:<br/>办事处:<br/>订单类型：<br/>申请操作:<br/>审核步骤:<br/></div><div style='float:left;width:300px'>{5}<br/>{0}</br>{4}<br/>{1}<br/>{2}<br/>{3}<br/></div>", salerName, orderType, operateType, stepName, depName, copName);
            content += "<div style='clear:both'><br/>单击以下链接可进入系统审核这张单据。</div>";
            content += string.Format("<div><a href='{0}{1}{2}{3}{4}'>内网用户点击此链接</a></div>", webAddress, urlPrefix, returnUrl, accountParam, isInnerFrame);
            content += string.Format("<div><a href='{0}{1}{2}{3}{4}'>外网统一点击此链接</a></div></div>", outAddress, urlPrefix, returnUrl, accountParam, isInnerFrame);
            if (SendEmail(content, nextEmailAdds, null, orderType + "审批"))
                return true;
            else
                return false;
        }

        //退货流程前段走完，需补货的开始走换货流程，通知出货组
        public static bool SendToShippingGroup(string sys_no, string salerName, string depName, string nextEmailAdds, string returnDep)
        {
            username = salerName;
            string content = "<div>你好:</div>";
            content += string.Format("<div style='margin-left:30px;'><div>你有一张待处理的流水号为{0}的换货申请，请尽快处理。</div>", sys_no);
            content += String.Format("<div style='float:left;width:100px'>公司:<br/>申请人:<br/>办事处:<br/>发货部门:<br/></div><div style='float:left;width:300px'>{0}<br/>{1}</br>{2}<br/>{3}<br/></div>", copName, salerName, depName, returnDep);
            if (SendEmail(content, nextEmailAdds))
                return true;
            else
                return false;
        }

        //退货流程未上线部分走完，不需补货，则要退红字，通知市场部导红字的人
        public static bool SendToImportRedBill(string sys_no, string salerName, string depName, string emailAdd)
        {
            username = salerName;
            string content = "<div>你好:</div>";
            content += string.Format("<div style='margin-left:30px;'><div>你有一张待处理的流水号为{0}的退货申请，请尽快导入K3红字出库。</div>", sys_no);
            content += String.Format("<div style='float:left;width:100px'>公司:<br/>申请人:<br/>办事处:<br/></div><div style='float:left;width:300px'>{0}<br/>{1}</br>{2}<br/></div>", copName, salerName, depName);
            if (SendEmail(content, emailAdd))
                return true;
            else
                return false;
        }

        //发送到下一环节变更审批人,下一环节审批人可以有多个
        public static bool SendToNextUpdate(string bill_no, string salerName, string nextEmailAdds, string orderType, string operateType)
        {
            username = salerName;
            string content = "";
            content += String.Format("<div style='float:left;width:100px'>申请人:<br/>订单类型：<br/>单据编号:<br/>申请操作:<br/></div><div style='float:left;width:300px'>{0}</br>{1}<br/>{2}<br/>{3}<br/></div>", salerName, orderType, bill_no, operateType);
            if (SendEmail(content, nextEmailAdds))
                return true;
            else
                return false;
        }

        //使用自己的邮箱发送邮件
        public static bool UseMyEmail(string content, string emailAddress)
        {
            try
            {
                WebMail.SmtpServer = server;
                WebMail.SmtpPort = 25;
                WebMail.UserName = "liyihan.ic";
                WebMail.From = "\"订单平台\"<liyihan.ic@truly.com.cn>";
                WebMail.Password = "tru123**";
                WebMail.Send(
                to: emailAddress,
                bcc: bcc,
                subject: "订单申请审批",
                body: content + "<br /><div style='clear:both'><hr />来自:移动销售平台订单系统</div>",
                isBodyHtml: true
                );
            }
            catch (Exception ex)
            {
                SomeUtils utl = new SomeUtils();
                utl.writeDownErrors(username, ex.Message.ToString());
                return false;
            }
            return true;
        }

        //使用半导体(.cn)的邮箱发送邮件
        //public static bool UseSemiEmail(string content, string emailAddress, string subject = "订单申请审批")
        //{
        //    try
        //    {
        //        WebMail.SmtpServer = server;
        //        WebMail.SmtpPort = 25;
        //        WebMail.UserName = "crm";
        //        WebMail.From = "\"订单平台\"<crm@truly.com.cn>";
        //        WebMail.Password = "ic3508**";
        //        WebMail.Send(
        //        to: emailAddress,
        //        bcc: bcc,
        //        subject: subject + "（光电）",
        //        body: content + "<br /><div style='clear:both'><hr />来自:移动销售平台订单系统</div>",
        //        isBodyHtml: true
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        SomeUtils utl = new SomeUtils();
        //        utl.writeDownErrors(username, ex.Message.ToString());
        //        return false;
        //    }
        //    return true;
        //}
    }
}