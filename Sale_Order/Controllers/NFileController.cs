using Sale_Order.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Utils;
using Sale_Order.Services;
using Newtonsoft.Json;
using Sale_Order.Filter;

namespace Sale_Order.Controllers
{
    [SessionTimeOutFilter]
    public class NFileController : BaseController
    {
        private const string TAG = "文件模块";
        private SomeUtils util = new SomeUtils();

        private void Wlog(string log, string sysNo = "", int unusual = 0)
        {
            base.Wlog(TAG, log, sysNo, unusual);
        }

        /// <summary>
        /// 营业单据列表页面，导出Excel
        /// </summary>
        /// <param name="fc">form表单</param>
        public void ExportSalerExcel(FormCollection fc)
        {
            SalerSearchParamModel pm = new SalerSearchParamModel();
            util.SetFieldValueToModel(fc, pm);

            BillSv bill = (BillSv)new BillUtils().GetBillSvInstance(pm.billType);

            Wlog("营业员导出Excel：" + JsonConvert.SerializeObject(pm));

            bill.ExportSalerExcle(pm, currentUser.userId);
        }

        public void ExportAuditorExcel(string billType, FormCollection fc)
        {
            AuditSearchParamModel pm = new AuditSearchParamModel();
            util.SetFieldValueToModel(fc, pm);

            BillSv bill = (BillSv)new BillUtils().GetBillSvInstance(billType);

            if (bill == null) return;

            Wlog("审核人导出Excel：" + JsonConvert.SerializeObject(pm), billType);

            bill.ExportAuditorExcle(pm, currentUser.userId);
        }

        public ActionResult PrintReport(string sysNo)
        {
            try {
                BillSv bill = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(sysNo);

                var stream = bill.PrintReport(Server.MapPath("~/Reports/"));
                return File(stream, "application/pdf");
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Tip");
            }
        }

        //物流打印送货单
        public ActionResult PrintCHReport(string sysNo, int pageCount = 3, int numPerPage = 10)
        {
            var sv = new CHSv(sysNo);
            sv.UpdatePrintStatus();

            var bill = sv.GetBill(-1, currentUser.userId) as CHModel;
            ViewData["customerInfo"] = new K3ItemSv(bill.head.account).GetK3CustomerInfo(bill.head.customer_no);

            bill.head.account = GetCompanyByAccount(bill.head.account);            
            ViewData["bill"] = bill;
            ViewData["pageCount"] = pageCount;
            ViewData["numPerPage"] = numPerPage;
            ViewData["printer"] = currentUser.realName;

            return View();
        }

        //物流打印放行条，可合并打印
        public JsonResult BeforePrintOutbill(string sysNos)
        {
            string[] sysNoList = sysNos.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var chs = db.CH_bill.Where(h => sysNoList.Contains(h.sys_no)).ToList();
            if (chs.Where(c => c.out_status == "已放行").Count() > 0) {
                return Json(new SimpleResultModel(false, "存在已放行的单，不能再次打印："));
            }

            List<CH_out_log> outList = new List<CH_out_log>();
            string outNo=new CHSv().GetNextSysNo("CHO");
            foreach (var c in chs) {
                c.out_status = "已打印";

                outList.Add(new CH_out_log()
                {
                    out_no = outNo,
                    ch_sys_no = c.sys_no,
                    stock_no = c.k3_stock_no,
                    print_time = DateTime.Now,
                    print_user = currentUser.realName
                });
            }

            db.CH_out_log.InsertAllOnSubmit(outList);

            db.SubmitChanges();

            return Json(new SimpleResultModel(true,"",outNo));
        }

        public ActionResult PrintOutBill(string outNo, int numPerPage = 6)
        {
            var m = new CHOutBillModel();

            var list = (from o in db.CH_out_log
                        join c in db.CH_bill on o.ch_sys_no equals c.sys_no
                        join e in db.CH_bill_detail on c.sys_no equals e.sys_no
                        where outNo == o.out_no && e.real_qty > 0
                        select new
                        {
                            o,
                            c,
                            e
                        }).ToList();

            m.stockNos = string.Join(" ", list.Select(l => l.o.stock_no).Distinct());
            m.outNo = outNo;
            m.numPerPage = numPerPage;
            m.printer = currentUser.realName;
            m.entrys = list.GroupBy(l => l.e.item_model).Select(l => new CHOutBillEntryModel() { itemModel = l.Key, qty = l.Sum(a => a.e.real_qty) }).ToList();

            ViewData["m"] = m;
            return View();
        }

    }
}
