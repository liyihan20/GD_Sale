using Sale_Order.Models;
using Sale_Order.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sale_Order.Controllers
{
    public class NExtraController : BaseController
    {

        /// <summary>
        /// 合同编号是否已使用
        /// </summary>
        /// <param name="contractNo">合同号</param>
        /// <param name="sysNum">流水号</param>
        /// <param name="billType">单据类型</param>
        /// <returns></returns>
        public JsonResult IsContractNoExists(string contractNo, string sysNum, string billType)
        {
            try {
                db.isContractNoExists(billType, contractNo, sysNum);
            }
            catch (Exception ex) {
                return Json(new { suc = false, msg = ex.Message });
            }
            return Json(new { suc = true });
        }

        #region 出货申请

        public ActionResult ClerkAndCustomer()
        {
            return View();
        }

        public JsonResult GetCleckAndCustomer(string searchValue)
        {
            return Json(new CHSv().GetCleckAndCustomerList(searchValue));
        }

        public JsonResult SaveClerkAndCustomer(FormCollection fc)
        {
            try {
                new CHSv().SaveClerkAndCustomer(fc);
            }
            catch (Exception ex) {
                return Json(new SimpleResultModel(ex));
            }

            return Json(new SimpleResultModel(true, "保存成功"), "text/html");
        }

        public JsonResult RemoveClerkAndCustomer(int id)
        {
            try {
                new CHSv().RemoveClerkAndCustomer(id);
            }
            catch (Exception ex) {
                return Json(new SimpleResultModel(ex));
            }

            return Json(new SimpleResultModel(true, "删除成功"));
        }

        #endregion

    }
}
