using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Filter;
using Sale_Order.Utils;
using Sale_Order.Models;

namespace Sale_Order.Controllers
{    
    public class K3SalerController : Controller
    {
        //SaleDBDataContext db = new SaleDBDataContext();
        ////变更单据的搜索页面
        //[SessionTimeOutFilter()]
        //public ActionResult SearchK3SaleOrders()
        //{
        //    return View();
        //}

        ////变更单据的搜索结果
        //[HttpPost]
        //public JsonResult SearchK3SaleOrdersResult(FormCollection col)
        //{
        //    string orderNo = col.Get("order_no");
        //    string fromDate = col.Get("fromDate");
        //    string toDate = col.Get("toDate");
        //    string clerk = col.Get("clerk");
        //    string customer_name = col.Get("customer_name");

        //    if (string.IsNullOrWhiteSpace(orderNo) && string.IsNullOrWhiteSpace(fromDate)
        //        && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(clerk)
        //        && string.IsNullOrWhiteSpace(customer_name))
        //    {
        //        return Json(new { success = false, msg = "至少应该输入一项才能搜索" });
        //    }
        //    DateTime frDate = DateTime.Parse("1900-8-8");
        //    DateTime tDate = DateTime.Parse("2099-9-9");
        //    if (!string.IsNullOrWhiteSpace(fromDate))
        //    {
        //        frDate = DateTime.Parse(fromDate);
        //    }
        //    if (!string.IsNullOrWhiteSpace(toDate))
        //    {
        //        tDate = DateTime.Parse(toDate);
        //    }
        //    var result = from v in db.vwK3SaleOrder
        //                 where v.bill_no.Contains(orderNo)
        //                 && v.clerk_name.Contains(clerk)
        //                 && v.buy_unit_name.Contains(customer_name)
        //                 && v.bill_date >= frDate
        //                 && v.bill_date <= tDate
        //                 && v.order_type_name == "生产单"
        //                 orderby v.bill_date descending
        //                 select new
        //                 {
        //                     bill_id = v.bill_id,
        //                     order_date = v.bill_date,
        //                     order_no = v.bill_no,
        //                     clerk = v.clerk_name,
        //                     buy_unit = v.buy_unit_name,
        //                     product_name = v.product_name,
        //                     product_model = v.product_model,
        //                     qty = v.qty,
        //                     deal_price = v.deal_price,
        //                     order_date_string = ((DateTime)v.bill_date).ToShortDateString(),
        //                     audit_status = v.audit_status >= 1 ? "Y" : "",
        //                     close_status = v.close_status == 1 ? "Y" : ""
        //                 };

        //    return Json(new { success = true, msg = result, num = result.Count() });
        //}

        ////查询详细页面
        //public ActionResult CheckK3SaleOrder(int id)
        //{
        //    var vwOrder = db.vwK3SaleOrder.Where(v => v.bill_id == id).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View();
        //}
        //public ActionResult CheckK3SaleOrderByNo(string bill_no)
        //{
        //    var vwOrder = db.vwK3SaleOrder.Where(v => v.bill_no == bill_no).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View("CheckK3SaleOrder");
        //}

        ////进入变更页面
        //[SessionTimeOutFilter()]
        //public ActionResult ModifyK3SaleOrder(int id)
        //{
        //    var vwOrder = db.vwK3SaleOrder.Where(v => v.bill_id == id).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View();
        //}

        ////提交变更单据申请
        //public JsonResult SubmitUpdate(FormCollection fc)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    string[] en_name = fc.GetValues("en_name[]");
        //    string[] cn_name = fc.GetValues("cn_name[]");
        //    string[] entry_id = fc.GetValues("entry_id[]");
        //    string[] old_v = fc.GetValues("old_v[]");
        //    string[] new_v = fc.GetValues("new_v[]");
        //    string change_comm = fc.Get("change_comm");
        //    string bill_no = fc.Get("bill_no");
        //    string bill_type = fc.Get("bill_type");

        //    var isexist = db.SaleUpdateApply.Where(su => su.bill_no == bill_no && su.success == null).Count();
        //    if (isexist > 0)
        //    {
        //        return Json(new { success = false, msg = "该单据已申请了变更，不能再次申请" });
        //    }

        //    SaleUpdateApply sua = new SaleUpdateApply();
        //    try
        //    {
        //        sua.bill_type = bill_type;
        //        sua.bill_no = bill_no;
        //        sua.start_date = DateTime.Now;
        //        sua.ip = Request.UserHostAddress;
        //        sua.user_id = userId;
        //        sua.change_comment = change_comm;
        //        db.SaleUpdateApply.InsertOnSubmit(sua);

        //        List<UpdateInfos> uList = new List<UpdateInfos>();
        //        for (int i = 0; i < en_name.Length; i++)
        //        {
        //            UpdateInfos uis = new UpdateInfos();
        //            uis.ename = en_name[i];
        //            if (cn_name[i].IndexOf(':') >= 0)
        //            {
        //                uis.cname = cn_name[i].Remove(cn_name[i].IndexOf(':'));
        //            }
        //            else
        //            {
        //                uis.cname = cn_name[i];
        //            }
        //            uis.SaleUpdateApply = sua;
        //            if (string.IsNullOrWhiteSpace(entry_id[i]))
        //                uis.entry_id = null;
        //            else
        //                uis.entry_id = Int32.Parse(entry_id[i]);
        //            uis.before_value = old_v[i];
        //            uis.after_value = new_v[i];
        //            uis.belong_to = "S";
        //            uList.Add(uis);
        //        }
        //        db.UpdateInfos.InsertAllOnSubmit(uList);

        //        UpdateAudit ua = new UpdateAudit();
        //        ua.step = 1;
        //        ua.SaleUpdateApply = sua;
        //        db.UpdateAudit.InsertOnSubmit(ua);

        //        db.SubmitChanges();
        //    }
        //    catch
        //    {
        //        return Json(new { success = false, msg = "保存失败" });
        //    }

        //    //发送邮件
        //    //SomeUtils utis = new SomeUtils();
        //    //if (utis.EmailToNextPeopleUpdate(sua.id,"M"))
        //    //    return Json(new { success = true, msg = "成功提交变更申请" });
        //    //else
        //    //    return Json(new { success = false, msg = "保存成功，发送邮件给审批人失败" });
        //    return Json(new { success = true, msg = "成功提交变更申请" });
        //}

        ////查看自己的变更单据申请        
        //[SessionTimeOutFilter()]
        //public ActionResult CheckUpdateOrders()
        //{
        //    return View();
        //}

        ////加载查询结果列表
        //[SessionTimeOutFilter()]
        //public JsonResult SearchUpdateOrders(FormCollection fc)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    string bill_no = fc.Get("bill_no");
        //    string from_date = fc.Get("fromDate");
        //    string to_date = fc.Get("toDate");
        //    string billType = fc.Get("billType");
        //    int auditResult = Int32.Parse(fc.Get("auditResult"));
        //    DateTime fromDate = DateTime.Parse("1980-1-1");
        //    DateTime toDate = DateTime.Parse("2099-9-9");
        //    if (!string.IsNullOrWhiteSpace(from_date))
        //    {
        //        fromDate = DateTime.Parse(from_date);
        //    }
        //    if (!string.IsNullOrWhiteSpace(to_date))
        //    {
        //        toDate = DateTime.Parse(to_date);
        //    }

        //    List<BillChangeInfoModel> list = new List<BillChangeInfoModel>();
        //    var apps = from cm in db.SaleUpdateApply
        //                where cm.bill_no.Contains(bill_no)
        //                && cm.start_date >= fromDate
        //                && cm.start_date <= toDate
        //                && cm.user_id == userId
        //                //orderby cm.id descending
        //                select cm;
        //    //过滤单据类型
        //    if (!billType.Equals("all")) {
        //        apps = apps.Where(a => a.bill_type.StartsWith(billType));
        //    }
        //    //对单据进行排序
        //    apps = apps.OrderByDescending(a => a.id);
        //    foreach (var app in apps)
        //    {
        //        string applyStatus;
        //        string changeContent = "";
        //        switch (app.success)
        //        {
        //            case true:
        //                applyStatus = "申请成功";
        //                break;
        //            case false:
        //                applyStatus = "申请失败";
        //                break;
        //            default:
        //                applyStatus = "审批当中";
        //                break;
        //        }
        //        if ((auditResult == 0 && app.success == null) || (auditResult == 1 && app.success == true) || (auditResult == -1 && app.success == false) || auditResult == 10)
        //        {
        //            foreach (var info in app.UpdateInfos.Where(ui => ui.belong_to == "S"))
        //            {
        //                changeContent += info.cname + " ";
        //            }
        //            if (string.IsNullOrEmpty(changeContent))
        //                changeContent = "申请取消";
        //            list.Add(new BillChangeInfoModel()
        //            {
        //                update_id = app.id,
        //                bill_no = app.bill_no,
        //                bill_type = app.bill_type,
        //                apply_date = ((DateTime)app.start_date).ToShortDateString(),
        //                apply_status = applyStatus,
        //                change_content = changeContent
        //            });
        //        }
        //    }
        //    return Json(new { list = list, count = list.Count() });
        //}

        ////加载单据的营业员更新列表
        //public JsonResult ChangeInfoList(int id)
        //{
        //    var list = from s in db.UpdateInfos
        //               where s.update_id == id
        //               && s.belong_to == "S"
        //               select new
        //               {
        //                   field_en_name = s.ename,
        //                   field_cn_name = s.cname,
        //                   entry_id = s.entry_id,
        //                   old_value = s.before_value,
        //                   new_value = s.after_value
        //               };

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        ////营业员查看已编辑的变更单据
        //public ActionResult GetUpdateOrderDetails(int id)
        //{            
        //    var apply = db.SaleUpdateApply.Single(s => s.id == id);
        //    string changeComment = apply.change_comment;
        //    ViewData["bill_type"] = apply.bill_type;
        //    ViewData["bill_no"] = apply.bill_no;
        //    ViewData["update_id"] = id;
        //    ViewData["changeComment"] = changeComment;
        //    if (apply.bill_type.Length > 2) {
        //        //查看已编辑的取消单据
        //        return View("GetCancelOrderDetails");
        //    }
        //    return View();
        //}

        ////检查单据的变更申请是否已被提交
        //public JsonResult IfChangeApplySubimted(string bill_no)
        //{
        //    var apps = db.SaleUpdateApply.Where(su => su.bill_no == bill_no && su.success == null);
        //    if (apps.Count() > 0)
        //    {
        //        if (apps.Where(a => a.bill_type.Length > 2).Count() > 0)
        //        {
        //            return Json(new { suc = true, msg = "该单据正在申请取消操作，不能再变更或取消单据" });
        //        }
        //        else {
        //            return Json(new { suc = true, msg = "该单据正在申请变更操作，不能再变更或取消单据" });
        //        }

        //    }
        //    else
        //        return Json(new { suc = false, msg = "" });
        //}

        ////获取SO关联数量，出库数量和发票数量
        //public JsonResult getOtherQty(int entry_id, string bill_no)
        //{
        //    var res = db.getOtherQtyFromK3SO(entry_id, bill_no).First();
        //    string segName = "";
        //    decimal segValue = 0m;
        //    if (res.commit_qty >= res.stock_qty && res.stock_qty >= res.invoice_qty)
        //    {
        //        segName = "关联数量";
        //        segValue = res.commit_qty;
        //    }
        //    else if (res.stock_qty >= res.commit_qty && res.commit_qty >= res.invoice_qty)
        //    {
        //        segName = "出库数量";
        //        segValue = res.stock_qty;
        //    }
        //    else if (res.invoice_qty >= res.commit_qty && res.commit_qty >= res.stock_qty)
        //    {
        //        segName = "出库数量";
        //        segValue = res.stock_qty;
        //    }
        //    return Json(new { seg = segName, val = segValue });

        //}

        ////获取CM销售开模合同出库数量
        //public JsonResult getOutQtyFromK3CM(int entry_id, string bill_no) {
        //    var res = db.getOtherQtyFromK3CM(entry_id, bill_no).First();
        //    return Json(new { seg = "出库数量", val = res.FOutQty });
        //}

        ////变更维修单页面
        //[SessionTimeOutFilterAttribute()]
        //public ActionResult SearchK3Contracts() {
        //    return View();
        //}

        ////变更修理单的搜索结果
        //[HttpPost]
        //public JsonResult SearchK3ContractsResult(FormCollection col)
        //{
        //    string orderNo = col.Get("order_no");
        //    string fromDate = col.Get("fromDate");
        //    string toDate = col.Get("toDate");
        //    string clerk = col.Get("clerk");
        //    string customer_name = col.Get("customer_name");

        //    if (string.IsNullOrWhiteSpace(orderNo) && string.IsNullOrWhiteSpace(fromDate)
        //        && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(clerk)
        //        && string.IsNullOrWhiteSpace(customer_name))
        //    {
        //        return Json(new { success = false, msg = "至少应该输入一项才能搜索" });
        //    }
        //    DateTime frDate = DateTime.Parse("1800-8-8");
        //    DateTime tDate = DateTime.Parse("2099-9-9");
        //    if (!string.IsNullOrWhiteSpace(fromDate))
        //    {
        //        frDate = DateTime.Parse(fromDate);
        //    }
        //    if (!string.IsNullOrWhiteSpace(toDate))
        //    {
        //        tDate = DateTime.Parse(toDate);
        //    }
        //    var result = from v in db.vwK3SaleContract
        //                 where v.contract_no.Contains(orderNo)
        //                 && v.employee_name.Contains(clerk)
        //                 && v.customer_name.Contains(customer_name)
        //                 && v.contract_date >= frDate
        //                 && v.contract_date <= tDate
        //                 orderby v.contract_date descending
        //                 select new
        //                 {
        //                     bill_id = v.contract_id,
        //                     order_date = v.contract_date,
        //                     order_no = v.contract_no,
        //                     clerk = v.employee_name,
        //                     buy_unit = v.customer_name,
        //                     product_name = v.product_name,
        //                     product_model = v.product_model,
        //                     qty = v.qty,
        //                     deal_price = v.price,
        //                     order_date_string = ((DateTime)v.contract_date).ToShortDateString(),
        //                     audit_status = v.audit_status >= 1 ? v.audit_status.ToString() : ""
        //                     //audit_status = v.audit_status == 1 ? "已审核" : (v.audit_status==2?"已关闭":""),
        //                     //close_status = v.audit_status == 2 ? "Y" : ""
        //                 };

        //    return Json(new { success = true, msg = result, num = result.Count() });
        //}

        ////查看从k3过来的修理单的详细页面
        //public ActionResult CheckK3Contract(int id) {             
        
        //    var vwContract = db.vwK3SaleContract.Where(v => v.contract_id == id).ToList();
        //    ViewData["order"] = vwContract;
        //    return View();
        
        //}
        //public ActionResult CheckK3ContractByNo(string bill_no)
        //{

        //    var vwContract = db.vwK3SaleContract.Where(v => v.contract_no == bill_no).ToList();
        //    ViewData["order"] = vwContract;
        //    return View("CheckK3Contract");

        //}

        ////进入变更k3修理单页面
        //public ActionResult ModifyK3Contract(int id) {
        //    var vwContract = db.vwK3SaleContract.Where(v => v.contract_id == id).ToList();
        //    ViewData["order"] = vwContract;
        //    return View();
        //}

        ////进入取消销售订单界面
        //public ActionResult CancelK3SaleOrder(int id) {
        //    ViewData["order_id"] = id;
        //    ViewData["billType"] = "SO";
        //    return View("CancelK3Bill");
        //}
        
        ////进入取消修理单界面
        //public ActionResult CancelK3Contract(int id) {
        //    ViewData["order_id"] = id;
        //    ViewData["billType"] = "MB";
        //    return View("CancelK3Bill");
        //}

        ////提交取消单据的处理过程
        //public JsonResult SubmitCancelApply(FormCollection fc) {
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    string comment = fc.Get("comment");
        //    string bill_type = fc.Get("bill_type");
        //    string bill_id = fc.Get("bill_id");
        //    int bill_int_id=Int32.Parse(bill_id);
        //    string bill_no = "";            
        //    switch (bill_type) { 
        //        case "SO":
        //            bill_no = db.vwK3SaleOrder.Where(v => v.bill_id == bill_int_id).First().bill_no;
        //            break;
        //        case "MB":
        //            bill_no = db.vwK3SaleContract.Where(v => v.contract_id == bill_int_id).First().contract_no;
        //            break;
        //        default:
        //            bill_no = "";
        //            break;
        //    }
        //    var apExists = db.SaleUpdateApply.Where(s => s.bill_no == bill_no && s.success == null);
        //    if (apExists.Count() > 0) {
        //        return Json(new { suc = false, msg = "该单据正在申请修改或者取消操作，不能重复操作！" });
        //    }
        //    SaleUpdateApply app;
        //    try
        //    {
        //        app = new SaleUpdateApply();
        //        app.bill_no = bill_no;
        //        app.user_id = userId;
        //        app.start_date = DateTime.Now;
        //        app.ip = Request.UserHostAddress;
        //        app.change_comment = comment;
        //        app.bill_type = bill_type + "_CANCEL";
        //        db.SaleUpdateApply.InsertOnSubmit(app);
        //        UpdateAudit au = new UpdateAudit();
        //        au.SaleUpdateApply = app;
        //        au.step = 1;
        //        db.UpdateAudit.InsertOnSubmit(au);
        //        db.SubmitChanges();
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { success = false, msg = "保存失败，原因：" + e.Message });
        //    }

        //    SomeUtils utis = new SomeUtils();
        //    //if (utis.EmailToNextPeopleUpdate(app.id,"C"))
        //    //    return Json(new { success = true, msg = "成功提交取消单据申请" });
        //    //else
        //    //    return Json(new { success = false, msg = "保存成功，发送邮件给审批人失败" });
        //    return Json(new { success = true, msg = "成功提交取消单据申请" });
        //}

        ////验证是否有取消订单的权限
        //public JsonResult HasCancelPower(string billType) {
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    var powers = (from a in db.Authority
        //                  from u in db.Group
        //                  from ga in a.GroupAndAuth
        //                  from gu in u.GroupAndUser
        //                  where ga.group_id == u.id && gu.user_id == userId
        //                  select a.sname).ToArray();
        //    string p;
        //    switch (billType) { 
        //        case "SO":
        //            p = Powers.cancel_order.ToString();
        //            break;
        //        case "MB":
        //            p = Powers.cancel_fix.ToString();
        //            break;
        //        default:
        //            p = "xx";
        //            break;
        //    }
        //    return Json(new { suc = Array.IndexOf(powers,p) >= 0 });
        //}

        ////变更开模改模单的搜索页面
        //[SessionTimeOutFilter()]
        //public ActionResult SearchK3Models()
        //{
        //    return View();
        //}

        ////变更开模改模单的搜索结果
        //public JsonResult SearchK3ModelsResult(FormCollection col) {
        //    string orderNo = col.Get("contract_no");
        //    string fromDate = col.Get("fromDate");
        //    string toDate = col.Get("toDate");
        //    string clerk = col.Get("clerk");
        //    string customer_name = col.Get("customer_name");

        //    if (string.IsNullOrWhiteSpace(orderNo) && string.IsNullOrWhiteSpace(fromDate)
        //        && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(clerk)
        //        && string.IsNullOrWhiteSpace(customer_name))
        //    {
        //        return Json(new { success = false, msg = "至少应该输入一项才能搜索" });
        //    }
        //    DateTime frDate = DateTime.Parse("1800-8-8");
        //    DateTime tDate = DateTime.Parse("2099-9-9");
        //    if (!string.IsNullOrWhiteSpace(fromDate))
        //    {
        //        frDate = DateTime.Parse(fromDate);
        //    }
        //    if (!string.IsNullOrWhiteSpace(toDate))
        //    {
        //        tDate = DateTime.Parse(toDate);
        //    }
        //    var result = from v in db.VwK3ModelContract
        //                 where v.contract_no.Contains(orderNo)
        //                 && v.employee_name.Contains(clerk)
        //                 && v.customer_name.Contains(customer_name)
        //                 && v.bill_date >= frDate
        //                 && v.bill_date <= tDate
        //                 && v.contract_name != "免费样品单"
        //                 orderby v.bill_date descending
        //                 select new
        //                 {
        //                     bill_id = v.FID,
        //                     order_date = v.bill_date,
        //                     order_no = v.contract_no,
        //                     clerk = v.employee_name,
        //                     buy_unit = v.customer_name,
        //                     product_name = v.product_name,
        //                     product_model = v.product_model,
        //                     qty = v.FQuantity,
        //                     order_date_string = ((DateTime)v.bill_date).ToShortDateString(),
        //                     audit_status = v.FCheckerID > 0 ? "Y" : "",
        //                 };

        //    return Json(new { success = true, msg = result, num = result.Count() });
        //}

        ////查看k3开模改模单详细页面
        //public ActionResult CheckK3Model(int id)
        //{
        //    var vwOrder = db.VwK3ModelContract.Where(v => v.FID == id).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View();
        //}
        //public ActionResult CheckK3ModelByNo(string bill_no)
        //{
        //    var vwOrder = db.VwK3ModelContract.Where(v => v.contract_no == bill_no).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View("CheckK3Model");
        //}

        ////开始变更界面
        //public ActionResult ModifyK3Model(int id) {
        //    var vwModel = db.VwK3ModelContract.Where(v => v.FID == id).ToList();
        //    ViewData["order"] = vwModel;
        //    return View();
        //}

        ////变更免费样品单的搜索页面
        //[SessionTimeOutFilter()]
        //public ActionResult SearchK3FreeSamples()
        //{
        //    return View();
        //}

        ////变更免费样品单的搜索结果
        //public JsonResult SearchK3FreeSamplesResult(FormCollection col)
        //{
        //    string orderNo = col.Get("contract_no");
        //    string fromDate = col.Get("fromDate");
        //    string toDate = col.Get("toDate");
        //    string clerk = col.Get("clerk");
        //    string customer_name = col.Get("customer_name");

        //    if (string.IsNullOrWhiteSpace(orderNo) && string.IsNullOrWhiteSpace(fromDate)
        //        && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(clerk)
        //        && string.IsNullOrWhiteSpace(customer_name))
        //    {
        //        return Json(new { success = false, msg = "至少应该输入一项才能搜索" });
        //    }
        //    DateTime frDate = DateTime.Parse("1800-8-8");
        //    DateTime tDate = DateTime.Parse("2099-9-9");
        //    if (!string.IsNullOrWhiteSpace(fromDate))
        //    {
        //        frDate = DateTime.Parse(fromDate);
        //    }
        //    if (!string.IsNullOrWhiteSpace(toDate))
        //    {
        //        tDate = DateTime.Parse(toDate);
        //    }
        //    var result = from v in db.VwK3ModelContract
        //                 where v.contract_no.Contains(orderNo)
        //                 && v.employee_name.Contains(clerk)
        //                 && v.customer_name.Contains(customer_name)
        //                 && v.bill_date >= frDate
        //                 && v.bill_date <= tDate
        //                 && v.contract_name == "免费样品单"
        //                 orderby v.bill_date descending
        //                 select new
        //                 {
        //                     bill_id = v.FID,
        //                     order_date = v.bill_date,
        //                     order_no = v.contract_no,
        //                     clerk = v.employee_name,
        //                     buy_unit = v.customer_name,
        //                     product_name = v.product_name,
        //                     product_model = v.product_model,
        //                     qty = v.FQuantity,
        //                     order_date_string = ((DateTime)v.bill_date).ToShortDateString(),
        //                     audit_status = v.FCheckerID > 0 ? "Y" : "",
        //                 };

        //    return Json(new { success = true, msg = result, num = result.Count() });
        //}

        ////查看k3开模改模单详细页面
        //public ActionResult CheckK3FreeSample(int id)
        //{
        //    var vwOrder = db.VwK3ModelContract.Where(v => v.FID == id).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View();
        //}
        //public ActionResult CheckK3FreeSampleByNo(string bill_no)
        //{
        //    var vwOrder = db.VwK3ModelContract.Where(v => v.contract_no == bill_no).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View("CheckK3FreeSample");
        //}

        ////开始变更界面
        //public ActionResult ModifyK3FreeSample(int id)
        //{
        //    var vwModel = db.VwK3ModelContract.Where(v => v.FID == id).ToList();
        //    ViewData["order"] = vwModel;
        //    return View();
        //}

        ////变更收费样品单的搜索界面
        //[SessionTimeOutFilterAttribute()]
        //public ActionResult SearchK3ChargeSamples() {
        //    return View();
        //}

        ////变更收费样品单的搜索结果
        //[HttpPost]
        //public JsonResult SearchK3ChargeSamplesResult(FormCollection col)
        //{
        //    string orderNo = col.Get("order_no");
        //    string fromDate = col.Get("fromDate");
        //    string toDate = col.Get("toDate");
        //    string clerk = col.Get("clerk");
        //    string customer_name = col.Get("customer_name");

        //    if (string.IsNullOrWhiteSpace(orderNo) && string.IsNullOrWhiteSpace(fromDate)
        //        && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(clerk)
        //        && string.IsNullOrWhiteSpace(customer_name))
        //    {
        //        return Json(new { success = false, msg = "至少应该输入一项才能搜索" });
        //    }
        //    DateTime frDate = DateTime.Parse("1800-8-8");
        //    DateTime tDate = DateTime.Parse("2099-9-9");
        //    if (!string.IsNullOrWhiteSpace(fromDate))
        //    {
        //        frDate = DateTime.Parse(fromDate);
        //    }
        //    if (!string.IsNullOrWhiteSpace(toDate))
        //    {
        //        tDate = DateTime.Parse(toDate);
        //    }
        //    var result = from v in db.vwK3SaleOrder
        //                 where v.bill_no.Contains(orderNo)
        //                 && v.clerk_name.Contains(clerk)
        //                 && v.buy_unit_name.Contains(customer_name)
        //                 && v.bill_date >= frDate
        //                 && v.bill_date <= tDate
        //                 && v.order_type_name == "样品单"
        //                 orderby v.bill_date descending
        //                 select new
        //                 {
        //                     bill_id = v.bill_id,
        //                     order_date = v.bill_date,
        //                     order_no = v.bill_no,
        //                     clerk = v.clerk_name,
        //                     buy_unit = v.buy_unit_name,
        //                     product_name = v.product_name,
        //                     product_model = v.product_model,
        //                     qty = v.qty,
        //                     deal_price = v.deal_price,
        //                     order_date_string = ((DateTime)v.bill_date).ToShortDateString(),
        //                     audit_status = v.audit_status >= 1 ? "Y" : "",
        //                     close_status = v.close_status == 1 ? "Y" : ""
        //                 };

        //    return Json(new { success = true, msg = result, num = result.Count() });
        //}

        ////查询详细页面
        //public ActionResult CheckK3ChargeSample(int id)
        //{
        //    var vwOrder = db.vwK3SaleOrder.Where(v => v.bill_id == id).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View();
        //}
        //public ActionResult CheckK3ChargeSampleByNo(string bill_no)
        //{
        //    var vwOrder = db.vwK3SaleOrder.Where(v => v.bill_no == bill_no).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View("CheckK3ChargeSample");
        //}

        ////进入变更页面
        //[SessionTimeOutFilter()]
        //public ActionResult ModifyK3ChargeSample(int id)
        //{
        //    var vwOrder = db.vwK3SaleOrder.Where(v => v.bill_id == id).ToList();
        //    ViewData["order"] = vwOrder;
        //    return View();
        //}

        ////取消收费样品单
        //[SessionTimeOutFilter()]
        //public ActionResult CancelK3ChargeSample(int id) {
        //    ViewData["order_id"] = id;
        //    ViewData["billType"] = "CS";
        //    return View("CancelK3Bill");
        //}
    }
}
