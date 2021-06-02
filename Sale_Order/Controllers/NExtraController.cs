using Newtonsoft.Json;
using Sale_Order.Filter;
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

        [SessionTimeOutFilter]
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

        //获取用户可以出货的客户列表
        public JsonResult GetMyCHCustomers()
        {
            return Json(new CHSv().GetMyCustomers(currentUser.userId));
        }

        public JsonResult GetCHDeliveryInfos(string account, string customerNo)
        {
            //优先获取已保存的数据
            var infos = new CHSv().GetSavedDeliveryInfo(customerNo);
            if (infos.Count() == 0) {
                //没有再到k3基础资料获取
                infos.Add(new K3ItemSv(account).GetK3CustomerInfo(customerNo));
            }

            return Json(infos);
        }

        public JsonResult GetOrderInfo4CH(string account, string billType, string customerNo, DateTime fromDate, DateTime toDate, string orderNo, string itemModel)
        {
            return Json(new K3ItemSv(account).GetK3Order4CH(billType, customerNo, fromDate, toDate, orderNo, itemModel));
        }

        [SessionTimeOutFilter]
        //打印包装标签
        public ActionResult PrintCHPackageLabel(string sysNo)
        {
            ViewData["list"] = new CHSv(sysNo).GetPackageForLabel();
            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult CheckCHList4Log()
        {
            return View();
        }

        public JsonResult GetCHList4Log(string param)
        {
            var result = new CHSv().GetCHList4Log(JsonConvert.DeserializeObject<CHList4LogParam>(param));
            if (result.Count() > 0) {
                return Json(new { suc = true, data = result });
            }
            else {
                return Json(new SimpleResultModel() { suc = false, msg = "查询不到符合条件的单据" });
            }
        }

        public JsonResult GetShortAddr(string ids)
        {
            string addr = "";
            try {
                addr = new CHSv().GetShortAddr(ids);
            }
            catch (Exception ex) {
                return Json(new SimpleResultModel(ex));
            }
            return Json(new SimpleResultModel(true, "", addr));
        }

        public JsonResult GetCHExInfo(string ids,string stock_addr, string addr, int size_l, int size_w, int size_h, int cards_num)
        {
            return Json(new CHSv().GetChExInfo(ids, stock_addr,addr, size_l, size_w, size_h, cards_num));
        }

        public JsonResult UpdateCHEx(string stockNos, string exName, string exNo, string exType, string exComment, decimal exFee)
        {
            try {
                new CHSv().UpdateCHEx(stockNos, exName, exNo, exType, exComment, exFee);
            }
            catch (Exception ex) {
                return Json(new SimpleResultModel(ex));
            }
            return Json(new SimpleResultModel(true, "操作成功"));
        }

        [SessionTimeOutFilter]
        public ActionResult CheckCHSignList()
        {
            return View();
        }

        public JsonResult UpdateCHSignDate(string stockNo, DateTime day)
        {
            try {
                var bill = new CHSv().ValidateStockNo(stockNo);
                new K3ItemSv(bill.account).UpdateStockbillSignDate(stockNo, day.ToString("yyyy-MM-dd"));
                new CHSv(bill.sys_no).UpdateSignBack(day);
            }
            catch (Exception ex) {
                return Json(new SimpleResultModel(ex));
            }

            return Json(new SimpleResultModel(true, "操作成功"));
        }

        public JsonResult GetCHSignInfoList(DateTime fromDate, DateTime toDate, string stockNo, string customerName, string signStatus)
        {
            return Json(new CHSv().GetSignInfoList(fromDate, toDate, stockNo, customerName, signStatus));
        }

        public void ExportCHSignInfo(DateTime fromDate, DateTime toDate, string stockNo, string customerName, string signStatus)
        {
            var list = new CHSv().GetSignInfoList(fromDate, toDate, stockNo, customerName, signStatus);
            new CHSv().ExportSignInfoExcel(list);
        }

        public ActionResult CheckCHSOList()
        {
            return View();
        }

        public JsonResult GetCHSOList(string account, string billType, string customerNo, DateTime fromDate, DateTime toDate, string orderNo, string itemModel)
        {
            return Json(new K3ItemSv(account).GetK3SOList(billType, customerNo, fromDate, toDate, orderNo, itemModel));
        }

        public JsonResult GetCHStockDetail(string account, string billType, int orderId, int entryId)
        {
            return Json(new K3ItemSv(account).GetK3SOStockDetail(billType, orderId, entryId));
        }

        #endregion

        #region 销售订单

        //规格型号与客户、部门的对应关系，用于下销售订单时进行提示
        [SessionTimeOutFilter]
        public ActionResult ItemModelAndCustomer()
        {
            return View();
        }

        public JsonResult SearchItemModelAndCustomer(int page, int rows,string itemInfo, string customerInfo, string agencyInfo,string createUser)
        {
            var list = db.Sale_itemModelAndCustomer.Where(s => s.is_deleted == false);
            

            if (!string.IsNullOrWhiteSpace(itemInfo)) {
                list = list.Where(l => l.item_model.Contains(itemInfo) || l.item_name.Contains(itemInfo) || l.item_no.Contains(itemInfo));
            }

            if (!string.IsNullOrWhiteSpace(customerInfo)) {
                list = list.Where(l => l.customer_name.Contains(customerInfo) || l.customer_no.Contains(customerInfo));
            }

            if (!string.IsNullOrWhiteSpace(agencyInfo)) {
                list = list.Where(l => l.agency_name.Contains(agencyInfo) || l.agency_no.Contains(agencyInfo));
            }

            if (!string.IsNullOrWhiteSpace(createUser)) {
                list = list.Where(l => l.create_user.Contains(createUser));
            }

            int total = list.Count();
            var result = list.OrderBy(l => l.customer_name).ThenBy(l => l.customer_no).Skip((page - 1) * rows).Take(rows).ToList();

            return Json(new { rows = result, total = total });
        }

        public JsonResult SaveItemModelAndCustomer(string obj)
        {
            Sale_itemModelAndCustomer m = JsonConvert.DeserializeObject<Sale_itemModelAndCustomer>(obj);

            if (db.Sale_itemModelAndCustomer.Where(s => s.item_no == m.item_no && s.id != m.id && s.is_deleted == false).Count() > 0) {
                return Json(new SimpleResultModel(false, "此型号已被绑定，不能再次操作"));
            }
            
            m.create_user = currentUser.realName;
            m.create_date = DateTime.Now;

            db.Sale_itemModelAndCustomer.InsertOnSubmit(m);
            db.SubmitChanges();

            return Json(new SimpleResultModel(true));
        }

        public JsonResult DeleteItemModelAndCustomer(int id)
        {
            var m = db.Sale_itemModelAndCustomer.Where(s => s.id == id).FirstOrDefault();
            if (m == null) {
                return Json(new SimpleResultModel(false, "数据不存在"));
            }
            if (!m.create_user.Equals(currentUser.realName)) {
                return Json(new SimpleResultModel(false, "只能删除你自己创建的对应关系"));
            }

            m.is_deleted = true;
            db.SubmitChanges();

            return Json(new SimpleResultModel(true, "删除成功"));
        }

        //判断订单的型号是否与绑定的客户和部门不一致，型号可以有多个
        public JsonResult TestItemModelAndCustomer(string itemNos, string customerNo, string agencyNo)
        {
            var itemNoArr=itemNos.Split(new char[]{';'},StringSplitOptions.RemoveEmptyEntries);
            var items = db.Sale_itemModelAndCustomer.Where(s => itemNoArr.Contains(s.item_no) && s.is_deleted == false).ToList();
            if (items.Count() > 0) {
                //var noMathCustomers = string.Join("<br/>", items.Where(i => i.customer_no != customerNo).Select(i => i.item_model));
                var noMathAgencys = string.Join("<br/>", items.Where(i => i.agency_no != agencyNo).Select(i => i.item_model));
                string tip = "";
                //if (!string.IsNullOrWhiteSpace(noMathCustomers)) {
                //    tip += "以下型号与被绑定的客户不一致：<br/>" + noMathCustomers + "<br/>";
                //}
                if (!string.IsNullOrWhiteSpace(noMathAgencys)) {
                    tip += "以下型号与被绑定的部门不一致：<br/>" + noMathAgencys + "<br/>";
                }
                if (!string.IsNullOrWhiteSpace(tip)) {
                    tip += "是否继续保存？";
                    return Json(new SimpleResultModel(false, tip));
                }
            }

            return Json(new SimpleResultModel(true));
        }

        #endregion

        #region 备料单

        [SessionTimeOutFilter]
        public ActionResult CheckBusBLs()
        {
            return View();
        }

        public JsonResult SearchBusBLs(DateTime fromDate, DateTime toDate, string productModel, string customerName)
        {
            var myDeps = (from a in db.AuditorsRelation
                          join d in db.Department on a.relate_value equals d.dep_no
                          where a.step_name == "BL_事业部接单员"
                          && d.dep_type == "备料事业部"
                          && a.auditor_id == currentUser.userId
                          select d.name).ToList();

            if (myDeps.Count() > 0) {
                var list = (from b in db.Sale_BL
                            join a in db.Apply on b.sys_no equals a.sys_no
                            where a.success == true
                            && b.bl_date >= fromDate
                            && b.bl_date <= toDate
                            && myDeps.Contains(b.bus_dep)
                            && b.product_model.Contains(productModel)
                            && b.customer_name.Contains(customerName)
                            select new
                            {
                                b.id,
                                b.bl_date,
                                b.bill_no,
                                b.bus_dep,
                                b.customer_name,
                                b.product_model,
                                b.product_name,
                                b.qty,
                                b.market_dep,
                                b.clerk_name,
                                b.sys_no,
                                a.finish_date
                            }).ToList();
                return Json(new SimpleResultModel(true, "", JsonConvert.SerializeObject(list)));
            }
            else {
                return Json(new SimpleResultModel(false,"没有可查询的部门"));
            }

        }

        #endregion

    }
}
