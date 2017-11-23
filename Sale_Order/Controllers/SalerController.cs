using System;
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

        //uploadify 上传附件
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult UploadFile( HttpPostedFileBase FileData, string num)
        {
            
            string filename = "";
            string finalname = "NOFILE";

            if (null != FileData && !String.IsNullOrEmpty(num))
            {
                try
                {
                    filename = Path.GetFileName(FileData.FileName);//获得文件名
                    string ext = Path.GetExtension(filename);//获取拓展名
                    if (!".rar".Equals(ext)) {
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
        public ContentResult UploadFileWu(HttpPostedFileBase file, string num)
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
                    filename = ex.ToString();
                }
            }

            return Content(finalname);
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
            ViewData["orderId"] = id;
            ViewData["sys_no"] = sys_no;
            switch (pre)
            {
                case "SO":
                    utl.writeEventLog(SALEORDER, "修改订单", sys_no, Request);
                    return View("CreateSaleOrder");
                case "SB":
                    return RedirectToAction("SalerModifySampleBill", new { id = id });
                case "CC":
                    return RedirectToAction("SalerModifyCCMModelContract", new { id = id });
                case "CM":
                    return RedirectToAction("SalerModifyModelContract", new { id = id });
                case "BL":
                    return RedirectToAction("SalerModifyBLBill", new { id = id });
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
                    ViewData["orderId"] = id;
                    ViewData["sys_no"] = newSysNo;
                    return View("CreateSaleOrder");
                case "SB":
                    SampleBill sb = db.SampleBill.Single(s => s.sys_no == sys_no);
                    sb.sys_no = newSysNo;
                    sb.bill_date = DateTime.Now;
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
                    hasSaved = db.Order.Where(ot => ot.sys_no == sysNum).Count() > 0;
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
                    return CheckOwnSaleOrders(sysNo, fromDate, toDate, result, "SO");
                case 3:
                    utl.writeEventLog(CREATEMODEL, "查询单据，条件：" + fromDate.ToShortDateString() + "~" + toDate.ToShortDateString() + ";scope:" + result.ToString(), sysNo, Request);
                    return CheckOwnCreateModelBills(sysNo, fromDate, toDate, result);
                case 4:
                    utl.writeEventLog(SAMPLEBILL, "查询单据，条件：" + fromDate.ToShortDateString() + "~" + toDate.ToShortDateString() + ";scope:" + result.ToString(), sysNo, Request);
                    return CheckOwnSampleBills(sysNo, fromDate, toDate, result);
                case 5:
                    utl.writeEventLog(BLBILL, "查询单据，条件：" + fromDate.ToShortDateString() + "~" + toDate.ToShortDateString() + ";scope:" + result.ToString(), sysNo, Request);
                    return CheckOwnBLBills(sysNo, fromDate, toDate, result);
                default:
                    return null;
            }
        }

        #region 销售订单

        //新建销售订单
        [SessionTimeOutFilter()]
        public ActionResult CreateSaleOrder()
        {
            //检查审核链是否完整
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            User user = db.User.Single(u => u.id == userId);            

            ViewData["create_user"] = db.User.Single(u => u.id == userId).real_name;
            ViewData["sys_no"] = utl.getSystemNo("SO");
            var dep = db.vwItems.Where(v => v.what == "agency" && v.fname == user.Department1.name);
            if (dep.Count() > 0)
            {
                ViewData["department_name"] = user.Department1.name;
                ViewData["department_id"] = dep.First().interid;
            }
            utl.writeEventLog(SALEORDER, "新增成功", ViewData["sys_no"].ToString(), Request);
            return View();
        }
        
        //保存订单表头
        [HttpPost]
        public JsonResult saveSaleOrder(FormCollection col)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            string sysNum = col.Get("sys_no");

            //如已提交，则不能再保存
            var ap = db.Apply.Where(a => a.sys_no == sysNum);
            if (ap != null && ap.Count() > 0)
            {
                utl.writeEventLog("保存单据", "已提交不能再次保存", sysNum, Request, 10);
                return Json(new { success = false, msg = "已提交的单据不能再次保存！" }, "text/html");
            }

            #region 暂时只能下生产单和物料处理单，物料处理单的【生产部门】字段必须选择市场部
            int orderType = Int32.Parse(col.Get("order_type"));
            int procDep = Int32.Parse(col.Get("proc_dep"));
            string procDepName = db.Department.Single(d => d.dep_no == procDep && d.dep_type == "销售事业部").name;
            if (orderType != 40544 && orderType != 40547)
            {
                return Json(new { success = false, msg = "本系统暂时只能下生产单和物料处理单，保存失败！" }, "text/html");
            }
            if (orderType == 40547 && !procDepName.Equals("市场部")) {
                return Json(new { success = false, msg = "物料处理单的【生产部门】字段必须选择市场部，保存失败！" }, "text/html");
            }
            #endregion

            #region 验证购货单位和海外客户
            int buyUnitId = Int32.Parse(col.Get("buy_unit"));
            int currencyID = Int32.Parse(col.Get("currency"));
            int oversea_client_id = Int32.Parse(col.Get("oversea_client"));
            getCostomerByIdResult customer = db.getCostomerById(buyUnitId).First();
            getCostomerByIdResult overseaclient = db.getCostomerById(oversea_client_id).First();
            string trade_type = col.Get("trade_type");
            int tradeType = Int32.Parse(trade_type);
            if (currencyID == 1 && !customer.number.StartsWith("01") && !customer.number.StartsWith("04"))
            {
                return Json(new { success = false, msg = "现在的购货单位编号是：" + customer.number + ";内销单的购货客户代码必须是01开头" }, "text/html");
            }
            if (currencyID != 1 && !customer.name.Contains("香港信利光电有限公司"))
            {
                return Json(new { success = false, msg = "现在的购货单位编号是：" + customer.number + ";外销单的购货客户必须为香港信利光电有限公司" }, "text/html");
            }

            //验证海外客户 
            if (currencyID == 1 && !overseaclient.number.StartsWith("01") && !customer.number.StartsWith("04"))
            {
                return Json(new { success = false, msg = "现在的海外客户编号是：" + overseaclient.number + ";内销单的海外客户代码必须是01开头" }, "text/html");
            }
            if (DateTime.Now <= DateTime.Parse("2017-01-01"))
            {
                if (currencyID != 1 && !overseaclient.number.StartsWith("05"))
                {
                    return Json(new { success = false, msg = "现在的海外客户编号是：" + overseaclient.number + ";外销单的海外客户代码必须是05开头" }, "text/html");
                }
            }
            else { 
                //2017年启用外销单，办事处与海外客户之间的约束
                if (currencyID != 1) {
                    var user = db.User.Single(u => u.id == userId);
                    string errorMsg = utl.ValidateOverSearCustomerAndAgency(user.Department1.name, overseaclient.number);
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        return Json(new { success = false, msg = errorMsg }, "text/html");
                    }
                }
            }
            #endregion

            //根据触发器[Truly_SEOrder_FXGD]改编，贸易类型，客户和海外客户有着制约关系。
            //控制客户编码是以01，02开头的SO订单只能下国内贸易的单除三星外
            //--控制客户是以香港信利的贸易类型不能为国内贸易
            //--控制客户是以香港信利的海外客户必须是以05,06,04开头的单
            #region 根据触发器[Truly_SEOrder_FXGD]改编，贸易类型，客户和海外客户有着制约关系。
            if (buyUnitId != 559 && buyUnitId != 560)
            {
                if (tradeType != 1588 && (customer.number.StartsWith("01.") || customer.number.StartsWith("02.")) && !customer.name.Contains("三星") && !customer.name.Contains("SAMSUNG"))
                {
                    return Json(new { success = false, msg = "国内单贸易类型必须为国内贸易" }, "text/html");
                }
            }
            else
            {
                if (tradeType == 1588)
                {
                    return Json(new { success = false, msg = "国外单贸易类型不能为国内贸易" }, "text/html");
                }
                if (!(new string[] { "03.", "04.", "05." }).Contains(overseaclient.number.Substring(0, 3)))
                {
                    return Json(new { success = false, msg = "国外单海外客户必须选择国外客户" }, "text/html");
                }
            }
            #endregion

            #region 验证表尾说明字段的字符长度，不能超过255个字节。一个汉字包含2个字节。补充说明不能超过1000个字节。
            string description = col.Get("description");
            string further_info = col.Get("further_info");
            description = description.Replace("\r\n", " ").Replace("  ", "");
            further_info = further_info.Replace("\r\n", " ").Replace("  ", "");
            if (Encoding.Default.GetBytes(description).Length > 255)
            {
                return Json(new { success = false, msg = "【说明】字段内容太长，不能超过255个字符，请精简后再保存。注意：1个中文和全角符号算2个字符，1个英文、数字和半角符号算1个字符。" }, "text/html");
            }
            if (Encoding.Default.GetBytes(further_info).Length > 1000)
            {
                return Json(new { success = false, msg = "【补充说明】字段内容太长，不能超过1000个字符，请精简后再保存。注意：1个中文和全角符号算2个字符，1个英文、数字和半角符号算1个字符。" }, "text/html");
            }
            #endregion
            #region 验证营业员比例的合法性,并保存营业员比例
            string salerPercent = col.Get("saler_percent").Trim();
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
                    return Json(new { success = false, msg = "保存失败：以下营业员比例输入不合法：" + salerSplit[0] }, "text/html");
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
                                return Json(new { success = false, msg = "保存失败：以下营业员不可用：" + tpNamest }, "text/html");
                            }
                        }
                    }
                }
                else
                {
                    if (utl.getSalerId(tpName) == null)
                    {
                        utl.writeEventLog(SALEORDER, "保存失败：以下营业员不可用：" + tpName, sysNum, Request, 10);
                        return Json(new { success = false, msg = "保存失败：以下营业员不可用：" + tpName }, "text/html");
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
                    return Json(new { success = false, msg = "保存失败：以下比例超出范围：" + tpPercent }, "text/html");
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
            #region 验证项目编号与客户是否相互对应,467表示无客户编号
            string[] p_project_number = col.Get("p_project_number").Split(',');
            int pn_int;
            foreach (var pn in p_project_number)
            {
                if (string.IsNullOrEmpty(pn))
                {
                    return Json(new { success = false, msg = "保存失败：项目编号不能为空" }, "text/html");
                }
                if (!Int32.TryParse(pn, out pn_int))
                {
                    return Json(new { success = false, msg = "保存失败：项目名称[" + pn + "]不合法，必须在列表中选择" }, "text/html");
                }
                if (pn_int == 467)
                {
                    continue;
                }
                else
                {
                    if (db.VwProjectNumber.Where(v => v.id == pn_int && (v.client_number == customer.number || v.client_number == overseaclient.number)).Count() < 1)
                    {
                        utl.writeEventLog(SALEORDER, "保存失败：项目编号[" + pn + "]不属于当前客户。", sysNum, Request, 10);
                        return Json(new { success = false, msg = "保存失败：项目编号[" + pn + "]不属于当前客户。" }, "text/html");
                    }
                }
            }
            #endregion
            #region 表头其他字段
            string billType = col.Get("bill_type");//1表示销售订单，2表示收费样品单
            string orderDate = col.Get("order_date");
            //string trade_type = col.Get("trade_type");
            //string order_type = col.Get("order_type");
            string sale_way = col.Get("sale_way");
            string agency = col.Get("agency");
            string projectGroup = col.Get("project_group");
            string product_type = col.Get("product_type");
            string pro_type_name = col.Get("pro_type_name");
            string product_use = col.Get("product_use");
            //string currency = col.Get("currency");
            string exchange = col.Get("exchange");
            string clearingWay = col.Get("clearing_way");
            string contractNo = col.Get("contract_no");
            //string buyUnit = col.Get("buy_unit");
            string finalClient = col.Get("final_client");
            string planFirm = col.Get("plan_firm");
            //string oversea_client = col.Get("oversea_client");
            string trade_rule = col.Get("trade_rule");

            string clerk = col.Get("clerk");
            string clerk2 = col.Get("clerk2");
            string clerk3 = col.Get("clerk3");
            //string group1 = col.Get("group1");
            //string group2 = col.Get("group2");
            string percent1 = col.Get("percent1");
            string percent2 = col.Get("percent2");
            string percent3 = col.Get("percent3");
            string charger = col.Get("charger");
            string deliveryPlace = col.Get("delivery_place");
            string overseaPercentage = col.Get("oversea_percentage");
            string backpaperConfirm = col.Get("backpaper_confirm");
            string produceWay = col.Get("produce_way");
            string printTruly = col.Get("print_truly");
            string clientLogo = col.Get("client_logo");
            #endregion

            //保存表单
            Order otp = new Order();
            //单据类别，1:销售订单；2：开模销售合同
            otp.bill_type = short.Parse(billType);
            otp.step_version = 0;//开始创建版本0
            otp.user_id = userId;
            if (!string.IsNullOrEmpty(orderDate))
                otp.order_date = DateTime.Parse(orderDate);
            otp.sys_no = sysNum;
            otp.proc_dep_id = procDep;
            if (!string.IsNullOrWhiteSpace(trade_type))
                otp.trade_type = Int32.Parse(trade_type);
            otp.order_type = orderType;
            //otp.order_type = 40544;//生产单
            if (!string.IsNullOrWhiteSpace(sale_way))
                otp.sale_way = Int32.Parse(sale_way);
            otp.department_id = Int32.Parse(agency);
            otp.project_group = Int32.Parse(projectGroup);
            otp.clerk = Int32.Parse(clerk);
            otp.product_type = Int32.Parse(product_type);
            otp.product_use = product_use;
            otp.currency = currencyID;
            if (!string.IsNullOrEmpty(exchange))
                otp.exchange_rate = double.Parse(exchange);
            otp.clearing_way = Int32.Parse(clearingWay);
            otp.contract_no = contractNo;
            otp.buy_unit = buyUnitId;
            if (!string.IsNullOrEmpty(finalClient))
                otp.final_client = Int32.Parse(finalClient);
            if (!string.IsNullOrEmpty(planFirm))
                otp.plan_firm = Int32.Parse(planFirm);
            otp.oversea_client = oversea_client_id;
            if (!string.IsNullOrEmpty(charger))
                otp.charger = Int32.Parse(charger);
            if (!string.IsNullOrEmpty(trade_rule))
                otp.trade_rule = Int32.Parse(trade_rule);
            otp.delivery_place = deliveryPlace;
            otp.oversea_percentage = string.IsNullOrEmpty(overseaPercentage) ? 0 : decimal.Parse(overseaPercentage);
            if (!string.IsNullOrEmpty(backpaperConfirm))
                otp.backpaper_confirm = Int32.Parse(backpaperConfirm);
            if (!string.IsNullOrEmpty(produceWay))
                otp.produce_way = Int32.Parse(produceWay);
            if (!string.IsNullOrEmpty(printTruly))
                otp.print_truly = Int32.Parse(printTruly);
            if (!string.IsNullOrEmpty(clientLogo))
                otp.client_logo = Int32.Parse(clientLogo);
            otp.description = description;
            otp.further_info = further_info;
            otp.salePs = salerPercent;

            //otp.group1 = group1;
            //otp.group2 = group2;
            otp.percent1 = decimal.Parse(percent1);
            otp.percent2 = string.IsNullOrWhiteSpace(percent2) ? 0 : decimal.Parse(percent2);
            otp.percent3 = string.IsNullOrWhiteSpace(percent3) ? 0 : decimal.Parse(percent3);
            if (!string.IsNullOrWhiteSpace(clerk2))
            {
                otp.clerk2 = Int32.Parse(clerk2);
            }
            if (!string.IsNullOrWhiteSpace(clerk3))
            {
                otp.clerk3 = Int32.Parse(clerk3);
            }
            db.Order.InsertOnSubmit(otp);

            //保存营业员比例
            List<SalerPercentage> lsp = new List<SalerPercentage>();
            foreach (var sp in spList)
            {
                lsp.Add(new SalerPercentage()
                {
                    Order = otp,
                    saler_id = sp.salerId,
                    percentage = sp.percent,
                    card_number = sp.cardNumber,
                    saler_name = sp.salerName,
                    zt = sp.zt
                });
            }
            db.SalerPercentage.InsertAllOnSubmit(lsp);

            //保存订单表体
            string detailMsg = saveOrderDetails(col, otp, pro_type_name);
            if (!string.IsNullOrEmpty(detailMsg))
            {
                utl.writeEventLog(SALEORDER, "表体保存失败,原因:" + detailMsg, sysNum, Request, -1);
                return Json(new { success = false, msg = "表体保存失败:" + detailMsg }, "text/html");
            }

            //如果存在2张以上相同流水号的订单，表示该订单时修改而成，此时要将旧的订单删除。
            var existedBills = db.Order.Where(o => o.sys_no == sysNum && o.step_version == 0).OrderBy(o => o.id);
            if (existedBills.Count() >= 1)
            {
                db.SalerPercentage.DeleteAllOnSubmit(existedBills.First().SalerPercentage);
                db.OrderDetail.DeleteAllOnSubmit(existedBills.First().OrderDetail);
                db.Order.DeleteOnSubmit(existedBills.First());
            }
            try
            {
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                utl.writeEventLog(SALEORDER, "订单保存失败：" + ex.Message.ToString(), sysNum, Request, -1);
                return Json(new { success = false }, "text/html");
            }
            utl.writeEventLog(SALEORDER, "订单保存成功", sysNum, Request);
            return Json(new { success = true, orderId = otp.id }, "text/html");

        }

        //保存订单表体
        public string saveOrderDetails(FormCollection col, Order order, string pro_type)
        {
            string[] p_ids = col.Get("p_id").Split(',');
            string[] p_qty = col.Get("p_qty").Split(',');
            string[] p_quote = col.Get("p_quote").Split(',');
            string[] p_cost = col.Get("p_cost").Split(',');
            string[] p_deal = col.Get("p_deal").Split(',');
            string[] discount_rate = col.Get("p_discount_rate").Split(',');
            string[] unit_price = col.Get("p_unit_price").Split(',');
            string[] p_aux = col.Get("p_aux").Split(',');
            string[] p_fee_rate = col.Get("p_fee_rate").Split(',');
            string[] p_tax_rate = col.Get("p_tax_rate").Split(',');
            string[] p_del_date = col.Get("p_del_date").Split(',');
            string[] p_tar_date = col.Get("p_tar_date").Split(',');
            string[] p_comment = col.Get("p_comment").Split(',');
            string[] p_project_number = col.Get("p_project_number").Split(',');
            double exchange = order.exchange_rate == null ? 0 : (double)order.exchange_rate;
            SomeUtils utl = new SomeUtils();
            //保存表体
            try
            {
                List<OrderDetail> ots = new List<OrderDetail>();
                for (int i = 0; i < p_ids.Count(); i++)
                {
                    var deal_price = string.IsNullOrEmpty(p_deal[i]) ? 0 : decimal.Parse(p_deal[i]);
                    var cost = string.IsNullOrEmpty(p_cost[i]) ? 0 : decimal.Parse(p_cost[i]);
                    var tax_rate = string.IsNullOrEmpty(p_tax_rate[i]) ? 0 : decimal.Parse(p_tax_rate[i]);
                    var qty = decimal.Parse(p_qty[i]);
                    decimal? fee_rate = null, MU = 0, commission = 0, commission_rate = 0;
                    //人民币，税率必须为17；其它，税率必须是0
                    if (order.currency == 1 && tax_rate != 17) {
                        return "第"+(i+1).ToString()+ "行：币别为人民币，税率必须是17";
                    }
                    else if (order.currency != 1 && tax_rate != 0) {
                        return "第"+(i+1).ToString()+ "行：币别不是人民币，税率必须是0";
                    }
                    //有时会出现合同含税价不是0但是不含税单价是0的情况，以下代码处理这一情况。2013-8-19
                    var unitPrice = string.IsNullOrEmpty(unit_price[i]) ? 0 : decimal.Parse(unit_price[i]);
                    var auxUnitPrice = string.IsNullOrEmpty(p_aux[i]) ? 0 : decimal.Parse(p_aux[i]);
                    if (unitPrice == 0 && auxUnitPrice > 0)
                    {
                        unitPrice = auxUnitPrice / (1 + tax_rate / 100);
                    }
                    else if (unitPrice > 0 && auxUnitPrice == 0)
                    {
                        auxUnitPrice = unitPrice * (1 + tax_rate / 100);
                    }
                    if (!string.IsNullOrEmpty(p_fee_rate[i]))
                    {
                        fee_rate = decimal.Parse(p_fee_rate[i]);
                        if (deal_price > 0)
                        {
                            MU = 100 * (1 - ((cost * (1 + tax_rate / 100)) / (deal_price * (decimal)exchange))) - fee_rate;
                            if (MU <= 0)
                            {
                                commission_rate = 0;
                            }
                            else
                            {
                                commission_rate = (decimal)utl.GetCommissionRate(pro_type, (double)MU);
                            }
                            if (pro_type == "CCM" && MU < -6)
                            {
                                commission = deal_price * cost * 0.002m * (decimal)exchange;
                            }
                            else
                            {
                                commission = deal_price * qty * (decimal)exchange * commission_rate / 100;
                            }
                            commission = Decimal.Round((decimal)commission, 2);
                        }
                    }


                    ots.Add(new OrderDetail()
                    {
                        Order = order,
                        entry_id = i + 1,
                        product_id = Int32.Parse(p_ids[i]),
                        qty = decimal.Parse(p_qty[i]),
                        quote_no = p_quote[i],
                        cost = cost,
                        deal_price = deal_price,
                        discount_rate = string.IsNullOrEmpty(discount_rate[i]) ? 0 : decimal.Parse(discount_rate[i]),
                        unit_price = unitPrice,
                        aux_tax_price = auxUnitPrice,
                        fee_rate = fee_rate,
                        tax_rate = tax_rate,
                        MU = MU,
                        commission_rate = commission_rate,
                        commission = commission,
                        delivery_date = string.IsNullOrEmpty(p_del_date[i]) ? null : (DateTime?)(DateTime.Parse(p_del_date[i])),
                        target_date = string.IsNullOrEmpty(p_tar_date[i]) ? null : (DateTime?)(DateTime.Parse(p_tar_date[i])),
                        project_number = string.IsNullOrEmpty(p_project_number[i]) ? 467 : Int32.Parse(p_project_number[i]),//467表示无客户编码
                        comment = p_comment[i]
                    });
                }

                db.OrderDetail.InsertAllOnSubmit(ots);
                //db.SubmitChanges();
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return "";
        }
        //[HttpPost]
        //public JsonResult saveSaleOrderDetails(FormCollection col)
        //{            
        //    string[] p_ids = col.GetValues("p_id[]");
        //    string[] p_qty = col.GetValues("p_qty[]");
        //    string[] p_quote = col.GetValues("p_quote[]");
        //    string[] p_cost = col.GetValues("p_cost[]");
        //    string[] p_deal = col.GetValues("p_deal[]");
        //    string[] discount_rate = col.GetValues("p_discount_rate[]");
        //    string[] unit_price = col.GetValues("p_unit_price[]");
        //    string[] p_aux = col.GetValues("p_aux[]");
        //    string[] p_fee_rate = col.GetValues("p_fee_rate[]");
        //    string[] p_tax_rate = col.GetValues("p_tax_rate[]");
        //    string[] p_del_date = col.GetValues("p_del_date[]");
        //    string[] p_tar_date = col.GetValues("p_tar_date[]");
        //    string[] p_comment = col.GetValues("p_comment[]");
        //    int orderId = Int32.Parse(col.Get("orderId"));
        //    double exchange = double.Parse(col.Get("exchange"));
        //    string pro_type = col.Get("pro_type");
        //    SomeUtils utl = new SomeUtils();
        //    //保存表体
        //    try
        //    {
        //        List<OrderDetail> ots = new List<OrderDetail>();
        //        for (int i = 0; i < p_ids.Count(); i++)
        //        {
        //            var deal_price = string.IsNullOrEmpty(p_deal[i]) ? 0 : decimal.Parse(p_deal[i]);
        //            var cost = string.IsNullOrEmpty(p_cost[i]) ? 0 : decimal.Parse(p_cost[i]);
        //            var tax_rate = string.IsNullOrEmpty(p_tax_rate[i]) ? 0 : decimal.Parse(p_tax_rate[i]);
        //            var qty = decimal.Parse(p_qty[i]);
        //            decimal? fee_rate = null, MU = 0, commission = 0, commission_rate = 0;
        //            if (!string.IsNullOrEmpty(p_fee_rate[i]))
        //            {
        //                fee_rate = decimal.Parse(p_fee_rate[i]);
        //                if (deal_price > 0) {
        //                    MU = 100 * (1 - ((cost * (1 + tax_rate / 100)) / (deal_price * (decimal)exchange))) - fee_rate;
        //                    if (MU <= 0)
        //                    {
        //                        commission_rate = 0;
        //                    }
        //                    else
        //                    {
        //                        commission_rate = (decimal)utl.GetCommissionRate(pro_type, (double)MU);
        //                    }
        //                    if (pro_type == "CCM" && MU < -6)
        //                    {
        //                        commission = deal_price * cost * 0.002m * (decimal)exchange;
        //                    }
        //                    else
        //                    {
        //                        commission = deal_price * qty * (decimal)exchange * commission_rate / 100;
        //                    }
        //                    commission=Decimal.Round((decimal)commission, 2);
        //                }
        //            }


        //            ots.Add(new OrderDetail()
        //            {
        //                order_id = orderId,
        //                entry_id = i + 1,
        //                product_id = Int32.Parse(p_ids[i]),
        //                qty = decimal.Parse(p_qty[i]),
        //                quote_no = p_quote[i],
        //                cost = cost,
        //                deal_price = deal_price,
        //                discount_rate = string.IsNullOrEmpty(discount_rate[i]) ? 0 : decimal.Parse(discount_rate[i]),
        //                unit_price = string.IsNullOrEmpty(unit_price[i]) ? 0 : decimal.Parse(unit_price[i]),
        //                aux_tax_price = string.IsNullOrEmpty(p_aux[i]) ? 0 : decimal.Parse(p_aux[i]),
        //                fee_rate = fee_rate,
        //                tax_rate = tax_rate,
        //                MU=MU,
        //                commission_rate=commission_rate,
        //                commission=commission,
        //                delivery_date = string.IsNullOrEmpty(p_del_date[i]) ? null : (DateTime?)(DateTime.Parse(p_del_date[i])),
        //                target_date = string.IsNullOrEmpty(p_tar_date[i]) ? null : (DateTime?)(DateTime.Parse(p_tar_date[i])),
        //                comment = p_comment[i]
        //            });
        //        }

        //        //如果存在2张以上相同流水号的订单，表示该订单时修改而成，此时要将旧的订单删除。
        //        string sysNum = db.Order.Single(o => o.id == orderId).sys_no;
        //        var existedBills = db.Order.Where(o => o.sys_no == sysNum && o.step_version==0).OrderBy(o=>o.id);
        //        if (existedBills.Count() >= 2)
        //        {
        //            db.SalerPercentage.DeleteAllOnSubmit(existedBills.First().SalerPercentage);
        //            db.OrderDetail.DeleteAllOnSubmit(existedBills.First().OrderDetail);
        //            db.Order.DeleteOnSubmit(existedBills.First());
        //        }
        //        db.OrderDetail.InsertAllOnSubmit(ots);
        //        db.SubmitChanges();
        //    }
        //    catch
        //    {
        //        return Json(new { success = false });
        //    }
        //    return Json(new { success = true });
        //}

        ////加入表体保存失败，则将表头新增的订单删除。
        //[HttpPost]
        //public JsonResult deleteOrderIfFail(int order_id) {
        //    var order = db.Order.Where(o => o.id == order_id);
        //    if (order.Count() > 0) {
        //        db.SalerPercentage.DeleteAllOnSubmit(order.First().SalerPercentage);
        //        db.Order.DeleteAllOnSubmit(order);
        //        db.SubmitChanges();
        //    }
        //    return Json(true);
        //}

        //管理单据
        

        //查看自己订单方法
        //审核结果auditResult: 10(全部），0（正在审核），1（成功），-1（失败）
        public JsonResult CheckOwnSaleOrders(string sysNo, DateTime fromDate, DateTime toDate, int auditResult, string billType)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var result = (from v in db.VwOrder
                         where v.user_id == userId
                         && v.step_version == 0
                         && (v.sys_no.Contains(sysNo) || v.product_model.Contains(sysNo))
                         && v.sys_no.StartsWith(billType)
                         && v.order_date >= fromDate
                         && v.order_date <= toDate
                         select v).ToList();
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
                    bill_id = res.id,
                    apply_status = status,
                    buy_unit = res.buy_unit_name,
                    deal_price = res.deal_price,
                    product_model = res.product_model,
                    product_name = res.product_name,
                    qty = res.qty,
                    sys_no = res.sys_no,
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
        public JsonResult GetSaleOrder(int id)
        {
            var vots = db.VwOrder.Where(v => v.id == id);
            if (vots.Count() < 1)
            {
                return Json(new { success = false });
            }
            vots = vots.OrderBy(v => v.entry_id);
            return Json(new { success = true, list = vots });
        }
                
        [SessionTimeOutFilter()]
        public ActionResult CheckSaleOrder(int id)
        {
            List<VwOrder> vots = db.VwOrder.Where(v => v.id == id).OrderBy(v => v.entry_id).ToList();
            if (vots.Count() < 1)
            {
                return View("Error");
            }
            ViewData["vots"] = vots;
            return View();
        }
        
        //开始提交申请
        [SessionTimeOutFilter()]
        public ActionResult BeginApplySo(string sys_no)
        {
            string pre = sys_no.Substring(0, 2);
            Order ord = db.Order.Single(o => o.sys_no == sys_no);
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);

            string processType = pre;
            //var prs = db.Process.Where(p=>p.is_finish == true && p.begin_time < DateTime.Now && p.end_time > DateTime.Now);
            //Process pr;
            //部门办事处
            var department = db.vwItems.Where(v => v.what == "agency" && v.interid == ord.department_id).First().fname;
            //订单类型
            var orderTypeName = db.vwItems.Where(v => v.what == "order_type" && v.interid == ord.order_type).First().fname;
            if (department.Equals("总裁办")) { 
                //总裁办申请的流程，即陈晓欣申请的，不经过运作中心、事业部和市场部一审
                processType = "SO_2";
            }
            //else if (orderTypeName.Equals("生产单") && !ord.ProduceDep.name.Equals("市场部"))
            //{
            //    //只有生产单且生产部门不是市场部的，要经过运作中心和事业部
            //    processType = "SO_4";
            //}
            else {
                processType = "SO";
            }

            Apply apply = new Apply();
            apply.user_id = userId;
            apply.sys_no = sys_no;
            apply.start_date = DateTime.Now;
            apply.ip = Request.UserHostAddress;
            apply.order_type = pre;
            apply.p_model = db.vwProductInfo.Where(v => v.item_id == ord.OrderDetail.First().product_id).First().item_model;
            db.Apply.InsertOnSubmit(apply);
                        
            //2013-7-18:新增一个测试标志，TestFlag为true表示为测试状态，所有审核人都是自己
            //2013-9-17:新增can_modify字段到ApplyDetails表，表示该审核环节能否修改订单
            bool testFlag = Boolean.Parse(ConfigurationManager.AppSettings["TestFlag"]);
            List<ApplyDetails> ads = new List<ApplyDetails>();

            try
            {
                if (!testFlag)
                {
                    ads = utl.getApplySequence(apply, processType, userId, db.User.Single(u => u.id == userId).Department1.dep_no, ord.proc_dep_id);
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

            //自动生成订单编号，2013-12-31
            //string orderNumber = "";
            //if (DateTime.Now >= DateTime.Parse("2014-1-1"))
            //{
            //    var orderMes = from so in db.Order
            //                   join ag in db.AgencyAndMarket on so.department_id equals ag.agency_id
            //                   join pt in db.vwItems on so.product_type equals pt.interid
            //                   join bt in db.vwItems on so.order_type equals bt.interid
            //                   where so.sys_no == sys_no
            //                   select new
            //                   {
            //                       currencyId = so.currency,
            //                       bigType = ag.big_type,
            //                       productType = pt.fname,
            //                       orderType = bt.fname
            //                   };
            //    try
            //    {
            //        db.getNextOrderNumber("opto", orderMes.First().currencyId == 1 ? "in" : "out",
            //            orderMes.First().orderType == "物料处理" ? "物料处理" : orderMes.First().bigType,
            //            orderMes.First().productType, "", ref orderNumber);
            //        utl.writeEventLog("提交申请", "分配订单编号：" + orderNumber, sys_no, Request);
            //        var ThisOrder = db.Order.Where(o => o.sys_no == sys_no).First();
            //        ThisOrder.order_no = orderNumber;
            //    }
            //    catch (Exception e)
            //    {
            //        utl.writeEventLog("提交申请", "提交失败:" + e.Message, sys_no, Request, 100);
            //        ViewBag.tip = e.Message;
            //        return View("tip");
            //    }
            //}
            try
            {
                db.SubmitChanges();
            }
            catch (Exception e)
            {                
                utl.writeEventLog("提交申请", "不能重复提交申请", sys_no, Request, 100);
                //将订单编号回收
                //if (!string.IsNullOrEmpty(orderNumber))
                //{
                //    try
                //    {
                //        db.put_number_in_recycle(orderNumber);
                //        utl.writeEventLog("提交申请", "提交失败，回收编号", sys_no, Request, 100);
                //    }
                //    catch (Exception ex)
                //    {
                //        utl.writeEventLog("提交申请", "提交失败，回收编号失败:" + ex.Message, sys_no, Request, 100);
                //    }
                //}
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
            cmc.product_type = "CCM";
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
            int? quotationClerkId = null;

            if ("CC".Equals(pre)) {
                processType = "CM_CCM";
                var cc = db.CcmModelContract.Where(c => c.sys_no == sys_no).First();
                projectTeam = cc.project_team;
                quotationClerkId = cc.quotation_clerk_id;
                pModel = cc.product_model;
            }
            else if ("CM".Equals(pre))
            {
                processType = "CM";
                var mc = db.ModelContract.Where(m => m.sys_no == sys_no).First();
                projectTeam = mc.project_team;
                quotationClerkId = mc.quotation_clerk_id;
                busDep = mc.bus_dep;
                pModel = mc.product_model;
            }
            else {
                ViewBag.tip = "流程出错";
                return View("Tip");
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
                    auditorsDic.Add("表单报价员值ID", quotationClerkId);
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

            if (sb.is_free.Equals("免费"))
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
                    auditorsDic.Add("表单报价员值ID", sb.quotation_clerk_id);
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
                //2017-10-16 增加是否有下挂bom的提示
                string busDep = col.Get("bus_dep");
                string productNo = col.Get("product_no");
                var result = db.ExecuteQuery<BomProductModel>("exec [dbo].[getBomInfo] @bus_dep = {0},@mat_number = {1},@is_main = {2}", busDep, productNo, 1).ToList();
                if (result.Count() > 0) {
                    return Json(new { suc = true, msg = "" });
                }
                else {
                    return Json(new { suc = true, msg = "查询不到此产品的bom明细，请催促研发尽快生效bom，否则将影响订料员审批时效。" });
                }
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

        //查看所有类型单据详细信息
        [SessionTimeOutFilter()]
        public ActionResult CheckOrderDetail(int id, string billType)
        {
            string sysNum;
            int newestId;
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var notContractPrice = utl.hasGotPower(userId, Powers.not_contract_price.ToString());
            var notAllPrice = utl.hasGotPower(userId, Powers.not_all_price.ToString());
            ViewData["hiddenPrice"] = notContractPrice || notAllPrice ? "true" : "false";
            ViewData["hiddenAll"] = notAllPrice ? "true" : "false";
            
            switch (billType)
            {
                case "SO":
                case "1":
                    sysNum = db.VwOrder.Where(v => v.id == id).First().sys_no;
                    newestId = db.VwOrder.Where(v => v.sys_no == sysNum).OrderByDescending(m => m.id).First().id;
                    ViewData["vots"] = db.VwOrder.Where(v => v.id == newestId).OrderBy(v => v.entry_id).ToList();
                    if (db.Apply.Where(a => a.sys_no == sysNum).Count() > 0)
                    {
                        ViewData["create_user"] = db.Apply.Where(a => a.sys_no == sysNum).First().User.real_name;
                    }
                    else
                    {
                        ViewData["create_user"] = db.Order.Where(o => o.sys_no == sysNum).First().User.real_name;
                    }
                    utl.writeEventLog(SALEORDER, "查看单据明细", sysNum, Request);
                    //挂起订单信息
                    var blockInfo = db.BlockOrder.Where(b => b.sys_no == sysNum).OrderBy(b => b.step).ToList();
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
                    id = db.Order.Where(o => o.sys_no == ap.sys_no).First().id;
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

    }
}
