using Newtonsoft.Json;
using Sale_Order.Filter;
using Sale_Order.Models;
using Sale_Order.Services;
using Sale_Order.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sale_Order.Controllers
{
    public class NSalerController : BaseController
    {
        BillSv bill;
        private SomeUtils utils = new SomeUtils();
        private const string TAG = "申请者模块";

        private void Wlog(string log, string sysNo = "", int unusual = 0)
        {
            base.Wlog(TAG, log, sysNo, unusual);
        }

        /// <summary>
        /// 根据单据类型设置单据空的对象实例
        /// </summary>
        /// <param name="billType">单据类型</param>
        private void SetBillByType(string billType)
        {
            bill = (BillSv)new BillUtils().GetBillSvInstance(billType);
        }

        /// <summary>
        /// 根据流水号设置单据对象实例
        /// </summary>
        /// <param name="sysNo">流水号</param>
        private void SetBillBySysNo(string sysNo)
        {
            bill = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(sysNo);
        }

        [SessionTimeOutFilter]
        public ActionResult CreateBill(string billType,string account="光电总部")
        {
            Wlog("新建单据，billType:" + billType);

            SetBillByType(billType);
            bill.account = account;

            ViewData["bill"] = bill.GetNewBill(currentUser);

            return View(bill.CreateViewName);
        }

        public JsonResult SaveBill(FormCollection fc)
        {
            string sysNo = fc.Get("sys_no");
            SetBillByType(new BillUtils().GetBillEnType(sysNo));
            try {
                string result = bill.SaveBill(fc, currentUser);
            }
            catch (Exception ex) {
                return Json(new SResultModel() { suc = false, msg = ex.Message });
            }
            Wlog("保存单据", sysNo);

            return Json(new SResultModel() { suc = true, msg = "保存成功" });

        }

        [SessionTimeOutFilter]
        public ActionResult CheckBillList(string billType)
        {
            Wlog("打开单据列表视图,billType:" + billType);

            SalerSearchParamModel pm;
            var queryData = Request.Cookies["crm_sa_" + billType + "_qd"];
            if (queryData != null) {
                pm = JsonConvert.DeserializeObject<SalerSearchParamModel>(utils.DecodeToUTF8(queryData.Value));
            }
            else {
                pm = new SalerSearchParamModel();
                pm.auditResult = 0;
                pm.fromDate = DateTime.Now.AddDays(-7);
                pm.toDate = DateTime.Now;
                pm.billType = billType;                
            }
            ViewData["queryParams"] = pm;

            SetBillByType(billType);
            return View(bill.CheckListViewName);
        }

        public JsonResult GetBillList(FormCollection fc)
        {
            SalerSearchParamModel pm = new SalerSearchParamModel();
            utils.SetFieldValueToModel(fc, pm);

            var queryData = Request.Cookies["crm_sa_" + pm.billType + "_qd"];
            if (queryData == null) {
                queryData = new HttpCookie("crm_sa_" + pm.billType + "_qd");
            }
            queryData.Expires = DateTime.Now.AddDays(20);
            queryData.Value = utils.EncodeToUTF8(JsonConvert.SerializeObject(pm));
            Response.AppendCookie(queryData);

            Wlog("获取列表数据:" + JsonConvert.SerializeObject(pm));

            SetBillByType(pm.billType);
            return Json(bill.GetBillList(pm, currentUser.userId), "text/html");
        }

        public JsonResult ApplyHasBegan(string sysNo)
        {
            return Json(new SResultModel() { suc = new ApplySv().ApplyHasBegan(sysNo) });
        }

        [SessionTimeOutFilter]
        public ActionResult ModifyBill(string sysNo, int stepVersion)
        {
            Wlog("进入单据修改视图", sysNo + ":" + stepVersion);

            SetBillBySysNo(sysNo);
            ViewData["bill"] = bill.GetBill(stepVersion, currentUser.userId);
            ViewData["step"] = stepVersion;
            if (new ApplySv().ApplyHasBegan(sysNo)) {
                ViewData["blockInfo"] = new ApplySv(sysNo).GetBlockInfo();
            }
            return View(bill.CreateViewName);
        }

        [SessionTimeOutFilter]
        public ActionResult CheckBill(string sysNo)
        {
            Wlog("进入单据查看视图", sysNo);

            SetBillBySysNo(sysNo);
            ViewData["bill"] = bill.GetBill(0,currentUser.userId);
            var hasSubmited = new ApplySv().ApplyHasBegan(sysNo);
            if (hasSubmited) {
                ViewData["blockInfo"] = new ApplySv(sysNo).GetBlockInfo();
            }
            return View(bill.CheckViewName);
        }

        [SessionTimeOutFilter]
        public ActionResult CreateNewBillFromOld(string sysNo)
        {
            Wlog("从旧模板新增", sysNo);

            SetBillBySysNo(sysNo);
            ViewData["bill"] = bill.GetNewBillFromOld();
            return View(bill.CreateViewName);
        }

        [SessionTimeOutFilter]
        public JsonResult BeginApply(string sysNo)
        {
            SetBillByType(new BillUtils().GetBillEnType(sysNo));
            if (!bill.HasOrderSaved(sysNo)) {
                Wlog("没有保存单据就提交", sysNo, -10);
                return Json(new SResultModel() { suc = false, msg = "请先保存单据再提交" });
            }

            SetBillBySysNo(sysNo);
            try {
                new ApplySv().BeginApply(
                        bill.BillType,
                        currentUser.userId,
                        GetIPAddr(),
                        sysNo,
                        bill.GetProductModel(),
                        bill.GetProcessNo(),
                        bill.GetProcessDic()
                        );
            }
            catch (Exception ex) {
                Wlog("提交失败：" + ex.Message, sysNo, -10);
                return Json(new SResultModel() { suc = false, msg = "提交失败：" + ex.Message });
            }

            Wlog("提交成功", sysNo);
            return Json(new SResultModel() { suc = true, msg = "提交成功！等待跳转..." });
        }

        /// <summary>
        /// 选择公司模块
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="controllerName"></param>
        /// <returns></returns>
        [SessionTimeOutFilter]
        public ActionResult ChooseCompany(string billType, string actionName = "CreateBill", string controllerName = "NSaler", string companys = "光电总部,光电仁寿")
        {
            ViewData["actionName"] = actionName;
            ViewData["controllerName"] = controllerName;
            ViewData["companys"] = companys.Split(new char[] { ',' });
            ViewData["billType"] = billType;
            return View();
        }

    }
}
