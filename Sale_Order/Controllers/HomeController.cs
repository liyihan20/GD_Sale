using Sale_Order.Filter;
using Sale_Order.Models;
using Sale_Order.Utils;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sale_Order.Controllers
{    
    public class HomeController : BaseController
    {
        SomeUtils utl = new SomeUtils();

        [SessionTimeOutFilter]
        public ActionResult Index()
        {
            return View();
        }

        [SessionTimeOutFilter()]
        public ActionResult Main(string url) {            
            var powers = (from a in db.Authority
                      from u in db.Group
                      from ga in a.GroupAndAuth
                      from gu in u.GroupAndUser
                      where ga.group_id == u.id && gu.user_id == currentUser.userId
                      select a.sname).ToArray();
            ViewData["url"] = string.IsNullOrEmpty(url)?"": utl.MyUrlDecoder(url);
            ViewData["powers"] = powers;
            ViewData["username"] = currentUser.realName;
            ViewData["depName"] = currentUser.departmentName;
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

        //保存错误信息
        [SessionTimeOutFilter()]
        public JsonResult WriteDownErrors(string message)
        {
            var err=new SystemErrors();
            err.user_name = currentUser.userName;
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
            return utl.getMD5("vvv855##");
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
