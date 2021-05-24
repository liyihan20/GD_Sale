using Sale_Order.Interfaces;
using Sale_Order.Models;
using Sale_Order.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Sale_Order.Services
{
    public class ApplySv : BaseSv
    {
        const string WEB_ADDRESS = "http://192.168.90.100/SaleOrder";
        const string OUT_ADDRESS = "http://59.37.42.23/SaleOrder";
        const string URL_PREFIX = "/Account/Login?url=";
        const string ACCOUNT_PARAM = "&accountset=op";
        const string IS_INNER_FRAME = "&isInnerFrame=true";
        const string NOT_INNER_FRAME = "&isInnerFrame=false";
        const string COP_NAME = "信利光电股份有限公司";

        private Apply ap;

        public ApplySv()
        {

        }

        public ApplySv(string sysNo)
        {
            try {
                ap = db.Apply.Single(a => a.sys_no == sysNo);
            }
            catch {
                throw new Exception("申请流水号不存在");
            }
        }

        public ApplySv(int applyId)
        {
            try {
                ap = db.Apply.Single(a => a.id == applyId);
            }
            catch {
                throw new Exception("申请ID号不存在");
            }
        }

        /// <summary>
        /// (无参构造）是否开始申请
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public bool ApplyHasBegan(string sysNo)
        {
            return db.Apply.Where(a => a.sys_no == sysNo).Count() > 0;
        }

        /// <summary>
        /// (无参构造）申请是否已完结
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public bool ApplyHasFinished(string sysNo)
        {
            return db.Apply.Where(a => a.sys_no == sysNo && a.success != null).Count() > 0;
        }


        /// <summary>
        /// (无参构造)提交申请
        /// </summary>
        /// <param name="orderType">单据类型</param>
        /// <param name="userId">申请人ID</param>
        /// <param name="userName">申请人姓名</param>
        /// <param name="ipAddr">ip地址</param>
        /// <param name="sysNo">流水号</param>
        /// <param name="pModel">规格型号</param>
        /// <param name="pro">流程对象</param>
        /// <param name="auditorsDic">审核关系字典</param>
        /// <returns></returns>
        public void BeginApply(string orderType, int userId, string ipAddr, string sysNo, string pModel, string processNo, Dictionary<string, int?> auditorsDic)
        {
            if (db.Apply.Where(a => a.sys_no == sysNo).Count() > 0) {
                throw new Exception("单据已提交，不能重复操作");
            }

            BillSv bill;
            try {
                bill = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(sysNo);
                bill.DoWhenBeforeApply();
            }
            catch (Exception ex) {

                throw new Exception(ex.Message);
            }

            ap = new Apply();
            ap.user_id = userId;
            ap.sys_no = sysNo;
            ap.p_model = pModel;
            ap.order_type = orderType;
            ap.start_date = DateTime.Now;
            ap.ip = ipAddr;

            int minusStep = 0;
            var pro = db.Process.Where(p => p.bill_type == processNo && p.begin_time <= DateTime.Now && p.end_time > DateTime.Now).FirstOrDefault();
            if (pro == null) {
                throw new Exception("流程引擎不存在或已过期");
            }
            List<ApplyDetails> ads = new List<ApplyDetails>();
            foreach (var det in pro.ProcessDetail.OrderBy(p => p.step)) {
                var auditorRel = db.AuditorsRelation.Where(a => a.step_value == det.step_type);
                string relateType = auditorRel.First().relate_type;
                string stepName = auditorRel.First().step_name;
                List<int?> auditors = new List<int?>();
                int? dicValue = 0;

                switch (relateType) {
                    case "固定人员":
                        auditors.Add(det.user_id);
                        break;
                    case "申请者":
                        auditors.Add(userId);
                        break;
                    default:
                        //其它除了从表单直接传过来的审核人，全部通过关联关系进行查询审核人。统一使用关联类型+“NO"这个变量名，值从上一级传进来
                        string realteTypeID = relateType + "NO";
                        if (auditorsDic != null && auditorsDic.TryGetValue(realteTypeID, out dicValue)) {
                            if (relateType.StartsWith("表单")) {
                                //如果以表单开头，表示是从表单直接选择审核人员，那么不用关联直接加入审核人
                                auditors.Add(dicValue);
                            }
                            else {
                                auditors = auditorRel.Where(a => a.relate_value == dicValue).Select(a => a.auditor_id).ToList();
                            }
                        }
                        break;
                }

                if (auditors.Count() < 1) {
                    if (det.can_be_null == false) {
                        throw new Exception("步骤【" + det.step_name + "】审核人员不存在");
                    }
                    else if (det.countersign == null || det.countersign == false) {
                        //该步骤的审核人为空且被允许，所以不是会签的要将后续步骤-1                        
                        minusStep++;
                    }
                    continue;
                }

                foreach (int auditor in auditors) {
                    ads.Add(new ApplyDetails()
                    {
                        Apply = ap,
                        can_modify = det.can_modify,
                        step = det.step - minusStep,
                        step_name = det.countersign == true ? det.step_name + "(" + stepName.Substring(stepName.IndexOf("_") + 1).Split(new string[] { "审", "负" }, StringSplitOptions.None)[0] + ")" : det.step_name,
                        user_id = auditor,
                        countersign = det.countersign
                    });
                }
            }

            try {
                db.Apply.InsertOnSubmit(ap);
                db.ApplyDetails.InsertAllOnSubmit(ads);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                throw ex;
            }

            try {
                bill.DoWhenAfterApply();
            }
            catch (Exception ex) {
                throw new Exception("提交成功，但执行回调失败：" + ex.Message);
            }

            SendNotificationEmail();

        }

        /// <summary>
        /// (有参)发送通知邮件，包括完成通知、审批通知
        /// </summary>
        /// <param name="ap"></param>
        /// <returns></returns>
        private bool SendNotificationEmail()
        {
            bool sendEmail = bool.Parse(ConfigurationManager.AppSettings["SendEmail"]);
            if (!sendEmail) return true;

            ap = db.Apply.Single(a => a.sys_no == ap.sys_no);//重新从数据库拿最新的
            if (ap.success != null) {
                //审批已完成，发回申请者
                return SendEmailToApplier();
            }

            //先判断当前步骤是否会签，并且当前的会签还未结束，如是，不发送邮件
            int nextStep = 1;
            var auditedDetails = ap.ApplyDetails.Where(a => a.pass != null);
            if (auditedDetails.Count() > 0) {
                var currentDetail = auditedDetails.OrderByDescending(a => a.finish_date).First();
                if (currentDetail.countersign == true) {
                    if (ap.ApplyDetails.Where(a => a.step == currentDetail.step && a.pass == null).Count() > 0) {
                        return true; //会签未结束，返回
                    }
                }
                nextStep = (int)currentDetail.step + 1;
            }
            return SendEmailToNextAuditor(nextStep);
        }

        /// <summary>
        /// (有参)发送审批完成通知给申请者
        /// </summary>
        /// <param name="ap"></param>
        /// <returns></returns>
        private bool SendEmailToApplier()
        {
            string emailTemplate = @"
                <div>你好:</div>
                <div style='margin-left:30px;'>
                    <div>你申请的流水号为{0}的单据已经处理完毕，结果如下。</div>
                    <table style='width:400px;font-size:14px;' border='0' cellpadding='0' cellspacing='3'>
                        <tr><td style='width:100px'>公司:</td><td style='width:300px'>{1}</td></tr>
                        <tr><td>单据类型:</td><td>{2}</td></tr>
                        <tr><td>单据编号:</td><td>{6}</td></tr>
                        <tr><td>规格型号:</td><td>{3}</td></tr>
                        <tr><td>申请结果:</td><td>{4}</td></tr>"
                + (ap.success == true ? "{5}" : @"
                        <tr><td>失败原因:</td><td>{5}</td></tr>                   
                ")
                + @"
                    </table>
                </div>
            ";
            BillSv bs = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(ap.sys_no);
            string billType = bs.GetSpecificBillTypeName();
            string emailContent = string.Format(
                emailTemplate,
                ap.sys_no,
                COP_NAME,
                billType,
                ap.p_model,
                ap.success == true ? "申请成功" : "申请失败",
                ap.success == true ? "" : ap.ApplyDetails.Where(ad => ad.pass == false).First().comment,
                bs.GetOrderNumber()
                );

            string ccEmails = null;
            var ife = bs as IFinishEmail;
            if (ife != null) {
                ccEmails = ife.ccToOthers(ap.sys_no, ap.success ?? false);
                if (ife.needReport(ap.success ?? false)) {
                    string returnUrl = new SomeUtils().MyUrlEncoder("NFile/PrintReport?sysNo=" + ap.sys_no);
                    emailContent += "<br /><div>单击以下链接可进入查看PDF文件：</div>";
                    emailContent += string.Format("<div><a href='{0}{1}{2}{3}{4}'>内网用户点击此链接</a></div>", WEB_ADDRESS, URL_PREFIX, returnUrl, ACCOUNT_PARAM, NOT_INNER_FRAME);
                    emailContent += string.Format("<div><a href='{0}{1}{2}{3}{4}'>外网用户点击此链接</a></div>", OUT_ADDRESS, URL_PREFIX, returnUrl, ACCOUNT_PARAM, NOT_INNER_FRAME);
                }
            }

            return new EmailUtil().SendEmail(emailContent, ap.User.email, ccEmails, billType + "申请完成");
        }

        /// <summary>
        /// (有参)发送审批通知到下一个审批人
        /// </summary>
        /// <param name="ap"></param>
        /// <param name="nextStep">下一步骤</param>
        /// <returns></returns>
        private bool SendEmailToNextAuditor(int nextStep)
        {
            string emailTemplate = @"
                <div>你好:</div>
                <div style='margin-left:30px;'>
                    <div>你有一张待审核的流水号为{0}的单据，请尽快处理。</div>
                    <table style='width:400px;font-size:14px;' border='0' cellpadding='0' cellspacing='3'>
                        <tr><td style='width:100px'>公司:</td><td style='width:300px'>{1}</td></tr>
                        <tr><td>申请人:</td><td>{2}</td></tr>
                        <tr><td>办事处:</td><td>{3}</td></tr>
                        <tr><td>单据类型:</td><td>{4}</td></tr>
                        <tr><td>规格型号:</td><td>{5}</td></tr>
                        <tr><td>审核步骤:</td><td>{6}</td></tr>
                    </table>
                    <br />
                    <div>单击以下链接可进入系统审核这张单据。</div>
                    <div><a href='{7}{8}{9}{10}{11}'>内网用户点击此链接</a></div>
                    <div><a href='{12}{8}{9}{10}{11}'>外网用户点击此链接</a></div>
                </div>
            ";
            var ads = ap.ApplyDetails.Where(a => a.pass != null);
            var auditorsEmailArr = ap.ApplyDetails.Where(a => a.step == nextStep).Select(a => a.User.email).ToArray();
            string emailAddrs = string.Join(",", auditorsEmailArr);
            BillSv bs = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(ap.sys_no);
            string billType = bs.GetSpecificBillTypeName();
            string stepName = ap.ApplyDetails.Where(a => a.step == nextStep).First().step_name;
            string depName = ap.User.Department1.name;
            string returnUrl = new SomeUtils().MyUrlEncoder("NAudit/BeginNAudit?step=" + nextStep + "&applyId=" + ap.id);
            string emailContent = string.Format(
                emailTemplate,
                ap.sys_no,
                COP_NAME,
                ap.User.real_name,
                depName,
                billType,
                ap.p_model,
                stepName,
                WEB_ADDRESS,
                URL_PREFIX,
                returnUrl,
                ACCOUNT_PARAM,
                IS_INNER_FRAME,
                OUT_ADDRESS
                );
            return new EmailUtil().SendEmail(emailContent, emailAddrs, null, billType + "审批");
        }

        /// <summary>
        /// (有参)发送挂起通知邮件
        /// </summary>
        /// <param name="operatorName">操作人</param>
        /// <param name="reason">挂起原因</param>
        /// <returns></returns>
        private bool SendBlockNotification(string operatorName, string reason)
        {
            bool sendEmail = bool.Parse(ConfigurationManager.AppSettings["SendEmail"]);
            if (!sendEmail) return true;

            string emailTemplate = @"
                <div>你好:</div>
                <div style='margin-left:30px;'>
                    <div>你申请的流水号为{0}的单据被挂起，详细如下:</div>
                    <table style='width:400px;font-size:14px;' border='0' cellpadding='0' cellspacing='3'>
                        <tr><td style='width:100px'>公司:</td><td style='width:300px'>{1}</td></tr>
                        <tr><td>单据类型:</td><td>{2}</td></tr>
                        <tr><td>规格型号:</td><td>{3}</td></tr>
                        <tr><td>挂起操作人:</td><td>{4}</td></tr>
                        <tr><td>挂起原因:</td><td>{5}</td></tr>
                    </table>
                </div>
            ";
            BillSv bs = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(ap.sys_no);
            string billType = bs.GetSpecificBillTypeName();

            string emailContent = string.Format(
                emailTemplate,
                ap.sys_no,
                COP_NAME,
                billType,
                ap.p_model,
                operatorName,
                reason
                );

            return new EmailUtil().SendEmail(emailContent, ap.User.email, null, billType + "被挂起");
        }

        /// <summary>
        /// (有参构造)取得当前申请的审批历史记录
        /// </summary>
        /// <returns></returns>
        public List<AuditStatusModel> GetAuditStatus()
        {
            List<AuditStatusModel> list = new List<AuditStatusModel>();
            list.Add(new AuditStatusModel()
            {
                auditor = ap.User.real_name,
                department = ap.User.Department1.name,
                step = 0,
                stepName = "提交申请",
                date = ((DateTime)ap.start_date).ToString("yyyy-MM-dd"),
                time = ((DateTime)ap.start_date).ToString("HH:mm"),
                pass = true
            });

            foreach (var ad in ap.ApplyDetails.Where(a => a.pass != null).OrderBy(a => a.finish_date)) {
                list.Add(new AuditStatusModel()
                {
                    auditor = ad.User.real_name,
                    department = ap.User.Department1.name,
                    step = (int)ad.step,
                    stepName = ad.step_name,
                    date = ((DateTime)ad.finish_date).ToString("yyyy-MM-dd"),
                    time = ((DateTime)ad.finish_date).ToString("HH:mm"),
                    pass = ad.pass,
                    comment = ad.comment
                });
            }

            if (ap.success == true) {
                list.Add(new AuditStatusModel()
                {
                    auditor = "系统",
                    department = "信息管理部",
                    step = (int)ap.ApplyDetails.Max(a => a.step) + 1,
                    stepName = "申请完成",
                    date = ((DateTime)ap.finish_date).ToString("yyyy-MM-dd"),
                    time = ((DateTime)ap.finish_date).ToString("HH:mm"),
                    pass = true
                });
            }
            return list;
        }

        /// <summary>
        /// (有参构造)取得下一审核步骤的step
        /// </summary>
        /// <returns></returns>
        private int GetNextAuditStep()
        {
            if (ap.success != null) {
                return 100; //已完成
            }
            var currentSteps = ap.ApplyDetails.Where(a => a.pass != null).OrderByDescending(a => a.step);
            if (currentSteps.Count() < 1) {
                return 1;
            }
            var currentStep = currentSteps.First();
            if (currentStep.countersign == true) {
                if (ap.ApplyDetails.Where(a => a.pass == null && a.step == currentStep.step).Count() > 0) {
                    return (int)currentStep.step;
                }

            }
            return (int)currentStep.step + 1;
        }

        /// <summary>
        /// (有参构造)取得下一审核步骤名称
        /// </summary>
        /// <returns></returns>
        public string GetNextStepName()
        {
            int nextStep = GetNextAuditStep();
            if (nextStep == 100) {
                return "无";
            }
            return ap.ApplyDetails.Where(a => a.step == nextStep).First().step_name;
        }

        /// <summary>
        /// (有参构造)取得下一审核步骤审核人的姓名
        /// </summary>
        /// <returns></returns>
        public string GetNextStepAudiors()
        {
            int nextStep = GetNextAuditStep();
            if (nextStep == 100) {
                return "无";
            }
            return string.Join("/", ap.ApplyDetails.Where(a => a.step == nextStep && a.pass == null).Select(a => a.User.real_name).ToArray());
        }

        /// <summary>
        /// (无餐构造) 取得审核列表
        /// </summary>
        /// <param name="userId">审核人ID</param>
        /// <param name="sysNo">流水号</param>
        /// <param name="salerName">申请人姓名</param>
        /// <param name="proModel">规格型号</param>
        /// <param name="fromDate">起始日期</param>
        /// <param name="toDate">结束日期</param>
        /// <param name="auditResult">审核结果</param>
        /// <param name="finalResult">最终审核结果</param>
        /// <returns></returns>
        public List<AuditListModel> GetAuditList(int userId, AuditSearchParamModel pm)
        {
            DateTime fDate, tDate;
            if (!DateTime.TryParse(pm.from_date, out fDate)) {
                fDate = DateTime.Parse("2010-6-1");
            }
            if (!DateTime.TryParse(pm.to_date, out tDate)) {
                tDate = DateTime.Parse("2049-9-9");
            }
            tDate = tDate.AddDays(1);

            pm.proModel = pm.proModel ?? "";
            pm.sysNo = pm.sysNo ?? "";

            var result = (from a in db.Apply
                          from ad in a.ApplyDetails
                          join u in db.User on a.user_id equals u.id
                          where ad.user_id == userId
                          && a.sys_no.Contains(pm.sysNo)
                          && a.p_model.Contains(pm.proModel)
                          && a.start_date >= fDate
                          && a.start_date <= tDate
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
                          select new AuditListModel()
                          {
                              applyId = a.id,
                              applyDetailId = ad.id,
                              previousStepTime = DateTime.Parse(a.start_date.ToString()).ToString("yyyy-MM-dd HH:mm"),
                              salerName = u.real_name,
                              model = a.p_model,
                              depName = u.Department1.name,
                              sysNum = a.sys_no,
                              step = (int)ad.step,
                              stepName = ad.step_name,
                              finalStatus = a.success == true ? "PASS" : a.success == false ? "NG" : "----",
                              status = ad.pass == true ? "已通过" : ad.pass == false ? "已NG" : "待确认",
                              orderType = a.order_type,
                              flag = true
                          }).Take(200).ToList();

            //单据详细类型和状态的逻辑比较复杂，不能在上面的sql语句中处理。
            result.ForEach(r =>
            {
                var bill = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(r.sysNum);
                if (bill != null) {
                    r.orderType = bill.GetSpecificBillTypeName();
                }
                if (r.status.Equals("待确认")) {
                    if (IsCurrentStepAuditing(db.ApplyDetails.Single(a => a.id == r.applyDetailId))) {
                        if (db.BlockOrder.Where(b => b.sys_no == r.sysNum && b.step == r.step && b.@operator == userId).Count() > 0) {
                            r.status = "挂起中";
                        }
                        else {
                            r.status = "待审核";
                        }
                    }
                    else {
                        r.flag = false;
                        r.status = "已结束";
                    }
                }
                if (r.finalStatus.Equals("PASS")) {
                    bool? importFlag = false;
                    if (db.ImportSysNoLog.Where(im => im.sys_no == r.sysNum).Count() > 0) {
                        importFlag = true;
                    }
                    else {
                        db.hasImportIntoK3(r.sysNum, r.orderType, ref importFlag);
                        if (importFlag == true) {
                            db.ImportSysNoLog.InsertOnSubmit(new ImportSysNoLog() { sys_no = ap.sys_no });
                            db.SubmitChanges();
                        }
                    }
                    r.hasImportK3 = importFlag == true ? "Y" : "N";
                }
            });

            return result.Where(r=>r.flag==true).ToList();

        }

        /// <summary>
        /// (有参构造)检查本步骤当前是否可以审核
        /// </summary>
        /// <param name="ad"></param>
        /// <returns></returns>
        private bool IsCurrentStepAuditing(ApplyDetails ad)
        {
            if (ad.pass != null) return false; //此步骤已审核
            ap = ad.Apply;
            //当前步骤不是处于审核的步骤
            if (GetNextAuditStep() != ad.step) return false;

            if (ad.countersign == false || ad.countersign == null) {
                if (ap.ApplyDetails.Where(a => a.step == ad.step && a.pass == true).Count() > 0) {
                    return false; //当前步骤不是会签，且被本步骤其它人员所审核
                }
            }

            return true;

        }

        /// <summary>
        /// (无参构造)取得审核单据需要的信息，只读或者可update等
        /// </summary>
        /// <param name="step">审核步骤</param>
        /// <param name="applyId">申请ID</param>
        /// <param name="userId">审核热ID</param>
        /// <returns>AuditInfoModel</returns>
        public AuditInfoModel GetAuditInfo(int step, int applyId, int userId)
        {
            AuditInfoModel am = new AuditInfoModel();
            var aps = db.Apply.Where(a => a.id == applyId);
            if (aps.Count() == 0) {
                throw new Exception("申请不存在,请确认公司名是否正确");
            }
            ap = aps.First();
            var ads = ap.ApplyDetails.Where(a => a.step == step && a.user_id == userId);
            if (ads.Count() == 0) {
                throw new Exception("没有审核权限");
            }
            var ad = ads.OrderBy(a => a.pass).First();

            if (ap.success == false && ap.ApplyDetails.Where(a => a.step == ad.step && a.pass != null).Count() == 0) {
                throw new Exception("没有权限审核");
            }

            if (ap.success == null && ad.step > GetNextAuditStep()) {
                //当前步骤大于此申请的当前步骤
                throw new Exception("还未轮到你审核");
            }

            am.sysNo = ap.sys_no;
            am.editType = "r"; //r表示只读
            am.stepName = ad.step_name;
            if (ad.can_modify == true) {
                bool hasEdited = false;
                if (ap.success != null || ad.pass != null) {
                    hasEdited = true;
                }
                else if (ad.countersign == false || ad.countersign == null) {
                    //不是会签，如果同组的人审核了，他也被当作已审核
                    hasEdited = ap.ApplyDetails.Where(a => a.step == ad.step && a.pass != null).Count() > 0;
                }
                if (!hasEdited) {
                    am.editType = "m"; //m表示可编辑
                }
            }
            return am; 
        }

        /// <summary>
        /// (有参构造)挂起单据
        /// </summary>
        /// <param name="step">步骤</param>
        /// <param name="userId">审核人ID</param>
        /// <param name="blockReason">挂起原因</param>
        /// <returns></returns>
        public string BlockOrder(int step, int userId, string userName, string blockReason)
        {
            if (db.BlockOrder.Where(b => b.sys_no == ap.sys_no && b.step == step && b.@operator == userId).Count() > 0) {
                return "不能重复挂起";
            }

            try {
                db.BlockOrder.InsertOnSubmit(new BlockOrder()
                {
                    @operator = userId,
                    block_time = DateTime.Now,
                    reason = blockReason,
                    step = step,
                    step_name = ap.ApplyDetails.Where(a => a.step == step).First().step_name,
                    sys_no = ap.sys_no
                });
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            SendBlockNotification(userName, blockReason);

            return "";
        }
        /// <summary>
        /// (有参构造)取得此申请的挂起信息
        /// </summary>
        /// <returns></returns>
        public List<BlockOrder> GetBlockInfo()
        {
            return db.BlockOrder.Where(b => b.sys_no == ap.sys_no).OrderBy(b => b.step).ToList();
        }

        /// <summary>
        /// (有参构造)取得当前步骤的审批结果
        /// </summary>
        /// <param name="step">审核步骤</param>
        /// <param name="userId">用户ID</param>
        /// <returns></returns>
        public AuditResultModel GetAuditResult(int step, int userId)
        {
            AuditResultModel arm = new AuditResultModel();
            var ads = ap.ApplyDetails.Where(a => a.step == step);

            if (ap.success != null) {
                //已结束的
                var ad = ads.Where(a => a.user_id == userId).OrderByDescending(a => a.pass).First(); //优先取已审批记录
                arm.canAudit = false;
                arm.auditResult = ad.pass == null ? "已结束" : ad.pass == true ? "已同意" : "已NG";
                arm.comment = ad.comment;
            }
            else {
                if (ads.Where(a => a.pass == true).Count() == 0) {
                    //本轮还没人审批
                    arm.canAudit = true;
                }
                else {
                    //本轮已有人审批
                    var ad = ads.Where(a => a.user_id == userId).OrderBy(a => a.pass).First(); //优先取未审批记录
                    if (ad.countersign == true) {
                        //会签
                        if (ad.pass == null) {
                            arm.canAudit = true;
                        }
                        else {
                            arm.canAudit = false;
                            arm.auditResult = ad.pass == true ? "已通过" : "已NG";
                            arm.comment = ad.comment;
                        }
                    }
                    else {
                        //不是会签
                        ad = ads.Where(a => a.user_id == userId).OrderByDescending(a => a.pass).First(); //优先取已审批记录
                        arm.canAudit = false;
                        arm.auditResult = ad.pass == null ? "已结束" : ad.pass == true ? "已同意" : "已NG";
                        arm.comment = ad.comment;
                    }
                }
            }

            return arm;

        }

        /// <summary>
        /// (有参构造)开始审核
        /// </summary>
        /// <param name="step">步骤</param>
        /// <param name="userId">用户ID</param>
        /// <param name="isPass">是否通过</param>
        /// <param name="comment">意见</param>
        /// <returns></returns>
        public string HandleAudit(int step, int userId, bool isPass, string comment, string ipAdd)
        {
            if (ap.success != null) {
                return "此申请已结束";
            }
            var details = ap.ApplyDetails.ToList();
            var ad = details.Where(a => a.step == step && a.user_id == userId).OrderBy(a => a.pass).First();
            if (ad.pass != null) {
                return "不能重复审批";
            }
            else {
                if (ad.countersign == null || ad.countersign == false) {
                    if (details.Where(a => a.step == step && a.pass == true).Count() > 0) {
                        return "已被同组其它人审批";
                    }
                }
            }

            BillSv bill = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(ap.sys_no);

            //审批之前，单据需要做的事
            try {
                bill.DoWhenBeforeAudit(step, ad.step_name, isPass, userId);
            }
            catch (Exception ex) {
                return ex.Message;
            }

            ad.pass = isPass;
            ad.comment = comment;
            ad.ip = ipAdd;
            ad.finish_date = DateTime.Now;

            //是否最后一步审批
            bool isLastStep = ad.step == details.Max(a => a.step);
            if (isLastStep && ad.countersign == true) {
                if (details.Where(a => a.step == ad.step && a.pass == null && a.user_id != userId).Count() > 0) {
                    isLastStep = false;
                }
            }

            if (!isPass || isLastStep) {
                ap.success = isPass;
                ap.finish_date = DateTime.Now;
            }
            try {
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            if (!isPass || isLastStep) {
                //审批完成之后需要做的事情
                try {
                    bill.DoWhenFinishAudit(isPass);
                }
                catch (Exception ex) {
                    return "审批成功；但发生以下错误:" + ex.Message + ";可尝试收回后重试，如果还是出现，请联系管理员处理";
                }
            }

            SendNotificationEmail();

            return "";
        }

        //收回步骤
        public void StepRollBack(int step, int userId)
        {
            //判断当前step步骤是本人审批的
            var ad = ap.ApplyDetails.Where(d => d.step == step && d.user_id == userId && d.pass != null).FirstOrDefault();
            if (ad == null) {
                throw new Exception("当前审核步骤不是你本人已处理的");
            }

            //判断后续step还未审批
            if (ap.ApplyDetails.Where(d => d.step > step && d.pass != null).Count() > 0) {
                throw new Exception("后续步骤已处理，不能收回");
            }

            RestoreAuditedStep(ad);
        }

        //退回上一步
        public void StepBackward(int step, int userId)
        {
            //判断当前step是本人的且还未处理的
            if (ap.ApplyDetails.Where(d => d.step == step && d.user_id == userId).Count() == 0) {
                throw new Exception("当前审核步骤不是你本人在处理的");
            }

            //判断本节点和后续节点是否已处理
            if (ap.ApplyDetails.Where(d => d.step >= step && d.pass != null).Count() > 0) {
                throw new Exception("本步骤或后续步骤已处理，不能退回上一步");
            }

            //判断是否存在上一step
            var ads = ap.ApplyDetails.Where(d => d.step == step - 1).ToList();
            if (ads.Count() == 0) {
                throw new Exception("不存在上一处理步骤，退回失败");
            }
            else {
                if (ads.Count() > 0 && ads.First().countersign == true) {
                    throw new Exception("上一处理步骤属于多人会签节点，不能退回");
                }
                else {
                    var ad = ads.Where(d => d.pass != null).FirstOrDefault();
                    if (ad != null) {
                        RestoreAuditedStep(ad);
                    }
                }
            }
        }

        //清空并恢复某一步的已审状态为未审状态
        private void RestoreAuditedStep(ApplyDetails ad)
        {
            BillSv bill = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(ap.sys_no);
            bill.BeforeRollBack((int)ad.step); //先到各类流程验证是否和恢复

            //如果当前节点有子步骤，就先将子步骤删除（客退系统的客服环节会生成子步骤，出货组和营业确认）,并将后续步骤-子步骤的数量
            if (ap.ApplyDetails.Where(d => d.parent_step == ad.step).Count() > 0) {
                var childrenCount = ap.ApplyDetails.Where(d => d.parent_step == ad.step).Select(d => d.step).Distinct().Count();

                foreach (var d in ap.ApplyDetails.Where(d => d.step > ad.step)) {
                    if (d.parent_step == ad.step) {
                        db.ApplyDetails.DeleteOnSubmit(d);
                    }
                    else {
                        d.step -= childrenCount;
                    }
                }

            }

            //更新当前节点
            ad.pass = null;
            ad.finish_date = null;
            ad.comment = null;
            ad.ip = null;

            //如果已结束的，更新表头
            if (ap.success != null) {
                ap.success = null;
                ap.finish_date = null;
            }

            db.SubmitChanges();
        }

        //修改某步骤的审核人
        public void UpdateStepAuditor(string stepName, int auditorId)
        {
            var detail = ap.ApplyDetails.Where(a => a.step_name == stepName).FirstOrDefault();
            if (detail != null) {
                detail.user_id = auditorId;
            }
            db.SubmitChanges();
        }

        public string CeoBatchAudit(int[] applyDetailIds, int userId, bool isPass, string comment, string ipAdd)
        {
            int record = 0;
            foreach (int detailId in applyDetailIds) {
                var ad = db.ApplyDetails.SingleOrDefault(a => a.id == detailId);
                ap = ad.Apply;

                if (ap.success != null) {
                    continue;
                }
                var details = ap.ApplyDetails.ToList();

                if (ad.pass != null) {
                    continue;
                }
                else {
                    if (ad.countersign == null || ad.countersign == false) {
                        if (details.Where(a => a.step == ad.step && a.pass == true).Count() > 0) {
                            continue;
                        }
                    }
                }

                BillSv bill = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(ap.sys_no);

                //审批之前，单据需要做的事,批量处理的时候不做，不然时间太长
                //try {
                //    bill.DoWhenBeforeAudit((int)ad.step, ad.step_name, isPass, userId);
                //}
                //catch {
                //    continue;
                //}

                ad.pass = isPass;
                ad.comment = comment;
                ad.ip = ipAdd;
                ad.finish_date = DateTime.Now;

                //是否最后一步审批
                bool isLastStep = ad.step == details.Max(a => a.step);
                if (isLastStep && ad.countersign == true) {
                    if (details.Where(a => a.step == ad.step && a.pass == null && a.user_id != userId).Count() > 0) {
                        isLastStep = false;
                    }
                }

                if (!isPass || isLastStep) {
                    ap.success = isPass;
                    ap.finish_date = DateTime.Now;

                    //审批完成之后需要做的事情
                    bill.DoWhenFinishAudit(isPass);
                }
                try {
                    db.SubmitChanges();
                }
                catch {
                    continue;
                }

                SendNotificationEmail();
                record++;
            }
            return "已批量处理" + record.ToString() + "行记录";
        }
    }
}