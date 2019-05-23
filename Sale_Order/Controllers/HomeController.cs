using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Models;
using Sale_Order.Filter;
using System.Configuration;
using System.IO;
using Sale_Order.Utils;
namespace Sale_Order.Controllers
{    
    public class HomeController : Controller
    {
        SaleDBDataContext db = new SaleDBDataContext();
        SomeUtils utl = new SomeUtils();

        [SessionTimeOutFilter()]
        public ActionResult Main(string url) {

            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);
            var powers = (from a in db.Authority
                      from u in db.Group
                      from ga in a.GroupAndAuth
                      from gu in u.GroupAndUser
                      where ga.group_id == u.id && gu.user_id == userId
                      select a.sname).ToArray();
            ViewData["url"] = string.IsNullOrEmpty(url)?"": utl.MyUrlDecoder(url);
            ViewData["powers"] = powers;
            ViewData["username"] = user.real_name;
            ViewData["depName"] = user.Department1.name;
            return View();
        }

        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to kick-start your ASP.NET MVC application." ;
            
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        
        public ActionResult ChangeLang(string lang)
        {
            /*记录语言设置到cookies*/
            SomeUtils utl = new SomeUtils();
            utl.writeEventLog("切换语言", lang, "", Request);
            HttpCookie cookie = new HttpCookie("CoolCode_Lang", lang);
            cookie.Expires = DateTime.Now.AddMonths(1);
            Response.AppendCookie(cookie);
            /*重定向到上一个Action*/
            return new RedirectResult(this.Request.ServerVariables["HTTP_REFERER"]);
            // return RedirectToAction("Index");
        }

        //下载Google浏览器
        public FileStreamResult DownloadChrome()
        {
            string fileName = "Chrome.rar";
            string absoluFilePath = ConfigurationManager.AppSettings["AttachmentPath1"] + fileName;
            FileInfo info = new FileInfo(absoluFilePath);
            if (!info.Exists)
            {
                return null;
            }
            return File(new FileStream(absoluFilePath, FileMode.Open), "application/octet-stream", Server.UrlEncode(fileName));
        }

        //public ActionResult moveFile() {
        //    foreach (var no in (from ap in db.Apply.Where(a => a.success == true) select ap.sys_no)) {
        //        SomeUtils.moveToFormalDir(no);
        //    }            
        //    return View("tip");
        //}

        public bool testEmail(int applyId) {
            return utl.emailToNextAuditor(applyId);
        }

        //删除测试数据
        [SessionTimeOutFilter()]
        public ActionResult deleteMyTest() {
            var me = db.User.Single(u => u.id == 1);
            int i = 0;
            foreach (var order in me.Order) {
                i++;
                if (db.Apply.Where(a => a.sys_no == order.sys_no).Count() > 0)
                {
                    var ap = db.Apply.Where(a => a.sys_no == order.sys_no).First();
                    db.ApplyDetails.DeleteAllOnSubmit(ap.ApplyDetails);
                    db.Apply.DeleteOnSubmit(ap);
                }
                db.SalerPercentage.DeleteAllOnSubmit(order.SalerPercentage);
                db.OrderDetail.DeleteAllOnSubmit(order.OrderDetail);
                db.Order.DeleteOnSubmit(order);
            }
            db.SubmitChanges();

            ViewBag.tip = "成功删除的记录数为："+i.ToString();
            return View("tip");
        }

        public ActionResult ps() {
            foreach (var or in db.Order.Where(o => o.salePs == "")) {                
                or.salePs = db.Order.Where(o=>o.sys_no==or.sys_no && o.step_version==0).First().salePs;
            }
            db.SubmitChanges();
            ViewBag.tip = "ok";
            return View("tip");
        }

        //保存错误信息
        [SessionTimeOutFilter()]
        public JsonResult WriteDownErrors(string message)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var err=new SystemErrors();
            err.user_name=db.User.Single(u=>u.id==userId).username;
            err.exception=message;
            err.op_time = DateTime.Now;
            db.SystemErrors.InsertOnSubmit(err);
            db.SubmitChanges();
            return Json("");
        }

        //测试权限
        [SessionTimeOutFilter()]
        public ActionResult TestPower(int userId, string powerName) {
            SomeUtils utl = new SomeUtils();
            if (utl.hasGotPower(userId, powerName))
                ViewBag.tip = "YES";
            else
                ViewBag.tip = "NO";
            return View("tip");
        }

        public string getStr() {
            string result = @"";
            return result;
        }

        public string SetFileFlag(string fr,string to) {
            DateTime fromDate = DateTime.Parse(fr);
            DateTime toDate = DateTime.Parse(to);
            var sysNos = (from a in db.Apply
                         where a.start_date >= fromDate
                         && a.finish_date < toDate
                         && a.success == true
                         && (a.order_type == "SO"
                         || a.order_type == "TH")
                         select a.sys_no).ToArray();
            foreach (string no in sysNos)
            {
                var absoluFilePath = Path.Combine(SomeUtils.getOrderPath(no), no + ".rar");
                var info = new FileInfo(absoluFilePath);
                if (info.Exists)
                {
                    if (db.HasAttachment.Where(h => h.sys_no == no).Count() < 1)
                    {
                        var att = new HasAttachment();
                        att.sys_no = no;
                        db.HasAttachment.InsertOnSubmit(att);
                    }
                }
            }
            try
            {
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "ok";
        }

    }
}
