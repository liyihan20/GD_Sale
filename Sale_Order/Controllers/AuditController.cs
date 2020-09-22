using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Sale_Order.Models;
using Sale_Order.Utils;
using Sale_Order.Filter;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace Sale_Order.Controllers
{
    public class AuditController : BaseController
    {
        SomeUtils utl = new SomeUtils();

        //审批人查看自己审核的单据
        [SessionTimeOutFilter()]
        public ActionResult CheckAuditList()
        {
            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd"];
            if (queryData != null && queryData.Values.Get("au_so") != null)
            {
                ViewData["sys_no"] = utl.DecodeToUTF8(queryData.Values.Get("au_so"));
                ViewData["saler"] = utl.DecodeToUTF8(queryData.Values.Get("au_sa"));
                ViewData["audit_result"] = queryData.Values.Get("au_ar");
                ViewData["final_result"] = queryData.Values.Get("au_fr");
                ViewData["from_date"] = queryData.Values.Get("au_fd");
                ViewData["to_date"] = queryData.Values.Get("au_td");
                ViewData["pro_model"] = queryData.Values.Get("au_pm");
            }
            else
            {
                ViewData["audit_result"] = 0;
                ViewData["final_result"] = 0;
            }

            return View();
        }

        //默认搜索输入条件的结果
        //public JsonResult GetAuditList()
        //{
        //    string sysNo = "", saler = "", fromDate = "", toDate = "",proModel = "";
        //    int auditResult = 0, isFinish = 0;
        //    var queryData = Request.Cookies["op_qd"];
        //    if (queryData != null && queryData.Values.Get("au_so") != null)
        //    {
        //        sysNo = utl.DecodeToUTF8(queryData.Values.Get("au_so"));
        //        saler = utl.DecodeToUTF8(queryData.Values.Get("au_sa"));
        //        auditResult = Int32.Parse(queryData.Values.Get("au_ar"));
        //        isFinish = Int32.Parse(queryData.Values.Get("au_fr"));
        //        fromDate = queryData.Values.Get("au_fd");
        //        toDate = queryData.Values.Get("au_td");
        //        proModel = queryData.Values.Get("au_pm");
        //    }

        //    return SearchAuditBase(sysNo, saler,proModel, fromDate, toDate, auditResult, isFinish);
        //}

        //审批人搜索单据
        [SessionTimeOutFilter()]
        public JsonResult SearchAuditList(FormCollection fc)
        {
            string sysNo = fc.Get("sys_no");
            //string saler = fc.Get("saler");
            string fromDateString = fc.Get("fromDate");
            string toDateString = fc.Get("toDate");
            string result = fc.Get("auditResult");
            string finalResult = fc.Get("finalResult");
            string proModel = fc.Get("proModel");

            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd"];
            if (queryData == null) queryData = new HttpCookie("op_qd");
            queryData.Values.Set("au_so", utl.EncodeToUTF8(sysNo));
            //queryData.Values.Set("au_sa", utl.EncodeToUTF8(saler));
            queryData.Values.Set("au_pm", proModel);
            queryData.Values.Set("au_ar", result);
            queryData.Values.Set("au_fr", finalResult);
            queryData.Values.Set("au_fd", fromDateString);
            queryData.Values.Set("au_td", toDateString);
            queryData.Expires = DateTime.Now.AddDays(7);
            Response.AppendCookie(queryData);

            utl.writeEventLog("查询审核单据", fromDateString + "~" + toDateString + ";model:"+proModel+";auditResult:" + result + ";finalResult:" + finalResult, sysNo, Request);
            return SearchAuditBaseNew(sysNo, "",proModel, fromDateString, toDateString, Int32.Parse(result), Int32.Parse(finalResult));
        }


        public JsonResult SearchAuditBaseNew(string sysNo, string saler, string proModel, string from_date, string to_date, int auditResult, int isFinish)
        {
            DateTime fromDate = string.IsNullOrWhiteSpace(from_date) ? DateTime.Parse("1980-1-1") : DateTime.Parse(from_date);
            DateTime toDate = string.IsNullOrWhiteSpace(to_date) ? DateTime.Parse("2099-9-9") : DateTime.Parse(to_date).AddDays(1);
            List<AuditListModel> list = new List<AuditListModel>();            
            string status;
            string finalStatus;
            bool? importFlag;
            int maxRecordNum = 200; //最多只能导出200条记录
            int recordNum = 0;

            var result = from a in db.Apply
                         join u in db.User on a.user_id equals u.id
                         join dep in db.Department on u.department equals dep.id
                         join ad in db.ApplyDetails on a.id equals ad.apply_id
                         join pad in db.ApplyDetails on new { apply_id = ad.apply_id, step = ad.step - 1 } equals new { apply_id = pad.apply_id, step = pad.step } into tmpad
                         from pre in tmpad.DefaultIfEmpty()
                         join cad in db.ApplyDetails on new { apply_id = ad.apply_id, step = ad.step } equals new { apply_id = cad.apply_id, step = cad.step }
                         join bo in db.BlockOrder on new { sysNo = a.sys_no, step = ad.step, user = ad.user_id } equals new { sysNo = bo.sys_no, step = bo.step, user = bo.@operator } into tmpbo
                         from block in tmpbo.DefaultIfEmpty()
                         where ad.user_id == currentUser.userId
                         && a.start_date >= fromDate
                         && a.start_date <= toDate
                         select new
                         {
                             apply = a,
                             detail = ad,
                             p_detail = pre,
                             c_detail = cad,
                             userName = u.username,
                             depName = dep.name,
                             block = block
                         };

            if (!string.IsNullOrEmpty(sysNo)) {
                result = result.Where(r => r.apply.sys_no.Contains(sysNo));
            }
            if (!string.IsNullOrEmpty(proModel)) {
                result = result.Where(r => r.apply.p_model.Contains(proModel));
            }
            if (isFinish != 10) {
                result = result.Where(r => (r.apply.success == true && isFinish == 1) || (r.apply.success == false && isFinish == -1) || r.apply.success == null && isFinish == 0);
            }
            
            if (auditResult == 1) {
                result = result.Where(r => r.detail.pass == true || ((r.detail.countersign == false || r.detail.countersign == null) && r.c_detail.pass == true));
            }
            else if (auditResult == -1) {
                result = result.Where(r => r.detail.pass == false || ((r.detail.countersign == false || r.detail.countersign == null) && r.c_detail.pass == false));
            }
            else if (auditResult == 0) {
                result = result.Where(r => r.detail.pass == null);
            }

            var data = result.ToList();
            var billTypes = db.Sale_BillTypeName.ToList();
            foreach (var d in data.Select(da => da.detail).Distinct().ToList()) {
                var ap = data.Where(da => da.detail == d).Select(da => da.apply).First();
                importFlag = null;
                finalStatus = (ap.success == true ? "PASS" : (ap.success == false ? "NG" : "----"));

                // 全部或未审批的需要先过滤掉还未到这一步的
                if (Math.Abs(auditResult) != 1) {
                    var pre = data.Where(da => da.detail == d).Select(da => da.p_detail).ToList();
                    if (pre.First() != null) { 
                        //存在上一步
                        if (pre.First().countersign == true) {
                            if (pre.Where(p => p.pass == null).Count() > 0) {
                                continue;//上一步是会签且有人未审批
                            }
                        }
                        else {
                            if (pre.Where(p => p.pass == true).Count() < 1) {
                                continue;//上一步是或签且没有人审批通过
                            }
                        }
                    }
                }
                var curd = data.Where(da => da.detail == d).Select(da => da.c_detail).ToList();
                if (d.pass == true || (true != d.countersign && curd.Where(c => c.pass == true).Count() > 0)) {
                    status = "审核成功";
                }else if (d.pass == false || (true != d.countersign && curd.Where(c => c.pass == false).Count() > 0)) {
                    status = "审核失败";
                }
                else {
                    if (ap.success != null) {
                        status = "审核结束";
                    }
                    else {
                        if (data.Where(da => da.detail == d).Select(da => da.block).Where(b => b != null).Count() > 0) {
                            status = "挂起中";
                        }
                        else {
                            status = "待审核";
                        }
                    }
                }
                if (ap.success == true) {
                    if (db.ImportSysNoLog.Where(im => im.sys_no == ap.sys_no).Count() > 0) {
                        importFlag = true;
                    }
                    else {
                        db.hasImportIntoK3(ap.sys_no, ap.order_type, ref importFlag);
                        if (importFlag == true) {
                            db.ImportSysNoLog.InsertOnSubmit(new ImportSysNoLog() { sys_no = ap.sys_no });
                            db.SubmitChanges();
                        }
                    }
                }
                list.Add(new AuditListModel()
                {
                    depName = data.Where(da=>da.detail==d).Select(da=>da.depName).FirstOrDefault(),
                    applyId = ap.id,
                    previousStepTime = ((DateTime)ap.start_date).ToString("yyyy-MM-dd HH:mm"),//改成下单时间，之前是到达时间
                    salerName = data.Where(da => da.detail == d).Select(da => da.userName).FirstOrDefault(),
                    step = (int)d.step,
                    stepName = d.step_name,
                    sysNum = ap.sys_no,
                    status = status,
                    hasImportK3 = (importFlag == true) ? "Y" : ((importFlag == false) ? "N" : ""),
                    finalStatus = finalStatus,
                    encryptNo = utl.myEncript(ap.sys_no),
                    orderType = billTypes.Where(b => b.p_type == ap.order_type).Select(b => b.p_name).FirstOrDefault(),
                    model = ap.p_model,
                    account = ap.account
                });
                recordNum++;
                if (recordNum >= maxRecordNum) {
                    break;
                }
            }            
            list = list.OrderBy(l => DateTime.Parse(l.previousStepTime)).ToList();
            return Json(list, "text/html");
        }
        //搜索单据base方法
        //[SessionTimeOutFilter()]
        //public JsonResult SearchAuditBase(string sysNo, string saler,string proModel, string from_date, string to_date, int auditResult, int isFinish)
        //{
        //    DateTime fromDate = string.IsNullOrWhiteSpace(from_date) ? DateTime.Parse("1980-1-1") : DateTime.Parse(from_date);
        //    DateTime toDate = string.IsNullOrWhiteSpace(to_date) ? DateTime.Parse("2099-9-9") : DateTime.Parse(to_date).AddDays(1);
        //    List<AuditListModel> list = new List<AuditListModel>();
        //    int step;
        //    string status;
        //    string finalStatus;
        //    Apply ap;
        //    bool? importFlag;
        //    int maxRecordNum = 200; //最多只能导出200条记录
        //    int recordNum = 0;                       

        //    var details = (from ad in db.ApplyDetails
        //                   join a in db.Apply on ad.apply_id equals a.id
        //                  where ad.user_id == currentUser.userId
        //                  && a.sys_no.Contains(sysNo)
        //                  //&& ad.Apply.User.real_name.Contains(saler)  去掉搜索申请者的过滤条件，大幅优化查询速度
        //                  && a.start_date >= fromDate
        //                  && a.start_date <= toDate
        //                  && a.p_model.Contains(proModel)
        //                  && (
        //                  (isFinish == 10
        //                  || a.success == true && isFinish == 1)
        //                  || (a.success == null && isFinish == 0)
        //                  || (a.success == false && isFinish == -1)
        //                  )
        //                  && (auditResult == 10
        //                  || ((ad.pass == true || ((ad.countersign == null || ad.countersign == false) && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == true).Count() > 0)) && auditResult == 1)
        //                  || ((ad.pass == false || ((ad.countersign == null || ad.countersign == false) && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == false).Count() > 0)) && auditResult == -1)
        //                  || (((ad.countersign == true && ad.pass == null) || ((ad.countersign == null || ad.countersign == false) && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass != null).Count() == 0)) && auditResult == 0)
        //                  )
        //                  select ad).ToList();
        //    var billTypes = db.Sale_BillTypeName.ToList();
        //    foreach (var ad in details)
        //    {
        //        importFlag = null;
        //        finalStatus = "----";
        //        ap = ad.Apply;
        //        step = (int)ad.step;
        //        //string model = "";

        //        //还没到这一步或者已经在之前结束
        //        if (step >= 2 && (ap.ApplyDetails.Where(a => a.step == step - 1 && a.pass == true).Count() < 1))
        //        {
        //            continue;
        //        }
        //        //如果上一步是会签，而且还未结束，即跳出当前循环
        //        if (ap.ApplyDetails.Where(a => a.step == step - 1 && a.countersign == true && a.pass == null).Count() > 0)
        //        {
        //            continue;
        //        }
        //        //status = (ad.pass == true ? "审核成功" : (ad.pass == false ? "审核失败" : "待审核"));
        //        if (auditResult == 1 || ad.pass == true)
        //        {
        //            status = "审核成功";
        //        }
        //        else if (auditResult == -1 || ad.pass == false)
        //        {
        //            status = "审核失败";
        //        }
        //        else if (auditResult == 0)
        //        {
        //            status = "待审核";
        //        }
        //        else
        //        {
        //            //获取组内其它人审核结果
        //            if ((ad.countersign == null || ad.countersign == false) && ad.Apply.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == true).Count() > 0)
        //            {
        //                status = "审核成功";
        //            }
        //            else if ((ad.countersign == null || ad.countersign == false) && ad.Apply.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == false).Count() > 0)
        //            {
        //                status = "审核失败";
        //            }
        //            else
        //            {
        //                if (ap.success != null)
        //                {
        //                    status = "审核结束";
        //                }
        //                else { 
        //                    status = "待审核"; 
        //                }
        //            }
        //        }
        //        if (ad.pass == null && db.BlockOrder.Where(b => b.sys_no == ap.sys_no && b.step == step && b.@operator == ad.user_id).Count() > 0)
        //        {
        //            status = "挂起中";
        //        }

        //        finalStatus = (ap.success == true ? "PASS" : (ap.success == false ? "NG" : "----"));
        //        if (ap.success == true)
        //        {
        //            if (db.ImportSysNoLog.Where(im => im.sys_no == ap.sys_no).Count() > 0)
        //            {
        //                importFlag = true;
        //            }
        //            else
        //            {
        //                db.hasImportIntoK3(ap.sys_no, ap.order_type, ref importFlag);
        //                if (importFlag == true)
        //                {
        //                    db.ImportSysNoLog.InsertOnSubmit(new ImportSysNoLog() { sys_no = ap.sys_no });
        //                    db.SubmitChanges();
        //                }
        //            }
        //        }
        //        list.Add(new AuditListModel()
        //                    {
        //                        depName = ap.User.Department1.name,
        //                        applyId = ap.id,
        //                        previousStepTime = ((DateTime)ap.start_date).ToString("yyyy-MM-dd HH:mm"),//改成下单时间，之前是到达时间
        //                        salerName = ap.User.real_name,
        //                        step = step,
        //                        stepName = ad.step_name,
        //                        sysNum = ap.sys_no,
        //                        status = status,
        //                        hasImportK3 = (importFlag == true) ? "Y" : ((importFlag == false) ? "N" : ""),
        //                        finalStatus = finalStatus,
        //                        encryptNo = utl.myEncript(ap.sys_no),
        //                        orderType = billTypes.Where(b => b.p_type == ap.order_type).Select(b => b.p_name).FirstOrDefault(),
        //                        model = ap.p_model,
        //                        account = ap.account
        //                    });
        //        recordNum++;
        //        if (recordNum >= maxRecordNum)
        //        {
        //            break;
        //        }

        //    }
        //    list = list.OrderBy(l => DateTime.Parse(l.previousStepTime)).ToList();

        //    return Json(list, "text/html");
        //}

        //审批人审核
        [SessionTimeOutFilter()]
        public ActionResult BeginAudit(int step, int applyId)
        {            
            var apps = db.Apply.Where(a => a.id == applyId);
            if (apps == null || apps.Count() < 1)
            {
                utl.writeEventLog("审核单据", "单据不存在,applyId:" + applyId.ToString(), "", Request, 1000);
                ViewBag.tip = "单据不存在，请确认公司名是否正确。";
                return View("Tip");
            }
            Apply ap = apps.First();
            var ads = ap.ApplyDetails.Where(a => a.step == step && a.user_id == currentUser.userId);
            //验证是否有审核权限
            if (ads.Count() < 1)
            {
                utl.writeEventLog("审核单据", "没有权限审核,applyId:" + applyId.ToString() + ";step:" + step.ToString(), "", Request, 1000);
                ViewBag.tip = "对不起，你没有权限审核";
                return View("Tip");
            }
            var ad = ads.OrderBy(a => a.pass).First();
            int currentStep = step;

            //上一步还未审核OK，不能审核
            if (ad.step >= 2 && (ap.ApplyDetails.Where(a => a.step == currentStep - 1 && a.pass == true).Count() < 1))
            {
                ViewBag.tip = "还没有轮到你审核";
                return View("Tip");
            }
            //如果上一步是会签，而且还未结束，即不能审核
            if (ap.ApplyDetails.Where(a => a.step == currentStep - 1 && a.countersign == true && a.pass == null).Count() > 0)
            {
                ViewBag.tip = "上一步的会签还未完成";
                return View("Tip");
            }
            ViewData["step"] = currentStep;
            ViewData["applyId"] = applyId;
            ViewData["orderType"] = ap.order_type;
            ViewData["sys_no"] = ap.sys_no;
            ViewData["create_user"] = ap.User.real_name;
            ViewData["can_select_next"] = ad.can_select_next == true ? "Y" : "N";
            //该审核步骤是否可编辑
            bool? canEdit = ad.can_modify;
            //该审核步骤是否已处理
            bool hasEdit;
            if (ap.success != null) {
                hasEdit = true;
            }
            else if (ad.pass != null)
            {
                hasEdit = true;
            }
            else if (ad.countersign == false || ad.countersign == null)
            {
                //不是会签
                hasEdit = ap.ApplyDetails.Where(a => a.step == currentStep && a.pass != null).Count() > 0;
            }
            else
            {
                //是会签
                hasEdit = ap.ApplyDetails.Where(a => a.step == currentStep && a.pass == false).Count() > 0;
            }
            switch (ap.order_type)
            {
                case "SO":
                    ViewData["order_id"] = db.Sale_SO.Single(s => s.sys_no == ap.sys_no).id;
                    ViewData["step_name"] = ad.step_name;
                    break;
                case "CC":
                    ViewData["order_id"] = db.CcmModelContract.Single(s => s.sys_no == ap.sys_no).id;
                    break;
                case "CM":
                    ViewData["order_id"] = db.ModelContract.Single(s => s.sys_no == ap.sys_no).id;
                    break;
                case "SB":
                    ViewData["order_id"] = db.SampleBill.Single(s => s.sys_no == ap.sys_no).id;
                    break;
                case "BL":
                    ViewData["order_id"] = db.Sale_BL.Single(s => s.sys_no == ap.sys_no).id;
                    ViewData["step_name"] = ad.step_name;
                    break;
                case "TH":
                    ViewData["order_id"] = db.ReturnBill.Where(r => r.sys_no == ap.sys_no).First().id;
                    break;
                case "PJ":
                    ViewData["order_id"] = db.Project_bills.Where(r => r.sys_no == ap.sys_no).First().id;
                    break;
                case "HC":
                    ViewData["order_id"] = db.Sale_HC.SingleOrDefault(h => h.sys_no == ap.sys_no).id;
                    break;
                default:
                    return View("Error");
            }
            //可编辑并且未审核，转到对应编辑界面
            if (canEdit == true && !hasEdit)
            {
                //挂起信息
                ViewData["blockInfo"] = db.BlockOrder.Where(b => b.sys_no == ap.sys_no).OrderBy(b => b.step).ToList();
                utl.writeEventLog("审核单据", "进入可编辑界面", ap.sys_no + ":" + step.ToString(), Request);
                switch (ap.order_type)
                {
                    case "SO":
                        return RedirectToAction("AuditorModifySOBill", "Saler", new { apply_id = ap.id, sys_no = ap.sys_no, step = currentStep });
                    case "MB":
                        return View("ContractEdit");
                    case "CC":
                        return RedirectToAction("AuditorModifyCCMModelContract", "Saler", new { apply_id = ap.id, sys_no = ap.sys_no, step = currentStep });
                    case "CM":
                        return RedirectToAction("AuditorModifyModelContract", "Saler", new { apply_id = ap.id, sys_no = ap.sys_no, step = currentStep });
                    case "SB":
                        return RedirectToAction("AuditorModifySampleBill", "Saler", new { apply_id = ap.id, sys_no = ap.sys_no, step = currentStep });
                    case "BL":
                        return RedirectToAction("AuditorModifyBLBill", "Saler", new { apply_id = ap.id, sys_no = ap.sys_no, step = currentStep });
                    case "TH":
                        var bill = db.ReturnBill.Single(r => r.sys_no == ap.sys_no);
                        ViewData["bill"] = bill;
                        ViewData["details"] = bill.ReturnBillDetail.OrderBy(r => r.entry_no).ToList();
                        ViewData["userName"] = bill.User.real_name;
                        ViewData["status"] = "审核中";
                        ViewData["currentAuditor"] = currentUser.realName;
                        ViewData["return_dep"] = db.Department.Where(d => d.dep_type == "退货事业部" && d.dep_no == bill.return_dept).First().name;
                        if (ad.step_name.Contains("客服")) {
                            return View("EditReturnBillQty");
                        }
                        else if (ad.step_name.Contains("物流")) {
                            return View("LogEditReturnBill");
                        }
                        else {
                            return View("Error");
                        }
                    case "PJ":
                        ViewData["bill"] = db.Project_bills.Single(r => r.sys_no == ap.sys_no);
                        return View("ProjectBillEdit");
                    case "HC":
                        return RedirectToAction("AuditorModifyHCBill", "Saler", new { apply_id = ap.id, sys_no = ap.sys_no, step = currentStep });
                    default:
                        return View("Error");
                }
            }
            else
            {
                utl.writeEventLog("审核单据", "进入只读界面", ap.sys_no + ":" + step.ToString(), Request);
                return View("MarketAudit");
            }
        }

        //审核员处理申请
        public JsonResult HandleAgencyAudit(FormCollection fc)
        {
            int applyId = int.Parse(fc.Get("applyId"));
            Apply ap = db.Apply.Single(a => a.id == applyId);
            int step = int.Parse(fc.Get("step"));
            bool isOK = bool.Parse(fc.Get("okFlag"));
            string comment = fc.Get("agency_comment");
            string backToPrevious = fc.Get("backToPrevious");
            string nextDetails = fc.Get("nextDetails");
            string newProcDept = fc.Get("new_dep");
            string msg = "审核成功";
            //int maxStep = (int)ap.ApplyDetails.Max(ad => ad.step);

            //如果已结束，提示
            if (ap.success != null) {
                utl.writeEventLog("审核单据", "此申请已结束，不能审批:", ap.sys_no + ":" + step.ToString(), Request, 100);
                return Json(new { success = false, msg = "此申请已结束" }, "text/html");
            }
            
            ApplyDetails thisDetail = ap.ApplyDetails.Where(ad => ad.user_id == currentUser.userId && ad.step == step).OrderBy(ad=>ad.pass).First();
            //会签&不是会签的已审批判断
            if (thisDetail.pass != null || ap.ApplyDetails.Where(ad => ad.step == step && ad.pass != null && (ad.countersign == null || ad.countersign == false)).Count() > 0)
            {
                utl.writeEventLog("审核单据", "该订单已被审核:", ap.sys_no + ":" + step.ToString(), Request, 100);
                return Json(new { success = false, msg = "该订单已被审核" }, "text/html");
            }

            int maxStep = (int)db.ApplyDetails.Where(ad => ad.apply_id == ap.id).Max(ad => ad.step);

            #region 退换货申请特殊处理
            if (ap.order_type.Equals("TH"))
            {
                //退换货申请，验证勾稽状态与蓝字发票状态是否一致
                if (isOK && step == maxStep)
                {
                    string validateResult = utl.ValidateHasInvoiceFlag(ap.sys_no);
                    if (!string.IsNullOrEmpty(validateResult))
                    {
                        utl.writeEventLog("审核申请", validateResult, ap.sys_no, Request, 10);
                        return Json(new { success = false, msg = validateResult }, "text/html");
                    }
                }

                //退换货申请，可以退回到上一步 2014-2-25新增
                if (!isOK && backToPrevious.Equals("1") && step > 1)
                {
                    return BackToPreviousStep(ap, step, comment);
                }

                //退换货，市场部林秋海（步骤2）需要将符合规则的意见（以备注：开头）插入到市场部备注字段，然后将意见删除
                if (step == 2)
                {
                    if (comment.Trim().StartsWith("备注：") || comment.Trim().StartsWith("备注:"))
                    {
                        var returnBill = db.ReturnBill.Single(r => r.sys_no == ap.sys_no);
                        returnBill.market_comment = comment.Substring(3);
                        comment = "";
                    }
                }

                //2020-9-14 物流需保存运输费用和责任方
                string expressFee = fc.Get("express_fee");
                string whoToBlame = fc.Get("who_to_blame");
                if (!string.IsNullOrEmpty(expressFee)) {
                    var returnBill = db.ReturnBill.Single(r => r.sys_no == ap.sys_no);
                    returnBill.express_fee = decimal.Parse(expressFee);
                    returnBill.who_to_blame = whoToBlame;
                }

            }
            #endregion

            #region 核心审核代码
            try
            {
                //更新这一步骤的状态                
                thisDetail.ip = Request.UserHostAddress;
                thisDetail.pass = isOK;
                //thisDetail.user_id = userId;
                thisDetail.comment = comment;
                thisDetail.finish_date = DateTime.Now;

                //如果通过并且未到最后一级，则到达下一审核,否则审核失败
                if (!isOK || step == maxStep)
                {
                    ap.success = isOK;
                    ap.finish_date = DateTime.Now;
                    if (isOK)
                    {
                        msg = "最终审核成功，请尽快将数据导入K3.";
                        utl.moveToFormalDir(ap.sys_no);
                        if (ap.order_type.Equals("BL")) {
                            //写入备料单号
                            var bl = db.Sale_BL.Single(b => b.sys_no == ap.sys_no);
                            bl.bill_no = utl.getBLbillNo(bl.market_dep,bl.bus_dep,(int)bl.original_user_id,bl.trade_type_name);

                            //写入备料库存
                            db.Sale_BL_stock.InsertOnSubmit(new Sale_BL_stock()
                            {
                                bill_no = bl.bill_no,
                                bl_time = DateTime.Now,
                                clerk_id = bl.User.id,
                                clerk_name = bl.User.real_name,
                                bus_dep = bl.bus_dep,
                                product_model = bl.product_model,
                                product_name = bl.product_name,
                                product_no = bl.product_no,
                                qty = bl.qty,
                                remain_qty = bl.qty
                            });

                            //备料明细entry_no重新排序
                            int entry_index = 1;
                            foreach (var det in bl.Sale_BL_details.OrderBy(d => d.id)) {
                                det.entry_no = entry_index++;
                            }
                        } else if (ap.order_type.Equals("SB")) {
                            //写入样品单号
                            var sb = db.SampleBill.Single(s => s.sys_no == ap.sys_no);
                            sb.bill_no = utl.getYPBillNo(sb.currency_no, sb.is_free == "免费",sb.account);
                        }
                    }
                }
                //提交数据
                db.SubmitChanges();
                utl.writeEventLog("审核单据", msg + ",审核结果:" + isOK.ToString() + ";审核意见：" + comment, ap.sys_no + ":" + step.ToString(), Request);
            }
            catch (Exception ex)
            {
                utl.writeEventLog("审核单据", "抛出异常：" + ex.Message.ToString(), ap.sys_no + ":" + step.ToString(), Request, -1);
                return Json(new { success = false, msg = "审核发生错误" }, "text/html");
            }
            #endregion

            #region 不是会签或者会签已结束，发送通知邮件给下一环节审批人
            if (thisDetail.countersign == null || thisDetail.countersign == false || ap.ApplyDetails.Where(a => a.step == step && a.pass == null).Count() < 1)
            {
                if (utl.emailToNextAuditor(applyId))
                {
                    utl.writeEventLog("发送邮件", "通知下一环节：发送成功", ap.sys_no + ":" + step.ToString(), Request);
                }
                else
                {
                    utl.writeEventLog("发送邮件", "通知下一环节：发送失败", ap.sys_no + ":" + step.ToString(), Request, -1);
                }
            }
            else {
                utl.writeEventLog("发送邮件", "会签中，不用发送", ap.sys_no + ":" + step.ToString(), Request);
            }
            #endregion

            return Json(new { success = true, msg = msg }, "text/html");
        }

        //退回上一审批人，目前只有退换货申请有这个功能
        public JsonResult BackToPreviousStep(Apply ap, int step, string reason)
        {
            try
            {
                //将上一步的审核操作清空
                ApplyDetails detail = ap.ApplyDetails.Where(a => a.step == step - 1 && a.pass != null).First();

                detail.pass = null;
                detail.comment = "";
                detail.ip = "";
                detail.finish_date = null;

                db.SubmitChanges();
                utl.writeEventLog("审核单据", "审核结果:退回到上一审核步骤", ap.sys_no + ":" + step.ToString(), Request);
            }
            catch (Exception ex)
            {
                utl.writeEventLog("审核单据", "抛出异常：" + ex.Message.ToString(), ap.sys_no + ":" + step.ToString(), Request, -1);
                return Json(new { success = false, msg = "审核发生错误" }, "text/html");
            }

            //如果有，删除母步骤是parentStep的子步骤            
            if (utl.RemoveChildrenStep(ap.id, step - 1))
            {
                utl.writeEventLog("审核单据", "删除子步骤成功", ap.sys_no + ":" + step.ToString(), Request);
            }
            else
            {
                utl.writeEventLog("审核单据", "删除子步骤失败", ap.sys_no + ":" + step.ToString(), Request);
                return Json(new { success = false, msg = "审核发生错误，删除子步骤失败" }, "text/html");
            }

            //发送邮件通知上一环节审批人
            if (utl.emailToPrevious(ap.id, step - 1, reason, currentUser.realName))
            {
                utl.writeEventLog("发送邮件", "通知下一环节：发送成功", ap.sys_no + ":" + step.ToString(), Request);
            }
            else
            {
                utl.writeEventLog("发送邮件", "通知下一环节：发送失败", ap.sys_no + ":" + step.ToString(), Request, -1);
            }

            return Json(new { success = true, msg = "成功退回到上一步骤审核人" });
        }

        //退红字单，客服审核，如果数量有变更，需要通知营业员，插入一个审核步骤
        public JsonResult HandleQtyEditTHAudit(FormCollection fc)
        {
            int applyId = int.Parse(fc.Get("applyId"));
            Apply ap = db.Apply.Single(a => a.id == applyId);
            int step = int.Parse(fc.Get("step"));
            bool isOK = bool.Parse(fc.Get("okFlag"));
            string comment = fc.Get("agency_comment");
            string backToPrevious = fc.Get("backToPrevious");
            string[] FRealQty = fc.Get("FRealQty").Split(',');
            string[] FEntryNo = fc.Get("FEntryNo").Split(',');
            string[] FIsOnline = fc.Get("FIsOnline").Split(',');
            string[] FChDepName = fc.Get("FChDepName").Split(',');
            string FQtyComment = fc.Get("FQtyComment");
            string msg = "审核成功";
            decimal sumRealQty = 0, sumReturnQty = 0;

            //如果审核状态不为空，说明已经被审核了
            if (ap.ApplyDetails.Where(ad => ad.step == step && ad.pass != null).Count() > 0)
            {
                utl.writeEventLog("审核单据", "该订单已被审核:", ap.sys_no + ":" + step.ToString(), Request, 100);
                return Json(new { success = false, msg = "该订单已被审核" }, "text/html");
            }

            //退换货申请，可以退回到上一步 2014-2-25新增
            if (!isOK && backToPrevious.Equals("1") && step > 1)
            {
                return BackToPreviousStep(ap, step, comment);
            }

            try
            {
                //更新这一步骤的状态
                ApplyDetails thisDetail = ap.ApplyDetails.Where(ad => ad.user_id == currentUser.userId && ad.step == step).First();
                thisDetail.ip = Request.UserHostAddress;
                thisDetail.pass = isOK;
                thisDetail.comment = comment;
                thisDetail.finish_date = DateTime.Now;

                var returnBill = db.ReturnBill.Where(r => r.sys_no == ap.sys_no).First();
                returnBill.qty_comment = FQtyComment;

                if (!isOK)
                {
                    ap.success = false;
                    ap.finish_date = DateTime.Now;
                }
                else
                {
                    //将实退数量update回退货单
                    var dets = returnBill.ReturnBillDetail;
                    //int j = 0;
                    sumRealQty = FRealQty.Sum(r => decimal.Parse(r));
                    sumReturnQty = (decimal)dets.Select(d => new { d.entry_no, d.return_qty }).Distinct().Sum(d => d.return_qty);

                    //重新新增一遍
                    for (int i = 0; i < FRealQty.Length; i++)
                    {
                        int entryNo = Int32.Parse(FEntryNo[i]);
                        var oldRdb = dets.Where(d => d.entry_no == entryNo).First();
                        ReturnBillDetail rbd = new ReturnBillDetail();
                        rbd.bill_id = oldRdb.bill_id;
                        rbd.aux_qty = oldRdb.aux_qty;
                        rbd.can_apply_qty = oldRdb.can_apply_qty;
                        rbd.entry_no = oldRdb.entry_no;
                        rbd.product_model = oldRdb.product_model;
                        rbd.product_name = oldRdb.product_name;
                        rbd.product_number = oldRdb.product_number;
                        rbd.return_qty = oldRdb.return_qty;
                        rbd.seorder_no = oldRdb.seorder_no;
                        rbd.stock_entry_id = oldRdb.stock_entry_id;
                        rbd.stock_inter_id = oldRdb.stock_inter_id;
                        rbd.stock_no = oldRdb.stock_no;
                        rbd.real_return_qty = decimal.Parse(FRealQty[i]);
                        rbd.is_online = FIsOnline[i].Equals("已上线") ? true : false;
                        rbd.ch_dep_name = FChDepName[i];
                        db.ReturnBillDetail.InsertOnSubmit(rbd);
                    }

                    //将旧的删除
                    db.ReturnBillDetail.DeleteAllOnSubmit(dets);                                        

                }
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                utl.writeEventLog("审核单据", "抛出异常：" + ex.Message.ToString(), ap.sys_no + ":" + step.ToString(), Request, -100);
                return Json(new { success = false, msg = "审核发生错误" }, "text/html");
            }

            if (isOK)
            {
                //审核成功之后，插入出货组和营业
                foreach (var FChDep in FChDepName.Distinct().OrderBy(c => c))
                {
                    if (!string.IsNullOrEmpty(FChDep) && !FChDep.Equals("无"))
                    {
                        int chDepId = (int)db.Department.Single(d => d.name == FChDep && d.dep_type == "退货出货组").dep_no;
                        //int?[] chAuditors = db.ReturnDeptStepAuditor.Where(r => r.step_name == "出货组" && r.return_dept == chDepId).Select(r => r.user_id).ToArray();
                        int?[] chAuditors = db.AuditorsRelation.Where(a => a.step_name == "RED_事业部出货组" && a.relate_value == chDepId).Select(a => a.auditor_id).ToArray();

                        //先插入出货组审核，如果有需要，再插入营业审核，这样营业就会排在出货组之前
                        if (utl.InsertStepAfterStep(applyId, step, FChDep + "出货组审核", chAuditors))
                        {
                            utl.writeEventLog("审核单据", "插入出货组审核环节成功", ap.sys_no + ":" + step.ToString() + ";dep:" + FChDep, Request);
                        }
                        else
                        {
                            utl.writeEventLog("审核单据", "插入出货组审核环节失败", ap.sys_no + ":" + step.ToString() + ";dep:" + FChDep, Request, -100);
                        }

                    }
                }
                if (Math.Abs(sumReturnQty - sumRealQty) > 0.00001m)
                {
                    //首先判断下一步是不是已经营业员了
                    if (ap.ApplyDetails.Where(ad => ad.step == step + 1).First().user_id != ap.user_id)
                    {
                        //插入下一步，由流程发起者确认数量变更
                        if (utl.InsertStepAfterStep(applyId, step, "营业员确认", new int?[] { ap.user_id }))
                        {
                            utl.writeEventLog("审核单据", "插入营业员确认环节成功", ap.sys_no + ":" + step.ToString(), Request);
                        }
                        else
                        {
                            utl.writeEventLog("审核单据", "插入营业员确认环节失败", ap.sys_no + ":" + step.ToString(), Request, -100);
                        }
                    }
                }
            }
            //发送邮件通知下一环节审批人
            if (utl.emailToNextAuditor(applyId))
            {
                utl.writeEventLog("发送邮件", "通知下一环节：发送成功", ap.sys_no + ":" + step.ToString(), Request);
            }
            else
            {
                utl.writeEventLog("发送邮件", "通知下一环节：发送失败", ap.sys_no + ":" + step.ToString(), Request, -1);
            }

            return Json(new { success = true, msg = msg }, "text/html");
        }
        //审核员挂起申请
        public JsonResult BlockOrder(FormCollection fc)
        {
            int applyId = int.Parse(fc.Get("applyId"));
            Apply ap = db.Apply.Single(a => a.id == applyId);
            int step = int.Parse(fc.Get("step"));
            string comment = fc.Get("agency_comment");

            //验证是否有重复挂起操作
            var existblocks = db.BlockOrder.Where(b => b.sys_no == ap.sys_no && b.step == step);
            if (existblocks.Count() > 0)
            {
                return Json(new { success = false, msg = "不能重复进行挂起操作。" }, "text/html");
            }

            db.BlockOrder.InsertOnSubmit(new BlockOrder()
            {
                @operator = currentUser.userId,
                block_time = DateTime.Now,
                step = step,
                step_name = ap.ApplyDetails.Where(ad => ad.step == step).First().step_name,
                reason = comment,
                sys_no = ap.sys_no
            });
            db.SubmitChanges();

            utl.writeEventLog("审核单据", "将订单暂时挂起", ap.sys_no, Request);

            //发送通知邮件给申请者
            if (utl.emailForBlock(applyId, currentUser.userId, comment))
            {
                utl.writeEventLog("发送邮件", "挂起通知营业员：发送成功", ap.sys_no + ":" + step.ToString(), Request);
            }
            else
            {
                utl.writeEventLog("发送邮件", "挂起通知营业员：发送失败", ap.sys_no + ":" + step.ToString(), Request);
            }

            return Json(new { success = true, msg = "挂起成功" }, "text/html");
        }

        //刷新处理结果
        public JsonResult RefleshAuditResult(int applyId, int step)
        {
            var details = db.ApplyDetails.Where(ad => ad.apply_id == applyId && ad.step == step);
            bool hasAudited = false;
            bool? pass = false;
            string comment = "";

            if (details.Where(d => d.pass == false).Count() > 0)
            {
                //被NG的
                hasAudited = true;
                pass = false;
                comment = details.Where(d => d.user_id == currentUser.userId).First().comment;
            }
            else if (details.Where(d => d.pass != null).Count() == 0)
            {
                //全部未被审核的
                hasAudited = false;
            }
            else { 
                //部分被审核，且没有NG的，分会签和不会签两种情况
                if (details.First().countersign == false || details.First().countersign == null)
                {
                    //不是会签
                    hasAudited = true;
                    pass = true;
                    comment = details.Where(d => d.user_id == currentUser.userId).First().comment;
                }
                else { 
                    //是会签
                    if (details.Where(d => d.user_id == currentUser.userId && d.pass == null).Count() > 0)
                    {
                        hasAudited = false;
                    }
                    else {
                        hasAudited = true;
                        pass = true;
                        comment = details.Where(d => d.user_id == currentUser.userId).First().comment;
                    }
                }
            }

            //此步骤已审核的：1.步骤内必须至少有一人审核结果不为空；2. 此人是自己或者此人不是会签里的人
            //if (details.Where(d => d.user_id == userId && d.pass != null).Count() > 0)
            //{
            //    hasAudited = true;
            //    pass = details.Where(d => d.user_id == userId && d.pass != null).First().pass;
            //    comment = details.Where(d => d.user_id == userId && d.pass != null).First().comment;
            //}
            //else if (details.First().countersign == false || details.First().countersign == null)
            //{
            //    //不是会签
            //    var detailsTmp = details.Where(d => d.pass != null);
            //    if (detailsTmp.Count() > 0)
            //    {
            //        hasAudited = true;
            //        pass = detailsTmp.First().pass;
            //        comment = detailsTmp.First().comment;
            //    }
            //}
            //else
            //{
            //    //会签
            //    var detailsNG = details.Where(d => d.pass == false);
            //    if (detailsNG.Count() > 0)
            //    {
            //        hasAudited = true;
            //        pass = false;
            //        comment = "";
            //    }
            //}
            //取得处理结果
            return Json(new { success = hasAudited, pass = pass, comment = comment });
        }

        //营业员通过id与单据类型查看申请状态
        public JsonResult CheckApplyStatusSO(string sys_no)
        {
            string nextStepName = "无";
            string nextAuditors = "无";
            string pre = sys_no.Substring(0, 2);
            var apps = db.Apply.Where(a => a.sys_no == sys_no);
            if (apps.Count() == 0)
            {
                return Json(new { success = false }, "text/html");   //未提交申请
            }
            Apply app = apps.First();
            List<AuditStatusModel> list = new List<AuditStatusModel>();
            list.Add(new AuditStatusModel()
            {
                auditor = app.User.real_name,
                department=app.User.Department1.name,
                step = 0,
                stepName = "提交申请",
                date = ((DateTime)app.start_date).ToShortDateString(),
                time = ((DateTime)app.start_date).ToShortTimeString(),
                pass = true
            });
            AuditStatusModel asm;
            //int maxStep = db.Process.Where(p => p.bill_type == pre & p.is_finish==true).First().ProcessDetail.Max(pr => pr.step);
            int maxStep = (int)app.ApplyDetails.Max(ad => ad.step);
            foreach (var appDetail in app.ApplyDetails.Where(ap => ap.pass != null).OrderBy(ap => ap.finish_date))
            {
                asm = new AuditStatusModel();
                asm.step = (int)appDetail.step;
                asm.stepName = appDetail.step_name;
                //asm.stepName = utl.getStepName(asm.step);
                asm.auditor = appDetail.User.real_name;
                asm.department = appDetail.User.Department1.name;
                asm.date = ((DateTime)appDetail.finish_date).ToShortDateString();
                asm.time = ((DateTime)appDetail.finish_date).ToShortTimeString();
                asm.pass = appDetail.pass;
                asm.comment = appDetail.comment;
                list.Add(asm);
            }
            //审核成功
            if (app.success == true)
            {
                list.Add(new AuditStatusModel()
                {
                    step = maxStep + 1,
                    stepName = "完成申请",
                    auditor = "系统",
                    department="信息中心",
                    date = ((DateTime)app.finish_date).ToShortDateString(),
                    time = ((DateTime)app.finish_date).ToShortTimeString(),
                    pass = true
                });
            }
            else if (app.success == null)
            {
                //已有审核的最大审核步骤
                int? currentStep = app.ApplyDetails.Where(a => a.pass == true).Max(a => a.step);
                int nextStep ;
                //如果此步骤不是会签的，那么下一步骤=此步骤+1；否则如果是会签的，但是已经会签结束了，也是+1；
                if (currentStep == null) {
                    nextStep = 1;
                }
                else if (app.ApplyDetails.Where(a => a.step == currentStep && (a.countersign == null || a.countersign == false)).Count() > 0)
                {
                    nextStep = (int)currentStep + 1;
                }
                else if (app.ApplyDetails.Where(a => a.step == currentStep && a.pass == null).Count() < 1) {
                    nextStep = (int)currentStep + 1;
                }
                else
                {
                    //会签
                    nextStep = (int)currentStep;
                }
                foreach (var det in app.ApplyDetails.Where(a => a.step == nextStep && a.pass == null))
                {
                    if (nextAuditors.Equals("无"))
                    {
                        nextAuditors = det.User.real_name;
                        nextStepName = det.step_name;
                    }
                    else
                    {
                        nextAuditors += "/" + det.User.real_name;
                    }
                }

            }
            utl.writeEventLog("查看状态", "单据审核流转记录", sys_no, Request);
            return Json(new { success = true, result = list, nextAuditors = nextAuditors, nextStepName = nextStepName });
        }
       
        public JsonResult GetGroupUnits(int id)
        {
            var gr = from g in db.getGroupBelongToUnit(id)
                     select new
                     {
                         id = g.unit_id,
                         name = g.unit_name
                     };
            return Json(gr, "text/html");

        }
        
        ////保存订单表头
        //[HttpPost]
        //public JsonResult saveSaleOrder(FormCollection col)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

        //    //表头
        //    string billType = col.Get("bill_type");
        //    int stepVersion = Int32.Parse(col.Get("step_version"));
        //    string orderDate = col.Get("order_date");
        //    string sysNum = col.Get("sys_no");
        //    int proc_dep_id = Int32.Parse(col.Get("proc_dep"));
        //    string agency = col.Get("agency");
        //    string projectGroup = col.Get("project_group");
        //    string product_type = col.Get("product_type");
        //    string product_use = col.Get("product_use");
        //    string currency = col.Get("currency");
        //    string exchange = col.Get("exchange");
        //    string clearingWay = col.Get("clearing_way");
        //    string contractNo = col.Get("contract_no");
        //    string buyUnit = col.Get("buy_unit");
        //    string finalClient = col.Get("final_client");
        //    string planFirm = col.Get("plan_firm");
        //    string order_no = col.Get("Order_no");
        //    string trade_type = col.Get("trade_type");
        //    string order_type = col.Get("order_type");
        //    string sale_way = col.Get("sale_way");
        //    string oversea_client = col.Get("oversea_client");
        //    string trade_rule = col.Get("trade_rule");

        //    string salerPercentage = col.Get("saler_percent");

        //    //表尾
        //    string clerk = col.Get("clerk");
        //    string clerk2 = col.Get("clerk2");
        //    string clerk3 = col.Get("clerk3");
        //    //string group1 = col.Get("group1");
        //    //string group2 = col.Get("group2");
        //    string percent1 = col.Get("percent1");
        //    string percent2 = col.Get("percent2");
        //    string percent3 = col.Get("percent3");
        //    string deliveryPlace = col.Get("delivery_place");
        //    string overseaPercentage = col.Get("oversea_percentage");
        //    string backpaperConfirm = col.Get("backpaper_confirm");
        //    string produceWay = col.Get("produce_way");
        //    string printTruly = col.Get("print_truly");
        //    string clientLogo = col.Get("client_logo");
        //    string description = col.Get("description");
        //    string further_info = col.Get("further_info");
        //    string charger = col.Get("charger");
        //    //string create_user = col.Get("create_user");

        //    #region 查询这张订单是否已被审核
        //    var existedBills = db.Order.Where(o => o.sys_no == sysNum && o.step_version == stepVersion);
        //    if (existedBills.Count() > 0)
        //    {
        //        utl.writeEventLog("审核单据_保存订单", "订单已被审核,step" + stepVersion.ToString(), sysNum, Request, 100);
        //        return Json(new { success = false, msg = "该订单已被审核" }, "text/html");
        //    }
        //    #endregion

        //    #region 验证表尾说明字段的字符长度，不能超过255个字节。一个汉字包含2个字节。
        //    if (Encoding.Default.GetBytes(description).Length > 255)
        //    {
        //        return Json(new { success = false, msg = "【说明】字段内容太长，不能超过255个字符，请精简后再保存。注意：1个中文和全角符号算2个字符，1个英文、数字和半角符号算1个字符。" }, "text/html");
        //    }
        //    if (Encoding.Default.GetBytes(further_info).Length > 1000)
        //    {
        //        return Json(new { success = false, msg = "【补充说明】字段内容太长，不能超过1000个字符，请精简后再保存。注意：1个中文和全角符号算2个字符，1个英文、数字和半角符号算1个字符。" }, "text/html");
        //    }
        //    #endregion

        //    #region 市场部下单组必须保证订单编号已填写，此时step为4，且订单编号在K3中不存在
        //    //if (stepVersion == 4)
        //    //{
        //    if (string.IsNullOrWhiteSpace(order_no))
        //    {
        //        return Json(new { success = false, msg = "订单编号必须由下单组填写，保存失败" }, "text/html");
        //    }
        //    bool? existflag = false;
        //    db.isDublicatedBillNo(order_no, "SO", ref existflag);
        //    if (existflag == true)
        //    {
        //        utl.writeEventLog("审核单据_保存订单", "订单编号在K3已经存在，保存失败,step:" + stepVersion.ToString(), sysNum, Request, 100);
        //        return Json(new { success = false, msg = "订单编号在K3已经存在，保存失败" }, "text/html");
        //    }
        //    if (db.Order.Where(o => o.order_no == order_no && o.step_version == stepVersion).Count() > 0)
        //    {
        //        utl.writeEventLog("审核单据_保存订单", "订单编号在下单系统中已经存在，保存失败,step:" + stepVersion.ToString(), sysNum, Request, 100);
        //        return Json(new { success = false, msg = "订单编号在下单系统中已经存在，不能重复保存" }, "text/html");
        //    }
        //    //}
        //    #endregion

        //    #region 验证贸易类型和客户的关系
        //    //根据触发器[Truly_SEOrder_FXGD]改编，贸易类型，客户和海外客户有着制约关系。
        //    //控制客户编码是以01，02开头的SO订单只能下国内贸易的单除三星外
        //    //--控制客户是以香港信利的贸易类型不能为国内贸易
        //    //--控制客户是以香港信利的海外客户必须是以05,06,04开头的单
        //    int buyUint_id = Int32.Parse(buyUnit);
        //    getCostomerByIdResult customer = db.getCostomerById(buyUint_id).First();
        //    getCostomerByIdResult overseaclient = db.getCostomerById(Int32.Parse(oversea_client)).First(); ;
        //    int tradeType = Int32.Parse(trade_type);
        //    if (buyUint_id != 559 && buyUint_id != 560)
        //    {
        //        if (tradeType != 1588 && (customer.number.StartsWith("01.") || customer.number.StartsWith("02.")) && !customer.name.Contains("三星") && !customer.name.Contains("SAMSUNG"))
        //        {
        //            return Json(new { success = false, msg = "国内单贸易类型必须为国内贸易" }, "text/html");
        //        }
        //    }
        //    else
        //    {
        //        if (tradeType == 1588)
        //        {
        //            return Json(new { success = false, msg = "国外单贸易类型不能为国内贸易" }, "text/html");
        //        }
        //        if (!(new string[] { "03.", "04.", "05." }).Contains(overseaclient.number.Substring(0, 3)))
        //        {
        //            return Json(new { success = false, msg = "国外单海外客户必须选择国外客户" }, "text/html");
        //        }
        //    }
        //    #endregion

        //    #region 验证项目编号与客户是否相互对应,467表示无客户编号
        //    string[] p_project_number = col.Get("p_project_number").Split(',');
        //    int pn_int;
        //    foreach (var pn in p_project_number)
        //    {
        //        if (string.IsNullOrEmpty(pn))
        //        {
        //            return Json(new { success = false, msg = "保存失败：项目编号不能为空" }, "text/html");
        //        }
        //        if (!Int32.TryParse(pn, out pn_int))
        //        {
        //            return Json(new { success = false, msg = "保存失败：项目名称[" + pn + "]不合法，必须在列表中选择" }, "text/html");
        //        }
        //        if (pn_int == 467)
        //        {
        //            continue;
        //        }
        //        else
        //        {
        //            if (db.VwProjectNumber.Where(v => v.id == pn_int && (v.client_number == customer.number || v.client_number == overseaclient.number)).Count() < 1)
        //            {
        //                return Json(new { success = false, msg = "保存失败：项目编号 " + pn + " 不属于当前客户。" }, "text/html");
        //            }
        //        }
        //    }
        //    #endregion

        //    //保存表单
        //    Order otp = new Order();
        //    //单据类别，1:销售订单；2：销售合同；3：开模销售合同
        //    otp.bill_type = short.Parse(billType);
        //    otp.step_version = stepVersion;
        //    otp.user_id = userId;
        //    if (!string.IsNullOrEmpty(orderDate))
        //        otp.order_date = DateTime.Parse(orderDate);
        //    otp.sys_no = sysNum;
        //    otp.proc_dep_id = proc_dep_id;
        //    otp.department_id = Int32.Parse(agency);
        //    otp.project_group = Int32.Parse(projectGroup);
        //    otp.product_type = Int32.Parse(product_type);
        //    otp.product_use = product_use;
        //    otp.currency = Int32.Parse(currency);
        //    if (!string.IsNullOrEmpty(exchange))
        //        otp.exchange_rate = double.Parse(exchange);
        //    otp.clearing_way = Int32.Parse(clearingWay);
        //    otp.contract_no = contractNo;
        //    otp.buy_unit = Int32.Parse(buyUnit);
        //    if (!string.IsNullOrEmpty(finalClient))
        //        otp.final_client = Int32.Parse(finalClient);
        //    if (!string.IsNullOrEmpty(planFirm))
        //        otp.plan_firm = Int32.Parse(planFirm);
        //    otp.order_no = order_no;
        //    otp.trade_type = Int32.Parse(trade_type);
        //    if (!string.IsNullOrWhiteSpace(order_type))
        //        otp.order_type = Int32.Parse(order_type);
        //    //otp.order_type = 40544;//生产单
        //    otp.sale_way = Int32.Parse(sale_way);
        //    if (!string.IsNullOrEmpty(oversea_client))
        //    {
        //        otp.oversea_client = Int32.Parse(oversea_client);
        //    }
        //    if (!string.IsNullOrEmpty(trade_rule))
        //    {
        //        otp.trade_rule = Int32.Parse(trade_rule);
        //    }
        //    //otp.create_user = Int32.Parse(create_user);
        //    if (!string.IsNullOrEmpty(charger))
        //    {
        //        otp.charger = Int32.Parse(charger);
        //    }
        //    otp.delivery_place = deliveryPlace;
        //    otp.oversea_percentage = string.IsNullOrEmpty(overseaPercentage) ? 0 : decimal.Parse(overseaPercentage);
        //    if (!string.IsNullOrEmpty(backpaperConfirm))
        //        otp.backpaper_confirm = Int32.Parse(backpaperConfirm);
        //    if (!string.IsNullOrEmpty(produceWay))
        //        otp.produce_way = Int32.Parse(produceWay);
        //    if (!string.IsNullOrEmpty(printTruly))
        //        otp.print_truly = Int32.Parse(printTruly);
        //    if (!string.IsNullOrEmpty(clientLogo))
        //        otp.client_logo = Int32.Parse(clientLogo);
        //    otp.description = description;
        //    otp.further_info = further_info;
        //    otp.salePs = salerPercentage;

        //    otp.clerk = Int32.Parse(clerk);
        //    //otp.group1 = group1;
        //    //otp.group2 = group2;
        //    otp.percent1 = decimal.Parse(percent1);
        //    otp.percent2 = string.IsNullOrWhiteSpace(percent2) ? 0 : decimal.Parse(percent2);
        //    otp.percent3 = string.IsNullOrWhiteSpace(percent3) ? 0 : decimal.Parse(percent3);
        //    if (!string.IsNullOrWhiteSpace(clerk2))
        //    {
        //        otp.clerk2 = Int32.Parse(clerk2);
        //    }
        //    if (!string.IsNullOrWhiteSpace(clerk3))
        //    {
        //        otp.clerk3 = Int32.Parse(clerk3);
        //    }
        //    db.Order.InsertOnSubmit(otp);

        //    //保存表体
        //    if (!saveOrderDetails(col, otp))
        //    {
        //        utl.writeEventLog("审核单据_保存订单", "表体保存失败,step" + stepVersion.ToString(), sysNum, Request, -1);
        //        return Json(new { success = false, msg = "保存表体失败。" }, "text/html");
        //    }

        //    try
        //    {
        //        db.SubmitChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        utl.writeEventLog("审核单据_保存订单", "保存事务提交失败,exception:" + ex.Message.ToString(), sysNum, Request, -1);
        //        return Json(new { success = false }, "text/html");
        //    }
        //    utl.writeEventLog("审核单据_保存订单", "保存成功", sysNum, Request);
        //    return Json(new { success = true, orderId = otp.id }, "text/html");
        //}

        ////保存订单表体
        //public bool saveOrderDetails(FormCollection col, Order order)
        //{
        //    string[] p_ids = col.Get("p_id").Split(',');
        //    string[] p_qty = col.Get("p_qty").Split(',');
        //    string[] p_quote = col.Get("p_quote").Split(',');
        //    string[] p_cost = col.Get("p_cost").Split(',');
        //    string[] p_deal = col.Get("p_deal").Split(',');
        //    string[] p_aux = col.Get("p_aux").Split(',');
        //    string[] p_del_date = col.Get("p_del_date").Split(',');
        //    string[] p_tar_date = col.Get("p_tar_date").Split(',');
        //    string[] p_comment = col.Get("p_comment").Split(',');
        //    string[] p_MU = col.Get("p_MU").Split(',');
        //    string[] p_commission = col.Get("p_commission").Split(',');
        //    string[] p_commissionRate = col.Get("p_commissionRate").Split(',');
        //    string[] p_feeRate = col.Get("p_feeRate").Split(',');
        //    string[] p_disccountRate = col.Get("p_disccountRate").Split(',');
        //    string[] p_unit = col.Get("p_unit").Split(',');
        //    string[] p_unit_price = col.Get("p_unit_price").Split(',');
        //    string[] p_tax_rate = col.Get("p_tax_rate").Split(',');
        //    string[] p_suggest_date = col.Get("p_suggest_date").Split(',');
        //    string[] p_confirm_date = col.Get("p_confirm_date").Split(',');
        //    string[] p_project_number = col.Get("p_project_number").Split(',');

        //    //保存表体
        //    try
        //    {
        //        List<OrderDetail> ots = new List<OrderDetail>();
        //        for (int i = 0; i < p_ids.Count(); i++)
        //        {
        //            OrderDetail od = new OrderDetail();
        //            od.Order = order;
        //            od.entry_id = i + 1;
        //            od.product_id = Int32.Parse(p_ids[i]);
        //            od.qty = decimal.Parse(p_qty[i]);
        //            od.quote_no = p_quote[i];
        //            od.cost = string.IsNullOrEmpty(p_cost[i]) ? 0 : decimal.Parse(p_cost[i]);
        //            od.deal_price = string.IsNullOrEmpty(p_deal[i]) ? 0 : decimal.Parse(p_deal[i]);
        //            od.aux_tax_price = string.IsNullOrEmpty(p_aux[i]) ? 0 : decimal.Parse(p_aux[i]);
        //            od.delivery_date = string.IsNullOrEmpty(p_del_date[i]) ? null : (DateTime?)(DateTime.Parse(p_del_date[i]));
        //            od.target_date = string.IsNullOrEmpty(p_tar_date[i]) ? null : (DateTime?)(DateTime.Parse(p_tar_date[i]));
        //            od.comment = p_comment[i];
        //            od.MU = decimal.Parse(p_MU[i]);
        //            od.commission = decimal.Parse(p_commission[i]);
        //            od.commission_rate = decimal.Parse(p_commissionRate[i]);
        //            od.fee_rate = decimal.Parse(p_feeRate[i]);
        //            od.discount_rate = string.IsNullOrEmpty(p_disccountRate[i]) ? 0 : decimal.Parse(p_disccountRate[i]);
        //            od.unit = Int32.Parse(p_unit[i]);
        //            od.unit_price = string.IsNullOrEmpty(p_unit_price[i]) ? 0 : decimal.Parse(p_unit_price[i]);
        //            od.tax_rate = string.IsNullOrEmpty(p_tax_rate[i]) ? 0 : decimal.Parse(p_tax_rate[i]);
        //            od.suggested_delivery_date = string.IsNullOrEmpty(p_suggest_date[i]) ? null : (DateTime?)(DateTime.Parse(p_suggest_date[i]));
        //            od.confirm_date = string.IsNullOrEmpty(p_confirm_date[i]) ? null : (DateTime?)(DateTime.Parse(p_confirm_date[i]));
        //            od.project_number = string.IsNullOrEmpty(p_project_number[i]) ? 467 : Int32.Parse(p_project_number[i]);//467表示无客户编码
        //            od.customer_po = db.Order.Where(o => o.sys_no == order.sys_no).First().OrderDetail.Where(e => e.product_id == od.product_id).FirstOrDefault().customer_po;
        //            od.customer_pn = db.Order.Where(o => o.sys_no == order.sys_no).First().OrderDetail.Where(e => e.product_id == od.product_id).FirstOrDefault().customer_pn;
        //            ots.Add(od);
        //        }

        //        db.OrderDetail.InsertAllOnSubmit(ots);

        //        //db.SubmitChanges();
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}
        
        //查看订单信息


        //获取订单营业员比例
        //public JsonResult GetSalerPercentage(int id)
        //{
        //    //取得step=0的order_id
        //    string sys_no = db.Order.Single(o => o.id == id).sys_no;
        //    int orderId = db.Order.Where(o => o.sys_no == sys_no && o.step_version == 0).First().id;
        //    var sps = from sp in db.SalerPercentage
        //              where sp.order_id == orderId
        //              select new
        //              {
        //                  sale_name = sp.saler_name,
        //                  percent = sp.percentage
        //              };
        //    if (sps.Count() >= 0)
        //    {
        //        string res = "";
        //        foreach (var sp in sps)
        //        {
        //            res += string.Format("{0}:{1}%;", sp.sale_name, Math.Round((double)sp.percent, 1));
        //        }
        //        return Json(new { success = true, ps = res }, "text/html");
        //    }
        //    return Json(new { success = false }, "text/html");
        //}

        //审核人保存样品单

        //public JsonResult AuditorSaveSampleBill(FormCollection fc)
        //{
        //    int step = -1;
        //    if (!Int32.TryParse(fc.Get("step"), out step)) {
        //        return Json(new { suc = false, msg = "步骤不对，保存失败" }, "text/html");
        //    }

        //    string saveResult = utl.saveSampleBill(fc, step, currentUser.userId);
        //    if (string.IsNullOrWhiteSpace(saveResult)) {
        //        return Json(new { suc = true }, "text/html");
        //    }
        //    else {
        //        return Json(new { suc = false, msg = saveResult }, "text/html");
        //    }
        //}

        ////审核人保存CCM开改模单
        public JsonResult AuditorSaveCCMModelContract(FormCollection fc)
        {
            int step = -1;
            if (!Int32.TryParse(fc.Get("step"), out step)) {
                return Json(new { suc = false, msg = "步骤不对，保存失败" }, "text/html");
            }

            string saveResult = utl.saveCCMModelContract(fc, step, currentUser.userId);
            if (string.IsNullOrWhiteSpace(saveResult)) {
                return Json(new { suc = true }, "text/html");
            }
            else {
                return Json(new { suc = false, msg = saveResult }, "text/html");
            }
        }

        //审核人保存开改模单
        //public JsonResult AuditorSaveModelContract(FormCollection fc)
        //{
        //    int step = -1;
        //    if (!Int32.TryParse(fc.Get("step"), out step))
        //    {
        //        return Json(new { suc = false, msg = "步骤不对，保存失败" }, "text/html");
        //    }

        //    string saveResult = utl.saveModelContract(fc, step, currentUser.userId);
        //    if (string.IsNullOrWhiteSpace(saveResult))
        //    {
        //        return Json(new { suc = true }, "text/html");
        //    }
        //    else
        //    {
        //        return Json(new { suc = false, msg = saveResult }, "text/html");
        //    }
        //}

        //审核人保存备料单
        public JsonResult AuditorSaveBLBill(FormCollection fc)
        {
            int step = -1;
            if (!Int32.TryParse(fc.Get("step"), out step)) {
                return Json(new { suc = false, msg = "步骤不对，保存失败" }, "text/html");
            }
            string sysNo = fc.Get("sys_no");
            string stepName = db.Apply.Single(a => a.sys_no == sysNo).ApplyDetails.Where(ad => ad.step == step).First().step_name;
            Sale_BL bl = db.Sale_BL.Single(s => s.sys_no == sysNo);

            if (stepName.Contains("成控")) {
                decimal dealPrice = decimal.Parse(fc.Get("deal_price"));
                if (dealPrice != bl.deal_price) {
                    utl.writeEventLog("备料单", "成控修改成交价：" + bl.deal_price + "->" + dealPrice, bl.sys_no, Request);
                    bl.deal_price = dealPrice;
                }
            }else if (stepName.Contains("计划")) {
                //计划员指定订料员
                string orderIds = fc.Get("order_ids");
                string orderNames = fc.Get("order_names");
                string plannerComment = fc.Get("planner_comment");
                if (string.IsNullOrEmpty(orderIds)) {
                    return Json(new { suc = false, msg = "必须至少选择一个订料员" }, "text/html");
                }

                bl.order_ids = orderIds;
                bl.order_names = orderNames;
                bl.update_user_id = currentUser.userId;
                bl.step_version = step;
                bl.planner_comment = plannerComment;
            }
            else if (stepName.Contains("订料")) {
                //订料员只能修改备料明细
                string blDetails = fc.Get("Sale_BL_details");
                var details = JsonConvert.DeserializeObject<List<Sale_BL_details>>(blDetails);
                if (details.Count() == 0) {
                    return Json(new { suc = true }, "text/html");
                    //return Json(new { suc = false, msg = "必须录入备料清单明细" }, "text/html");
                }
                if (!utl.ModelsToString<Sale_BL_details>(bl.Sale_BL_details.ToList()).Equals(utl.ModelsToString<Sale_BL_details>(details))) {
                    int entryNo = 1;
                    foreach (var detail in details) {
                        if (detail.order_qty == null || detail.order_qty == 0) {
                            return Json(new { suc = false, msg = "第" + entryNo + "行的订料数量必须填写且不能为0" }, "text/html");
                        }
                        detail.entry_no = entryNo++;
                    }

                    //先备份数据
                    BackupData bd = new BackupData();
                    bd.sys_no = sysNo;
                    bd.user_id = bl.update_user_id;
                    bd.op_date = DateTime.Now;
                    bd.secondary_data = utl.ModelsToString<Sale_BL_details>(bl.Sale_BL_details.ToList());
                    db.BackupData.InsertOnSubmit(bd);

                    //因为是会签，同时审批时如果将旧数据删除，会造成数据丢失的情况，A、B同时编辑时，A保存后，B再保存，那么A编辑的内容将会消失。
                    //改为只删除和插入自己的那些分录，其它不动。

                    db.Sale_BL_details.DeleteAllOnSubmit(bl.Sale_BL_details.Where(b => b.order_id == currentUser.userId));
                    bl.Sale_BL_details.AddRange(details.Where(d => d.order_id == currentUser.userId));
                    bl.update_user_id = currentUser.userId;
                    bl.step_version = step;
                }
            }

            try {
                db.Sale_BL_details.DeleteAllOnSubmit(db.Sale_BL_details.Where(d => d.bl_id == null));
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return Json(new { suc = false, msg = ex.Message }, "text/html");
            }

            return Json(new { suc = true }, "text/html");
        }

        //审核人保存华为出货报告
        public JsonResult AuditorSaveHCBill(FormCollection fc)
        {
            int step = -1;
            if (!Int32.TryParse(fc.Get("step"), out step)) {
                return Json(new { suc = false, msg = "步骤不对，保存失败" }, "text/html");
            }

            string saveResult = utl.saveHCBill(fc, step, currentUser.userId);
            if (string.IsNullOrWhiteSpace(saveResult)) {
                return Json(new { suc = true }, "text/html");
            }
            else {
                return Json(new { suc = false, msg = saveResult }, "text/html");
            }
        }

        public string test()
        {
            return utl.ValidateHasInvoiceFlag("SWTH18082901");
        }
    }
}
