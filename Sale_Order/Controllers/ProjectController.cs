using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Models;
using Sale_Order.Utils;
using Sale_Order.Filter;
using System.Configuration;

namespace Sale_Order.Controllers
{
    [SessionTimeOutFilter()]
    public class ProjectController : Controller
    {
        SaleDBDataContext db = new SaleDBDataContext();
        SomeUtils utl = new SomeUtils();
        String MODEL = "客户立项";

        public ActionResult NewProject()
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);
            String sysNo=utl.getSystemNo("PJ");
            ViewData["sys_no"] = sysNo;
            ViewData["biller"] = user.real_name;

            utl.writeEventLog(MODEL, "新建一张申请", sysNo, Request);
            return View();
        }

        [HttpPost]
        public JsonResult SaveProjectBill(FormCollection col)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            string sysNum = col.Get("sys_no");

            //如已提交，则不能再保存
            var ap = db.Apply.Where(a => a.sys_no == sysNum);
            if (ap != null && ap.Count() > 0)
            {
                utl.writeEventLog("保存单据", "已提交不能再次保存", sysNum, Request, 10);
                return Json(new { suc = false, msg = "已提交的单据不能再次保存！" }, "text/html");
            }

            #region 获取表头各个字段
            string project_name = col.Get("project_name");
            string customer_no = col.Get("customer_no");
            string customer_name = col.Get("customer_name");
            string screen_size = col.Get("screen_size");            
            string product_serial = col.Get("product_serial");
            string project_status = col.Get("project_status");
            string dot_matrix = col.Get("dot_matrix");
            string screen_pixel = col.Get("screen_pixel");
            string classification = col.Get("classification");
            string operation_sys = col.Get("operation_sys");
            string camera_pixel = col.Get("camera_pixel");
            string TP_type = col.Get("TP_type");
            string platform_client = col.Get("platform_client");
            string platform_model = col.Get("platform_model");
            string start_sell_date = col.Get("start_sell_date");
            string amount = col.Get("amount");
            string price = col.Get("price");
            string end_sell_date = col.Get("end_sell_date");
            string project_group = col.Get("project_group");
            string competitor = col.Get("competitor");
            string truly_first_vendor = col.Get("truly_first_vendor");
            string truly_percentage = col.Get("truly_percentage");
            string comment = col.Get("comment");
            #endregion

            #region 设置表头各字段
            Project_bills bill = new Project_bills();
            bill.sys_no = sysNum;
            bill.project_name = project_name;
            bill.customer_name = customer_name;
            bill.customer_no = customer_no;
            bill.screen_size = screen_size;            
            bill.product_serial = product_serial;
            bill.project_status = project_status;
            bill.dot_matrix = dot_matrix;
            bill.screen_pixel = screen_pixel;
            bill.classification = classification;
            bill.operation_sys = operation_sys;
            bill.camera_pixel = camera_pixel;
            bill.TP_type = TP_type;
            bill.platform_client = platform_client;
            bill.platform_model = platform_model;   
            try
            {
                bill.start_sell_date = string.IsNullOrEmpty(start_sell_date) ? (DateTime?)null : DateTime.Parse(start_sell_date);
            }
            catch
            {
                return Json(new { suc = false, msg = "上市时间日期格式不合法，必须是年-月-日格式" }, "text/html");
            }
            bill.amount = string.IsNullOrEmpty(amount) ? (int?)null : int.Parse(amount);
            bill.price = string.IsNullOrEmpty(price) ? (decimal?)null : decimal.Parse(price);
            try
            {
                bill.end_sell_date = string.IsNullOrEmpty(end_sell_date) ? (DateTime?)null : DateTime.Parse(end_sell_date);
            }
            catch
            {
                return Json(new { suc = false, msg = "停产时间日期格式不合法，必须是年-月-日格式" }, "text/html");
            }
            bill.competitor = competitor;
            bill.project_group = project_group;
            bill.truly_first_vendor = bool.Parse(truly_first_vendor);
            bill.truly_percentage = string.IsNullOrEmpty(truly_percentage) ? (int?)null : int.Parse(truly_percentage);
            bill.comment = comment;

            bill.user_id = userId;
            bill.edit_time = DateTime.Now;
            #endregion

            try
            {
                var existBill = db.Project_bills.Where(p => p.sys_no == sysNum);
                if (existBill.Count() > 0)
                {
                    db.Project_bills.DeleteAllOnSubmit(existBill);
                }
                db.Project_bills.InsertOnSubmit(bill);
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                return Json(new { suc = false, msg = ex.Message }, "text/html");                
            }
            utl.writeEventLog(MODEL, "保存成功", sysNum, Request);
            return Json(new { suc = true }, "text/html");
        }
                
        public ActionResult CheckProjectBills() {

            var queryData = Request.Cookies["op_qd_pj"];
            if (queryData != null && queryData.Values.Get("sa_pj_cu") != null)
            {
                ViewData["cust_no"] = utl.DecodeToUTF8(queryData.Values.Get("sa_pj_cu"));
                ViewData["project_name"] = utl.DecodeToUTF8(queryData.Values.Get("sa_pj_pn"));
                ViewData["sys_no"] = utl.DecodeToUTF8(queryData.Values.Get("sa_pj_sn"));
                ViewData["from_date"] = queryData.Values.Get("sa_pj_fd");
                ViewData["to_date"] = queryData.Values.Get("sa_pj_td");
                ViewData["audit_result"] = queryData.Values.Get("sa_pj_ar");
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
        public JsonResult CheckProjectBills(FormCollection fcl) {

            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            string cust_no = fcl.Get("cust_no");
            string fromDateStr = fcl.Get("fromDate");
            string toDateStr = fcl.Get("toDate");
            string project_name = fcl.Get("project_name");
            string sys_no = fcl.Get("sys_no");
            string auditResultStr = fcl.Get("auditResult");

            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd_pj"];
            if (queryData == null) queryData = new HttpCookie("op_qd_pj");
            queryData.Values.Set("sa_pj_cu", utl.EncodeToUTF8(cust_no));
            queryData.Values.Set("sa_pj_pn", utl.EncodeToUTF8(project_name));
            queryData.Values.Set("sa_pj_sn", utl.EncodeToUTF8(sys_no));
            queryData.Values.Set("sa_pj_fd", fromDateStr);
            queryData.Values.Set("sa_pj_td", toDateStr);
            queryData.Values.Set("sa_pj_ar", auditResultStr);
            queryData.Expires = DateTime.Now.AddDays(30);
            Response.AppendCookie(queryData);

            //处理一下参数
            DateTime fromDate, toDate;
            if (!DateTime.TryParse(fromDateStr, out fromDate)) fromDate = DateTime.Parse("1980-1-1");
            if (!DateTime.TryParse(toDateStr, out toDate)) toDate = DateTime.Parse("2099-9-9");

            toDate = toDate.AddDays(1);

            //可以查看所有申请的权限
            bool CanCheckAll = utl.hasGotPower(userId, Powers.Chk_all_PJ.ToString());

            var result = (from p in db.Project_bills
                          join a in db.Apply on p.sys_no equals a.sys_no into x
                          from y in x.DefaultIfEmpty()
                          where (CanCheckAll || p.user_id == userId)
                          && p.edit_time >= fromDate
                          && p.edit_time <= toDate
                          && (p.customer_name.Contains(cust_no) || p.customer_no.Contains(cust_no))
                          && p.project_name.Contains(project_name)
                          && p.sys_no.Contains(sys_no)
                          && (auditResultStr == "10"
                          || (auditResultStr == "0" && y.success == null)
                          || (auditResultStr == "1" && y.success == true)
                          || (auditResultStr == "-1" && y.success == false))
                          orderby p.edit_time descending
                          select new
                          {
                              FBillID = p.id,
                              FUserNumber = p.User.job,
                              FDate = p.edit_time,
                              FSysNo = p.sys_no,
                              FCustomerName = p.customer_name,
                              FProjectName = p.project_name,
                              FProjectStatus = p.project_status,
                              FUserName = p.User.real_name,
                              FScreenSize = p.screen_size,
                              FAmount = p.amount,
                              FProductSerial = p.product_serial,
                              FApplyStatus = y.success == true ? "申请成功" : (y.success == false ? "申请失败" : (y.success == null && y.id == null ? "未开始申请" : "申请中"))
                          }).Take(300).ToList();

            utl.writeEventLog(MODEL, string.Format("搜索申请列表:{0}~{1},Cus:{2},PJ{3},SysNo:{4},Audit:{5}",fromDateStr,toDateStr,cust_no,project_name,sys_no,auditResultStr), "", Request);
            return Json(new { suc = true, list = result }, "text/html");

        }

        public ActionResult CheckSingleProjectBill(int id) {

            var bill = db.Project_bills.Single(p => p.id == id);
            ViewData["bill"] = bill;

            var blockInfo = db.BlockOrder.Where(b => b.sys_no == bill.sys_no).OrderBy(b => b.step).ToList();
            ViewData["blockInfo"] = blockInfo;
            
            return View();
        }

        public ActionResult CheckAProjectBill(int id) {

            var ap = from a in db.Apply
                     join b in db.Project_bills on a.sys_no equals b.sys_no
                     where b.id == id
                     select a;
            if (ap.Count() > 0 && ap.First().success == null)
            {
                ViewData["emergency_quot"] = "yes";
            }

            var bill = db.Project_bills.Single(p => p.id == id);
            ViewData["bill"] = bill;

            var blockInfo = db.BlockOrder.Where(b => b.sys_no == bill.sys_no).OrderBy(b => b.step).ToList();
            ViewData["blockInfo"] = blockInfo;

            utl.writeEventLog(MODEL, "查看单张申请", bill.sys_no, Request);
            return View("CheckSingleProjectBill");
        }

        public ActionResult EditProjectBill(int id,string sys_no,int is_new) {
            ViewData["bill_id"] = id;
            if (is_new == 0)
            {                
                ViewData["sys_no"] = sys_no;
            }
            else { 
                //新增的
                ViewData["sys_no"] = utl.getSystemNo("PJ");
            }
            return View("NewProject");
        }

        public JsonResult GetSingleProjectBill(int id) {
            var bill = db.Project_bills.Single(p => p.id == id);
            var billerName = bill.User.real_name;
            bill.User = null;

            utl.writeEventLog(MODEL, "编辑单张申请", bill.sys_no, Request);
            return Json(new { bill = bill, billerName = billerName });
        }

        //提交之前验证
        public JsonResult ValidateBeforApply(string sys_no)
        {
            //1. 没有保存不能提交
            if (db.Project_bills.Where(r => r.sys_no == sys_no).Count() < 1)
            {
                utl.writeEventLog(MODEL, "提交之前请先保存单据！", sys_no, Request,10);
                return Json(new { suc = false, msg = "提交之前请先保存单据！" });
            }

            //2. 不能重复提交
            if (db.Apply.Where(a => a.sys_no == sys_no).Count() > 0)
            {
                utl.writeEventLog(MODEL, "不能重复提交！", sys_no, Request, 100);
                return Json(new { suc = false, msg = "不能重复提交！" });
            }

            return Json(new { suc = true });
        }

        //提交申请
        public ActionResult BeginApply(string sys_no)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);            
            string processType = "PJ";

            Apply apply = new Apply();
            apply.user_id = userId;
            apply.sys_no = sys_no;
            apply.start_date = DateTime.Now;
            apply.ip = Request.UserHostAddress;
            apply.order_type = "PJ";
            db.Apply.InsertOnSubmit(apply);

            bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
            List<ApplyDetails> ads = new List<ApplyDetails>();

            try
            {
                if (testFlag)
                {
                    ads = utl.getTestApplySequence(apply, processType, userId);
                }
                else
                {
                    ads = utl.getApplySequence(apply, processType, userId, db.User.Single(u => u.id == userId).department);
                }
            }
            catch (Exception ex)
            {
                ViewBag.tip = ex.Message;
                return View("tip");
            }

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
                utl.writeEventLog(MODEL, "提交申请——发送邮件,发送成功", sys_no, Request);
                ViewBag.tip = "提交成功，在15分钟后有相关的人员进行处理，请耐心等候。";
                return View("tip");
            }
            else
            {
                utl.writeEventLog(MODEL, "提交申请——发送邮件,发送失败", sys_no, Request, -1);
                ViewBag.tip = "提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
                return View("tip");
            }
        }
        
        //搜索历史数据
        public ActionResult SearchOldProjectBills() {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            ViewData["userNumber"] = db.User.Single(u => u.id == userId).job;
            return View();
        }

        public JsonResult GetOldProjectBills(FormCollection fc) {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            string projectName = fc.Get("project_name");
            string client = fc.Get("client");
            string otherKey = fc.Get("other_key");

            //可以查看所有申请的权限
            bool CanCheckAll = utl.hasGotPower(userId, Powers.Chk_all_PJ.ToString());

            var user=db.User.Single(u=>u.id==userId);
            var res = (from v in db.VwoldProjectBill
                       where v.project_name.Contains(projectName)
                       && (v.customer_name.Contains(client) || v.customer_no.Contains(client))
                       && (v.classification.Contains(otherKey) || v.operation_sys.Contains(otherKey)
                       || v.platform_client.Contains(otherKey) || v.platform_model.Contains(otherKey)
                       || v.product_serial.Contains(otherKey) || v.sys_no.Contains(otherKey)
                       || v.TP_type.Contains(otherKey) || v.user_name.Contains(otherKey)
                       || v.user_number.Contains(otherKey))
                       && (user.job.Equals(v.user_number) || user.real_name.Equals(v.user_name) || CanCheckAll)
                       orderby v.id descending 
                       select new
                       {
                           id = v.id,
                           project_name = v.project_name,
                           project_status = v.project_status,
                           product_serial = v.product_serial,
                           screen_size = v.screen_size,
                           screen_pixel = v.screen_pixel,
                           customer_name=v.customer_name,
                           //platform_client = v.platform_client,
                           //platform_model = v.platform_model,
                           amount = v.amount,
                           operation_sys = v.operation_sys,
                           user_name = v.user_name,
                           user_number=v.user_number
                       }).Take(100).ToList();
            utl.writeEventLog("搜索历史立项单据",string.Format("projectName:{0};client:{1};otherKey:{2}",projectName,client,otherKey),"",Request);
            return Json(res, "text/html");
        }

        public ActionResult CheckSingleOldProjectBill(int id) {
            var bill = db.VwoldProjectBill.Where(v => v.id == id).First();
            Project_bills pb = new Project_bills()
            {
                id = bill.id,
                project_name = bill.project_name,
                product_serial = bill.product_serial,
                operation_sys = bill.operation_sys,
                camera_pixel = bill.camera_pixel,
                dot_matrix = bill.dot_matrix,
                TP_type = bill.TP_type,
                customer_name = bill.customer_name,
                start_sell_date = bill.start_sell_date,
                price = 0,
                platform_client = bill.platform_client,
                platform_model = bill.platform_model,
                comment = bill.comment,
                project_status = bill.project_status,
                customer_no = bill.customer_no,
                classification = bill.classification,
                User = new Models.User() { real_name = bill.user_name, job = bill.user_number },
                screen_pixel = bill.screen_pixel,
                edit_time = bill.edit_time,
                screen_size = bill.screen_size,
                end_sell_date = bill.end_sell_date,
                sys_no = bill.sys_no,
                competitor = bill.competitor,
                amount = 0,
                project_group = bill.project_group,
                truly_percentage = bill.truly_percentage
            };
            decimal price = 0;
            int amount = 0;
            if (decimal.TryParse(bill.price, out price)) {
                pb.price = price;
            }
            if (int.TryParse(bill.amount, out amount)) {
                pb.amount = amount;
            }

            ViewData["bill"] = pb;
            ViewData["canQuot"] = "yes";
            utl.writeEventLog(MODEL, "查看单张历史客户立项数据：" + bill.project_name, bill.sys_no, Request);
            return View("CheckSingleProjectBill");
        }

        public JsonResult SaveTempCustomer(FormCollection fc) {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            var user=db.User.Single(u=>u.id==userId);
            string tp_customer_name = fc.Get("tp_customer_name");
            string tp_contacts = fc.Get("tp_contacts");
            string tp_customer_short = fc.Get("tp_customer_short");
            string tp_mobile_phone = fc.Get("tp_mobile_phone");
            string tp_en_name = fc.Get("tp_en_name");
            string tp_phone = fc.Get("tp_phone");
            string tp_email = fc.Get("tp_email");
            string tp_tax = fc.Get("tp_tax");
            string tp_project_group = fc.Get("tp_project_group");
            string tp_customer_addr = fc.Get("tp_customer_addr");
            string tp_en_addr = fc.Get("tp_en_addr");

            string number = "";
            try
            {
                var res = db.InsertIntoTempCustomer(tp_customer_name, tp_en_name, tp_customer_short, tp_customer_addr, tp_en_addr,
                        tp_contacts, tp_email, tp_mobile_phone, tp_phone, tp_tax, tp_project_group, user.Department1.name, user.real_name,
                        DateTime.Now.ToShortDateString());
                number = res.First().number;
            }
            catch (Exception ex)
            {
                utl.writeEventLog("新增临时客户", tp_customer_name + ",失败：" + ex.Message, "", Request, 100);
                return Json(new { suc = false, msg = ex.Message }, "text/html");
            }

            utl.writeEventLog("新增临时客户", String.Format("新增成功，名称：{0};编码：{1}",tp_customer_name,number),"", Request);
            return Json(new { suc = true, number = number }, "text/html");
        }
    }
}
