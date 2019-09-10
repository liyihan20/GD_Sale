﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Utils;
using System.IO;
using Sale_Order.Models;
using System.Configuration;
using Sale_Order.Filter;
using System.Text;
using Newtonsoft.Json;

namespace Sale_Order.Controllers
{
    public class SalerController : Controller
    {
        SaleDBDataContext db = new SaleDBDataContext();
        SomeUtils utl = new SomeUtils();
        const string SALEORDER = "销售订单";
        const string SALECONTRACT = "销售合同";
        const string CREATEMODEL = "开模改模单";
        const string SAMPLEBILL = "样品单";
        const string BLBILL = "备料单";
        const string HCBILL = "华为出货报告";

        //uploadify 上传附件
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult UploadFile(HttpPostedFileBase FileData, string num)
        {
            
            string filename = "";
            string finalname = "NOFILE";

            if (null != FileData && !String.IsNullOrEmpty(num))
            {
                try
                {
                    filename = Path.GetFileName(FileData.FileName);//获得文件名
                    string ext = Path.GetExtension(filename);//获取拓展名
                    if (! ".rar".Equals(ext)) {
                        return Content("FILETYPE");
                    }
                    finalname = num + ext;
                    saveFile(FileData, finalname);
                }
                catch (Exception ex)
                {
                    filename = ex.ToString();
                }
            }

            return Content(finalname);
        }

        //WebUploader 上传附件
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult UploadFileWu(HttpPostedFileBase file, string num)
        {

            string filename = "";
            string finalname = "NOFILE";

            if (null != file && !String.IsNullOrEmpty(num)) {
                try {
                    filename = Path.GetFileName(file.FileName);//获得文件名
                    string ext = Path.GetExtension(filename);//获取拓展名
                    finalname = num + ext;
                    saveFile(file, finalname);
                }
                catch (Exception ex) {
                    return Json(new SimpleResultModel() { suc = false, msg = ex.Message });
                }
            }

            return Json(new SimpleResultModel() { suc = true });
        }

        //保存附件
        [NonAction]
        private bool saveFile(HttpPostedFileBase postedFile, string saveName)
        {
            bool result = false;
            string filepath = ConfigurationManager.AppSettings["AttachmentPath1"];// "D:\\Sale_upload_temp\\";
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
            try
            {
                postedFile.SaveAs(Path.Combine(filepath, saveName));
                result = true;
                utl.writeEventLog("保存附件", "上传附件成功，名称：" + saveName, "", Request);
            }
            catch (Exception e)
            {
                utl.writeEventLog("保存附件", "上传附件失败，名称：" + saveName + ";原因:" + e.Message.ToString(), "", Request, -1);
                throw new ApplicationException(e.Message);
            }
            return result;
        }
        
        public JsonResult GetFileInfo(string sys_no)
        {
            var fileName = sys_no + ".rar";
            FileInfo info = new FileInfo(ConfigurationManager.AppSettings["AttachmentPath1"] + fileName);
            if (!info.Exists)
            {
                info = new FileInfo(Path.Combine(SomeUtils.getOrderPath(sys_no), fileName));
                if (!info.Exists)
                    return Json(new { success = false });
            }
            AttachmentModel am = new AttachmentModel()
            {
                file_name = fileName,
                file_size = info.Length / 1024 + "K",
                upload_time = info.CreationTime.ToString()
            };
            return Json(new { success = true, am = am });
        }

        //下载附件
        public FileStreamResult DownFile(string sysNum)
        {
            string fileName = sysNum + ".rar";
            string absoluFilePath = ConfigurationManager.AppSettings["AttachmentPath1"] + fileName;
            FileInfo info = new FileInfo(absoluFilePath);
            if (!info.Exists)
            {
                absoluFilePath = Path.Combine(SomeUtils.getOrderPath(sysNum), fileName);
                info = new FileInfo(absoluFilePath);
                if (!info.Exists)
                {
                    return null;
                }
            }
            utl.writeEventLog("下载附件", "下载成功", sysNum, Request);
            return File(new FileStream(absoluFilePath, FileMode.Open), "application/octet-stream", Server.UrlEncode(fileName));
        }

        //编辑订单前检测是否已开始申请，已申请的单据不能修改
        public JsonResult ISBeginApply(string sys_no)
        {
            var ap = db.Apply.Where(a => a.sys_no == sys_no);
            if (ap != null && ap.Count() > 0)
            {
                utl.writeEventLog("修改单据", "已提交不能修改", sys_no, Request, 10);
                return Json(new { success = true }, "text/html");
            }
            return Json(new { success = false }, "text/html");
        }
        //编辑未开始申请的订单        
        [SessionTimeOutFilter()]
        public ActionResult EditOrderNew(int id, string sys_no)
        {
            string pre = sys_no.Substring(0, 2);
            switch (pre)
            {
                case "SO":
                    return RedirectToAction("SalerModifySOBill", new { sysNo = sys_no });                    
                case "SB":
                    return RedirectToAction("SalerModifySampleBill", new { id = id });
                case "CC":
                    return RedirectToAction("SalerModifyCCMModelContract", new { id = id });
                case "CM":
                    return RedirectToAction("SalerModifyModelContract", new { id = id });
                case "BL":
                    return RedirectToAction("SalerModifyBLBill", new { id = id });
                case "HC":
                    return RedirectToAction("SalerModifyHCBill", new { id = id });
                default:
                    return View("Error");

            }
        }

        //从之前的订单新建新的订单
        public ActionResult NewOrderFromOld(int id, string sys_no)
        {
            SomeUtils utl = new SomeUtils();
            string pre = sys_no.Substring(0, 2);
            string newSysNo = utl.getSystemNo(pre);
            utl.writeEventLog(pre, "从旧单新增，旧单sysNo：" + sys_no, newSysNo, Request);
            switch (pre)
            {
                case "SO":
                    Sale_SO order = db.Sale_SO.Single(s => s.sys_no == sys_no);
                    List<Sale_SO_details> details = db.Sale_SO_details.Where(s => s.order_id == order.id).ToList();

                    order.step_version = 0;
                    order.sys_no = newSysNo;
                    order.order_date = DateTime.Now;
                    ViewData["order"] = order;
                    ViewData["details"] = details;

                    return View("CreateSaleOrder");
                case "SB":
                    SampleBill sb = db.SampleBill.Single(s => s.sys_no == sys_no);
                    sb.sys_no = newSysNo;
                    sb.bill_date = DateTime.Now;
                    sb.step_version = 0;
                    ViewData["sb"] = sb;
                    ViewData["step"] = 0;
                    return View("CreateSampleBill");
                case "CC":
                    CcmModelContract cc = db.CcmModelContract.Single(s => s.sys_no == sys_no);
                    cc.sys_no = newSysNo;
                    cc.bill_date = DateTime.Now;
                    cc.product_number = null;
                    cc.step_version = 0;
                    ViewData["cmc"] = cc;
                    return View("CreateCCMModelContract");
                case "CM":
                    ModelContract cm = db.ModelContract.Single(s => s.sys_no == sys_no);
                    cm.sys_no = newSysNo;
                    cm.bill_date = DateTime.Now;
                    cm.product_number = null;
                    cm.step_version = 0;
                    ViewData["cm"] = cm;
                    return View("CreateModelContract");
                case "BL":
                    Sale_BL bl = db.Sale_BL.Single(s => s.sys_no == sys_no);
                    bl.sys_no = newSysNo;
                    bl.bl_date = DateTime.Now;
                    bl.step_version = 0;
                    bl.bill_no = "";
                    ViewData["BL"] = bl;
                    return View("CreateBLBill");
                case "HC":
                    Sale_HC hc = db.Sale_HC.Single(h => h.sys_no == sys_no);
                    hc.bill_date = DateTime.Now;
                    hc.sys_no = newSysNo;
                    ViewData["hc"] = hc;
                    ViewData["step"] = 0;
                    ViewData["userName"] = hc.user_name;

                    if (utl.CopyFiles(sys_no, newSysNo)) {
                        var fileInfo = db.Sale_HC_fileInfo.Where(h => h.sys_no == sys_no).ToList();
                        var fileResult = (from fi in fileInfo
                                          join at in utl.GetAttachmentInfo(hc.sys_no) on fi.file_name equals at.file_name
                                          select new AttachmentModelNew()
                                          {
                                              file_name = fi.file_name,
                                              file_id = at.file_id,
                                              file_size = at.file_size,
                                              uploader = fi.uploader,
                                              file_status = "已上传",
                                          }).Distinct().ToList();
                        ViewData["fileResult"] = JsonConvert.SerializeObject(fileResult);
                    }
                    else {
                        ViewData["fileResult"] = "[]";
                    }

                    return View("CreateHCBill");
                default:
                    return View("Error");
            }
        }

        //提交申请之前检测订单是否已保存
        public JsonResult CheckIsBillExist(string sysNum)
        {
            string pre = sysNum.Substring(0, 2);
            bool hasSaved = false;
            switch (pre)
            {
                case "SO":
                    hasSaved = db.Sale_SO.Where(ot => ot.sys_no == sysNum).Count() > 0;
                    break;
                case "CC":
                    hasSaved = db.CcmModelContract.Where(c => c.sys_no == sysNum).Count() > 0;
                    break;
                case "CM":
                    hasSaved = db.ModelContract.Where(m => m.sys_no == sysNum).Count() > 0;
                    break;
                case "SB":
                    hasSaved = db.SampleBill.Where(s => s.sys_no == sysNum).Count() > 0;
                    break;
                case "BL":
                    hasSaved = db.Sale_BL.Where(s => s.sys_no == sysNum).Count() > 0;
                    break;
                case "HC":
                    hasSaved = db.Sale_HC.Where(h => h.sys_no == sysNum).Count() > 0;
                    break;
            }
            return Json(new { success = hasSaved });
        }

        [SessionTimeOutFilter()]
        public ActionResult CheckAllOrders(int bill_type)
        {
            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd"];
            if (queryData != null && queryData.Values.Get("sa_so") != null)
            {
                ViewData["sys_no"] = utl.DecodeToUTF8(queryData.Values.Get("sa_so"));
                ViewData["audit_result"] = utl.DecodeToUTF8(queryData.Values.Get("sa_ar"));
                ViewData["from_date"] = queryData.Values.Get("sa_fd");
                ViewData["to_date"] = queryData.Values.Get("sa_td");
            }
            else
            {
                ViewData["from_date"] = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                ViewData["to_date"] = DateTime.Now.ToString("yyyy-MM-dd");
                ViewData["audit_result"] = 10;
            }

            ViewData["bill_type"] = bill_type;
            return View();
        }

        //搜索自己的订单
        public JsonResult CheckOwnOrders(FormCollection fc)
        {
            string sysNo = fc.Get("sys_no");
            string fromDateStr = fc.Get("fromDate");
            string toDateStr = fc.Get("toDate");
            string auditResult = fc.Get("auditResult");
            string billType = fc.Get("billType");

            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd"];
            if (queryData == null) queryData = new HttpCookie("op_qd");
            queryData.Values.Set("sa_so", utl.EncodeToUTF8(sysNo));
            queryData.Values.Set("sa_ar", utl.EncodeToUTF8(auditResult));
            queryData.Values.Set("sa_fd", fromDateStr);
            queryData.Values.Set("sa_td", toDateStr);
            queryData.Expires = DateTime.Now.AddDays(7);
            Response.AppendCookie(queryData);

            DateTime fromDate = DateTime.Parse("1990-1-1");
            DateTime toDate = DateTime.Parse("2099-9-9"); ;
            int result = Int32.Parse(auditResult);
            int bill_type = Int32.Parse(billType);

            if (!string.IsNullOrEmpty(fromDateStr))
            {
                fromDate = DateTime.Parse(fromDateStr);
            }
            if (!string.IsNullOrEmpty(toDateStr))
            {
                toDate = DateTime.Parse(toDateStr);
            }
            switch (bill_type)
            {
                case 1:
                    utl.writeEventLog(SALEORDER, "查询单据，条件：" + fromDate.ToShortDateString() + "~" + toDate.ToShortDateString() + ";scope:" + result.ToString(), sysNo, Request);
                    return CheckOwnSaleOrders(sysNo, fromDate, toDate, result);
                case 3:
                    utl.writeEventLog(CREATEMODEL, "查询单据，条件：" + fromDate.ToShortDateString() + "~" + toDate.ToShortDateString() + ";scope:" + result.ToString(), sysNo, Request);
                    return CheckOwnCreateModelBills(sysNo, fromDate, toDate, result);
                case 4:
                    utl.writeEventLog(SAMPLEBILL, "查询单据，条件：" + fromDate.ToShortDateString() + "~" + toDate.ToShortDateString() + ";scope:" + result.ToString(), sysNo, Request);
                    return CheckOwnSampleBills(sysNo, fromDate, toDate, result);
                case 5:
                    utl.writeEventLog(BLBILL, "查询单据，条件：" + fromDate.ToShortDateString() + "~" + toDate.ToShortDateString() + ";scope:" + result.ToString(), sysNo, Request);
                    return CheckOwnBLBills(sysNo, fromDate, toDate, result);
                case 7:
                    utl.writeEventLog(HCBILL, "查询单据，条件：" + fromDate.ToShortDateString() + "~" + toDate.ToShortDateString() + ";scope:" + result.ToString(), sysNo, Request);
                    return CheckOwnHCBills(sysNo, fromDate, toDate, result);
                default:
                    return null;
            }
        }

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


        #region 销售订单

        //新建销售订单
        [SessionTimeOutFilter()]
        public ActionResult CreateSaleOrder()
        {            
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);

            Sale_SO order = new Sale_SO();
            List<Sale_SO_details> details = new List<Sale_SO_details>();

            order.user_name = db.User.Single(u => u.id == userId).real_name;
            order.user_id = userId;
            order.sys_no = utl.getSystemNo("SO");            
            order.order_date = DateTime.Now;
            order.percent1 = 100;
            order.step_version = 0;

            var dep = db.vwItems.Where(v => v.what == "agency" && v.fname == user.Department1.name).FirstOrDefault();
            if (dep!=null)
            {
                order.department_name = dep.fname;
                order.department_no = dep.fid;
            }
            ViewData["order"] = order;
            ViewData["details"] = details;

            utl.writeEventLog(SALEORDER, "新增成功", order.sys_no, Request);
            return View();
        }
        
        //保存订单表头
        [HttpPost]
        public JsonResult SaveSaleOrder(FormCollection col)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            Sale_SO h = JsonConvert.DeserializeObject<Sale_SO>(col.Get("head"));
            List<Sale_SO_details> ds = JsonConvert.DeserializeObject<List<Sale_SO_details>>(col.Get("details"));
            
            //如已提交，则不能再保存
            if (h.step_version == 0) {
                var ap = db.Apply.Where(a => a.sys_no == h.sys_no);
                if (ap.Count() > 0) {
                    utl.writeEventLog("保存单据", "已提交不能再次保存", h.sys_no, Request, 10);
                    return Json(new SimpleResultModel(false, "已提交的单据不能再次保存！"));
                }
            }

            //订单号判断是否有重复
            if (!string.IsNullOrEmpty(h.order_no)) {
                if (db.Sale_SO.Where(s => s.order_no == h.order_no).Count() > 0) {
                    return Json(new SimpleResultModel(false, "此订单号之前已使用！"));
                }
            }

            #region 验证营业员比例的合法性,并保存营业员比例
            string salerPercent = h.salePs.Trim();
            string[] salers = salerPercent.Split(new char[] { ';', '；' });
            List<SalerPerModel> spList = new List<SalerPerModel>();
            for (int i = 0; i < salers.Count(); i++)
            {
                string[] salerSplit = salers[i].Split(new char[] { ':', '：' });
                if (salerSplit.Count() != 2)
                {
                    if (string.IsNullOrWhiteSpace(salerSplit[0]))
                    {
                        continue;
                    }
                    return Json(new SimpleResultModel(false, "保存失败：以下营业员比例输入不合法：" + salerSplit[0]));
                }
                string tpName = salerSplit[0].Trim();
                string tpPercent = salerSplit[1].Trim();
                float percent;
                //2013-7-23 增加一种格式：周启校（林伟源）：50%
                if (tpName.Contains("(") || tpName.Contains("（"))
                {
                    string[] tpNameSplit = tpName.Split(new char[] { '(', ')', '（', '）' });
                    foreach (string tpNamest in tpNameSplit)
                    {
                        if (!string.IsNullOrWhiteSpace(tpNamest))
                        {
                            if (utl.getSalerId(tpNamest.Trim()) == null)
                            {
                                return Json(new SimpleResultModel(false, "保存失败：以下营业员不可用：" + tpNamest));
                            }
                        }
                    }
                }
                else
                {
                    if (utl.getSalerId(tpName) == null)
                    {
                        return Json(new SimpleResultModel(false, "保存失败：以下营业员不可用："));
                    }
                }
                if (tpPercent.Contains("%"))
                {
                    tpPercent = tpPercent.Substring(0, tpPercent.IndexOf('%'));
                }
                if (!float.TryParse(tpPercent, out percent))
                {
                    return Json(new { success = false, msg = "保存失败：以下比例不合法：" + tpPercent }, "text/html");
                }
                if (percent < 0 || percent > 100)
                {
                    return Json(new SimpleResultModel(false, "保存失败：以下比例超出范围：" + tpPercent));
                }
                SalerPerModel spm = new SalerPerModel();
                if (tpName.Contains("(") || tpName.Contains("（"))
                {
                    spm.salerName = tpName;
                    spm.percent = (float)Math.Round(percent, 1);
                    spm.salerId = 0;
                    spm.cardNumber = "0";
                    spm.zt = "gd";
                }
                else
                {
                    var thisSaler = db.getSaler(tpName, 1).First();
                    spm.salerId = thisSaler.id;
                    spm.salerName = thisSaler.name;
                    spm.cardNumber = thisSaler.cardNum;
                    spm.zt = thisSaler.zt;
                    spm.percent = (float)Math.Round(percent, 1);
                }
                spList.Add(spm);
            }
            if (spList.Sum(l => l.percent) != 100)
            {
                return Json(new { success = false, msg = "保存失败：营业员比例之和不等以100%" }, "text/html");
            }
            #endregion

            #region 表体验证
            int taxRate = 13;
            List<int> projectNos = db.VwProjectNumber.Where(v => v.client_number == h.buy_unit_no || v.client_number == h.oversea_client_no || v.id == 467).Select(v => v.id).ToList();
            int currentIndex = 0;
            foreach (var d in ds) {
                currentIndex++;                
                d.entry_id = currentIndex;
                if (d.project_no == null) {
                    return Json(new SimpleResultModel(false, "保存失败：项目编号不能为空"));
                }
                else if (!projectNos.Contains((int)d.project_no)) {
                    return Json(new SimpleResultModel(false, "保存失败：项目名称[" + d.project_name + "]不属于当前客户"));
                }

                if (h.currency_no=="RMB" && d.tax_rate != taxRate) {
                    return Json(new SimpleResultModel(false, "保存失败：第" + currentIndex + "行：币别为人民币的税率必须是" + taxRate));
                }
                else if (h.currency_no != "RMB" && d.tax_rate != 0) {
                    return Json(new SimpleResultModel(false, "保存失败：第" + currentIndex + "行：币别为非人民币的税率必须是0"));
                }

                d.unit_price = d.unit_price ?? 0;
                d.aux_tax_price = d.aux_tax_price ?? 0;
                if (Math.Abs((decimal)(d.unit_price * (1 + d.tax_rate/100) - d.aux_tax_price)) > 0.0001m) {
                    return Json(new SimpleResultModel(false, "保存失败：第" + currentIndex + "行：不含税单价 * (1+税率%）不等于含税单价"));
                }

                if (d.deal_price > 0) {
                    d.MU = 100 * (1 - ((d.cost * (1 + d.tax_rate / 100)) / (d.deal_price * (decimal)h.exchange_rate))) - d.fee_rate;
                    if (d.MU <= 0) {
                        d.commission_rate = 0;
                    }
                    else {
                        d.commission_rate = (decimal)utl.GetCommissionRate(h.product_type_name, (double)d.MU);
                    }
                    //2018-10-1开始，佣金计算公式修改，将佣金再除（1+税率%）
                    if (h.product_type_name == "CCM" && d.MU < -6) {
                        d.commission = (d.deal_price / (1 + d.tax_rate / 100)) * d.cost * 0.002m * (decimal)h.exchange_rate;
                    }
                    else {
                        d.commission = (d.deal_price / (1 + d.tax_rate / 100)) * d.qty * (decimal)h.exchange_rate * d.commission_rate / 100;
                    }
                    d.commission = Decimal.Round((decimal)d.commission, 2);
                }

                d.suggested_delivery_date = d.suggested_delivery_date ?? d.delivery_date;
                d.confirm_date = d.confirm_date ?? d.target_date;

            }

            #endregion
            

            //如果存在2张以上相同流水号的订单，表示该订单时修改而成，此时要将旧的订单删除。
            var existedBills = db.Sale_SO.Where(s => s.sys_no == h.sys_no).ToList();
            if (existedBills.Count() >= 1)
            {
                var existedDetails = db.Sale_SO_details.Where(d => existedBills.Select(s => s.id).Contains(d.order_id));

                BackupData bd = new BackupData();
                bd.op_date = DateTime.Now;
                bd.sys_no = h.sys_no;
                bd.user_id = userId;
                bd.main_data = JsonConvert.SerializeObject(existedBills);
                bd.secondary_data = JsonConvert.SerializeObject(existedDetails);
                db.BackupData.InsertOnSubmit(bd);

                db.Sale_SO_details.DeleteAllOnSubmit(existedDetails);
                db.Sale_SO.DeleteAllOnSubmit(existedBills);
            }
            try
            {
                db.Sale_SO.InsertOnSubmit(h);
                db.Sale_SO_details.InsertAllOnSubmit(ds);
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                utl.writeEventLog(SALEORDER, "订单保存失败：" + ex.Message, h.sys_no, Request, -1);
                return Json(new SimpleResultModel(false, "订单保存失败：" + ex.Message));
            }

            //最后保存表头与表体的关系
            foreach (var d in ds) {
                d.order_id = h.id;
            }
            db.SubmitChanges();

            utl.writeEventLog(SALEORDER, "订单保存成功", h.sys_no, Request);
            return Json(new SimpleResultModel());

        }

        public ActionResult SalerModifySOBill(string sysNo)
        {
            var order = db.Sale_SO.Where(s => s.sys_no == sysNo).FirstOrDefault();
            if (order == null) {
                ViewBag.tip = "单据不存在";
                return View("Tip");
            }

            var details = db.Sale_SO_details.Where(s => s.order_id == order.id).ToList();

            ViewData["order"] = order;
            ViewData["details"] = details;

            utl.writeEventLog(SALEORDER, "修改单据", order.sys_no, Request);
            return View("CreateSaleOrder");
        }


        //查看自己订单方法
        //审核结果auditResult: 10(全部），0（正在审核），1（成功），-1（失败）
        public JsonResult CheckOwnSaleOrders(string sysNo, DateTime fromDate, DateTime toDate, int auditResult)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            //var result = (from v in db.VwOrder
            //             where v.user_id == userId
            //             && v.step_version == 0
            //             && v.order_date >= fromDate
            //             && v.order_date <= toDate
            //             select v).ToList();
            var result = (from o in db.Sale_SO
                          join e in db.Sale_SO_details on o.id equals e.order_id
                          where o.user_id == userId
                          && o.order_date >= fromDate
                          && o.order_date <= toDate
                          select new { o, e }).ToList();

            if (!string.IsNullOrEmpty(sysNo)) {
                result = result.Where(r => r.o.sys_no.Contains(sysNo) || r.e.item_modual.Contains(sysNo)).ToList();
            }
            List<OrderModel> omList = new List<OrderModel>();
            foreach (var res in result.OrderByDescending(r => r.o.sys_no))
            {
                string status = "";
                var app = db.Apply.Where(a => a.sys_no == res.o.sys_no);
                if (app.Count() < 1)
                {
                    status = "未开始申请";
                }
                else
                {
                    var sucFlag = app.First().success;
                    if (sucFlag == true)
                    {
                        status = "成功申请";
                    }
                    else if (sucFlag == false)
                    {
                        status = "申请失败";
                    }
                    else if (db.BlockOrder.Where(b => b.sys_no == res.o.sys_no).Count() > 0)
                    {
                        var step = db.BlockOrder.Where(b => b.sys_no == res.o.sys_no).OrderByDescending(b => b.step).First().step;
                        if (app.First().ApplyDetails.Where(ad => ad.step == step && ad.pass != null).Count() == 0)
                        {
                            status = "挂起中";
                        }
                        else {
                            status = "审批当中";
                        }
                    }
                    else
                    {
                        status = "审批当中";
                    }
                }
                omList.Add(new OrderModel()
                {
                    bill_id = res.o.id,
                    apply_status = status,
                    buy_unit = res.o.buy_unit_name,
                    deal_price = res.e.deal_price,
                    product_model = res.e.item_modual,
                    product_name = res.e.item_name,
                    qty = res.e.qty,
                    sys_no = res.o.sys_no,
                    apply_date = app.Count() >= 1 ? ((DateTime)app.First().start_date).ToString("yyyy-MM-dd HH:mm") : ""
                });

            }
            if (auditResult == 0)
            {
                omList = omList.Where(o => o.apply_status == "审批当中" || o.apply_status == "未开始申请").ToList();
            }
            else if (auditResult == 1)
            {
                omList = omList.Where(o => o.apply_status == "成功申请").ToList();
            }
            else if (auditResult == -1)
            {
                omList = omList.Where(o => o.apply_status == "申请失败").ToList();
            }
            return Json(omList, "text/html");
        }

        
        //获取订单视图
        //public JsonResult GetSaleOrder(int id)
        //{
        //    var vots = db.VwOrder.Where(v => v.id == id);
        //    if (vots.Count() < 1)
        //    {
        //        return Json(new { success = false });
        //    }
        //    vots = vots.OrderBy(v => v.entry_id);
        //    return Json(new { success = true, list = vots });
        //}
                
        //[SessionTimeOutFilter()]
        //public ActionResult CheckSaleOrder(int id)
        //{
        //    List<VwOrder> vots = db.VwOrder.Where(v => v.id == id).OrderBy(v => v.entry_id).ToList();
        //    if (vots.Count() < 1)
        //    {
        //        return View("Error");
        //    }
        //    ViewData["vots"] = vots;
        //    return View();
        //}
        
        //开始提交申请
        [SessionTimeOutFilter()]
        public ActionResult BeginApplySo(string sys_no)
        {
            string pre = sys_no.Substring(0, 2);
            var ord = db.Sale_SO.Where(s => s.sys_no == sys_no).FirstOrDefault();
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            string processType = pre;
            
            if (ord.department_name.Equals("总裁办")) { 
                //总裁办申请的流程，即陈晓欣申请的，不经过运作中心、事业部和市场部一审
                processType = "SO_2";
            }

            Apply apply = new Apply();
            apply.user_id = userId;
            apply.sys_no = sys_no;
            apply.start_date = DateTime.Now;
            apply.ip = Request.UserHostAddress;
            apply.order_type = pre;
            apply.p_model = db.Sale_SO_details.Where(s => s.order_id == ord.id).First().item_modual;
            db.Apply.InsertOnSubmit(apply);
                        
            //2013-7-18:新增一个测试标志，TestFlag为true表示为测试状态，所有审核人都是自己
            //2013-9-17:新增can_modify字段到ApplyDetails表，表示该审核环节能否修改订单
            bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
            List<ApplyDetails> ads = new List<ApplyDetails>();

            try
            {
                if (!testFlag)
                {
                    ads = utl.getApplySequence(apply, processType, userId, db.User.Single(u => u.id == userId).Department1.dep_no, ord.produce_dep_id);
                }
                else
                {
                    ads = utl.getTestApplySequence(apply, processType, userId);
                }
            }
            catch (Exception ex)
            {
                ViewBag.tip = ex.Message;
                utl.writeEventLog("提交申请", "提交失败:" + ex.Message, sys_no, Request, 100);
                return View("tip");
            }

            db.ApplyDetails.InsertAllOnSubmit(ads);
                        
            try
            {
                db.SubmitChanges();
            }
            catch (Exception e)
            {                
                utl.writeEventLog("提交申请", "不能重复提交申请", sys_no, Request, 100);                
                ViewBag.tip = e.Message;
                return View("tip");
            }

            utl.writeEventLog("提交申请", "成功提交", sys_no, Request);

            //发送邮件通知下一步的人员

            SomeUtils utis = new SomeUtils();
            if (utis.emailToNextAuditor(apply.id))
            {
                utl.writeEventLog("提交申请——发送邮件", "发送成功", sys_no, Request);
                return RedirectToAction("CheckAllOrders", new { bill_type = 1 });
            }
            else
            {
                utl.writeEventLog("提交申请——发送邮件", "发送失败", sys_no, Request, -1);
                ViewBag.tip = "订单提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
                return View("tip");
            }
        }

        //审核员修改
        [SessionTimeOutFilter()]
        public ActionResult AuditorModifySOBill(int apply_id, string sys_no, int step)
        {
            var order = db.Sale_SO.Where(s => s.sys_no == sys_no).FirstOrDefault();
            if (order == null) {
                ViewBag.tip = "单据不存在";
                return View("Tip");
            }

            order.step_version = step;
            var details = db.Sale_SO_details.Where(s => s.order_id == order.id).ToList();

            ViewData["order"] = order;
            ViewData["details"] = details;
            ViewData["applyId"] = apply_id;
            ViewData["blockInfo"] = db.BlockOrder.Where(b => b.sys_no == sys_no).OrderBy(b => b.step).ToList();
            
            return View("CreateSaleOrder");
        }

        #endregion

        #region 开改模单

        //新增CCM开模改模单
        [SessionTimeOutFilter()]
        public ActionResult CreateCCMModelContract()
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);
            string sys_no = utl.getSystemNo("CC");
            CcmModelContract cmc = new CcmModelContract();
            cmc.sys_no = sys_no;
            var dep = db.vwItems.Where(v => v.what == "agency" && v.fname == user.Department1.name);
            if (dep.Count() > 0)
            {
                cmc.agency_no = dep.First().fid;
            }
            cmc.User = user;
            cmc.step_version = 0;
            cmc.bill_date = DateTime.Now;
            //cmc.product_type = "CCM";
            ViewData["cmc"] = cmc;

            utl.writeEventLog(CREATEMODEL, "新建一张CCM开改模单", sys_no, Request);
            return View();
        }

        //新增开改模单（除CCM）
        [SessionTimeOutFilter()]
        public ActionResult CreateModelContract()
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);
            string sys_no = utl.getSystemNo("CM");
            ModelContract cm = new ModelContract();
            cm.sys_no = sys_no;
            var dep = db.vwItems.Where(v => v.what == "agency" && v.fname == user.Department1.name);
            if (dep.Count() > 0)
            {
                cm.agency_no = dep.First().fid;
            }
            cm.User = user;
            cm.step_version = 0;
            cm.bill_date = DateTime.Now;            
            ViewData["cm"] = cm;

            utl.writeEventLog(CREATEMODEL, "新建一张开改模单", sys_no, Request);
            return View();
        }

        //保存CCM开改模单
        public JsonResult SalerSaveCCMModelContract(FormCollection col) {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            int stepVersion = 0;
            string saveResult = utl.saveCCMModelContract(col, stepVersion, userId);
            if (string.IsNullOrWhiteSpace(saveResult))
            {
                return Json(new { suc = true }, "text/html");
            }
            else
            {
                return Json(new { suc = false, msg = saveResult }, "text/html");
            }
        }
        //保存开改模单（除CCM）
        public JsonResult SalerSaveModelContract(FormCollection col)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            int stepVersion = 0;
            string saveResult = utl.saveModelContract(col, stepVersion, userId);
            if (string.IsNullOrWhiteSpace(saveResult))
            {
                return Json(new { suc = true }, "text/html");
            }
            else
            {
                return Json(new { suc = false, msg = saveResult }, "text/html");
            }
        }

        public JsonResult CheckOwnCreateModelBills(string sysNo, DateTime fromDate, DateTime toDate, int auditResult) {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var result = (from v in db.CcmModelContract
                         where v.user_id==userId
                         && (v.sys_no.Contains(sysNo) || v.product_model.Contains(sysNo))
                         && v.bill_date >= fromDate
                         && v.bill_date <= toDate
                         select new
                         {                             
                             id = v.id,
                             sys_no = v.sys_no,
                             customer_name = v.customer_name,
                             product_model = v.product_model,
                             product_name = v.product_name,
                             qty = v.qty,
                             cost = v.price,
                             deal_price = v.price
                         }).Union(
                             from v in db.ModelContract
                             where v.original_user_id == userId
                             && (v.sys_no.Contains(sysNo) || v.product_model.Contains(sysNo))
                             && v.bill_date >= fromDate
                             && v.bill_date <= toDate
                             select new
                             {                                 
                                 id = v.id,
                                 sys_no = v.sys_no,
                                 customer_name = v.customer_name,
                                 product_model = v.product_model,
                                 product_name = v.product_name,
                                 qty = v.qty,
                                 cost = v.price,
                                 deal_price = v.price
                             }
                         );
            List<OrderModel> omList = new List<OrderModel>();
            foreach (var res in result.OrderByDescending(r => r.sys_no))
            {
                string status = "";
                var app = db.Apply.Where(a => a.sys_no == res.sys_no);
                if (app.Count() < 1)
                {
                    status = "未开始申请";
                }
                else
                {
                    var sucFlag = app.First().success;
                    if (sucFlag == true)
                    {
                        status = "成功申请";
                    }
                    else if (sucFlag == false)
                    {
                        status = "申请失败";
                    }
                    else if (db.BlockOrder.Where(b => b.sys_no == res.sys_no).Count() > 0)
                    {
                        var step = db.BlockOrder.Where(b => b.sys_no == res.sys_no).OrderByDescending(b => b.step).First().step;
                        if (app.First().ApplyDetails.Where(ad => ad.step == step && ad.pass != null).Count() == 0)
                        {
                            status = "挂起中";
                        }
                        else
                        {
                            status = "审批当中";
                        }
                    }
                    else
                    {
                        status = "审批当中";
                    }
                }
                omList.Add(new OrderModel()
                {
                    bill_id = res.id,
                    apply_status = status,
                    buy_unit = res.customer_name,
                    product_model = res.product_model,
                    product_name = res.product_name,
                    qty = (decimal)res.qty,
                    sys_no = res.sys_no,
                    deal_price = res.deal_price == null ? res.cost : res.deal_price,
                    apply_date = app.Count() >= 1 ? ((DateTime)app.First().start_date).ToString("yyyy-MM-dd HH:mm") : ""
                });

            }
            if (auditResult == 0)
            {
                omList = omList.Where(o => o.apply_status == "审批当中" || o.apply_status == "未开始申请").ToList();
            }
            else if (auditResult == 1)
            {
                omList = omList.Where(o => o.apply_status == "成功申请").ToList();
            }
            else if (auditResult == -1)
            {
                omList = omList.Where(o => o.apply_status == "申请失败").ToList();
            }
            return Json(omList, "text/html");
        }

        //营业修改
        public ActionResult SalerModifyCCMModelContract(int id)
        {
            ViewData["cmc"] = db.CcmModelContract.Single(s => s.id == id);
            return View("CreateCCMModelContract");
        }        
        public ActionResult SalerModifyModelContract(int id)
        {
            ViewData["cm"] = db.ModelContract.Single(s => s.id == id);
            return View("CreateModelContract");
        }

        //审核员修改
        [SessionTimeOutFilter()]
        public ActionResult AuditorModifyCCMModelContract(int apply_id, string sys_no, int step)
        {
            var cc = db.CcmModelContract.Single(s => s.sys_no == sys_no);
            cc.step_version = step;
            ViewData["cmc"] = cc;
            ViewData["applyId"] = apply_id;
            ViewData["blockInfo"] = db.BlockOrder.Where(b => b.sys_no == sys_no).OrderBy(b => b.step).ToList();
            return View("CreateCCMModelContract");
        }
        [SessionTimeOutFilter()]
        public ActionResult AuditorModifyModelContract(int apply_id, string sys_no, int step)
        {
            var cm = db.ModelContract.Single(s => s.sys_no == sys_no);
            cm.step_version = step;
            ViewData["cm"] = cm;
            ViewData["applyId"] = apply_id;
            ViewData["blockInfo"] = db.BlockOrder.Where(b => b.sys_no == sys_no).OrderBy(b => b.step).ToList();
            return View("CreateModelContract");
        }

        //提交
        [SessionTimeOutFilter()]
        public ActionResult BeginApplyCM(string sys_no)
        {
            string pre = sys_no.Substring(0, 2);
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            string processType = pre;
            string projectTeam = "";
            string busDep = "";
            string pModel = "";
            string modelType = "";
            int? quotationClerkId = null;

            if ("CC".Equals(pre)) {
                processType = "CM_CCM";
                var cc = db.CcmModelContract.Where(c => c.sys_no == sys_no).First();
                projectTeam = cc.project_team;
                quotationClerkId = cc.quotation_clerk_id;
                pModel = cc.product_model;
                modelType = cc.model_type;
            }
            else if ("CM".Equals(pre))
            {
                processType = "CM";
                var mc = db.ModelContract.Where(m => m.sys_no == sys_no).First();
                projectTeam = mc.project_team;
                quotationClerkId = mc.quotation_clerk_id;
                busDep = mc.bus_dep;
                pModel = mc.product_model;
                modelType = mc.model_type;

                //OLED的需要一个固定报价员审批 2018-10-24
                if (busDep.Equals("OLED")) {
                    processType = "CM_OLED";
                }
            }
            else {
                ViewBag.tip = "流程出错";
                return View("Tip");
            }

            if (modelType.Equals("开模")) {
                //开模要判断规格型号是否重复
                if (db.Apply.Where(a => a.order_type == pre && a.p_model == pModel && (a.success == null || a.success == true)).Count() > 0) {
                    ViewBag.tip = "存在已提交的重复的开模规格型号，提交失败";
                    return View("Tip");
                }
            }

            Apply apply = new Apply();
            apply.user_id = userId;
            apply.sys_no = sys_no;
            apply.start_date = DateTime.Now;
            apply.ip = Request.UserHostAddress;
            apply.order_type = pre;
            apply.p_model = pModel;

            db.Apply.InsertOnSubmit(apply);

            //2013-7-18:新增一个测试标志，TestFlag为true表示为测试状态，所有审核人都是自己
            //2013-9-17:新增can_modify字段到ApplyDetails表，表示该审核环节能否修改订单
            bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
            List<ApplyDetails> ads = new List<ApplyDetails>();

            try
            {
                if (!testFlag)
                {
                    //获取部门id，事业部id，项目组id，报价员id
                    Dictionary<string, int?> auditorsDic = new Dictionary<string, int?>();
                    auditorsDic.Add("部门ID", db.User.Single(u => u.id == userId).Department1.dep_no);
                    auditorsDic.Add("研发项目组ID", db.Department.Single(d => d.name == projectTeam && d.dep_type == "研发项目组").dep_no);
                    if (quotationClerkId != null) {
                        auditorsDic.Add("表单报价员值ID", quotationClerkId);
                    }
                    if ("CM".Equals(pre)) {
                        auditorsDic.Add("开模事业部ID", db.Department.Single(d => d.name == busDep && d.dep_type == "开模事业部").dep_no);
                    }

                    ads = utl.getApplySequence(apply, processType, userId, auditorsDic);
                }
                else
                {
                    ads = utl.getTestApplySequence(apply, processType, userId);
                }
            }
            catch (Exception ex)
            {
                ViewBag.tip = ex.Message;
                utl.writeEventLog("提交申请", "提交失败:" + ex.Message, sys_no, Request, 100);
                return View("tip");
            }

            db.ApplyDetails.InsertAllOnSubmit(ads);
            try
            {
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                utl.writeEventLog("提交申请", "不能重复提交申请", sys_no, Request, 100);
                ViewBag.tip = e.Message;
                return View("tip");
            }

            utl.writeEventLog("提交申请", "成功提交", sys_no, Request);
            //发送邮件通知下一步的人员

            if (utl.emailToNextAuditor(apply.id))
            {
                return RedirectToAction("CheckAllOrders", new { bill_type = 3 });
            }
            else
            {
                ViewBag.tip = "订单提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
                return View("tip");
            }
        }

        #endregion

        #region 样品单（包括免费和收费）

        //新建
        [SessionTimeOutFilter()]
        public ActionResult CreateSampleBill()
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);
            string sys_no = utl.getSystemNo("SB");
            SampleBill sb = new SampleBill();
            sb.sys_no = sys_no;
            var dep = db.vwItems.Where(v => v.what == "agency" && v.fname == user.Department1.name);
            if (dep.Count() > 0)
            {
                sb.agency_no = dep.First().fid;
            }
            sb.User = user;
            sb.bill_date = DateTime.Now;
            utl.writeEventLog(SAMPLEBILL, "新建一张样品单", sys_no, Request);

            ViewData["step"] = 0;
            ViewData["sb"] = sb;
            return View();
        }

        //保存
        public JsonResult SalerSaveSampleBill(FormCollection col)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            int stepVersion = 0;
            string saveResult = utl.saveSampleBill(col, stepVersion, userId);
            if (string.IsNullOrWhiteSpace(saveResult))
            {
                return Json(new { suc = true });
            }
            else
            {
                return Json(new { suc = false, msg = saveResult });
            }
        }

        //查看
        public JsonResult CheckOwnSampleBills(string sysNo, DateTime fromDate, DateTime toDate, int auditResult)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var result = from v in db.SampleBill
                         where v.original_user_id == userId
                         && (v.sys_no.Contains(sysNo) || v.product_model.Contains(sysNo))
                         && v.bill_date >= fromDate
                         && v.bill_date <= toDate
                         select new
                         {
                             id = v.id,
                             sys_no = v.sys_no,
                             customer_name = v.customer_name,
                             product_model = v.product_model,
                             product_name = v.product_name,
                             qty = v.sample_qty,
                             cost = v.cost,
                             deal_price = v.deal_price
                         };
            List<OrderModel> omList = new List<OrderModel>();
            foreach (var res in result.OrderByDescending(r => r.sys_no))
            {
                string status = "";
                var app = db.Apply.Where(a => a.sys_no == res.sys_no);
                if (app.Count() < 1)
                {
                    status = "未开始申请";
                }
                else
                {
                    var sucFlag = app.First().success;
                    if (sucFlag == true)
                    {
                        status = "成功申请";
                    }
                    else if (sucFlag == false)
                    {
                        status = "申请失败";
                    }
                    else if (db.BlockOrder.Where(b => b.sys_no == res.sys_no).Count() > 0)
                    {
                        var step = db.BlockOrder.Where(b => b.sys_no == res.sys_no).OrderByDescending(b => b.step).First().step;
                        if (app.First().ApplyDetails.Where(ad => ad.step == step && ad.pass != null).Count() == 0)
                        {
                            status = "挂起中";
                        }
                        else
                        {
                            status = "审批当中";
                        }
                    }
                    else
                    {
                        status = "审批当中";
                    }
                }
                omList.Add(new OrderModel()
                {
                    bill_id = res.id,
                    apply_status = status,
                    buy_unit = res.customer_name,
                    product_model = res.product_model,
                    product_name = res.product_name,
                    qty = (decimal)res.qty,
                    sys_no = res.sys_no,
                    deal_price = res.deal_price == null ? res.cost : res.deal_price,
                    apply_date = app.Count() >= 1 ? ((DateTime)app.First().start_date).ToString("yyyy-MM-dd HH:mm") : ""
                });

            }
            if (auditResult == 0)
            {
                omList = omList.Where(o => o.apply_status == "审批当中" || o.apply_status == "未开始申请").ToList();
            }
            else if (auditResult == 1)
            {
                omList = omList.Where(o => o.apply_status == "成功申请").ToList();
            }
            else if (auditResult == -1)
            {
                omList = omList.Where(o => o.apply_status == "申请失败").ToList();
            }
            return Json(omList, "text/html");
        }

        //营业修改
        public ActionResult SalerModifySampleBill(int id)
        {
            ViewData["sb"] = db.SampleBill.Single(s => s.id == id);
            ViewData["step"] = 0;
            return View("CreateSampleBill");
        }

        //审核人修改
        [SessionTimeOutFilter()]
        public ActionResult AuditorModifySampleBill(int apply_id, string sys_no, int step)
        {
            ViewData["sb"] = db.SampleBill.Single(s => s.sys_no == sys_no);
            ViewData["step"] = step;
            ViewData["applyId"] = apply_id;
            ViewData["blockInfo"] = db.BlockOrder.Where(b => b.sys_no == sys_no).OrderBy(b => b.step).ToList();
            return View("CreateSampleBill");
        }

        //提交
        [SessionTimeOutFilter()]
        public ActionResult BeginApplySB(string sys_no)
        {
            string pre = sys_no.Substring(0, 2);
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            SampleBill sb = db.SampleBill.Single(m => m.sys_no == sys_no);
            string processType = pre;

            if (sb.project_team.StartsWith("OLED")) {
                processType += "_OLED";
            }
            else if (sb.is_free.Equals("免费"))
            {
                processType += "_Free";
            }
            else
            {
                if (sb.project_team.StartsWith("CCM"))
                {
                    processType += "_CCM_Charge";
                }
                else
                {
                    processType += "_Other_Charge";
                }
            }

            Apply apply = new Apply();
            apply.user_id = userId;
            apply.sys_no = sys_no;
            apply.start_date = DateTime.Now;
            apply.ip = Request.UserHostAddress;
            apply.order_type = pre;
            apply.p_model = sb.product_model;
            db.Apply.InsertOnSubmit(apply);

            //2013-7-18:新增一个测试标志，TestFlag为true表示为测试状态，所有审核人都是自己
            //2013-9-17:新增can_modify字段到ApplyDetails表，表示该审核环节能否修改订单
            bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
            List<ApplyDetails> ads = new List<ApplyDetails>();

            try
            {
                if (!testFlag)
                {
                    //获取部门id，事业部id，项目组id，报价员id
                    Dictionary<string, int?> auditorsDic = new Dictionary<string, int?>();
                    auditorsDic.Add("部门ID", db.User.Single(u => u.id == userId).Department1.dep_no);
                    auditorsDic.Add("研发项目组ID", db.Department.Single(d => d.name == sb.project_team && d.dep_type == "研发项目组").dep_no);
                    if (sb.quotation_clerk_id != null) {
                        auditorsDic.Add("表单报价员值ID", sb.quotation_clerk_id);
                    }
                    ads = utl.getApplySequence(apply, processType, userId, auditorsDic);
                }
                else
                {
                    ads = utl.getTestApplySequence(apply, processType, userId);
                }
            }
            catch (Exception ex)
            {
                ViewBag.tip = ex.Message;
                utl.writeEventLog("提交申请", "提交失败:" + ex.Message, sys_no, Request, 100);
                return View("tip");
            }

            db.ApplyDetails.InsertAllOnSubmit(ads);
            try
            {
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                utl.writeEventLog("提交申请", "不能重复提交申请", sys_no, Request, 100);
                ViewBag.tip = e.Message;
                return View("tip");
            }

            utl.writeEventLog("提交申请", "成功提交", sys_no, Request);
            //发送邮件通知下一步的人员

            if (utl.emailToNextAuditor(apply.id))
            {
                return RedirectToAction("CheckAllOrders", new { bill_type = 4 });
            }
            else
            {
                ViewBag.tip = "订单提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
                return View("tip");
            }
        }

        #endregion

        #region 备料单

        [SessionTimeOutFilter()]
        public ActionResult CreateBLBill()
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);
            string sys_no = utl.getSystemNo("BL");
            var bl = new Sale_BL();
            bl.sys_no = sys_no;
            bl.User = user;
            bl.bl_date = DateTime.Now;
            bl.step_version = 0;

            ViewData["BL"] = bl;
            utl.writeEventLog(BLBILL, "新建一张备料单", sys_no, Request);
            return View();
        }

        //保存
        public JsonResult SalerSaveBLBill(FormCollection col)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            int stepVersion = 0;
            string saveResult = utl.saveBLBill(col, stepVersion, userId);
            if (string.IsNullOrWhiteSpace(saveResult)) {
                //2017-10-16 增加是否有下挂bom的提示 2018-1-22不需要提示了，因为不用再经过订料员
                //string busDep = col.Get("bus_dep");
                //string productNo = col.Get("product_no");
                //var result = db.ExecuteQuery<BomProductModel>("exec [dbo].[getBomInfo] @bus_dep = {0},@mat_number = {1},@is_main = {2}", busDep, productNo, 1).ToList();
                //if (result.Count() > 0) {
                //    return Json(new { suc = true, msg = "" });
                //}
                //else {
                //    return Json(new { suc = true, msg = "查询不到此产品的bom明细，请催促研发尽快生效bom，否则将影响订料员审批时效。" });
                //}
                return Json(new { suc = true });
            }
            else {
                return Json(new { suc = false, msg = saveResult });
            }
        }

        public JsonResult CheckOwnBLBills(string sysNo, DateTime fromDate, DateTime toDate, int auditResult)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var result = from v in db.Sale_BL
                         where v.original_user_id == userId
                         && (v.sys_no.Contains(sysNo) || v.product_model.Contains(sysNo))
                         && v.bl_date >= fromDate
                         && v.bl_date <= toDate
                         select new
                         {
                             id = v.id,
                             sys_no = v.sys_no,
                             customer_name = v.customer_name,
                             product_model = v.product_model,
                             product_name = v.product_name,
                             qty = v.qty,
                             deal_price = v.deal_price
                         };
            List<OrderModel> omList = new List<OrderModel>();
            foreach (var res in result.OrderByDescending(r => r.sys_no)) {
                string status = "";
                var app = db.Apply.Where(a => a.sys_no == res.sys_no);
                if (app.Count() < 1) {
                    status = "未开始申请";
                }
                else {
                    var sucFlag = app.First().success;
                    if (sucFlag == true) {
                        status = "成功申请";
                    }
                    else if (sucFlag == false) {
                        status = "申请失败";
                    }
                    else if (db.BlockOrder.Where(b => b.sys_no == res.sys_no).Count() > 0) {
                        var step = db.BlockOrder.Where(b => b.sys_no == res.sys_no).OrderByDescending(b => b.step).First().step;
                        if (app.First().ApplyDetails.Where(ad => ad.step == step && ad.pass != null).Count() == 0) {
                            status = "挂起中";
                        }
                        else {
                            status = "审批当中";
                        }
                    }
                    else {
                        status = "审批当中";
                    }
                }
                omList.Add(new OrderModel()
                {
                    bill_id = res.id,
                    apply_status = status,
                    buy_unit = res.customer_name,
                    product_model = res.product_model,
                    product_name = res.product_name,
                    qty = (decimal)res.qty,
                    sys_no = res.sys_no,
                    deal_price = res.deal_price,
                    apply_date = app.Count() >= 1 ? ((DateTime)app.First().start_date).ToString("yyyy-MM-dd HH:mm") : ""
                });

            }
            if (auditResult == 0) {
                omList = omList.Where(o => o.apply_status == "审批当中" || o.apply_status == "未开始申请").ToList();
            }
            else if (auditResult == 1) {
                omList = omList.Where(o => o.apply_status == "成功申请").ToList();
            }
            else if (auditResult == -1) {
                omList = omList.Where(o => o.apply_status == "申请失败").ToList();
            }
            return Json(omList, "text/html");
        }

        //修改
        public ActionResult SalerModifyBLBill(int id)
        {
            ViewData["BL"] = db.Sale_BL.Single(s => s.id == id);
            return View("CreateBLBill");
        }

        //审核人进入审核编辑界面
        public ActionResult AuditorModifyBLBill(int apply_id, string sys_no, int step)
        {
            var bl = db.Sale_BL.Single(s => s.sys_no == sys_no);
            bl.step_version = step;
            string stepName = db.ApplyDetails.Where(ad => ad.apply_id == apply_id && ad.step == step).First().step_name;
            ViewData["BL"] = bl;
            ViewData["applyId"] = apply_id;
            ViewData["stepName"] = stepName;
            ViewData["blockInfo"] = db.BlockOrder.Where(b => b.sys_no == sys_no).OrderBy(b => b.step).ToList();
            if (stepName.Contains("订料")) {
                int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
                ViewData["order_id"] = userId;
                ViewData["order_name"] = db.User.Single(u => u.id == userId).real_name;
            }
            return View("CreateBLBill");
        }

        [SessionTimeOutFilter()]
        public ActionResult BeginApplyBL(string sys_no)
        {
            string pre = sys_no.Substring(0, 2);
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            Sale_BL bl = db.Sale_BL.Single(s => s.sys_no == sys_no);
            string processType = pre;

            if (bl.bus_dep.Contains("TDD") || bl.bus_dep.Contains("IMDS")) {
                processType = "BL_TDD";
            }
            else {
                processType = "BL_other";
            }

            //2018年启用的新流程
            if (DateTime.Now > DateTime.Parse("2018-04-23")) {
                processType = "BL";
            }

            Apply apply = new Apply();
            apply.user_id = userId;
            apply.sys_no = sys_no;
            apply.start_date = DateTime.Now;
            apply.ip = Request.UserHostAddress;
            apply.order_type = pre;
            apply.p_model = bl.product_model;
            db.Apply.InsertOnSubmit(apply);

            //2013-7-18:新增一个测试标志，TestFlag为true表示为测试状态，所有审核人都是自己
            //2013-9-17:新增can_modify字段到ApplyDetails表，表示该审核环节能否修改订单
            bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
            List<ApplyDetails> ads = new List<ApplyDetails>();

            try {
                if (!testFlag) {
                    //获取部门id，事业部id，项目组id，报价员id
                    Dictionary<string, int?> auditorsDic = new Dictionary<string, int?>();
                    auditorsDic.Add("部门ID", db.User.Single(u => u.id == userId).Department1.dep_no);
                    auditorsDic.Add("备料事业部ID", db.Department.Single(d => d.name == bl.bus_dep && d.dep_type == "备料事业部").dep_no);
                    ads = utl.getApplySequence(apply, processType, userId, auditorsDic);
                }
                else {
                    ads = utl.getTestApplySequence(apply, processType, userId);
                }
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                utl.writeEventLog("提交申请", "提交失败:" + ex.Message, sys_no, Request, 100);
                return View("tip");
            }
            db.ApplyDetails.InsertAllOnSubmit(ads);

            try {
                db.SubmitChanges();
            }
            catch (Exception e) {
                utl.writeEventLog("提交申请", "不能重复提交申请", sys_no, Request, 100);
                ViewBag.tip = e.Message;
                return View("tip");
            }

            utl.writeEventLog("提交申请", "成功提交", sys_no, Request);
            //发送邮件通知下一步的人员

            if (utl.emailToNextAuditor(apply.id)) {
                return RedirectToAction("CheckAllOrders", new { bill_type = 5 });
            }
            else {
                ViewBag.tip = "订单提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
                return View("tip");
            }
        }

        #endregion


        #region 华为出货报告

        [SessionTimeOutFilter()]
        public ActionResult CreateHCBill()
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);
            string sys_no = utl.getSystemNo("HC");
            var hc = new Sale_HC();
            hc.sys_no = sys_no;            
            hc.user_name = user.real_name;
            hc.bill_date = DateTime.Now;

            ViewData["HC"] = hc;
            ViewData["step"] = 0;
            ViewData["userName"] = user.real_name;
            ViewData["fileResult"] = "[]";
            utl.writeEventLog(BLBILL, "新建一张华为出货报告", sys_no, Request);
            return View();
        }

        public JsonResult SalerSaveHCBill(FormCollection fc)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            var result = utl.saveHCBill(fc, 0, userId);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new SimpleResultModel() { suc = false, msg = result });
            }

            return Json(new SimpleResultModel() { suc = true });
        }

        public JsonResult CheckOwnHCBills(string sysNo, DateTime fromDate, DateTime toDate, int auditResult)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var result = from v in db.Sale_HC
                         where v.user_id == userId
                         && (v.sys_no.Contains(sysNo) || v.item_model.Contains(sysNo))
                         && v.bill_date >= fromDate
                         && v.bill_date <= toDate
                         select new
                         {
                             id = v.id,
                             sys_no = v.sys_no,
                             customer_name = "华为",
                             product_model = v.item_model,
                             product_name = v.item_name,
                             qty = v.item_qty,
                             deal_price = 0
                         };
            List<OrderModel> omList = new List<OrderModel>();
            foreach (var res in result.OrderByDescending(r => r.sys_no)) {
                string status = "";
                var app = db.Apply.Where(a => a.sys_no == res.sys_no);
                if (app.Count() < 1) {
                    status = "未开始申请";
                }
                else {
                    var sucFlag = app.First().success;
                    if (sucFlag == true) {
                        status = "成功申请";
                    }
                    else if (sucFlag == false) {
                        status = "申请失败";
                    }
                    else {
                        status = "审批当中";
                    }
                }
                omList.Add(new OrderModel()
                {
                    bill_id = res.id,
                    apply_status = status,
                    buy_unit = res.customer_name,
                    product_model = res.product_model,
                    product_name = res.product_name,
                    qty = (decimal)res.qty,
                    sys_no = res.sys_no,
                    deal_price = res.deal_price,
                    apply_date = app.Count() >= 1 ? ((DateTime)app.First().start_date).ToString("yyyy-MM-dd HH:mm") : ""
                });

            }
            if (auditResult == 0) {
                omList = omList.Where(o => o.apply_status == "审批当中" || o.apply_status == "未开始申请").ToList();
            }
            else if (auditResult == 1) {
                omList = omList.Where(o => o.apply_status == "成功申请").ToList();
            }
            else if (auditResult == -1) {
                omList = omList.Where(o => o.apply_status == "申请失败").ToList();
            }
            return Json(omList, "text/html");
        }

        public ActionResult SalerModifyHCBill(int id)
        {
            var hc = db.Sale_HC.Single(s => s.id == id);
            var fileInfo = db.Sale_HC_fileInfo.Where(h => h.sys_no == hc.sys_no).ToList();
            var fileResult = (from fi in fileInfo
                              join at in utl.GetAttachmentInfo(hc.sys_no) on fi.file_name equals at.file_name
                              select new AttachmentModelNew()
                              {
                                  file_name = fi.file_name,
                                  file_id = at.file_id,
                                  file_size = at.file_size,
                                  uploader = fi.uploader,
                                  file_status = "已上传",
                              }).Distinct().ToList();

            ViewData["hc"] = hc;
            ViewData["step"] = 0;
            ViewData["userName"] = hc.user_name;
            ViewData["fileResult"] = JsonConvert.SerializeObject(fileResult);

            return View("CreateHCBill");
        }

        [SessionTimeOutFilter()]
        public ActionResult BeginApplyHC(string sys_no)
        {
            string pre = sys_no.Substring(0, 2);
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            Sale_HC bl = db.Sale_HC.Single(s => s.sys_no == sys_no);
            string processType = pre;                       

            Apply apply = new Apply();
            apply.user_id = userId;
            apply.sys_no = sys_no;
            apply.start_date = DateTime.Now;
            apply.ip = Request.UserHostAddress;
            apply.order_type = pre;
            apply.p_model = bl.item_model;
            db.Apply.InsertOnSubmit(apply);

            //2013-7-18:新增一个测试标志，TestFlag为true表示为测试状态，所有审核人都是自己
            //2013-9-17:新增can_modify字段到ApplyDetails表，表示该审核环节能否修改订单
            bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
            List<ApplyDetails> ads = new List<ApplyDetails>();

            try {
                if (!testFlag) {
                    //获取部门id，事业部id，项目组id，报价员id
                    Dictionary<string, int?> auditorsDic = new Dictionary<string, int?>();
                    auditorsDic.Add("华为出货事业部ID", db.Department.Single(d => d.name == bl.dep_name && d.dep_type == "华为出货事业部").dep_no);
                    ads = utl.getApplySequence(apply, processType, userId, auditorsDic);
                }
                else {
                    ads = utl.getTestApplySequence(apply, processType, userId);
                }
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                utl.writeEventLog("提交申请", "提交失败:" + ex.Message, sys_no, Request, 100);
                return View("tip");
            }
            db.ApplyDetails.InsertAllOnSubmit(ads);

            try {
                db.SubmitChanges();
            }
            catch (Exception e) {
                utl.writeEventLog("提交申请", "不能重复提交申请", sys_no, Request, 100);
                ViewBag.tip = e.Message;
                return View("tip");
            }

            utl.writeEventLog("提交申请", "成功提交", sys_no, Request);
            //发送邮件通知下一步的人员

            if (utl.emailToNextAuditor(apply.id)) {
                return RedirectToAction("CheckAllOrders", new { bill_type = 7 });
            }
            else {
                ViewBag.tip = "订单提交成功，但邮件服务器故障或暂时繁忙，通知邮件发送失败。如果紧急，可以手动发邮件或电话通知下一审核人。";
                return View("tip");
            }
        }

        //审核人进入审核编辑界面
        public ActionResult AuditorModifyHCBill(int apply_id, string sys_no, int step)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);
            var hc = db.Sale_HC.Single(s => s.sys_no == sys_no);
            var fileInfo = db.Sale_HC_fileInfo.Where(h => h.sys_no == hc.sys_no).ToList();
            var fileResult = (from fi in fileInfo
                              join at in utl.GetAttachmentInfo(hc.sys_no) on fi.file_name equals at.file_name
                              select new AttachmentModelNew()
                              {
                                  file_name = fi.file_name,
                                  file_id = at.file_id,
                                  file_size = at.file_size,
                                  uploader = fi.uploader,
                                  file_status = "已上传",
                              }).Distinct().ToList();
            
            ViewData["hc"] = hc;
            ViewData["applyId"] = apply_id;
            ViewData["step"] = step;
            ViewData["userName"] = user.real_name;
            ViewData["fileResult"] = JsonConvert.SerializeObject(fileResult);

            return View("CreateHCBill");
        }

        #endregion

        //查看所有类型单据详细信息
        [SessionTimeOutFilter()]
        public ActionResult CheckOrderDetail(int id, string billType, bool canCheckBLFile=false)
        {
            string sysNum;
            //int newestId;
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var notContractPrice = utl.hasGotPower(userId, Powers.not_contract_price.ToString());
            var notAllPrice = utl.hasGotPower(userId, Powers.not_all_price.ToString());
            ViewData["hiddenPrice"] = notContractPrice || notAllPrice ? "true" : "false";
            ViewData["hiddenAll"] = notAllPrice ? "true" : "false";
            
            switch (billType)
            {
                case "SO":
                case "1":
                    var order = db.Sale_SO.Where(s => s.id == id).FirstOrDefault();
                    if (order == null) {
                        ViewBag.tip = "单据已更新，请刷新后再查询";
                        return View("Tip");
                    }
                    sysNum = order.sys_no;
                    utl.writeEventLog(SALEORDER, "查看单据明细", sysNum, Request);
                    //挂起订单信息
                    var blockInfo = db.BlockOrder.Where(b => b.sys_no == sysNum).OrderBy(b => b.step).ToList();
                                        
                    ViewData["order"] = order;
                    ViewData["details"] = db.Sale_SO_details.Where(s => s.order_id == id).ToList();
                    ViewData["blockInfo"] = blockInfo;
                    return View("CheckSaleOrder");
                case "CC":
                    CcmModelContract cc = db.CcmModelContract.Single(c => c.id == id);
                    ViewData["cc"] = cc;
                    blockInfo = db.BlockOrder.Where(b => b.sys_no == cc.sys_no).OrderBy(b => b.step).ToList();
                    ViewData["blockInfo"] = blockInfo;
                    utl.writeEventLog(CREATEMODEL, "查看CCM开改模单", cc.sys_no, Request);
                    return View("CheckCCMModelContract");
                case "CM":
                    ModelContract cm = db.ModelContract.Single(c => c.id == id);
                    ViewData["mc"] = cm;
                    blockInfo = db.BlockOrder.Where(b => b.sys_no == cm.sys_no).OrderBy(b => b.step).ToList();
                    ViewData["blockInfo"] = blockInfo;
                    utl.writeEventLog(CREATEMODEL, "查看开改模单", cm.sys_no, Request);
                    return View("CheckModelContract");
                case "SB":
                case "4":
                    SampleBill sb = db.SampleBill.Single(m => m.id == id);
                    ViewData["sb"] = sb;
                    blockInfo = db.BlockOrder.Where(b => b.sys_no == sb.sys_no).OrderBy(b => b.step).ToList();
                    ViewData["blockInfo"] = blockInfo;
                    utl.writeEventLog(SAMPLEBILL, "查看样品单", sb.sys_no, Request);
                    return View("CheckSampleBill");
                case "BL":
                case "5":
                    Sale_BL bl = db.Sale_BL.Single(m => m.id == id);                    
                    blockInfo = db.BlockOrder.Where(b => b.sys_no == bl.sys_no).OrderBy(b => b.step).ToList();                    
                    utl.writeEventLog(BLBILL, "查看备料", bl.sys_no, Request);
                    string userDep = db.User.Single(u => u.id == userId).Department1.name;
                    ViewData["hiddenModel"] = new string[] { "上海", "北京", "深圳", "汕尾", "新加坡", "中国市场部", "香港", "光能", "杭州" }.Where(s => userDep.Contains(s)).Count() > 0 ? "true" : "false";
                    ViewData["bl"] = bl;
                    ViewData["blockInfo"] = blockInfo;
                    ViewData["canCheckBLFile"] = canCheckBLFile;//2018年开始限制只有市场部的才能查看附件
                    return View("CheckBLBill");
                case "6":
                case "TH":
                    return RedirectToAction("CheckReturnBill", "BadProduct", new { id = id });
                case "HH":
                    return RedirectToAction("CheckResendBill", "BadProduct", new { id = id });
                case "CH":
                    return RedirectToAction("CheckFetchBill", "BadProduct", new { id = id });
                case "PJ":
                    return RedirectToAction("CheckSingleProjectBill", "Project", new { id = id });
                case "7":
                case "HC":
                    Sale_HC hc = db.Sale_HC.Single(h => h.id == id);
                    var fileInfo = db.Sale_HC_fileInfo.Where(h => h.sys_no == hc.sys_no).ToList();
                    var fileResult = (from fi in fileInfo
                              join at in utl.GetAttachmentInfo(hc.sys_no) on fi.file_name equals at.file_name
                              select new AttachmentModelNew()
                              {
                                  file_name = fi.file_name,
                                  file_id = at.file_id,
                                  file_size = at.file_size,
                                  uploader = fi.uploader,
                                  file_status = "已上传",
                              }).Distinct().ToList();
                    ViewData["hc"] = hc;
                    ViewData["fileResult"] = JsonConvert.SerializeObject(fileResult);
                    utl.writeEventLog(HCBILL, "查看华为出货单", hc.sys_no, Request);
                    return View("CheckHCBill");
            }
            return View("error");
        }

        [SessionTimeOutFilter()]
        public ActionResult CheckOrderDetailByApplyId(int applyId)
        {
            var ap = db.Apply.Single(a => a.id == applyId);
            int id = 0;
            switch (ap.order_type)
            {
                case "SO":
                    id = db.Sale_SO.Where(o => o.sys_no == ap.sys_no).First().id;
                    break;
                case "TH":
                    id = db.ReturnBill.Where(r => r.sys_no == ap.sys_no).First().id;
                    break;
                case "HH":
                    id = db.ResendBill.Where(r => r.sys_no == ap.sys_no).First().id;
                    break;
                case "CH":
                    id = db.FetchBill.Where(r => r.sys_no == ap.sys_no).First().id;
                    break;
                case "PJ":
                    id = db.Project_bills.Where(r => r.sys_no == ap.sys_no).First().id;
                    break;
                case "SB":
                    id = db.SampleBill.Where(s => s.sys_no == ap.sys_no).First().id;
                    break;
                case "CC":
                    id = db.CcmModelContract.Where(c => c.sys_no == ap.sys_no).First().id;
                    break;
                case "CM":
                    id = db.ModelContract.Where(c => c.sys_no == ap.sys_no).First().id;
                    break;
                case "BL":
                    id = db.Sale_BL.Where(s => s.sys_no == ap.sys_no).First().id;
                    break;
                case "HC":
                    id = db.Sale_HC.Where(s => s.sys_no == ap.sys_no).First().id;
                    break;
            }
            if (id == 0)
            {
                ViewBag.tip = "找不到此单据";
                return View("Error");
            }
            return CheckOrderDetail(id, ap.order_type);
        }

        //public string ValidateQuote(string quoteNo, string model)
        //{
        //    string ConnectionString = ConfigurationManager.ConnectionStrings["Quotation_ConnectionString"].ConnectionString;
        //    OracleConnection conn = new OracleConnection(ConnectionString); //创建一个新连接 
        //    OracleCommand cmd = new OracleCommand("SELECT QUOTATIONNO,DEVICESPEC FROM QUOTATION.V_GET_QUOTATION WHERE DEVICESPEC = '" + model + "' and QUOTATIONNO = '" + quoteNo + "' order by QUOTATIONDATE DESC", conn);
        //    DataSet ds = new DataSet();
        //    OracleDataAdapter oda = new OracleDataAdapter();
        //    oda.SelectCommand = cmd;
        //    oda.Fill(ds);
        //    conn.Close();

        //    var rows = ds.Tables[0].Rows;
        //    if (rows.Count < 1) {
        //        return string.Format("规格型号[{0}]与报价序号[{1}]不匹配", model, quoteNo);
        //    }

        //    return "";
        //}

        public string UPro()
        {
            var list = (from a in db.Apply
                        where a.order_type == "BL"
                        && a.success == null
                        && a.ApplyDetails.Where(d => d.step_name == "市场总部审批").Count() == 0
                        select new
                        {
                            applyId = a.id,
                            maxStep = a.ApplyDetails.Max(d => d.step)
                        }).ToList();

            foreach (var l in list) {
                db.ApplyDetails.InsertOnSubmit(new ApplyDetails()
                {
                    apply_id = l.applyId,
                    step_name = "市场总部审批",
                    step = l.maxStep + 1,
                    can_modify = false,
                    countersign = false,
                    user_id = 87
                });
            }

            db.SubmitChanges();

            return "ok:" + list.Count();
        }

    }
}
