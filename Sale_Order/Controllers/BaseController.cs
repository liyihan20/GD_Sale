using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Models;

namespace Sale_Order.Controllers
{
    public class BaseController : Controller
    {
        public SaleDBDataContext db;
        private UserInfo _currentUser;        

        public BaseController()
        {
            db = new SaleDBDataContext();
        }

        public UserInfo currentUser
        {
            get
            {
                _currentUser = (UserInfo)Session["currentUser"];
                if (_currentUser == null) {
                    int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
                    var user = db.User.Where(u => u.id == userId).FirstOrDefault();
                    if (user != null) {
                        _currentUser = new UserInfo()
                        {
                            userId = userId,
                            userName = user.username,
                            realName = user.real_name,
                            departmentName = user.Department1.name,
                            email = user.email
                        };
                        Session["currentUser"] = _currentUser;
                    }
                }
                return _currentUser;
            }
        }
    }
}
