using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Utils;
using Sale_Order.Filter;
using Sale_Order.Models;
using System.Configuration;

namespace Sale_Order.Controllers
{
    public class BadProductController : Controller
    {
        SomeUtils utl = new SomeUtils();
        SaleDBDataContext db = new SaleDBDataContext();
        string MODELNAME = "退换货";

        //查询销售出库单
        [SessionTimeOutFilter()]
        public ActionResult SelectStockBill()
        {
            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd_th"];
            if (queryData != null && queryData.Values.Get("sa_cu") != null)
            {
                ViewData["cust_no"] = utl.DecodeToUTF8(queryData.Values.Get("sa_cu"));
                ViewData["order_no"] = utl.DecodeToUTF8(queryData.Values.Get("sa_so"));
                ViewData["stock_no"] = utl.DecodeToUTF8(queryData.Values.Get("sa_st"));
                ViewData["pro_model"] = utl.DecodeToUTF8(queryData.Values.Get("sa_mo"));
                ViewData["from_date"] = queryData.Values.Get("sa_fd");
                ViewData["to_date"] = queryData.Values.Get("sa_td");
                ViewData["hook_status"] = queryData.Values.Get("sa_hs");
            }
            else
            {
                ViewData["from_date"] = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                ViewData["to_date"] = DateTime.Now.ToString("yyyy-MM-dd");
                ViewData["hook_status"] = "A";
            }
            return View();
        }

        //在查询页面新增出库单
        [HttpPost]
        public JsonResult SelectStockBill(FormCollection fc)
        {
            string customerNumber = fc.Get("cust_no");
            string stockNo = fc.Get("stock_no");
            string orderNo = fc.Get("order_no");
            string model = fc.Get("pro_model");
            string fromDateStr = fc.Get("fromDate");
            string toDateStr = fc.Get("toDate");
            string hookStatus = fc.Get("hook_status");

            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd_th"];
            if (queryData == null) queryData = new HttpCookie("op_qd_th");
            queryData.Values.Set("sa_cu", utl.EncodeToUTF8(customerNumber));
            queryData.Values.Set("sa_so", utl.EncodeToUTF8(orderNo));
            queryData.Values.Set("sa_st", utl.EncodeToUTF8(stockNo));
            queryData.Values.Set("sa_mo", utl.EncodeToUTF8(model));
            queryData.Values.Set("sa_fd", fromDateStr);
            queryData.Values.Set("sa_td", toDateStr);
            queryData.Values.Set("sa_hs", hookStatus);
            queryData.Expires = DateTime.Now.AddDays(7);
            Response.AppendCookie(queryData);

            if (db.getCostomer(customerNumber, 1).Count() < 1)
            {
                utl.writeEventLog(MODELNAME, "客户编号不存在，请重新输入完整的客户编号。", "", Request, 10);
                return Json(new { suc = false, msg = "客户编号不存在，请重新输入完整的客户编号。" });
            }

            DateTime fromDate = new DateTime();
            DateTime toDate = new DateTime();
            if (!DateTime.TryParse(fromDateStr, out fromDate) || !DateTime.TryParse(toDateStr, out toDate))
            {
                return Json(new { suc = false, msg = "出货日期不合法" });
            }

            var result = (from v in db.VWBlueStockBill
                          where v.FCustomerNumber == customerNumber
                          && v.FOrderBillNo.Contains(orderNo)
                          && v.FBillNo.Contains(stockNo)
                          && v.FProductModel.Contains(model)
                          && v.FDate >= fromDate
                          && v.FDate <= toDate
                          && (hookStatus == "A" || hookStatus == "" || (hookStatus == "Y" && v.FHookStatus == 2) || (hookStatus == "N" && v.FHookStatus != 2))
                          orderby v.FInterID descending, v.FEntryID ascending
                          select v).Take(200).ToList();

            utl.writeEventLog(MODELNAME, "搜索出的销售出库单条数：" + result.Count(), "", Request);
            return Json(new { suc = true, list = result }, "text/html");
        }

        //在新增页面新增出库单
        [HttpPost]
        public JsonResult SelectMoreStockBill(FormCollection fc) {

            string customerNumber = fc.Get("customer_no");
            string stockNo = fc.Get("stock_no");
            string orderNo = fc.Get("order_no");
            string model = fc.Get("pro_model");
            string fromDateStr = fc.Get("from_date");
            string toDateStr = fc.Get("to_date");
            string hookStatusStr = fc.Get("hook_status");

            DateTime fromDate = new DateTime();
            DateTime toDate = new DateTime();
            if (!DateTime.TryParse(fromDateStr, out fromDate) || !DateTime.TryParse(toDateStr, out toDate))
            {
                return Json(new { suc = false, msg = "出货日期不合法" });
            }

            bool? hookStatus = null;
            if (!string.IsNullOrEmpty(hookStatusStr)) {
                hookStatus = bool.Parse(hookStatusStr);
            }

            var result = (from v in db.VWBlueStockBill
                          where v.FCustomerNumber == customerNumber
                          && v.FOrderBillNo.Contains(orderNo)
                          && v.FBillNo.Contains(stockNo)
                          && v.FProductModel.Contains(model)
                          && v.FDate >= fromDate
                          && v.FDate <= toDate
                          && ((v.FHookStatus == 2 && hookStatus == true) || (v.FHookStatus != 2 && hookStatus != true))
                          orderby v.FInterID descending, v.FEntryID ascending
                          select v).Take(100).ToList();
            utl.writeEventLog(MODELNAME, "新增界面再次搜索出库单：" + result.Count(), "", Request);
            return Json(new { suc = true, list = result }, "text/html");
        }

        //新建退修单
        [SessionTimeOutFilter()]
        public ActionResult CreateReturnBill(string FInterIDS, string FEntryIDS)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            ViewData["userName"] = db.User.Single(u => u.id == userId).real_name;

            string[] interIdArr = FInterIDS.Split(',');
            string[] entryIdArr = FEntryIDS.Split(',');

            ReturnBill bill = new ReturnBill();
            List<ReturnBillDetail> billDetails = new List<ReturnBillDetail>();
            ReturnBillDetail detail = null;

            //标识是本月的出库单，还是以往月的出库单，用于新增时的判断。本月和以往月不能合在同一张申请。2015-10-15
            DateTime firstDayInMonth = DateTime.Parse(DateTime.Now.Year + "-" + DateTime.Now.Month + "-01");
            int isCurrentMonth = 0;

            for (int i = 0; i < interIdArr.Length; i++)
            {
                int interId = Int32.Parse(interIdArr[i]);
                int entryId = Int32.Parse(entryIdArr[i]);
                var vw = db.VWBlueStockBill.Where(v => v.FInterID == interId && v.FEntryID == entryId).First();
                if (i == 0)
                {
                    bill.customer_name = vw.FCustomerName;
                    bill.customer_number = vw.FCustomerNumber;
                    bill.sys_no = utl.getReturnSystemNo(vw.FCustomerNumber.Substring(3, 2));
                    bill.fdate = DateTime.Now;
                    bill.has_invoice = vw.FHookStatus == 2 ? true : false;
                    if (vw.FDate >= firstDayInMonth) {
                        isCurrentMonth = 1;
                    }
                }
                detail = new ReturnBillDetail();
                detail.entry_no = i;
                detail.product_number = vw.FProductNumber;
                detail.product_name = vw.FProductName;
                detail.product_model = vw.FProductModel;
                detail.seorder_no = vw.FOrderBillNo;
                detail.stock_no = vw.FBillNo;
                detail.stock_inter_id = interId;
                detail.stock_entry_id = entryId;
                detail.aux_qty = vw.Fauxqty;
                detail.can_apply_qty = vw.FcanApplyQty;
                billDetails.Add(detail);
            }
            billDetails = billDetails.OrderBy(b=>b.stock_entry_id).OrderBy(b => b.stock_no).ToList();
            ViewData["bill"] = bill;
            ViewData["details"] = billDetails;
            ViewData["isCurrentMonth"] = isCurrentMonth;

            utl.writeEventLog(MODELNAME, "新建一张退换货申请", bill.sys_no, Request);
            return View();
        }

        //从旧退修单新增
        [SessionTimeOutFilter()]
        public ActionResult NewOrderFromOld(string sys_no) {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            ViewData["userName"] = db.User.Single(u => u.id == userId).real_name;

            var bill = db.ReturnBill.Where(b => b.sys_no == sys_no).First();
            ViewData["details"] = bill.ReturnBillDetail.ToList(); 

            bill.sys_no = utl.getReturnSystemNo(sys_no.Substring(0, 2));
            bill.old_sys_no = sys_no;
            bill.id = 0;
            bill.fdate = DateTime.Now;
            ViewData["bill"] = bill;
            
            utl.writeEventLog(MODELNAME, "从旧退修单【"+sys_no+"】新增", bill.sys_no, Request);
            return View("CreateReturnBill");
        }

        //保存退修单
        [HttpPost]
        public JsonResult saveReturnBill(FormCollection fc)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            //表头
            string fDate = fc.Get("fdate");
            string sysNo = fc.Get("sys_no");
            string returnDep = fc.Get("return_dep");
            //string pmc = fc.Get("pmc");
            string customerNumber = fc.Get("customer_number");
            string customerName = fc.Get("customer_name");
            string hasInvoice = fc.Get("has_invoice");
            string needResend = fc.Get("need_resend");
            string expressName = fc.Get("express_name");
            string expressNo = fc.Get("express_no");
            string expressQty = fc.Get("express_qty");
            string comment = fc.Get("comment");
            string salerComment = fc.Get("saler_comment");
            string oldSysNo = fc.Get("old_sys_no");

            //表体
            string[] tSeorderNo = fc.Get("seorder_no").Split(',');
            string[] tStockNo = fc.Get("stock_no").Split(',');
            string[] tStockInterID = fc.Get("stock_interid").Split(',');
            string[] tStockEntryID = fc.Get("stock_entryid").Split(',');
            string[] tProductNumber = fc.Get("product_number").Split(',');
            string[] tProductName = fc.Get("product_name").Split(',');
            string[] tProductModel = fc.Get("product_model").Split(',');
            string[] tAuxQty = fc.Get("aux_qty").Split(',');
            string[] tQty = fc.Get("qty").Split(',');
            string[] tCanApplyQty = fc.Get("can_apply_qty").Split(',');
            //string[] tCustomerBackQty = fc.Get("customer_back_qty").Split(',');
            //string[] tQcGoodQty = fc.Get("qc_good_qty").Split(',');
            //string[] tQcBadQty = fc.Get("qc_bad_qty").Split(',');
            //string[] tQcComment = fc.Get("qc_comment").Split(',');

            #region 保存之前的验证
            //申请未结束之前，不能对本批次再次申请，以下放到提交的方法。
            //for (int i = 0; i < tSeorderNo.Length; i++) {
            //    int stockInterID=Int32.Parse(tStockInterID[i]);
            //    int stockEntryID=Int32.Parse(tStockEntryID[i]);
            //    var existsStock = db.ReturnBillDetail.Where(r => r.stock_inter_id == stockInterID && r.stock_entry_id == stockEntryID && r.is_finish != true);
            //    if (existsStock.Count() > 0) {
            //        return Json(new { suc = false, msg = "存在未结束的申请" });
            //    }
            //}
            #endregion

            //表头内容设置
            ReturnBill bill = new ReturnBill();
            bill.user_id = userId;
            bill.fdate = DateTime.Parse(fDate);
            bill.sys_no = sysNo;
            bill.return_dept = Int32.Parse(returnDep);
            bill.customer_number = customerNumber;
            bill.customer_name = customerName;
            bill.has_invoice = hasInvoice == "0" ? false : true;
            bill.need_resend = needResend == "0" ? false : true;
            bill.express_name = expressName;
            bill.express_no = expressNo;
            bill.old_sys_no = oldSysNo;
            if (!string.IsNullOrEmpty(expressQty))
            {
                bill.express_qty = Int32.Parse(expressQty);
            }
            //if (!string.IsNullOrEmpty(pmc)) {
            //    bill.pmc_id = Int32.Parse(pmc);
            //}
            bill.comment = comment;
            bill.saler_comment = salerComment;
            bill.all_finish = false;

            //表体内容设置
            List<ReturnBillDetail> details = new List<ReturnBillDetail>();
            ReturnBillDetail detail;
            for (int i = 0; i < tSeorderNo.Length; i++)
            {
                detail = new ReturnBillDetail();
                detail.ReturnBill = bill;
                detail.entry_no = i + 1;
                detail.seorder_no = tSeorderNo[i];
                detail.stock_no = tStockNo[i];
                detail.stock_inter_id = Int32.Parse(tStockInterID[i]);
                detail.stock_entry_id = Int32.Parse(tStockEntryID[i]);
                detail.product_number = tProductNumber[i];
                detail.product_name = tProductName[i];
                detail.product_model = tProductModel[i];
                detail.aux_qty = decimal.Parse(tAuxQty[i]);
                detail.return_qty = decimal.Parse(tQty[i]);
                detail.can_apply_qty = decimal.Parse(tCanApplyQty[i]);
                //detail.customer_back_qty = String.IsNullOrEmpty(tCustomerBackQty[i]) ? (decimal?)(null) : decimal.Parse(tCustomerBackQty[i]);
                //detail.qc_good_qty = String.IsNullOrEmpty(tQcGoodQty[i]) ? (decimal?)(null) : decimal.Parse(tQcGoodQty[i]);
                //detail.qc_bad_qty = String.IsNullOrEmpty(tQcBadQty[i]) ? (decimal?)(null) : decimal.Parse(tQcBadQty[i]);
                //detail.qc_comment = tQcComment[i];
                detail.is_finish = false;
                detail.has_red_qty = 0;
                detail.has_replace_qty = 0;
                details.Add(detail);
            }

            try
            {
                //如果已存在，则删除
                var existedBill = db.ReturnBill.Where(r => r.sys_no == sysNo);
                if (existedBill.Count() > 0)
                {
                    db.ReturnBillDetail.DeleteAllOnSubmit(existedBill.First().ReturnBillDetail);
                    db.ReturnBill.DeleteOnSubmit(existedBill.First());
                }

                //保存表头和表体
                db.ReturnBill.InsertOnSubmit(bill);
                db.ReturnBillDetail.InsertAllOnSubmit(details);
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                return Json(new { suc = false, msg = e.Message }, "text/html");
            }

            utl.writeEventLog(MODELNAME, "保存退换货申请", sysNo, Request);
            return Json(new { suc = true }, "text/html");
        }

        //营业员查询退修单
        [SessionTimeOutFilter()]
        public ActionResult CheckAllReturnBill()
        {
            var queryData = Request.Cookies["op_qd_th"];
            if (queryData != null && queryData.Values.Get("sa_th_cu") != null)
            {
                ViewData["cust_no"] = utl.DecodeToUTF8(queryData.Values.Get("sa_th_cu"));
                ViewData["stock_no"] = utl.DecodeToUTF8(queryData.Values.Get("sa_th_st"));
                ViewData["pro_model"] = utl.DecodeToUTF8(queryData.Values.Get("sa_th_mo"));
                ViewData["from_date"] = queryData.Values.Get("sa_th_fd");
                ViewData["to_date"] = queryData.Values.Get("sa_th_td");
                ViewData["audit_result"] = queryData.Values.Get("sa_th_ar");
            }
            else
            {
                ViewData["from_date"] = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                ViewData["to_date"] = DateTime.Now.ToString("yyyy-MM-dd");
                ViewData["audit_result"] = 10;
            }
            return View();
        }

        [HttpPost]
        public JsonResult CheckAllReturnBill(FormCollection fc)
        {
            //db.updateReturnRedQty();
            //utl.writeEventLog(MODELNAME, "查询退换货申请之前，更新红字与状态", "", Request);

            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            string customerNumber = fc.Get("cust_no");
            string stockNo = fc.Get("stock_no");
            string model = fc.Get("pro_model");
            string fromDateStr = fc.Get("fromDate");
            string toDateStr = fc.Get("toDate");
            string auditResultStr = fc.Get("auditResult");

            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd_th"];
            if (queryData == null) queryData = new HttpCookie("op_qd_th");
            queryData.Values.Set("sa_th_cu", utl.EncodeToUTF8(customerNumber));
            queryData.Values.Set("sa_th_st", utl.EncodeToUTF8(stockNo));
            queryData.Values.Set("sa_th_mo", utl.EncodeToUTF8(model));
            queryData.Values.Set("sa_th_fd", fromDateStr);
            queryData.Values.Set("sa_th_td", toDateStr);
            queryData.Values.Set("sa_th_ar", auditResultStr);
            queryData.Expires = DateTime.Now.AddDays(7);
            Response.AppendCookie(queryData);

            //处理一下参数
            DateTime fromDate, toDate;
            int auditResult;
            if (!DateTime.TryParse(fromDateStr, out fromDate)) fromDate = DateTime.Parse("1980-1-1");
            if (!DateTime.TryParse(toDateStr, out toDate)) toDate = DateTime.Parse("2099-9-9");
            if (!Int32.TryParse(auditResultStr, out auditResult)) auditResult = 10;
            
            var result = (from v in db.VwReturnBill
                          where v.user_id == userId
                          && (v.hide_flag == null || v.hide_flag == false)
                          && (v.customer_number.Contains(customerNumber) || v.customer_name.Contains(customerNumber))
                          && (v.stock_no.Contains(stockNo) || v.seorder_no.Contains(stockNo))
                          && (v.product_model.Contains(model) || v.sys_no.Contains(model))
                          && v.fdate >= fromDate
                          && v.fdate <= toDate
                          && (auditResult == 10 || v.audit_result == auditResult || (v.audit_result == null && auditResult == 0))
                          orderby v.fdate, v.sys_no, v.entry_no
                          select new
                          {
                              FBillID = v.bill_id,
                              FDate = v.fdate,
                              FSysNo = v.sys_no,
                              FStockNo = v.stock_no,
                              FOrderBillNo = v.seorder_no,
                              FCustomerName = v.customer_name,
                              FProductName = v.product_name,
                              FProductModel = v.product_model,
                              Fauxqty = v.aux_qty,
                              FReturnQty = v.return_qty,
                              FHasInvoice = v.has_invoice == true ? "已开" : "未开",
                              FNeedResend = v.need_resend == true ? "换货" : "退红字",
                              apply_status = v.audit_result == null ? "未提交" : v.audit_result == 1 ? "申请成功" : v.audit_result == -1 ? "申请失败" : "审批中"
                          }).Take(200).ToList();

            utl.writeEventLog(MODELNAME, "查询的退换货申请条数：" + result.Count(), "", Request);
            return Json(new { suc = true, list = result }, "text/html");
        }

        //修改之前判断是否已提交
        public JsonResult IsBeginApply(string sys_no)
        {
            if (db.Apply.Where(a => a.sys_no == sys_no).Count() > 0)
            {
                utl.writeEventLog(MODELNAME, "已提交不能修改", sys_no, Request, 10);
                return Json(new { suc = true });
            }
            return Json(new { suc = false });
        }

        //修改已保存
        public ActionResult EditReturnBill(int id)
        {
            var bill = db.ReturnBill.Single(r => r.id == id);
            ViewData["bill"] = bill;
            ViewData["details"] = bill.ReturnBillDetail.ToList();
            ViewData["userName"] = bill.User.real_name;
            utl.writeEventLog(MODELNAME, "修改退换货申请", bill.sys_no, Request);
            return View("CreateReturnBill");
        }

        //隐藏退换货申请
        public ActionResult HideReturnBill(string sys_no) {

            try
            {
                var bill = db.ReturnBill.Single(r => r.sys_no == sys_no);
                bill.hide_flag = true;
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                return Json(new { suc = false, msg = ex.Message });
            }


            return Json(new { suc = true });
        }

        //退修单详情
        public ActionResult CheckReturnBill(int id)
        {
            string status;
            var bill = db.ReturnBill.Single(r => r.id == id);
            ViewData["bill"] = bill;
            ViewData["details"] = bill.ReturnBillDetail.ToList();
            ViewData["userName"] = bill.User.real_name;
            ViewData["return_dep"] = db.Department.Where(d => d.dep_type == "退货事业部" && d.dep_no == bill.return_dept).First().name;
            //退换货状态
            var aps = db.Apply.Where(a => a.sys_no == bill.sys_no);
            if (aps.Count() < 1)
                status = "未提交";
            else if (aps.Where(a => a.success == true).Count() > 0)
            {
                status = "申请成功";
                //if (bill.need_resend == true)
                //    status = "等待换货";
                //else
                //    status = "等待退红字";
            }
            else if (aps.Where(a => a.success == false).Count() > 0)
            {
                status = "申请失败";
            }
            else
                status = "审核中";
            ViewData["status"] = status;
            //挂起订单信息
            var blockInfo = db.BlockOrder.Where(b => b.sys_no == bill.sys_no).OrderBy(b => b.step).ToList();
            ViewData["blockInfo"] = blockInfo;

            utl.writeEventLog(MODELNAME, "查看退修单详情", bill.sys_no, Request);
            return View();
        }

        //提交之前验证
        public JsonResult ValidateBeforApply(string sys_no,string pmc)
        {
            //1. 没有保存不能提交
            if (db.ReturnBill.Where(r => r.sys_no == sys_no).Count() < 1)
            {
                return Json(new { suc = false, msg = "提交之前请先保存单据！" });
            }

            //2. 不能重复提交
            if (db.Apply.Where(a => a.sys_no == sys_no).Count() > 0)
            {
                return Json(new { suc = false, msg = "不能重复提交！" });
            }

            //2.1 PMC审核人没有选择不能保存
            //if (db.ReturnBill.Where(r => r.sys_no == sys_no && r.pmc_id != null).Count() < 1) {
            //    return Json(new { suc = false, msg = "退货PMC审核人不能为空！" });
            //}

            //3. 检查每一条单据分录，如果存在未完成的出库分录，则不能提交,作废
            //3.1. 检查每一条单据分录，如果在申请中的退货数量总量 + 7天内申请完成但未导入K3的数量 > K3中的可提交数量
            //4. 检查每一条出库单据分录，如果销售订单的关联数量小于退货数量，则不能提交
            //5. 检查每一条出库单据分录，如果这条分录是已钩稽，查看是否有未钩稽的数量，如果有，即提示优先选择未钩稽的数量
            int? FInterID, FEntryID, LineNo;
            foreach (var det in db.ReturnBill.Where(r => r.sys_no == sys_no).First().ReturnBillDetail)
            {
                FInterID = det.stock_inter_id;
                FEntryID = det.stock_entry_id;
                LineNo = det.entry_no;
                //3
                //var notFinished = db.ReturnBillDetail.Where(rd => rd.ReturnBill.sys_no != sys_no && rd.stock_inter_id == FInterID && rd.stock_entry_id == FEntryID && rd.is_finish != true);
                //var notFinished = from rd in db.ReturnBillDetail
                //                  join ap in db.Apply on rd.ReturnBill.sys_no equals ap.sys_no
                //                  where
                //                  ap.id != null
                //                  && ap.success == null
                //                  && rd.ReturnBill.sys_no != sys_no
                //                  && rd.stock_inter_id == FInterID
                //                  && rd.stock_entry_id == FEntryID
                //                  select rd;
                //if (notFinished.Count() > 0)
                //{
                //    utl.writeEventLog(MODELNAME, string.Format("存在未结束的申请，对应行号：{0};对应的退货编号为：{1}", LineNo, notFinished.First().ReturnBill.sys_no), sys_no, Request, 10);
                //    return Json(new { suc = false, msg = string.Format("存在未结束的申请，对应行号：{0};对应的退货编号为：{1}", LineNo, notFinished.First().ReturnBill.sys_no) });
                //}

                var SEOrders = db.VWBlueStockBill.Where(v => v.FInterID == FInterID && v.FEntryID == FEntryID);

                if (SEOrders.Count() < 1)
                {
                    return Json(new { suc = false, msg = "对应行号："+LineNo+";出库单号："+det.stock_no+";此出库单的可申请红字数量等于0，提交失败" });
                }

                var SEOrder = SEOrders.First();

                //3.1
                //正在申请中的数量
                var isAppliedQty = (from r in db.ReturnBillDetail
                                   join ap in db.Apply on r.ReturnBill.sys_no equals ap.sys_no
                                   where ap.id != null
                                   && ap.success == null
                                   && r.stock_inter_id == FInterID
                                   && r.stock_entry_id == FEntryID
                                   select r.real_return_qty==null? r.return_qty:r.real_return_qty).Sum();

                //记得添加此处申请的数量
                isAppliedQty += det.return_qty;

                //这里还要加上7天之内已申请完成但是未导入K3红字的数量 2015-9-30
                decimal? hasAppliedButNotK3Qty = 0m;
                DateTime sevenDaysAgo = DateTime.Now.AddDays(-7);
                var oldApplies = (from r in db.ReturnBillDetail
                                  join a in db.Apply on r.ReturnBill.sys_no equals a.sys_no
                                  where a.success == true
                                  && r.stock_inter_id == FInterID
                                  && r.stock_entry_id == FEntryID
                                  && a.start_date >= sevenDaysAgo
                                  select new
                                  {
                                      sysNo = a.sys_no,
                                      returnQty = r.real_return_qty
                                  }).ToList();
                foreach (var oldApply in oldApplies)
                {
                    if (db.ImportSysNoLog.Where(s => s.sys_no == oldApply.sysNo).Count() < 1)
                    {
                        bool? importFlag = null;
                        db.hasImportIntoK3(oldApply.sysNo, "TH", ref importFlag);
                        if (importFlag == false)
                        {
                            hasAppliedButNotK3Qty += oldApply.returnQty;
                        }
                    }
                }

                if (isAppliedQty + hasAppliedButNotK3Qty > SEOrder.FcanApplyQty)
                {
                    utl.writeEventLog(MODELNAME, "对应行号：" + LineNo + ";所有申请的退货数量之和：" + isAppliedQty + " 不能大于销售订单的可申请数量：" + SEOrder.FcanApplyQty, sys_no, Request, 10);
                    return Json(new { suc = false, msg = "对应行号：" + LineNo + ";所有申请的退货数量之和：" + isAppliedQty + " 不能大于销售订单的可申请数量：" + SEOrder.FcanApplyQty });
                }

                //4                
                if (SEOrder.FCommitQty < det.return_qty)
                {
                    utl.writeEventLog(MODELNAME, "对应行号：" + LineNo + ";申请的退货数量不能大于销售订单的关联数量：" + SEOrder.FCommitQty, sys_no, Request, 10);
                    return Json(new { suc = false, msg = "对应行号：" + LineNo + ";申请的退货数量不能大于销售订单的关联数量：" + SEOrder.FCommitQty });
                }
                if (SEOrder.FStockQty < det.return_qty)
                {
                    utl.writeEventLog(MODELNAME, "对应行号：" + LineNo + ";申请的退货数量不能大于销售订单的出库数量：" + SEOrder.FStockQty, sys_no, Request, 10);
                    return Json(new { suc = false, msg = "对应行号：" + LineNo + ";申请的退货数量不能大于销售订单的出库数量：" + SEOrder.FStockQty });
                }
            }
            //5. 检查勾稽状态是否与蓝字发票状态对应
            string validateResult = utl.ValidateHasInvoiceFlag(sys_no);
            if (!string.IsNullOrEmpty(validateResult)) {
                utl.writeEventLog(MODELNAME, validateResult, sys_no, Request, 10);
                return Json(new { suc = false, msg = validateResult });
            }

            return Json(new { suc = true });
        }
        
        //提交申请
        public ActionResult BeginApply(string sys_no)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var rb = db.ReturnBill.Where(r => r.sys_no == sys_no).First();

            string processType = "TH_RED";           

            Apply apply = new Apply();
            apply.user_id = userId;
            apply.sys_no = sys_no;
            apply.start_date = DateTime.Now;
            apply.ip = Request.UserHostAddress;
            apply.order_type = "TH";
            apply.p_model = db.VwReturnBill.Where(v => v.sys_no == sys_no).First().product_model;

            db.Apply.InsertOnSubmit(apply);

            bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
            List<ApplyDetails> ads = new List<ApplyDetails>();
            //int? aud_id = null;
            int? produceDepId = rb.return_dept;

            try
            {
                if (testFlag)
                {
                    ads = utl.getTestApplySequence(apply, processType, userId);
                }
                else
                {
                    ads = utl.getApplySequence(apply, processType, userId, db.User.Single(u => u.id == userId).Department1.dep_no, produceDepId);
                }
            }
            catch (Exception ex)
            {
                ViewBag.tip = ex.Message;
                return View("tip");
            }

            //退货的PMC审核人是营业员申请的时候选择的
            //int? pmc = db.ReturnBill.Where(r => r.sys_no == sys_no).First().pmc_id;
            //foreach (var ad in ads) {
            //    if (ad.step_name.StartsWith("PMC")) {
            //        ad.user_id = pmc;
            //    }
            //}
            //int? currentStep = 0;
            //foreach (var pd in pr.ProcessDetail.OrderBy(p => p.step))
            //{
            //    if (testFlag)   //测试
            //    {
            //        if (pd.step != currentStep)
            //        {
            //            ads.Add(new ApplyDetails()
            //            {
            //                Apply = apply,
            //                user_id = userId,
            //                step = pd.step,
            //                step_name = pd.step_name,
            //                can_modify = pd.can_modify
            //            });
            //        } 
            //        currentStep = pd.step;                    
            //    }
            //    else
            //    {
            //        if (pd.step_type == 0 || pd.step_type == 1 || pd.step_type == 8)
            //        {   //单人审核
            //            if (pd.step_type == 0)
            //                aud_id = pd.user_id;
            //            else if (pd.step_type == 1)
            //                aud_id = db.User.Single(u => u.id == userId).Department1.charge;
            //            else if (pd.step_type == 8)
            //                aud_id = db.User.Single(u => u.id == userId).Department1.th_charge;

            //            if (aud_id == null)
            //            {
            //                ViewBag.tip = "办事处的审核人员还未设置，请联系管理员！";
            //                return View("tip");
            //            }

            //            ads.Add(new ApplyDetails()
            //            {
            //                Apply = apply,
            //                user_id = aud_id,
            //                step = pd.step,
            //                step_name = pd.step_name,
            //                can_modify = pd.can_modify
            //            });
            //        }
            //        else if ((pd.step_type >= 4 && pd.step_type <= 7) || pd.step_type==9)
            //        {  //事业部客服、品质、计划、出货组、主管审核
            //            IQueryable<int?> auditors = null;
            //            auditors = from pa
            //                           in db.ProduceAuditor.Where(p => p.audit_type == pd.step_type && p.bill_type == "TH"
            //                               && p.produce_dep_id == produceDepId)
            //                       select pa.auditor_id;

            //            if (auditors.Count() < 1)
            //            {
            //                utl.writeEventLog(MODELNAME, "提交失败，事业部的审核人员还未设置，请联系管理员！步骤：" + pd.step_name, sys_no, Request, 100);
            //                ViewBag.tip = "事业部的审核人员还未设置，请联系管理员！代码：" + pd.step_type;
            //                return View("tip");
            //            }

            //            foreach (var auditor in auditors)
            //            {
            //                ads.Add(new ApplyDetails()
            //                {
            //                    Apply = apply,
            //                    user_id = auditor,
            //                    step = pd.step,
            //                    step_name = pd.step_name,
            //                    can_modify = pd.can_modify
            //                });
            //            }
            //        }

            //    }
            //}
            db.ApplyDetails.InsertAllOnSubmit(ads);

            try
            {
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                ViewBag.tip = "提交失败，原因：" + e.Message;
                return View("tip");
            }

            //发送邮件通知下一步的人员

            SomeUtils utis = new SomeUtils();
            if (utis.emailToNextAuditor(apply.id))
            {
                utl.writeEventLog("提交申请——发送邮件", "发送成功", sys_no, Request);
                return RedirectToAction("CheckAllReturnBill");
            }
            else
            {
                utl.writeEventLog("提交申请——发送邮件", "发送失败", sys_no, Request, -1);
                ViewBag.tip = "订单提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
                return View("tip");
            }
        }

        //搜索需要导入红字的退货申请单
        public ActionResult RedBillsToK3()
        {
            return View();
        }

        public JsonResult CheckRedBills()
        {
            return CheckRedBillsBase("", DateTime.Parse("1900-1-1"), DateTime.Parse("2099-9-9"), "", "", -1);
        }

        [HttpPost]
        public JsonResult SearchRedBills(FormCollection fc)
        {
            string customerInfo = fc.Get("cust_info");
            string fromDateStr = fc.Get("fromDate");
            string toDateStr = fc.Get("toDate");
            string model = fc.Get("pro_model");
            int hasImportK3 = Int32.Parse(fc.Get("hasImportK3"));

            DateTime fromDate, toDate;
            if (!DateTime.TryParse(fromDateStr, out fromDate)) fromDate = DateTime.Parse("1990-1-1");
            if (!DateTime.TryParse(toDateStr, out toDate)) toDate = DateTime.Parse("2099-9-9");

            return CheckRedBillsBase(customerInfo, fromDate, toDate, model, model, hasImportK3);
        }

        public JsonResult CheckRedBillsBase(string customerInfo, DateTime fromDate, DateTime toDate, string sys_no, string model, int hasImportToK3)
        {
            //首先与K3交互，更新本地红字数量和状态
            db.updateReturnRedQty();
            utl.writeEventLog(MODELNAME, "查询所有红字单之前，更新红字数量和状态", "", Request);

            //包括退换货的红字和换货最后退红字的红字
            var result = (from v in db.VwAllRedBill
                          where (v.customer_name.Contains(customerInfo) || v.customer_number.Contains(customerInfo))
                          && v.finish_date >= fromDate
                          && v.finish_date <= toDate
                          && (v.sys_no.Contains(sys_no) || v.product_model.Contains(model))
                          && (
                             hasImportToK3 == 0
                             || (hasImportToK3 == -1 && v.import_stock_no == null)
                             || (hasImportToK3 == 1 && v.import_stock_no != null)
                             )
                          orderby v.finish_date
                          select new
                          {
                              FRedType = v.red_type,
                              FBillID = v.id,
                              FArriveDate = v.finish_date,
                              FSysNo = v.sys_no,
                              FCustomerName = v.customer_name,
                              FProductName = v.product_name,
                              FProductModel = v.product_model,
                              FReturnQty = v.return_qty,
                              FImportK3No = v.import_stock_no
                          }).Take(300).ToList();
            utl.writeEventLog(MODELNAME, "查询到的红字单条数：" + result.Count(), "", Request);
            return Json(result, "text/html");
        }

        //检查某订单某型号是否存在有未钩稽的出库记录
        public JsonResult CheckStockBillsHasNoHook(string FCustomerNo, string FOrderBillNo, string FProductNumber)
        {
            //一些客户除外：珠海市魅族科技有限公司，
            if ("01.SZ.0046".Contains(FCustomerNo)) {
                return Json(new { suc = false });
            }
            var bills = db.VWBlueStockBill.Where(v => v.FOrderBillNo == FOrderBillNo && v.FProductNumber == FProductNumber && v.FcanApplyQty > 0 && v.FHookStatus == 0).ToList();
            if (bills.Count() > 0) {
                foreach (var bill in bills) {
                    var isAppliedQty = (from r in db.ReturnBillDetail
                                        join ap in db.Apply on r.ReturnBill.sys_no equals ap.sys_no
                                        where ap.id != null
                                        && ap.success == null
                                        && r.stock_inter_id == bill.FInterID
                                        && r.stock_entry_id == bill.FEntryID
                                        select r.real_return_qty == null ? r.return_qty : r.real_return_qty).Sum();
                    if (bill.Fauxqty > isAppliedQty) {
                        return Json(new { suc = true, msg = "此订单此型号存在未钩稽的出货单，请优先选择未钩稽的出库单做红字退货。未钩稽出库单号：【" + bill.FBillNo + "】" });
                    }
                }
                
            }
            return Json(new { suc = false });

        }

        //搜索需要换货的退货申请单
        //[SessionTimeOutFilter()]
        //public ActionResult ResendBills()
        //{
        //    return View();
        //}

        //public JsonResult CheckResendBills()
        //{
        //    return CheckResendBillsBase("", DateTime.Parse("1900-1-1"), DateTime.Parse("2099-9-9"), "", "");
        //}

        //[HttpPost]
        //public JsonResult SearchResendBills(FormCollection fc)
        //{
        //    string customerInfo = fc.Get("cust_info");
        //    string fromDateStr = fc.Get("fromDate");
        //    string toDateStr = fc.Get("toDate");
        //    string model = fc.Get("pro_model");

        //    DateTime fromDate, toDate;
        //    if (!DateTime.TryParse(fromDateStr, out fromDate)) fromDate = DateTime.Parse("1990-1-1");
        //    if (!DateTime.TryParse(toDateStr, out toDate)) toDate = DateTime.Parse("2099-9-9");

        //    return CheckResendBillsBase(customerInfo, fromDate, toDate, model, model);
        //}

        //public JsonResult CheckResendBillsBase(string customerInfo, DateTime fromDate, DateTime toDate, string sys_no, string model)
        //{
        //    //首先与K3交互，更新本地红字数量和状态
        //    db.updateReturnRedQty();
        //    utl.writeEventLog(MODELNAME, "查询换货申请之前，更新本地红字数量和状态", "", Request);

        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    var myOutGroups = (from pr in db.ProduceAuditor
        //                       where pr.bill_type == "TH"
        //                       && pr.auditor_id == userId
        //                       && pr.audit_type == 7  //7是出货组
        //                       select pr.ProduceDep.name).ToArray();
        //    var result = (from v in db.VwReturnBill
        //                  join ap in db.Apply on v.sys_no equals ap.sys_no
        //                  where (v.customer_name.Contains(customerInfo) || v.customer_number.Contains(customerInfo))
        //                  && v.need_resend == true  //换货申请单
        //                  && v.all_finish != true
        //                  && v.return_qty - v.has_red_qty - v.has_replace_qty > 0.000001m
        //                  && myOutGroups.Contains(v.return_dept) //只可查询到本出货组的换货单
        //                  && ap.success == true
        //                  && ap.finish_date >= fromDate
        //                  && ap.finish_date <= toDate
        //                  && (v.sys_no.Contains(sys_no) || v.product_model.Contains(model))
        //                  orderby ap.finish_date, v.entry_no
        //                  select new
        //                  {
        //                      FBillID = v.bill_id,
        //                      FDetailID = v.bill_detail_id,
        //                      FEntryNO = v.entry_no,
        //                      FArriveDate = ap.finish_date,
        //                      FSysNo = v.sys_no,
        //                      FCustomerName = v.customer_name,
        //                      FProductName = v.product_name,
        //                      FProductModel = v.product_model,
        //                      FReturnQty = v.return_qty,
        //                      FHasReturnQty = v.has_replace_qty + v.has_red_qty,
        //                      FCanRentrunQty = v.return_qty - (v.has_replace_qty == null ? 0 : v.has_replace_qty),
        //                      FEmpName = v.user_name
        //                  }).ToList();

        //    utl.writeEventLog(MODELNAME, "查询到的换货申请数量：" + result.Count(), "", Request);
        //    return Json(result, "text/html");
        //}

        ////新建申请之前，保证不存在未完成的申请.如是退红字的，还要检查是否已导入K3红字
        //public JsonResult IsResendNotFinish(string FEntryIDS)
        //{
        //    string[] entryIdArr = FEntryIDS.Split(',');
        //    foreach (string entryIdStr in entryIdArr)
        //    {
        //        int entryId = Int32.Parse(entryIdStr);
        //        var existedDetails = db.ResendBillDetail.Where(r => r.return_bill_detail_id == entryId).OrderByDescending(r => r.id);
        //        if (existedDetails.Count() > 0)
        //        {
        //            //取倒序的第一个，表示只验证最新的一个申请是否已结束就可以了
        //            string sys_no = existedDetails.First().ResendBill.sys_no;
        //            if (db.Apply.Where(a => a.sys_no == sys_no && a.success == null).Count() > 0)
        //            {
        //                utl.writeEventLog(MODELNAME, "存在未结束的申请，不能再新建换货申请", sys_no, Request);
        //                return Json(new { suc = true, sys_no = sys_no });
        //            }
        //            else if (existedDetails.First().ResendBill.is_red == true && db.ReturnBillDetail.Single(r => r.id == entryId).is_finish != true)
        //            {
        //                //如果是退红字的申请，还要检查是否已经导入K3。如未导入K3，也是不可以的。
        //                utl.writeEventLog(MODELNAME, "存在未导入K3红字的申请，不能再新建换货申请", sys_no, Request);
        //                return Json(new { suc = true, sys_no = sys_no + "未导入K3红字" });
        //            }


        //        }
        //    }

        //    return Json(new { suc = false });
        //}

        ////新建换货申请单
        //[SessionTimeOutFilter()]
        //public ActionResult CreateResendBill(string FEntryIDS)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    string userName = db.User.Single(u => u.id == userId).real_name;

        //    string[] entryIdArr = FEntryIDS.Split(',');

        //    List<VwResendBill> list = new List<VwResendBill>();
        //    ReturnBillDetail returnDetail = new ReturnBillDetail();
        //    VwResendBill vw;
        //    for (int i = 0; i < entryIdArr.Length; i++)
        //    {
        //        vw = new VwResendBill();
        //        int entryId = Int32.Parse(entryIdArr[i]);

        //        returnDetail = db.ReturnBillDetail.Single(r => r.id == entryId);
        //        vw.resend_id = 0;
        //        vw.is_red = false;
        //        vw.sys_no = "";
        //        vw.customer_name = returnDetail.ReturnBill.customer_name;
        //        vw.customer_no = returnDetail.ReturnBill.customer_number;
        //        vw.dep_id = returnDetail.ReturnBill.return_dept;
        //        vw.dep_name = returnDetail.ReturnBill.ProduceDep.name;
        //        vw.return_bill_detail_id = entryId;
        //        vw.fdate = DateTime.Now;
        //        vw.emp_name = returnDetail.ReturnBill.User.real_name;
        //        vw.user_name = userName;
        //        vw.stock_no = returnDetail.stock_no;
        //        vw.return_qty = returnDetail.return_qty;
        //        vw.has_replace_qty = (decimal)returnDetail.has_replace_qty;
        //        vw.product_name = returnDetail.product_name;
        //        vw.product_model = returnDetail.product_model;
        //        vw.product_number = returnDetail.product_number;
        //        list.Add(vw);
        //    }
        //    ViewData["old_sys_no"] = returnDetail.ReturnBill.sys_no;
        //    ViewData["list"] = list;
        //    utl.writeEventLog(MODELNAME, "根据" + returnDetail.ReturnBill.sys_no + "退换货申请，新建换货申请单", "", Request);
        //    return View();
        //}

        ////保存换货申请单
        //public JsonResult saveResendBill(FormCollection fc)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    //表头
        //    string sysNo = fc.Get("sys_no");
        //    string oldSysNo = fc.Get("old_sys_no");
        //    string FDateStr = fc.Get("fdate");
        //    string isRed = fc.Get("is_red");
        //    string description = fc.Get("description");
        //    string customerNo = fc.Get("customer_no");
        //    string customerName = fc.Get("customer_name");
        //    string depId = fc.Get("dep_id");

        //    //表体
        //    string[] FDetailsIDs = fc.Get("FDetailID").Split(',');
        //    string[] FApplyQtys = fc.Get("FApplyQty").Split(',');
        //    string[] FComments = fc.Get("FComment").Split(',');

        //    //sysNo为空表示这是新增的，所以要通过退换货申请带过来的oldSysNo生成。如果sysNo不为空，则表示是修改的，直接用这个sysNo
        //    if (string.IsNullOrEmpty(sysNo))
        //    {
        //        sysNo = utl.getResendSystemNo(oldSysNo);
        //    }

        //    ResendBill rs = new ResendBill();
        //    List<ResendBillDetail> list = new List<ResendBillDetail>();

        //    DateTime FDate = new DateTime();
        //    if (!DateTime.TryParse(FDateStr, out FDate))
        //    {
        //        return Json(new { suc = false, msg = "换货日期不合法，保存失败" }, "text/html");
        //    }
        //    rs.fdate = FDate;
        //    rs.is_red = isRed.Equals("0") ? false : true;
        //    rs.description = description;
        //    rs.user_id = userId;
        //    rs.dep_id = Int32.Parse(depId);
        //    rs.customer_name = customerName;
        //    rs.customer_no = customerNo;
        //    rs.sys_no = sysNo;

        //    for (int i = 0; i < FDetailsIDs.Length; i++)
        //    {
        //        ResendBillDetail rb = new ResendBillDetail();
        //        rb.ResendBill = rs;
        //        rb.resend_num = decimal.Parse(FApplyQtys[i]);
        //        rb.return_bill_detail_id = Int32.Parse(FDetailsIDs[i]);
        //        rb.comment = FComments[i];
        //        list.Add(rb);
        //    }

        //    try
        //    {
        //        if (!string.IsNullOrEmpty(sysNo))
        //        {
        //            var existedBill = db.ResendBill.Where(r => r.sys_no == sysNo);
        //            if (existedBill.Count() > 0)
        //            {
        //                db.ResendBillDetail.DeleteAllOnSubmit(existedBill.First().ResendBillDetail);
        //                db.ResendBill.DeleteOnSubmit(existedBill.First());
        //            }
        //        }

        //        db.ResendBill.InsertOnSubmit(rs);
        //        db.ResendBillDetail.InsertAllOnSubmit(list);

        //        db.SubmitChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { suc = false, msg = ex.Message }, "text/html");
        //    }

        //    utl.writeEventLog(MODELNAME, "保存换货申请", sysNo, Request);
        //    return Json(new { suc = true, sys_no = sysNo }, "text/html");
        //}

        ////查询所有自己的换货申请
        //[SessionTimeOutFilter()]
        //public ActionResult CheckAllResendBill()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public JsonResult CheckAllResendBill(FormCollection fc)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

        //    string customerNumber = fc.Get("cust_no");
        //    string stockNo = fc.Get("stock_no");
        //    string model = fc.Get("pro_model");
        //    string fromDateStr = fc.Get("fromDate");
        //    string toDateStr = fc.Get("toDate");
        //    string auditResultStr = fc.Get("auditResult");

        //    //处理一下参数
        //    DateTime fromDate, toDate;
        //    int auditResult;
        //    if (!DateTime.TryParse(fromDateStr, out fromDate)) fromDate = DateTime.Parse("1980-1-1");
        //    if (!DateTime.TryParse(toDateStr, out toDate)) toDate = DateTime.Parse("2099-9-9");
        //    if (!Int32.TryParse(auditResultStr, out auditResult)) auditResult = 10;

        //    var result = (from v in db.VwResendBill
        //                  where v.user_id == userId
        //                  && (v.customer_no.Contains(customerNumber) || v.customer_name.Contains(customerNumber))
        //                  && v.stock_no.Contains(stockNo)
        //                  && (v.product_model.Contains(model) || v.sys_no.Contains(model))
        //                  && v.fdate >= fromDate
        //                  && v.fdate <= toDate
        //                  && (auditResult == 10 || v.audit_result == auditResult || (v.audit_result == null && auditResult == 0))
        //                  orderby v.fdate, v.sys_no
        //                  select new
        //                  {
        //                      FBillID = v.resend_id,
        //                      FDate = v.fdate,
        //                      FSysNo = v.sys_no,
        //                      FStockNo = v.stock_no,
        //                      FCustomerName = v.customer_name,
        //                      FProductName = v.product_name,
        //                      FProductModel = v.product_model,
        //                      FReturnQty = v.return_qty,
        //                      FApplyQty = v.resend_num,
        //                      FIsRed = v.is_red == true ? "退红字" : "退货",
        //                      FApplyStatus = v.audit_result == null ? "未提交" : v.audit_result == 1 ? "申请成功" : v.audit_result == -1 ? "申请失败" : "审批中"
        //                  }).Take(200).ToList();

        //    utl.writeEventLog(MODELNAME, "查看换货申请单，条数：" + result.Count(), "", Request);
        //    return Json(new { suc = true, list = result });
        //}

        ////查看单张换货申请详细信息
        //[SessionTimeOutFilter()]
        //public ActionResult CheckResendBill(int id)
        //{
        //    var list = db.VwResendBill.Where(v => v.resend_id == id).ToList();
        //    //挂起订单信息
        //    string sysNo = db.ResendBill.Single(r => r.id == id).sys_no;
        //    var blockInfo = db.BlockOrder.Where(b => b.sys_no == sysNo).OrderBy(b => b.step).ToList();
        //    ViewData["blockInfo"] = blockInfo;

        //    //单据状态
        //    var first = list[0];
        //    var status = "";
        //    switch (first.audit_result)
        //    {
        //        case null:
        //            status = "未提交";
        //            break;
        //        case 0:
        //            status = "审核中";
        //            break;
        //        case -1:
        //            status = "审核失败";
        //            break;
        //        case 1:
        //            if (first.is_red == true)
        //            {
        //                if (first.is_finish == true)
        //                    status = "审核完成，已导入K3";
        //                else
        //                    status = "审核完成，等待导入K3";
        //            }
        //            else
        //            {
        //                status = "审核完成，已退货";
        //            }
        //            break;
        //    }

        //    ViewData["status"] = status;
        //    ViewData["list"] = list;
        //    return View();
        //}

        ////修改已保存的换货申请
        //public ActionResult EditResendBill(int id)
        //{
        //    ViewData["list"] = db.VwResendBill.Where(v => v.resend_id == id).ToList();
        //    return View("CreateResendBill");
        //}

        

        ////换货提交之前先验证
        //public JsonResult ValidateBeforApplyHH(string sys_no)
        //{
        //    //1. 没有保存不能提交
        //    if (db.ResendBill.Where(r => r.sys_no == sys_no).Count() < 1)
        //    {
        //        return Json(new { suc = false, msg = "提交之前请先保存单据！" });
        //    }

        //    //2. 不能重复提交
        //    if (db.Apply.Where(a => a.sys_no == sys_no).Count() > 0)
        //    {
        //        return Json(new { suc = false, msg = "不能重复提交！" });
        //    }

        //    return Json(new { suc = true });
        //}

        ////提交申请
        //public ActionResult BeginApplyHH(string sys_no)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

        //    string processtype;
            
        //    bool? isRed = db.ResendBill.Where(r => r.sys_no == sys_no).First().is_red;
        //    if (isRed == false)
        //    {
        //        //换货
        //        processtype = "HH_1";
        //    }
        //    else
        //    {
        //        //退红字
        //        processtype = "HH_2";                    
        //    }

        //    Apply apply = new Apply();
        //    apply.user_id = userId;
        //    apply.sys_no = sys_no;
        //    apply.start_date = DateTime.Now;
        //    apply.ip = Request.UserHostAddress;
        //    apply.order_type = "HH";
        //    db.Apply.InsertOnSubmit(apply);

        //    bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
        //    List<ApplyDetails> ads = new List<ApplyDetails>();
        //    int? produceDepId = db.ResendBill.Where(r => r.sys_no == sys_no).First().dep_id;

        //    try
        //    {
        //        if (testFlag)
        //        {
        //            ads = utl.getTestApplySequence(apply, processtype, userId);
        //        }
        //        else
        //        {
        //            ads = utl.getApplySequence(apply, processtype, userId, db.User.Single(u => u.id == userId).department, produceDepId);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewBag.tip = ex.Message;
        //        return View("tip");
        //    }

        //    db.ApplyDetails.InsertAllOnSubmit(ads);

        //    try
        //    {
        //        db.SubmitChanges();
        //    }
        //    catch (Exception e)
        //    {
        //        utl.writeEventLog(MODELNAME, "提交换货申请失败，原因：" + e.Message, sys_no, Request);
        //        ViewBag.tip = "提交失败，原因：" + e.Message;
        //        return View("tip");
        //    }

        //    //发送邮件通知下一步的人员

        //    SomeUtils utis = new SomeUtils();
        //    if (utis.emailToNextAuditor(apply.id))
        //    {
        //        utl.writeEventLog("提交申请——发送邮件", "发送成功", sys_no, Request);
        //        return RedirectToAction("CheckAllResendBill");
        //    }
        //    else
        //    {
        //        utl.writeEventLog("提交申请——发送邮件", "发送失败", sys_no, Request, -1);
        //        ViewBag.tip = "订单提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
        //        return View("tip");
        //    }
        //}

        ////营业员搜索可出货的单据
        //public ActionResult SelectForSendOut()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public JsonResult SearchCanSendOut(FormCollection fc) {
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

        //    string custInfo = fc.Get("cust_info");
        //    string fromDateStr = fc.Get("fromDate");
        //    string toDateStr = fc.Get("toDate");
        //    string stockNo = fc.Get("stock_no");
        //    string productModel = fc.Get("pro_model");

        //    DateTime toDate, fromDate;
        //    if (string.IsNullOrEmpty(fromDateStr)) fromDateStr = "1990-1-1";
        //    if (string.IsNullOrEmpty(toDateStr)) toDateStr = "2099-9-9";
        //    if (!DateTime.TryParse(fromDateStr, out fromDate) || !DateTime.TryParse(toDateStr, out toDate)) {
        //        return Json("");
        //    }

        //    var result = (from v in db.VwForSendOut
        //                  where v.fdate >= fromDate
        //                  && v.fdate <= toDate
        //                  && v.emp_id == userId
        //                  && (v.stock_no.Contains(stockNo) || v.sys_no.Contains(stockNo))
        //                  && (v.customer_name.Contains(custInfo) || v.customer_number.Contains(custInfo))
        //                  && v.product_model.Contains(productModel)
        //                  select new
        //                  {
        //                      FDetailID = v.return_detail_id,
        //                      FArriveDate = v.fdate,
        //                      FSysNo = v.sys_no,
        //                      FStockNO = v.stock_no,
        //                      FCustomerNumber=v.customer_number,
        //                      FCustomerName = v.customer_name,
        //                      FProductName = v.product_name,
        //                      FProductModel = v.product_model,
        //                      FReturnQty = v.return_qty,
        //                      FHasFetchQty = v.has_fetch_qty,
        //                      FStoreQty = v.store_qty,
        //                      FEmpName = v.emp_name
        //                  }).Take(200).ToList();

        //    return Json(result);
        //}

        ////新增出货之前检查是否已完成之前的流程
        //public ActionResult IsSendOutNotFinish(string FEntryIDS) {

        //    string[] entryIdArr = FEntryIDS.Split(',');
        //    foreach (string entryIdStr in entryIdArr)
        //    {
        //        int entryId = Int32.Parse(entryIdStr);
        //        var existedFetchBill = db.FetchBillDetail.Where(f => f.return_detail_id == entryId).OrderByDescending(f=>f.id);
        //        //取倒序的第一个，表示只验证最新的一个申请是否已结束就可以了
        //        if (existedFetchBill.Count() > 0) {
        //            var sysNo = existedFetchBill.First().FetchBill.sys_no;
        //            if (db.Apply.Where(a => a.sys_no == sysNo && a.success == null).Count() > 0) {
        //                utl.writeEventLog(MODELNAME, "存在未结束的申请，不能再新建出货申请", sysNo, Request);
        //                return Json(new { suc = true, sys_no = sysNo });
        //            }
        //        }
        //    }
        //    return Json(new { suc = false });
        //}

        ////新增营业员出货流程
        //public ActionResult CreateSendOutBill(string FEntryIDS) {
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    string[] entryIdArr = FEntryIDS.Split(',');
        //    List<VwFetchBill> list = new List<VwFetchBill>();
        //    int entryNo=1;
        //    foreach (string entryIdStr in entryIdArr)
        //    {
        //        int entryId = Int32.Parse(entryIdStr);
        //        VwFetchBill vw = new VwFetchBill();
        //        var send = db.VwForSendOut.Single(v => v.return_detail_id == entryId);

        //        vw.sys_no = utl.getSystemNo("CH");
        //        vw.customer_name = send.customer_name;
        //        vw.customer_no = send.customer_number;
        //        vw.contact = send.FContact;
        //        vw.address = send.FAddress;
        //        vw.phone = send.FPhone;
        //        vw.tax = send.FTax.ToString();
        //        vw.fdate = DateTime.Now;
        //        vw.user_id = send.emp_id;
        //        vw.user_name = send.emp_name;
        //        vw.accepte_unit = send.customer_name;

        //        vw.stock_no = send.stock_no;
        //        vw.return_detail_id = entryId;
        //        vw.product_no = send.product_number;
        //        vw.product_name = send.product_name;
        //        vw.product_model = send.product_model;
        //        vw.entry_no = entryNo++;
        //        vw.store_qty = send.store_qty;
        //        vw.has_fetch_qty = send.has_fetch_qty;
        //        vw.return_qty = send.return_qty;

        //        list.Add(vw);
        //    }

        //    ViewData["list"] = list;
        //    return View();
        //}

        ////保存出货申请
        //public JsonResult saveFetchBill(FormCollection fc) {
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
        //    //表头
        //    string sysNo = fc.Get("sys_no");
        //    string FDateStr = fc.Get("fdate");
        //    string customerNo = fc.Get("customer_no");
        //    string customerName = fc.Get("customer_name");
        //    string accepteUnit = fc.Get("accepte_unit");
        //    string phone = fc.Get("phone");
        //    string address = fc.Get("address");
        //    string contact = fc.Get("contact");
        //    string tax = fc.Get("tax");
        //    string description = fc.Get("description");

        //    //表体
        //    string[] FDetailsIDs = fc.Get("FDetailID").Split(',');
        //    string[] FApplyQtys = fc.Get("FApplyQty").Split(',');
        //    string[] FComments = fc.Get("FComment").Split(',');

        //    DateTime FDate = new DateTime();
        //    if (!DateTime.TryParse(FDateStr, out FDate)) {
        //        return Json(new { suc = false, msg = "出货日期格式不正确" });
        //    }

        //    FetchBill fb = new FetchBill();
        //    List<FetchBillDetail> dets = new List<FetchBillDetail>();

        //    fb.sys_no = sysNo;
        //    fb.fdate = FDate;
        //    fb.customer_no = customerNo;
        //    fb.customer_name = customerName;
        //    fb.accepte_unit = accepteUnit;
        //    fb.phone = phone;
        //    fb.address = address;
        //    fb.contact = contact;
        //    fb.tax = tax;
        //    fb.description = description;
        //    fb.user_id = userId;

        //    db.FetchBill.InsertOnSubmit(fb);

        //    for (int i = 0; i < FDetailsIDs.Length; i++) {
        //        int detailID = Int32.Parse(FDetailsIDs[i]);
        //        ReturnBillDetail rbd = db.ReturnBillDetail.Single(r => r.id == detailID);
        //        FetchBillDetail det = new FetchBillDetail();
        //        det.return_detail_id = detailID;
        //        det.entry_no = i + 1;
        //        det.FetchBill = fb;
        //        det.fetch_qty = decimal.Parse(FApplyQtys[i]);
        //        det.comment = FComments[i];
        //        det.product_model = rbd.product_model;
        //        det.product_name = rbd.product_name;
        //        det.product_no = rbd.product_number;
        //        det.stock_no = rbd.stock_no;
        //        dets.Add(det);
        //    }

        //    db.FetchBillDetail.InsertAllOnSubmit(dets);

        //    //如果已存在，将旧的删除掉
        //    var existedBill = db.FetchBill.Where(f => f.sys_no == sysNo);
        //    if (existedBill.Count() > 0) {
        //        db.FetchBill.DeleteAllOnSubmit(existedBill);
        //        db.FetchBillDetail.DeleteAllOnSubmit(existedBill.First().FetchBillDetail);
        //    }

        //    try {
        //        db.SubmitChanges();
        //    }
        //    catch (Exception ex) {
        //        return Json(new { suc = false, msg = "保存失败，原因："+ex.Message });
        //    }

        //    return Json(new { suc = true });
        //}

        ////营业员搜索出货申请
        //public ActionResult CheckAllFetchBill() {

        //    return View();
        //}

        //[HttpPost]
        //public JsonResult CheckAllFetchBill(FormCollection fc)
        //{
        //    string customerNumber = fc.Get("cust_no");
        //    string stockNo = fc.Get("stock_no");
        //    string model = fc.Get("pro_model");
        //    string fromDateStr = fc.Get("fromDate");
        //    string toDateStr = fc.Get("toDate");
        //    string auditResultStr = fc.Get("auditResult");

        //    //处理一下参数
        //    DateTime fromDate, toDate;
        //    int auditResult;
        //    if (!DateTime.TryParse(fromDateStr, out fromDate)) fromDate = DateTime.Parse("1980-1-1");
        //    if (!DateTime.TryParse(toDateStr, out toDate)) toDate = DateTime.Parse("2099-9-9");
        //    if (!Int32.TryParse(auditResultStr, out auditResult)) auditResult = 10;

        //    var result = (from v in db.VwFetchBill
        //                  where (v.customer_name.Contains(customerNumber) || v.customer_no.Contains(customerNumber))
        //                  && (v.stock_no.Contains(stockNo) || v.sys_no.Contains(stockNo))
        //                  && v.fdate >= fromDate
        //                  && v.fdate <= toDate
        //                  && (auditResult == 10
        //                  || (auditResult == v.audit_result)
        //                  || (v.audit_result == null && auditResult == 0)
        //                  )
        //                  select new
        //                  {
        //                      FBillID = v.bill_id,
        //                      FDate = v.fdate,
        //                      FSysNo = v.sys_no,
        //                      FCustomerName = v.customer_name,
        //                      FStockNo = v.stock_no,
        //                      FProductName = v.product_name,
        //                      FProductModel = v.product_model,
        //                      FReturnQty = v.return_qty,
        //                      FStoreQty = v.store_qty,
        //                      FApplyQty = v.fetch_qty,
        //                      apply_status = v.audit_result == 1 ? "申请成功" : v.audit_result == -1 ? "申请失败" : v.audit_result == 0 ? "审核中" : "未提交",
        //                  }).ToList();

        //    return Json(new { suc = true, list = result });
        //}

        ////查看出货申请
        //public ActionResult CheckFetchBill(int id) {

        //    var list = db.VwFetchBill.Where(v => v.bill_id == id).ToList();

        //    string sysNo = db.FetchBill.Single(f => f.id == id).sys_no;
        //    var blockInfo = db.BlockOrder.Where(b => b.sys_no == sysNo).OrderBy(b => b.step).ToList();
        //    ViewData["blockInfo"] = blockInfo;

        //    ViewData["list"] = list;
        //    return View();
        //}

        ////编辑出货申请
        //public ActionResult EditFetchBill(int id) {

        //    ViewData["list"] = db.VwFetchBill.Where(v => v.bill_id == id).ToList();
        //    return View("CreateSendOutBill");
        //}

        ////换货提交之前先验证
        //public JsonResult ValidateBeforApplyCH(string sys_no)
        //{
        //    //1. 没有保存不能提交
        //    if (db.FetchBill.Where(r => r.sys_no == sys_no).Count() < 1)
        //    {
        //        return Json(new { suc = false, msg = "提交之前请先保存单据！" });
        //    }

        //    //2. 不能重复提交
        //    if (db.Apply.Where(a => a.sys_no == sys_no).Count() > 0)
        //    {
        //        return Json(new { suc = false, msg = "不能重复提交！" });
        //    }

        //    return Json(new { suc = true });
        //}

        ////提交申请
        //public ActionResult BeginApplyCH(string sys_no)
        //{
        //    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
                        

        //    Apply apply = new Apply();
        //    apply.user_id = userId;
        //    apply.sys_no = sys_no;
        //    apply.start_date = DateTime.Now;
        //    apply.ip = Request.UserHostAddress;
        //    apply.order_type = "CH";
        //    db.Apply.InsertOnSubmit(apply);

        //    bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
        //    List<ApplyDetails> ads = new List<ApplyDetails>();

        //    try
        //    {
        //        if (testFlag)
        //        {
        //            ads = utl.getTestApplySequence(apply, "CH", userId);
        //        }
        //        else
        //        {
        //            ads = utl.getApplySequence(apply, "CH", userId, db.User.Single(u => u.id == userId).department, null);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewBag.tip = ex.Message;
        //        return View("tip");
        //    }

        //    db.ApplyDetails.InsertAllOnSubmit(ads);

        //    try
        //    {
        //        db.SubmitChanges();
        //    }
        //    catch (Exception e)
        //    {
        //        utl.writeEventLog(MODELNAME, "提交出货申请失败，原因：" + e.Message, sys_no, Request);
        //        ViewBag.tip = "提交失败，原因：" + e.Message;
        //        return View("tip");
        //    }

        //    //发送邮件通知下一步的人员

        //    SomeUtils utis = new SomeUtils();
        //    if (utis.emailToNextAuditor(apply.id))
        //    {
        //        utl.writeEventLog("提交申请——发送邮件", "发送成功", sys_no, Request);
        //        return RedirectToAction("CheckAllFetchBill");
        //    }
        //    else
        //    {
        //        utl.writeEventLog("提交申请——发送邮件", "发送失败", sys_no, Request, -1);
        //        ViewBag.tip = "订单提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
        //        return View("tip");
        //    }
        //}

        
    }
}
