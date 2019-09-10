using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Sale_Order.Models;
using System.IO;
using Sale_Order.Utils;
using System.Configuration;

namespace Sale_Order.Controllers
{

    public class AccountController : Controller
    {
        SaleDBDataContext db = new SaleDBDataContext();
        SomeUtils utl = new SomeUtils();
        [AllowAnonymous]
        public ActionResult Login(string url, string accountset, bool? isInnerFrame)
        {
            //return View("Redirect");

            bool maintainFlag = bool.Parse(ConfigurationManager.AppSettings["MaintainFlag"]);
            if (maintainFlag) {
                utl.writeEventLog("登录系统", "测试维护中。。。", "", Request);
                ViewBag.finish_time = ConfigurationManager.AppSettings["MaintainFinishTime"];
                return View("Maintain");
            }

            //如果从邮件直接进入系统，且登录状态的cookie有效，则直接跳转
            if (("semi").Equals(accountset) && Request.Cookies["order_semi_cookie"] != null) {
                utl.writeEventLog("登录系统", "从邮件直接跳转到：半导体:" + url, "", Request);
                if ((bool)isInnerFrame) {
                    return Redirect("../../SaleOrder_semi/Home/Main?url=" + url);
                }
                else {
                    return Redirect("../../SaleOrder_semi/" + utl.MyUrlDecoder(url));
                }
            }
            else if (("op").Equals(accountset) && Request.Cookies["order_cookie"] != null) {
                utl.writeEventLog("登录系统", "从邮件直接跳转到：光电:" + url, "", Request);
                if ((bool)isInnerFrame) {
                    return Redirect("../Home/Main?url=" + url);
                }
                else {
                    return Redirect("../" + utl.MyUrlDecoder(url));
                }
            }
            else if ((new string[] { "ele", "eqm" }).Contains(accountset) && Request.Cookies["order_ele_cookie"] != null) {
                utl.writeEventLog("登录系统", "从邮件直接跳转到：电子:" + url, "", Request);
                if ((bool)isInnerFrame) {
                    return Redirect("../../SaleOrder_ele/Home/Main?url=" + url);
                }
                else {
                    return Redirect("../../SaleOrder_ele/" + utl.MyUrlDecoder(url));
                }
            }

            ViewData["myName"] = utl.EncodeToGBK("liyihan.ic@truly.com.cn");
            ViewData["mySub"] = utl.EncodeToGBK("申请重置密码");
            ViewData["url"] = url;
            ViewData["isInnerFrame"] = isInnerFrame;
            ViewData["accountset"] = accountset;
            var cookie = Request.Cookies["userinfo"];
            if (cookie != null && string.IsNullOrEmpty(accountset)) {
                ViewData["accountset"] = cookie["cop_name"];
                ViewData["username"] = utl.DecodeToUTF8(cookie["user_name"]);
            }
            return View();

        }

        [AllowAnonymous]
        public ActionResult getImage()
        {
            string code = utl.CreateValidateNumber(4);
            Session["code"] = code.ToLower();
            byte[] bytes = utl.CreateValidateGraphic(code);
            return File(bytes, @"image/jpeg");
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult Login(FormCollection col)
        {
            string cop = col.Get("cop");
            string username = col.Get("username");
            string password = col.Get("password");
            string code = col.Get("validateText").ToLower();
            if (string.IsNullOrEmpty(cop)) {
                return Json(new { success = false, msg = "请选择公司名" }, "text/html");
            }
            if (Session["code"] == null || !code.Equals((string)Session["code"])) {
                return Json(new { success = false, msg = "验证码不正确,请重新输入" }, "text/html");
            }
            Session.Remove("code");
            var md5Pass = utl.getMD5(password);
            int? id = 0;
            try {
                db.SaleLogin(cop, username, password, md5Pass, GetUserIP(), ref id);
            }
            catch (Exception ex) {
                //返回从存储过程抛出的自定义异常
                return Json(new { success = false, msg = ex.Message }, "text/html");
            }

            HttpCookie cookie=new HttpCookie("order");
            if ("op".Equals(cop)) {
                cookie = new HttpCookie("order_cookie");
            }
            else if ("semi".Equals(cop)) {
                cookie = new HttpCookie("order_semi_cookie");
            }
            else if((new string[]{"ele","eqm"}).Contains(cop)){
                cookie = new HttpCookie("order_ele_cookie");
            }
            cookie.Expires = DateTime.Now.AddHours(12);
            cookie.Values.Add("userid", id.ToString());
            cookie.Values.Add("code", utl.getMD5(id.ToString()));
            cookie.Values.Add("username", utl.EncodeToUTF8(username));//用于记录日志
            Response.AppendCookie(cookie);

            //保存公司名和用户名到cookie，用于再次登录自动填写公司和用户名
            cookie = new HttpCookie("userinfo");
            cookie.Values.Add("cop_name", cop);
            cookie.Values.Add("user_name", utl.EncodeToUTF8(username));
            cookie.Expires = DateTime.Now.AddYears(1);
            Response.AppendCookie(cookie);

            //强制修改成复杂密码
            if (!string.IsNullOrEmpty(utl.validatePassword(password))) {
                return Json(new { success = true, needChange = true, cop = cop }, "text/html");
            }

            return Json(new { success = true, cop = cop }, "text/html");

        }
        //
        // GET: /Account/LogOff

        public ActionResult LogOut()
        {
            var cookie = Request.Cookies["order_cookie"];
            if (cookie != null) {
                utl.writeEventLog("登录模块", "登出系统", "", Request);
                cookie.Expires = DateTime.Now.AddSeconds(-1);
                Response.AppendCookie(cookie);
            }
            return RedirectToAction("Login");
        }

        public JsonResult ChangePassword(FormCollection fcl)
        {
            string oldPassword = fcl.Get("oldPass");
            string newPassword = fcl.Get("newPass");
            string cop = fcl.Get("cop_password");
            int userId = 0;
            if ("op".Equals(cop)) {
                userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            }
            else if ("semi".Equals(cop)) {
                userId = Int32.Parse(Request.Cookies["order_semi_cookie"]["userid"]);
            }
            else if ("ele".Equals(cop)) {
                userId = Int32.Parse(Request.Cookies["order_ele_cookie"]["userid"]);
            }
            SomeUtils utl = new SomeUtils();
            //验证是否复杂密码
            string validInfo = utl.validatePassword(newPassword);
            if (!string.IsNullOrEmpty(validInfo)) {
                return Json(new { success = false, msg = validInfo }, "text/html");
            }
            //验证旧密码是否正确，正确的话则更新密码
            try {
                db.ChangePassWord(cop, userId, utl.getMD5(oldPassword), utl.getMD5(newPassword), GetUserIP());
            }
            catch (Exception ex) {
                return Json(new { success = false, msg = ex.Message }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }


        public string GetUserIP()
        {
            string userIP;
            if (Request.ServerVariables["HTTP_VIA"] == null) {
                userIP = Request.UserHostAddress;
            }
            else {
                userIP = Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(',')[0].Trim();
            }
            return userIP;
        }

        public string getRand()
        {

            SomeUtils utl = new SomeUtils();
            return utl.getRandString(8);
        }

        public string getBall()
        {
            SomeUtils utl = new SomeUtils();
            return utl.getColorBalls(3);
        }

        //
        // GET: /Account/ChangePassword

        //public ActionResult ChangePassword()
        //{
        //    return View();
        //}

        ////
        //// POST: /Account/ChangePassword

        //[HttpPost]
        //public ActionResult ChangePassword(ChangePasswordModel model)
        //{
        //    if (ModelState.IsValid)
        //    {

        //        // ChangePassword will throw an exception rather
        //        // than return false in certain failure scenarios.
        //        bool changePasswordSucceeded;
        //        try
        //        {
        //            MembershipUser currentUser = Membership.GetUser(User.Identity.Name, userIsOnline: true);
        //            changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
        //        }
        //        catch (Exception)
        //        {
        //            changePasswordSucceeded = false;
        //        }

        //        if (changePasswordSucceeded)
        //        {
        //            return RedirectToAction("ChangePasswordSuccess");
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
        //        }
        //    }

        //    // If we got this far, something failed, redisplay form
        //    return View(model);
        //}

        ////
        //// GET: /Account/ChangePasswordSuccess

        //public ActionResult ChangePasswordSuccess()
        //{
        //    return View();
        //}

        private IEnumerable<string> GetErrorsFromModelState()
        {
            return ModelState.SelectMany(x => x.Value.Errors.Select(error => error.ErrorMessage));
        }

        #region Status Codes
        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus) {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion
    }
}
