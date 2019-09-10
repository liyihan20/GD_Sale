using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Models;
using Sale_Order.Filter;
using Sale_Order.Utils;
namespace Sale_Order.Controllers
{
    public class K3AuditController : Controller
    {
        //SaleDBDataContext db = new SaleDBDataContext();
        //SomeUtils utl = new SomeUtils();
        ////通过id查看变更申请状态
        //public JsonResult CheckApplyUpdateStatus(int id)
        //{
        //    var apps = db.SaleUpdateApply.Where(su => su.id == id);
        //    if (apps.Count() == 0)
        //    {
        //        return Json(new { success = false });   //未提交申请
        //    }
        //    SaleUpdateApply app = apps.First();
        //    List<AuditStatusModel> list = new List<AuditStatusModel>();
        //    list.Add(new AuditStatusModel()
        //    {
        //        auditor = app.User.real_name,
        //        step = 0,
        //        stepName = "提交申请",
        //        date = ((DateTime)app.start_date).ToShortDateString(),
        //        time = ((DateTime)app.start_date).ToShortTimeString(),
        //        pass = true
        //    });
        //    AuditStatusModel asm;
        //    foreach (var appDetail in app.UpdateAudit)
        //    {
        //        asm = new AuditStatusModel();
        //        asm.step = (int)appDetail.step;
        //        asm.stepName = utl.getStepName(asm.step);
        //        if (appDetail.pass != null)
        //        {
        //            asm.auditor = appDetail.User.real_name;
        //            asm.date = ((DateTime)appDetail.finish_date).ToShortDateString();
        //            asm.time = ((DateTime)appDetail.finish_date).ToShortTimeString();
        //            asm.pass = appDetail.pass;
        //            asm.comment = appDetail.comment;
        //        }
        //        list.Add(asm);
        //    }
        //    //审核成功
        //    if (app.success == true)
        //    {
        //        list.Add(new AuditStatusModel()
        //        {
        //            step = 5,
        //            stepName = "完成申请",
        //            auditor = "系统",
        //            date = ((DateTime)app.finish_date).ToShortDateString(),
        //            time = ((DateTime)app.finish_date).ToShortTimeString(),
        //            pass = true
        //        });
        //    }

        //    return Json(new { success = true, result = list });
        //}

        ////审核人搜索变更申请
        //public ActionResult CheckOwnUpdateApply()
        //{

        //    return View();
        //}

        ////审核人搜索变更申请的结果列表
        ////public JsonResult SearchUpdateOrders(FormCollection fc)
        ////{
        ////    string bill_no = fc.Get("bill_no");
        ////    string saler = fc.Get("saler");
        ////    string fromDateString = fc.Get("fromDate");
        ////    string toDateString = fc.Get("toDate");
        ////    string auditResult = fc.Get("auditResult");

        ////    DateTime fromDate = DateTime.Parse("1950-1-1");
        ////    DateTime toDate = DateTime.Parse("2099-1-1");
        ////    if (!string.IsNullOrEmpty(fromDateString))
        ////        fromDate = DateTime.Parse(fromDateString);
        ////    if (!string.IsNullOrEmpty(toDateString))
        ////        toDate = DateTime.Parse(toDateString);
        ////    int result = Int32.Parse(auditResult);

        ////    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

        ////    List<AuditUpdateListModel> list = new List<AuditUpdateListModel>();
        ////    foreach (var cha in db.User.Single(a => a.id == userId).Charger)
        ////    {
        ////        int step = (int)cha.step;
        ////        foreach (var ap in (from a in db.SaleUpdateApply
        ////                            where a.User.department == cha.dep_id
        ////                            && (a.bill_no.Contains(bill_no))
        ////                            && a.User.real_name.Contains(saler)
        ////                            select a))
        ////        {
        ////            var ads = ap.UpdateAudit.Where(ad => ad.step == step);
        ////            if (ads.Count() > 0)
        ////            {
        ////                var ad = ads.First();
        ////                DateTime ptime;
        ////                string status;
        ////                if (ad.pass == null)
        ////                {
        ////                    status = "等待审核";
        ////                }
        ////                else if (ad.pass == true)
        ////                {
        ////                    status = "审核成功";
        ////                }
        ////                else
        ////                {
        ////                    status = "审核失败";
        ////                }
        ////                if (step == 1)
        ////                    ptime = (DateTime)ap.start_date;
        ////                else
        ////                    ptime = (DateTime)ap.UpdateAudit.Where(a => a.step == (step - 1)).First().finish_date;

        ////                if (ptime >= fromDate && ptime <= toDate)
        ////                {
        ////                    if ((ad.pass == true && result == 1) || (ad.pass == false && result == -1) || (ad.pass == null && result == 0) || result == 10)
        ////                    {
        ////                        list.Add(new AuditUpdateListModel()
        ////                        {
        ////                            bill_type = ap.bill_type,
        ////                            depName = cha.Department.name,
        ////                            update_id = ap.id,
        ////                            previousStepTime = ptime.ToString(),
        ////                            salerName = ap.User.real_name,
        ////                            step = step,
        ////                            bill_no = ap.bill_no,
        ////                            status = status
        ////                        });
        ////                    }
        ////                }
        ////            }
        ////        }
        ////    }
        ////    return Json(new { list = list.OrderByDescending(l => DateTime.Parse(l.previousStepTime)), count = list.Count() });

        ////}

        ////审批人审核变更申请
        //[SessionTimeOutFilter()]
        //public ActionResult BeginAuditUpdate(int step, int updateId)
        //{
        //    SaleUpdateApply ap = db.SaleUpdateApply.Single(a => a.id == updateId);
        //    //ViewData["order_id"] = db.getBillIdByUpdateId(ap.id).First().bill_id;
        //    ViewData["bill_no"] = ap.bill_no;
        //    ViewData["bill_type"] = ap.bill_type;
        //    ViewData["step"] = step;
        //    ViewData["update_id"] = ap.id;
        //    ViewData["changeComment"] = ap.change_comment;
        //    //办事处审核
        //    if (step <= 2)
        //    {
        //        return View("AgencyAuditUpdate");
        //    }
        //    else if (step == 3)
        //    {
        //        //市场部一审，需要对订单进行新增字段和修改               
        //        if (ap.UpdateAudit.Where(ad => ad.step == step && ad.pass == null).Count() > 0)
        //        {
        //            //进入可以编辑界面         
        //            if (ap.bill_type.Equals("SO"))
        //            {
        //                List<vwK3SaleOrder> order = db.vwK3SaleOrder.Where(v => v.bill_no == ap.bill_no).ToList();
        //                ViewData["order"] = order;
        //                return View("MarketEditUpdate");
        //            }
        //            else if (ap.bill_type.Equals("MB")) {
        //                List<vwK3SaleContract> order = db.vwK3SaleContract.Where(v => v.contract_no == ap.bill_no).ToList();
        //                ViewData["order"] = order;
        //                return View("MarketEditContract");
        //            }
        //            else if (ap.bill_type.Equals("CM"))
        //            {
        //                List<VwK3ModelContract> order = db.VwK3ModelContract.Where(v => v.contract_no == ap.bill_no).ToList();
        //                ViewData["order"] = order;
        //                return View("MarketEditModel");
        //            }
        //            else if (ap.bill_type.Equals("FS"))
        //            {
        //                List<VwK3ModelContract> order = db.VwK3ModelContract.Where(v => v.contract_no == ap.bill_no).ToList();
        //                ViewData["order"] = order;
        //                return View("MarketEditFreeSample");
        //            } else if (ap.bill_type.Equals("CS"))
        //            {
        //                List<vwK3SaleOrder> order = db.vwK3SaleOrder.Where(v => v.bill_no == ap.bill_no).ToList();
        //                ViewData["order"] = order;
        //                return View("MarketEditChargeSample");
        //            }
        //        }
        //        else
        //        {
        //            //进入只读界面
        //            return View("AgencyAuditUpdate");
        //        }
        //    }
        //    else if (step == 4)
        //    {
        //        return View("AgencyAuditUpdate");
        //    }

        //    return View("Error");
        //}

        ////审核取消申请
        //[SessionTimeOutFilter()]
        //public ActionResult BeginAuditCancel(int step, int updateId) {
        //    SaleUpdateApply ap = db.SaleUpdateApply.Single(a => a.id == updateId);
        //    ViewData["bill_no"] = ap.bill_no;
        //    ViewData["bill_type"] = ap.bill_type;
        //    ViewData["step"] = step;
        //    ViewData["update_id"] = ap.id;
        //    ViewData["changeComment"] = ap.change_comment;
        //    return View();
        //}

        ////刷新更新申请处理结果
        //public JsonResult RefleshAuditUpdateResult(int updateId, int step)
        //{
        //    var details = db.UpdateAudit.Where(ad => ad.update_id == updateId && ad.step == step).First();
        //    if (details.pass == null)
        //    {
        //        //未审核，没有结果返回
        //        return Json(new { success = false });
        //    }
        //    //取得处理结果
        //    return Json(new { success = true, pass = details.pass, comment = details.comment });
        //}

        ////处理更新申请
        //public JsonResult HandleAgencyAuditUpdate(FormCollection fc)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

        //    int applyId = int.Parse(fc.Get("updateId"));
        //    int step = int.Parse(fc.Get("step"));
        //    bool isOK = bool.Parse(fc.Get("okFlag"));
        //    string comment = fc.Get("agency_comment");

        //    try
        //    {
        //        //更新这一步骤的状态
        //        UpdateAudit thisDetail = db.UpdateAudit.Where(ad => ad.update_id == applyId && ad.step == step).First();
        //        //如果审核状态不为空，说明已经被审核了
        //        if (thisDetail.pass != null)
        //        {
        //            return Json(new { success = false, msg = "该变更申请已被审核" });
        //        }
        //        thisDetail.ip = Request.UserHostAddress;
        //        thisDetail.pass = isOK;
        //        thisDetail.user_id = userId;
        //        thisDetail.comment = comment;
        //        thisDetail.finish_date = DateTime.Now;

        //        //如果通过并且未到最后一级，则到达下一审核,否则审核失败
        //        if (isOK && step < 4)
        //        {
        //            UpdateAudit ads = new UpdateAudit()
        //            {
        //                update_id = applyId,
        //                step = step + 1
        //            };
        //            db.UpdateAudit.InsertOnSubmit(ads);
        //        }
        //        else
        //        {
        //            SaleUpdateApply ap = db.SaleUpdateApply.Single(a => a.id == applyId);
        //            ap.success = isOK;
        //            ap.finish_date = DateTime.Now;
        //            if (isOK)
        //            {
        //                //到了这里表示该单据完成了所有审核，现通过存储过程导入K3
        //                try
        //                {
        //                    string result;
        //                    switch (ap.bill_type) { 
        //                        case "SO":
        //                            result = utl.parseSoSegment(ap.UpdateInfos.Where(u => u.belong_to == "M").ToList(), ap.bill_no);
        //                            db.ExecuteCommand(result);
        //                            //update 完字段之后，要将设计的总金额字段一并更新，这个用存储过程完成
        //                            db.updateK3SaleOrderAmount(ap.bill_no);
        //                            break;
        //                        case "MB":
        //                            result = utl.parseMBSegment(ap.UpdateInfos.Where(u => u.belong_to == "M").ToList(), ap.bill_no);
        //                            db.ExecuteCommand(result);
        //                            //update 完字段之后，要将设计的总金额字段和schema表一并更新，这个用存储过程完成
        //                            if (ap.UpdateInfos.Where(u => u.belong_to == "M" && u.entry_id != null).Count() > 0)
        //                            {
        //                                db.updateK3ContractAmount(ap.bill_no);
        //                            }
        //                            break;
        //                        case "CM":
        //                            result = utl.parseCMSegment(ap.UpdateInfos.Where(u => u.belong_to == "M").ToList(), ap.bill_no);
        //                            db.ExecuteCommand(result);
        //                            break;
        //                        case "FS":
        //                            result = utl.parseCMSegment(ap.UpdateInfos.Where(u => u.belong_to == "M").ToList(), ap.bill_no);
        //                            db.ExecuteCommand(result);
        //                            break;
        //                        case "CS":
        //                            result = utl.parseSoSegment(ap.UpdateInfos.Where(u => u.belong_to == "M").ToList(), ap.bill_no);
        //                            db.ExecuteCommand(result);
        //                            //update 完字段之后，要将设计的总金额字段一并更新，这个用存储过程完成
        //                            db.updateK3SaleOrderAmount(ap.bill_no);
        //                            break;
        //                        case "SO_CANCEL":
        //                        case "MB_CANCEL":
        //                        case "CS_CANCEL":
        //                            db.CancelK3Bills(ap.bill_type.Substring(0,2), ap.bill_no);
        //                            break;
        //                        default:
        //                            return Json(new { success = false, msg = "导入K3发生错误,审核不成功。" }, "text/html");
        //                    }
                            
        //                }
        //                catch (Exception ex)
        //                {
        //                    return Json(new { success = false, msg = "导入K3发生错误，原因：" + ex.Message.ToString() + ",审核不成功。" }, "text/html");
        //                }
        //            }
        //        }
        //        //提交数据
        //        db.SubmitChanges();
        //    }
        //    catch
        //    {
        //        return Json(new { success = false, msg = "审核发生错误" }, "text/html");
        //    }

        //    //发送邮件通知下一环节审批人            
        //    //utl.EmailToNextPeopleUpdate(applyId,"M");

        //    return Json(new { success = true, msg = "审核成功" }, "text/html");
        //}

        ////市场部一审编辑变更申请
        //public JsonResult MarketEditUpdate(FormCollection fc)
        //{
        //    string[] en_name = fc.GetValues("en_name[]");
        //    string[] cn_name = fc.GetValues("cn_name[]");
        //    string[] entry_id = fc.GetValues("entry_id[]");
        //    string[] old_v = fc.GetValues("old_v[]");
        //    string[] new_v = fc.GetValues("new_v[]");
        //    int update_id = Int32.Parse(fc.Get("update_id"));

        //    var updateApply = db.SaleUpdateApply.Single(s => s.id == update_id);
        //    if (updateApply.UpdateInfos.Where(u => u.belong_to == "M").Count() > 0)
        //    {
        //        return Json(new { success = false, msg = "该单据已被审核" });
        //    }

        //    List<UpdateInfos> uList = new List<UpdateInfos>();
        //    for (int i = 0; i < en_name.Length; i++)
        //    {
        //        UpdateInfos uis = new UpdateInfos();
        //        uis.ename = en_name[i];
        //        if (cn_name[i].IndexOf(':') >= 0)
        //        {
        //            uis.cname = cn_name[i].Remove(cn_name[i].IndexOf(':'));
        //        }
        //        else
        //        {
        //            uis.cname = cn_name[i];
        //        }
        //        uis.SaleUpdateApply = updateApply;
        //        if (string.IsNullOrWhiteSpace(entry_id[i]))
        //            uis.entry_id = null;
        //        else
        //            uis.entry_id = Int32.Parse(entry_id[i]);
        //        uis.before_value = old_v[i];
        //        uis.after_value = new_v[i];
        //        uis.belong_to = "M";
        //        uList.Add(uis);
        //    }
        //    db.UpdateInfos.InsertAllOnSubmit(uList);
        //    db.SubmitChanges();
        //    return Json(new { success = true });
        //}

        ////加载市场部的变更列表
        //public JsonResult ChangeInfoList(int id)
        //{
        //    var ui = db.UpdateInfos.Where(u => u.update_id == id && u.belong_to == "M");
        //    if (ui.Count() > 0)
        //    {
        //        var list = from s in db.UpdateInfos
        //                   where s.update_id == id
        //                   && s.belong_to == "M"
        //                   select new
        //                   {
        //                       field_en_name = s.ename,
        //                       field_cn_name = s.cname,
        //                       entry_id = s.entry_id,
        //                       old_value = s.before_value,
        //                       new_value = s.after_value
        //                   };

        //        return Json(list, JsonRequestBehavior.AllowGet);
        //    }
        //    else
        //    {
        //        var list = from s in db.UpdateInfos
        //                   where s.update_id == id
        //                   && s.belong_to == "S"
        //                   select new
        //                   {
        //                       field_en_name = s.ename,
        //                       field_cn_name = s.cname,
        //                       entry_id = s.entry_id,
        //                       old_value = s.before_value,
        //                       new_value = s.after_value
        //                   };

        //        return Json(list, JsonRequestBehavior.AllowGet);
        //    }
        //}

        ////测试生成的sql update语句
        //public string test()
        //{
        //    var ap = db.SaleUpdateApply.Where(s => s.id==20).First();
        //    var updateList = ap.UpdateInfos.Where(ui => ui.belong_to == "M").ToList();
        //    string result = utl.parseCMSegment(updateList, ap.bill_no);
        //    //try
        //    //{
        //    //    db.ExecuteCommand(result);
        //    //    db.updateK3ContractAmount(ap.bill_no);
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    return ex.Message.ToString();
        //    //}
        //    return result;
        //}

    }
}
