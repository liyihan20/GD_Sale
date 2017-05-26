using System.Web;
using System.Web.Mvc;
using Sale_Order.Utils;
using System.Text.RegularExpressions;

namespace Sale_Order.Filter
{
    public class SessionTimeOutFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            SomeUtils utl = new SomeUtils();
            HttpContextBase ctx = filterContext.HttpContext;
            if (ctx.Session != null)
            {
                //sessionCookie="ASP.NET_SessionId=xmxajqztk0ggqf3anpzxoqmy; order_cookie=userid=1&code=9775f157819fc0abc2d227a19f181176"
                string sessionCookie = ctx.Request.Headers["Cookie"];
                if ((null != sessionCookie) && (sessionCookie.IndexOf("order_cookie") >= 0))
                {
                    var result = new Regex(@"(?<=order_cookie=(?:code=.{32}&)?userid=)\d+").Match(sessionCookie);
                    if (result.Success) {
                        string id = result.Value;
                        result = new Regex(@"(?<=order_cookie=(?:userid=\d+&)?code=).{32}").Match(sessionCookie);
                        if (result.Success) {
                            string code = result.Value;
                            if (code.Equals(utl.getMD5(id)))
                            {
                                base.OnActionExecuting(filterContext);
                                return;
                            }
                        }
                    }                    
                }
            }
            filterContext.Result = new RedirectResult("~/Account/Login");
            //ctx.Response.Redirect("~/Account/Login");--虽可正常运行，但在调试模式下回出错，因为还是会在Action里面继续执行。

        }
    }    
}