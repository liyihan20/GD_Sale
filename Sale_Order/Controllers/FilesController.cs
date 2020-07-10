using CrystalDecisions.CrystalReports.Engine;
using Sale_Order.Controllers;
using Sale_Order.Filter;
using Sale_Order.Models;
using Sale_Order.Models.BLDTTableAdapters;
using Sale_Order.Models.CCDTTableAdapters;
using Sale_Order.Models.CMDTTableAdapters;
using Sale_Order.Models.SBDTTableAdapters;
using Sale_Order.Models.SODTTableAdapters;
using Sale_Order.Models.THDTTableAdapters;
using Sale_Order.Utils;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace TestCRM.Controllers.Sales
{
    public class FilesController : BaseController
    {
        //
        // GET: /Orders/        
        SomeUtils utl = new SomeUtils();
        string model = "报表导出";

        //public ActionResult Index()
        //{
        //    return View();
        //}

        //public FilePathResult writeDoc()
        //{
        //    ApplicationClass app = null;//定义应用程序对象         
        //    Document doc = null;        //定义word文档对象         
        //    Object missing = System.Reflection.Missing.Value;//定义空变量         
        //    Object isReadOnly = false;

        //    try
        //    {
        //        object filePath = Server.MapPath("~/Docs/CCM(最新）光电产品开改模单.doc");
        //        app = new ApplicationClass();
        //        doc = app.Documents.Open(ref filePath, ref missing, ref isReadOnly, ref missing, ref missing,
        //       ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing,
        //       ref missing, ref missing, ref missing, ref missing);
        //        doc.Activate();//激活文档   

        //        Bookmark bm;
        //        object lableName = "number";
        //        bm = doc.Bookmarks.get_Item(ref lableName);//返回标签 
        //        bm.Range.Text = "12345";

        //        lableName = "dropDate";
        //        bm = doc.Bookmarks.get_Item(ref lableName);//返回标签 
        //        bm.Range.Text = DateTime.Now.ToShortDateString();

        //        object savePath = "D:\\b.doc";
        //        Object saveChanges = app.Options.BackgroundSave;//关闭doc文档不提示保存   
        //        doc.SaveAs(ref savePath, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing,
        //            ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);
        //        doc.Close(ref saveChanges, ref missing, ref missing);//关闭文档   

        //        app.Quit(ref missing, ref missing, ref missing);     //关闭应用程序 

        //        return File(savePath.ToString(), "text/plain", "ab.doc");
        //    }
        //    catch{
        //    System.Diagnostics.Process[] MyProcess = System.Diagnostics.Process.GetProcessesByName("WINWORD");
        //    MyProcess[0].Kill();
        //    }
        //    return null;
        //}
        //\\192.168.90.100\d$\Sale
        public ActionResult printReport(string sysNo)
        {
            sysNo = utl.myDecript(sysNo);//解密
            var aps = db.Apply.Where(a => a.sys_no == sysNo);
            if (aps.Count() > 0) {
                if (aps.First().success == false) {
                    ViewBag.tip = "订单被NG，不能导出";
                    utl.writeEventLog(model, "订单被NG，不能导出", sysNo, Request, 10);
                    return View("Tip");
                }
                //else if (aps.First().success == null) {
                //    ViewBag.tip = "订单还未全部审批通过，不能导出";
                //    utl.writeEventLog(model, "订单还未全部审批通过，不能导出", sysNo, Request, 10);
                //    return View("Tip");
                //}
            }
            else
            {
                ViewBag.tip = "单据不存在";
                utl.writeEventLog(model, "单据不存在", sysNo, Request, 1000);
                return View("Tip");
            }

            string crystalFile = "";
            string orderType = "";
            db.getOrderTypeBySysNo(sysNo, ref orderType);
            switch (orderType)
            {
                case "销售订单":
                    crystalFile = "SO_A5_Report.rpt";//正常订单
                    break;
                case "销售订单_物料处理":
                    crystalFile = "SO_mat_A5_Report.rpt";//物料处理单
                    break;
                case "销售订单_免费":
                    crystalFile = "SO_free_A5_Report.rpt";//免费订单
                    break;
                default:
                    ViewBag.tip = "打印模板不存在";
                    return View("Tip");
            }
            utl.writeEventLog(model, "导出" + orderType, sysNo, Request, 0);

            Stream stream = null;
            using (ReportClass rptH = new ReportClass())
            {
                using (SODT normalSoDt = new SODT())
                {
                    using (SOReportTableAdapter SoTa = new SOReportTableAdapter())
                    {
                        SoTa.Fill(normalSoDt.SOReport, sysNo);
                    }
                    rptH.FileName = Server.MapPath("~/Reports/" + crystalFile);
                    rptH.Load();
                    rptH.SetDataSource(normalSoDt);
                }
                stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            }
            return File(stream, "application/pdf"); 
        }

        public ActionResult printTHReport(string sysNo) {            
            string crystalFile = "TH_A4_Report.rpt";

            if (db.Apply.Where(a => a.sys_no == sysNo && a.success == true).Count() < 1)
            {
                ViewBag.tip = "不能打印";
                return View("Tip");
            }

            utl.writeEventLog(model, "导出退货红字报表", sysNo, Request, 0);

            Stream stream = null;
            using (ReportClass rptH = new ReportClass())
            {
                using (THDT redTHDt = new THDT())
                {
                    using (THReportTableAdapter thTa = new THReportTableAdapter())
                    {
                        thTa.Fill(redTHDt.THReport, sysNo);
                    }
                    rptH.FileName = Server.MapPath("~/Reports/" + crystalFile);
                    rptH.Load();
                    rptH.SetDataSource(redTHDt);
                }
                stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            }
            return File(stream, "application/pdf"); 
        }

        //秋海打印签收报表
        public ActionResult printTHQSReport(string sysNo)
        {
            string crystalFile = "THQS_A4_Report.rpt";

            if (db.Apply.Where(a => a.sys_no == sysNo && (a.success==true || a.success==null)).Count() < 1)
            {
                ViewBag.tip = "不能打印";
                return View("Tip");
            }

            utl.writeEventLog(model, "导出退货红字报表", sysNo, Request, 0);

            try
            {                
                Stream stream = null;
                using (ReportClass rptH = new ReportClass())
                {
                    using (THDT redTHDt = new THDT())
                    {
                        using (THReportTableAdapter thTa = new THReportTableAdapter())
                        {
                            thTa.Fill(redTHDt.THReport, sysNo);
                        }
                        rptH.FileName = Server.MapPath("~/Reports/" + crystalFile);
                        rptH.Load();
                        rptH.SetDataSource(redTHDt);
                    }
                    stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                }
                return File(stream, "application/pdf");
            }
            catch (Exception ex)
            {
                utl.writeEventLog(model, "导出退货红字报表出错："+ex.ToString(), sysNo, Request, 0);
                ViewBag.tip = "打印出错";
                return View("Tip");
            }

        }

        //打印研发样品单报表
        [SessionTimeOutFilter()]
        public ActionResult printSBYFReport(string sysNo)
        {
            string crystalFile = "SBYF_A4_Report.rpt";

            if ((from a in db.Apply
                 from ad in a.ApplyDetails
                 where a.sys_no == sysNo
                 && ad.user_id == currentUser.userId
                 select ad).Count() < 1)
            {
                if (!utl.hasGotPower(currentUser.userId, "chk_pdf_report"))
                {
                    utl.writeEventLog(model, "流水号不存在或没有权限查看", sysNo, Request, -100);
                    ViewBag.tip = "流水号不存在或没有权限查看";
                    return View("Tip");
                }
            }

            utl.writeEventLog(model, "导出样品单报表", sysNo, Request, 0);

            //try
            //{
                //int id = db.SampleBill.Single(s => s.sys_no == sysNo).id;
                Stream stream = null;
                using (ReportClass rptH = new ReportClass())
                {
                    using (SBDT sbDt = new SBDT())
                    {
                        using (Sale_sample_billTableAdapter cmTa = new Sale_sample_billTableAdapter())
                        {
                            cmTa.Fill(sbDt.Sale_sample_bill, sysNo);
                        }
                        //设置办事处1、总裁办3，市场部2审核人名字
                        string agencyAuditor = "", ceoAuditor = "", marketAuditor = "", yfAdmin = "", yfManager = "", yfTopLevel = "", quotationAuditor = "",marketManager="";
                        var ad = db.Apply.Where(a => a.sys_no == sysNo).First().ApplyDetails.ToList();
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("办事处") && a.pass == true).Count() > 0)
                        {
                            agencyAuditor = ad.Where(a => a.step == 1 && a.step_name.Contains("办事处") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("总经理") && a.pass == true).Count() > 0) {
                            marketManager = ad.Where(a => a.step == 1 && a.step_name.Contains("总经理") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目经理") && a.pass == true).Count() > 0)
                        {
                            yfManager = ad.Where(a => a.step == 1 && a.step_name.Contains("项目经理") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目管理员") && a.pass == true).Count() > 0)
                        {
                            yfAdmin = ad.Where(a => a.step == 1 && a.step_name.Contains("项目管理员") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目组上级") && a.pass == true).Count() > 0)
                        {
                            yfTopLevel = ad.Where(a => a.step == 1 && a.step_name.Contains("项目组上级") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("报价员") && a.pass == true).Count() > 0)
                        {
                            quotationAuditor = ad.Where(a => a.step == 1 && a.step_name.Contains("报价员") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 2 && a.pass == true).Count() > 0)
                        {
                            marketAuditor = ad.Where(a => a.step == 2 && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 3 && a.pass == true).Count() > 0)
                        {
                            ceoAuditor = ad.Where(a => a.step == 3 && a.pass == true).First().User.real_name;
                        }
                        sbDt.Sample_Bill_Auditor.AddSample_Bill_AuditorRow(agencyAuditor, yfManager, yfTopLevel, yfAdmin, quotationAuditor, marketAuditor, ceoAuditor, marketManager);

                        rptH.FileName = Server.MapPath("~/Reports/" + crystalFile);
                        rptH.Load();
                        rptH.SetDataSource(sbDt);
                    }
                    stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                    //stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.WordForWindows);
                }
                return File(stream, "application/pdf");
                //return File(stream, "application/word");
            //}
            //catch (Exception ex)
            //{
            //    utl.writeEventLog(model, "导出开改模报表出错", sysNo, Request, 0);
            //    ViewBag.tip = "打印出错：" + ex.Message;
            //    return View("Tip");
            //}

        }

        //打印研发CCM开改模单报表
        [SessionTimeOutFilter()]
        public ActionResult printCCYFReport(string sysNo)
        {
            string crystalFile = "CCYF_A4_Report.rpt";

            if ((from a in db.Apply
                 from ad in a.ApplyDetails
                 where a.sys_no == sysNo
                 && ad.user_id == currentUser.userId
                 select ad).Count() < 1)
            {
                if (!utl.hasGotPower(currentUser.userId, "chk_pdf_report"))
                {
                    utl.writeEventLog(model, "流水号不存在或没有权限查看", sysNo, Request, -100);
                    ViewBag.tip = "流水号不存在或没有权限查看";
                    return View("Tip");
                }
            }

            utl.writeEventLog(model, "导出样品单报表", sysNo, Request, 0);

            try
            {
                //int id = db.SampleBill.Single(s => s.sys_no == sysNo).id;
                Stream stream = null;
                using (ReportClass rptH = new ReportClass())
                {
                    using (CCDT ccDt = new CCDT())
                    {
                        using (Sale_ccm_model_contractTableAdapter cmTa = new Sale_ccm_model_contractTableAdapter())
                        {
                            cmTa.Fill(ccDt.Sale_ccm_model_contract, sysNo);
                        }
                        //设置办事处1、总裁办3，市场部2审核人名字
                        string agencyAuditor = "", ceoAuditor = "", marketAuditor = "", yfAdmin = "", yfManager = "", yfTopLevel = "", quotationAuditor = "", marketManager = "";
                        var ad = db.Apply.Where(a => a.sys_no == sysNo).First().ApplyDetails.ToList();
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("办事处") && a.pass == true).Count() > 0)
                        {
                            agencyAuditor = ad.Where(a => a.step == 1 && a.step_name.Contains("办事处") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("总经理") && a.pass == true).Count() > 0) {
                            marketManager = ad.Where(a => a.step == 1 && a.step_name.Contains("总经理") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目经理") && a.pass == true).Count() > 0)
                        {
                            yfManager = ad.Where(a => a.step == 1 && a.step_name.Contains("项目经理") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目管理员") && a.pass == true).Count() > 0)
                        {
                            yfAdmin = ad.Where(a => a.step == 1 && a.step_name.Contains("项目管理员") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目组上级") && a.pass == true).Count() > 0)
                        {
                            yfTopLevel = ad.Where(a => a.step == 1 && a.step_name.Contains("项目组上级") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("报价员") && a.pass == true).Count() > 0)
                        {
                            quotationAuditor = ad.Where(a => a.step == 1 && a.step_name.Contains("报价员") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 2 && a.pass == true).Count() > 0)
                        {
                            marketAuditor = ad.Where(a => a.step == 2 && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 3 && a.pass == true).Count() > 0)
                        {
                            ceoAuditor = ad.Where(a => a.step == 3 && a.pass == true).First().User.real_name;
                        }
                        ccDt.CCM_modelContract_auditor.AddCCM_modelContract_auditorRow(agencyAuditor, yfManager, quotationAuditor, yfAdmin, marketAuditor, ceoAuditor, yfTopLevel, marketManager);

                        rptH.FileName = Server.MapPath("~/Reports/" + crystalFile);
                        rptH.Load();
                        rptH.SetDataSource(ccDt);
                    }
                    stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                    //stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.WordForWindows);
                }
                return File(stream, "application/pdf");
                //return File(stream, "application/word");
            }
            catch (Exception ex)
            {
                utl.writeEventLog(model, "导出开改模报表出错", sysNo, Request, 0);
                ViewBag.tip = "打印出错：" + ex.Message;
                return View("Tip");
            }

        }

        //打印研发开改模单报表
        [SessionTimeOutFilter()]
        public ActionResult printCMYFReport(string sysNo)
        {
            string crystalFile = "CMYF_A4_Report.rpt";

            if ((from a in db.Apply
                 from ad in a.ApplyDetails
                 where a.sys_no == sysNo
                 && ad.user_id == currentUser.userId
                 select ad).Count() < 1)
            {
                if (!utl.hasGotPower(currentUser.userId, "chk_pdf_report"))
                {
                    utl.writeEventLog(model, "流水号不存在或没有权限查看", sysNo, Request, -100);
                    ViewBag.tip = "流水号不存在或没有权限查看";
                    return View("Tip");
                }
            }

            utl.writeEventLog(model, "导出样品单报表", sysNo, Request, 0);

            try
            {
                //int id = db.SampleBill.Single(s => s.sys_no == sysNo).id;
                Stream stream = null;
                using (ReportClass rptH = new ReportClass())
                {
                    using (CMDT cmDt = new CMDT())
                    {
                        using (Sale_model_contractTableAdapter cmTa = new Sale_model_contractTableAdapter())
                        {
                            cmTa.Fill(cmDt.Sale_model_contract, sysNo);
                        }
                        //设置办事处1、总裁办3，市场部2审核人名字
                        string agencyAuditor = "", ceoAuditor = "", marketAuditor = "", yfAdmin = "", yfManager = "", yfTopLevel = "", quotationAuditor = "", busAuditor = "", marketManager = "";
                        var ad = db.Apply.Where(a => a.sys_no == sysNo).First().ApplyDetails.ToList();
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("办事处") && a.pass == true).Count() > 0)
                        {
                            agencyAuditor = ad.Where(a => a.step == 1 && a.step_name.Contains("办事处") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("总经理") && a.pass == true).Count() > 0) {
                            marketManager = ad.Where(a => a.step == 1 && a.step_name.Contains("总经理") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目经理") && a.pass == true).Count() > 0)
                        {
                            yfManager = ad.Where(a => a.step == 1 && a.step_name.Contains("项目经理") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目管理员") && a.pass == true).Count() > 0)
                        {
                            yfAdmin = ad.Where(a => a.step == 1 && a.step_name.Contains("项目管理员") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("项目组上级") && a.pass == true).Count() > 0)
                        {
                            yfTopLevel = ad.Where(a => a.step == 1 && a.step_name.Contains("项目组上级") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("报价员") && a.pass == true).Count() > 0)
                        {
                            quotationAuditor = ad.Where(a => a.step == 1 && a.step_name.Contains("报价员") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 1 && a.step_name.Contains("事业部") && a.pass == true).Count() > 0)
                        {
                            busAuditor = ad.Where(a => a.step == 1 && a.step_name.Contains("事业部") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 2 && a.pass == true).Count() > 0)
                        {
                            marketAuditor = ad.Where(a => a.step == 2 && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step == 3 && a.pass == true).Count() > 0)
                        {
                            ceoAuditor = ad.Where(a => a.step == 3 && a.pass == true).First().User.real_name;
                        }
                        cmDt.ModelContract_auditor.AddModelContract_auditorRow(agencyAuditor, yfManager, yfAdmin, yfTopLevel, quotationAuditor, busAuditor, marketAuditor, ceoAuditor, marketManager);

                        rptH.FileName = Server.MapPath("~/Reports/" + crystalFile);
                        rptH.Load();
                        rptH.SetDataSource(cmDt);
                    }
                    stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                    //stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.WordForWindows);
                }
                return File(stream, "application/pdf");
                //return File(stream, "application/word");
            }
            catch (Exception ex)
            {
                utl.writeEventLog(model, "导出开改模报表出错", sysNo, Request, 0);
                ViewBag.tip = "打印出错：" + ex.Message;
                return View("Tip");
            }

        }

        //打印备料单报表
        [SessionTimeOutFilter()]
        public ActionResult printBLReport(string sysNo)
        {
            string crystalFile = "BL_A4_Report.rpt";            

            utl.writeEventLog(model, "导出备料单报表", sysNo, Request, 0);

            try {
                Stream stream = null;
                using (ReportClass rptH = new ReportClass()) {
                    using (BLDT blDt = new BLDT()) {
                        using (Sale_BLTableAdapter cmTa = new Sale_BLTableAdapter()) {
                            cmTa.Fill(blDt.Sale_BL, sysNo);
                        }
                        string agencyAuditor = "", marketAuditor = "", runCenter = "", billReceiver = "", costController = "", planManager = "", planner = "", order="";
                        var ad = db.Apply.Where(a => a.sys_no == sysNo).First().ApplyDetails.ToList();
                        if (ad.Where(a => a.step_name.Contains("办事处") && a.pass == true).Count() > 0) {
                            agencyAuditor = ad.Where(a => a.step_name.Contains("办事处") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step_name.Contains("总部") && a.pass == true).Count() > 0) {
                            marketAuditor = ad.Where(a => a.step_name.Contains("总部") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step_name.Contains("运作中心") && a.pass == true).Count() > 0) { 
                            runCenter = ad.Where(a => a.step_name.Contains("运作中心") && a.pass == true).First().User.real_name;
                        }
                        //if (ad.Where(a => a.step_name.Contains("接单") && a.pass == true).Count() > 0) {
                        //    billReceiver = ad.Where(a => a.step_name.Contains("接单") && a.pass == true).First().User.real_name;
                        //}
                        if (ad.Where(a => a.step_name.Contains("成控") && a.pass == true).Count() > 0) {
                            costController = ad.Where(a => a.step_name.Contains("成控") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step_name.Contains("计划经理") && a.pass == true).Count() > 0) {
                            planManager = ad.Where(a => a.step_name.Contains("计划经理") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step_name.Contains("计划审批") && a.pass == true).Count() > 0) {
                            planner = ad.Where(a => a.step_name.Contains("计划审批") && a.pass == true).First().User.real_name;
                        }
                        if (ad.Where(a => a.step_name.Contains("订料") && a.pass == true).Count() > 0) {
                            order = String.Join(" ", ad.Where(a => a.step_name.Contains("订料") && a.pass == true).Select(a => a.User.real_name).ToArray());
                        }
                        blDt.BL_Auditors.AddBL_AuditorsRow(agencyAuditor, marketAuditor, runCenter, billReceiver, costController, planManager, planner, order);


                        rptH.FileName = Server.MapPath("~/Reports/" + crystalFile);
                        rptH.Load();
                        rptH.SetDataSource(blDt);
                    }
                    stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                    //stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.WordForWindows);
                }
                return File(stream, "application/pdf");
                //return File(stream, "application/word");
            }
            catch (Exception ex) {
                utl.writeEventLog(model, "导出备料单报表出错", sysNo, Request, 0);
                ViewBag.tip = "打印出错：" + ex.Message;
                return View("Tip");
            }

        }

        //使用webuploader上传文件
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult BeginUpload(HttpPostedFileBase file, string sysNum)
        {
            try {
                string folder = SomeUtils.getOrderPath(sysNum);
                folder = Path.Combine(folder, sysNum);
                if (!Directory.Exists(folder)) {
                    Directory.CreateDirectory(folder);
                }
                if (db.Sale_HC_fileInfo.Where(f => f.file_name == file.FileName && f.sys_no == sysNum).Count() > 0) {
                    return Json(new SimpleResultModel() { suc = false, msg = "上传失败：存在同名文件" });
                }
                file.SaveAs(Path.Combine(folder, file.FileName));
            }
            catch (Exception ex) {
                return Json(new SimpleResultModel() { suc = false, msg = ex.Message });
            }
            return Json(new SimpleResultModel() { suc = true });
        }

        //删除文件
        public JsonResult RemoveUploadedFile(string sysNum, string fileName)
        {
            try {
                var uploadFolder = Path.Combine(SomeUtils.getOrderPath(sysNum), sysNum);
                string fileDirectory = Path.Combine(uploadFolder, fileName);
                if (System.IO.File.Exists(fileDirectory)) {
                    System.IO.File.Delete(fileDirectory);
                }
            }
            catch (Exception ex) {
                return Json(new SimpleResultModel() { suc = false, msg = ex.Message });
            }
            return Json(new SimpleResultModel() { suc = true });
        }

        //下载附件
        public FileStreamResult DownLoadFile(string sysNum, string fileName)
        {
            try {
                var uploadFolder = Path.Combine(SomeUtils.getOrderPath(sysNum), sysNum);
                string fileDirectory = Path.Combine(uploadFolder, fileName);
                if (System.IO.File.Exists(fileDirectory)) {
                    return File(new FileStream(fileDirectory, FileMode.Open), "application/octet-stream", Server.UrlEncode(fileName));
                }
                else {
                    return null;
                }
            }
            catch {
                return null;
            }
        }

        

    }

}
