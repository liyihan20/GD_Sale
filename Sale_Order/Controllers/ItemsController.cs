using System;
using System.Linq;
using System.Web.Mvc;
using Sale_Order.Models;
using Sale_Order.Filter;
using Sale_Order.Utils;
using System.Collections.Generic;

namespace Sale_Order.Controllers
{
    public class ItemsController : Controller
    {
        //
        // GET: /Items/
        SaleDBDataContext db = new SaleDBDataContext();
        SomeUtils utl = new SomeUtils();

        //获取各个字段的选择列表
        public JsonResult getItems(string what)
        {
            var result = from it in db.vwItems
                         where it.what.Equals(what)
                         orderby it.fname
                         select new
                         {
                             id = it.interid,
                             no=it.fid,
                             name = it.fname
                         };
            return Json(result);
        }

        //获取汇率
        public JsonResult getExchangeRate(int currencyId)
        {
            double? result = 0;
            db.getExchangeRate(currencyId, ref result);
            return Json(result);
        }

        //获取业务员
        public JsonResult getClerks(string q)
        {
            var result = db.getClerk(q, 0);
            return Json(result);
        }

        //验证业务员
        public JsonResult verifyClerk(string q)
        {
            var result = db.getClerk(q, 1).ToList();
            if (result.Count() > 0)
            {
                return Json(new { success = true, itemId = result.First().id });
            }
            else
            {
                return Json(new { success = false });
            }
        }

        //获取营业员
        public JsonResult getSalers(string q)
        {
            var result = db.getSaler(q, 0);
            return Json(result);
        }

        //验证营业员
        public JsonResult verifySaler(string q)
        {
            var result = db.getSaler(q, 1).ToList();
            if (result.Count() > 0)
            {
                return Json(new { success = true, itemId = result.First().id });
            }
            else
            {
                return Json(new { success = false });
            }
        }

        //获取K3用户表，用于制单人
        public JsonResult getK3User()
        {
            var result = from ku in db.vwK3User
                         select new
                         {
                             id = ku.id,
                             name = ku.name
                         };
            return Json(result);
        }

        //获取客户
        public JsonResult getCostomers(string q)
        {
            var result = db.getCostomer(q, 0);
            return Json(result);
        }

        //获取光电和半导体的客户
        public JsonResult getAllCustomers(string q) {
            return Json(db.getAllCostomer(q).OrderBy(a => a.number));
        }

        //验证客户
        public JsonResult verifyCostomer(string q)
        {
            var result = db.getCostomer(q, 1).ToList();
            if (result.Count() > 0)
            {
                return Json(new { success = true, itemId = result.First().id });
            }
            else
            {
                return Json(new { success = false });
            }
        }

        //带币别验证客户
        public JsonResult verifyCostomer2(string q, int currency, string customer_id)
        {
            //首先从id入手
            if (!string.IsNullOrWhiteSpace(customer_id))
            {
                int id;
                if (Int32.TryParse(customer_id, out id))
                {
                    var customer = db.getCostomerById(id).First();
                    if (customer.name.Equals(q))
                    {
                        return Json(new { success = true, itemId = id });
                    }
                }
            }
            //没有id再从名称入手，比如不是通过搜索，而是通过复制
            var result = db.getCostomer(q, 1).ToList();
            if (result.Count() > 0)
            {
                if (result.Count() > 1)
                {
                    if (currency == 1 && result.Where(r => r.number.StartsWith("01")).Count() > 0)
                    {
                        return Json(new { success = true, itemId = result.Where(r => r.number.StartsWith("01")).First().id });
                    }
                    else if (currency != 1 && result.Where(r => r.number.StartsWith("05")).Count() > 0)
                    {
                        return Json(new { success = true, itemId = result.Where(r => r.number.StartsWith("05")).First().id });
                    }
                    else
                    {
                        return Json(new { success = true, itemId = result.First().id });
                    }
                }
                else
                {
                    return Json(new { success = true, itemId = result.First().id });
                }
            }

            return Json(new { success = false });
        }

        //获取产品信息
        public JsonResult getProductInfo(string q)
        {
            var result = (from vp in db.vwProductInfo
                          where vp.item_no.StartsWith(q)
                          || vp.item_name.Contains(q)
                          || vp.item_model.Contains(q)
                          select new
                          {
                              id = vp.item_id,
                              name = vp.item_name,
                              model = vp.item_model,
                              number = vp.item_no,
                              qtyPrecision = vp.qty_decimal,
                              taxRate = vp.tax_rate,
                              pricePrecision = vp.price_decimal,
                              unit_id = vp.unit_id,
                              unit_name = vp.unit_name,
                              unit_number = vp.unit_number
                          }).Take(20);
            return Json(result);
        }

        //获取项目编号（2013-7-19更新）
        //buy_unit:供货客户（用于国内单）；oversea_client：海外客户（用于国外单）
        public JsonResult getProjectNumbers(string buy_unit = null, string oversea_client = null)
        {
            string buy_unit_number = null, oversea_client_number = null;
            if (!string.IsNullOrEmpty(buy_unit))
            {
                buy_unit_number = db.getCostomerById(Int32.Parse(buy_unit)).First().number;
            }
            if (!string.IsNullOrEmpty(oversea_client))
            {
                oversea_client_number = db.getCostomerById(Int32.Parse(oversea_client)).First().number;
            }

            //id为467的表示无指定编号，number为无客户机型
            var result = from v in db.VwProjectNumber
                         where v.id == 467
                         || v.client_number == buy_unit_number
                         || v.client_number == oversea_client_number
                         orderby v.client_number
                         select new
                         {
                             id = v.id,
                             name = v.number,
                             client_name = v.client_name
                         };
            return Json(result);
        }
        
        //获取系统用户
        public JsonResult getSysUsers()
        {
            var users = from u in db.User
                        where !u.is_forbit
                        select new
                        {
                            id = u.id,
                            name = u.real_name
                        };
            return Json(users);
        }

        //下载文件
        [SessionTimeOutFilter()]
        public ActionResult downLoadFile(string sys_no)
        {
            //int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            //SomeUtils utl = new SomeUtils();
            //const string DOWNFILE = "下载附件";
            //userId=1为超级管理员
            //if (userId != 1)
            //{
            //    var order = db.Order.Where(o => o.sys_no == sys_no && o.step_version == 0);
            //    if (order.Count() < 1)
            //    {
            //        utl.writeEventLog(DOWNFILE, "流水号不存在", sys_no, Request,1000);
            //        ViewBag.tip = "此流水号不存在";
            //        return View("Tip");
            //    }
            //    else
            //    {
            //        if (userId != order.First().user_id)
            //        {
            //            var ap = db.Apply.Where(a => a.sys_no == sys_no);
            //            if (ap.Count() < 1)
            //            {
            //                utl.writeEventLog(DOWNFILE, "没有权限下载", sys_no, Request,1000);
            //                ViewBag.tip = "对不起，你没有权限下载附件";
            //                return View("Tip");
            //            }
            //            else
            //            {
            //                if (ap.First().ApplyDetails.Where(ad => ad.user_id == userId).Count() < 1)
            //                {
            //                    utl.writeEventLog(DOWNFILE, "没有权限下载", sys_no, Request,1000);
            //                    ViewBag.tip = "对不起，你没有权限下载附件";
            //                    return View("Tip");
            //                }
            //            }
            //        }
            //    }
            //}            
            ViewData["sys_no"] = sys_no;
            return View();
        }

        //将unicode编码为GBK
        public JsonResult EncodeToGBK(string url)
        {
            string result = System.Web.HttpUtility.UrlEncode(url, System.Text.Encoding.GetEncoding("GB2312"));
            return Json(result);
        }

        //只用于用户管理，因为有外键，所以用id而不是dep_no
        public JsonResult getUserDeps() {
            var deps = from d in db.Department
                       where d.dep_type == "部门"
                       select new
                       {
                           id = d.id,
                           name = d.name
                       };
            return Json(deps);
        }

        //获取普通部门
        public JsonResult getNormalDeps()
        {
            var deps = from d in db.Department
                       where d.dep_type == "部门"
                       select new
                       {
                           id = d.dep_no,
                           name = d.name
                       };
            return Json(deps);
        }

        //获取生产部门
        public JsonResult getProcDeps()
        {
            var deps = from d in db.Department
                       where d.dep_type == "销售事业部"
                       orderby d.name
                       select new
                       {
                           id = d.dep_no,
                           name = d.name
                       };
            return Json(deps);
        }

        //获取退货部门
        public JsonResult getReturnDeps()
        {
            var deps = from d in db.Department
                       where d.dep_type == "退货事业部"
                       orderby d.name
                       select new
                       {
                           id = d.dep_no,
                           name = d.name
                       };
            return Json(deps);
        }

        //根据类型获取部门
        public JsonResult getRelateDeps(string dep_type)
        {
            var deps = from d in db.Department
                       where d.dep_type == dep_type
                       orderby d.dep_no
                       select new
                       {
                           id = d.dep_no,
                           name = d.name
                       };
            return Json(deps);
        }

        //获取所有部门类型
        public JsonResult getDepsType()
        {
            var tp = (from d in db.Department
                      select new { name = d.dep_type }).Distinct();
            return Json(tp);
        }

        //获取所有事业部部门
        //public JsonResult getAllWorkDeps()
        //{
        //    var deps = from d in db.ProduceDep                       
        //               select new
        //               {
        //                   id = d.id,
        //                   name = d.name
        //               };
        //    return Json(deps);
        //}

        //获取自己可以查看的事业部订单
        public JsonResult GetMyCheckingDep(int userID,string orderType) {
            var user = db.User.Single(u => u.id == userID);
            String[] orderTypeDepts = new String[]{};
            if (orderType.Equals("SO")) {
                orderTypeDepts = db.Department.Where(d => d.dep_type == "销售事业部").Select(d => d.name).ToArray();
            }
            else if (orderType.Equals("TH")) {
                orderTypeDepts = db.Department.Where(d => d.dep_type == "退货事业部").Select(d => d.name).ToArray();
            }
            List<ResultModel> list = new List<ResultModel>();
            list.Add(new ResultModel() { value = "all", text = "所有" });
            foreach (var proc in user.can_check_deps.Split(new char[] { ',', '，' })) {
                if (orderTypeDepts.Contains(proc))
                {
                    list.Add(new ResultModel() { value = proc, text = proc });
                }
            }
            return Json(list);
        }

        //摘要：
        //获取用户配置文件，sel为1包括默认模板，为0只包括用户模板
        public JsonResult getExcelTemplate(int sel) {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            if (sel == 1)
            {
                var result = from ex in db.UserExcelTemplate
                             where ex.user_id == 0
                             || ex.user_id == userId
                             select new
                             {
                                 value = ex.id,
                                 label = ex.short_name
                             };
                return Json(result);
            }
            else {
                var result = from ex in db.UserExcelTemplate
                             where ex.user_id == userId
                             select new
                             {
                                 value = ex.id,
                                 label = ex.short_name
                             };
                return Json(result);
            }
        }

        //获取用户模板，放到datagrid
        public JsonResult GetMyTemplate() { 
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var result = db.UserExcelTemplate.Where(u => u.user_id == userId);
            return Json(result);
        }

        //获取默认模板
        public JsonResult getDefaultTemplate() {
            string info = db.UserExcelTemplate.Single(u => u.user_id == 0).seg_info;
            return Json(new { seg = info });
        }

        //新增模板
        public JsonResult addTemplate(FormCollection fc) {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            string shortName = fc.Get("short_name");
            string segInfo = fc.Get("seg_info");
            segInfo = segInfo.Replace('，', ',').Replace(",,", ",");
            if (segInfo.LastIndexOf(',') == segInfo.Length - 1) {
                segInfo = segInfo.Substring(0, segInfo.Length - 1);
            }
            if (db.UserExcelTemplate.Where(u => u.user_id == userId && u.short_name == shortName).Count() > 0)
            {
                return Json(new { success = false, msg = "模板名称不能重复，保存失败" }, "text/html");
            }
            string invalid = validateTemplateSegments(segInfo);
            if (!string.IsNullOrEmpty(invalid)) {
                return Json(new { success = false, msg = invalid }, "text/html");
            }
            var tem = new UserExcelTemplate();
            tem.short_name = shortName;
            tem.seg_info = segInfo;
            tem.bill_type = "SO";
            tem.user_id = userId;
            db.UserExcelTemplate.InsertOnSubmit(tem);
            db.SubmitChanges();

            utl.writeEventLog("用户模板管理", "新增模板:" + shortName, "", Request);
            
            return Json(new { success = true },"text/html");
        }

        //更新模板
        public JsonResult updateTemplate(int id, FormCollection fc)
        {
            string shortName = fc.Get("short_name");
            string segInfo = fc.Get("seg_info");
            segInfo = segInfo.Replace(" ","").Replace('，', ',').Replace(",,", ",");
            if (segInfo.LastIndexOf(',') == segInfo.Length - 1)
            {
                segInfo = segInfo.Substring(0, segInfo.Length - 1);
            }
            string invalid = validateTemplateSegments(segInfo);
            if (!string.IsNullOrEmpty(invalid)) {
                return Json(new { success = false, msg = invalid }, "text/html");
            }
            UserExcelTemplate ue = db.UserExcelTemplate.Single(u => u.id == id);
            ue.short_name = shortName;
            ue.seg_info = segInfo;
            db.SubmitChanges();

            utl.writeEventLog("用户模板管理", "更新模板:" + shortName, "", Request);
            return Json(new { success = true }, "text/html");
        }

        //删除模板
        public JsonResult deleteTemplate(int id) {
            UserExcelTemplate ue = db.UserExcelTemplate.Single(u => u.id == id);
            db.UserExcelTemplate.DeleteOnSubmit(ue);
            db.SubmitChanges();

            utl.writeEventLog("用户模板管理", "删除模板:" + id, "", Request);
            return Json(new { msg = "删除成功" });
        }

        //验证字段是否合法
        public string validateTemplateSegments(string seg_info) {
            string[] segArr = seg_info.Split(new char[] { ',', '，' });
            var segDB = db.ExcelSegments.Select(s => s.cn_name).ToArray();
            foreach (var seg in segArr) {
                if (!segDB.Contains(seg.Trim()))
                {
                    return "保存失败，以下字段不合法："+seg;
                }
            }
            return "";
        }

        //获取订单大类
        public JsonResult getBigType()
        {
            var result = (from or in db.OrderNumber
                         select new
                         {
                             label = or.big_type,
                             value = or.big_type
                         }).Distinct().ToList();
            return Json(result);
        }

        //根据订单大类获取小类
        public JsonResult getProductTypeByBigType(string bigType, string account, string in_out)
        {
            var result = (from or in db.OrderNumber
                          where or.big_type == bigType
                          && or.account==account
                          && or.in_out==in_out
                          select or.product_type).Distinct().ToList();
            List<ResultModel> list = new List<ResultModel>();
            foreach (var re in result) {
                string pt = re.Substring(1, re.Length - 2);
                if (pt.Contains(','))
                {
                    foreach (var p in pt.Split(','))
                    {
                        list.Add(new ResultModel() { value = p, text = p });
                    }
                }
                else {
                    list.Add(new ResultModel() { value = pt, text = pt });
                }
            }
            return Json(list);
        }

        //获取审核步骤
        public JsonResult getProcessStepName() {
            var list = (from a in db.AuditorsRelation
                        select new
                        {
                            id = a.step_value,
                            name = a.step_name
                        }).Distinct().ToList();
            return Json(list);
        }
        
        //获取关联类型
        public JsonResult getProcessRelationType() {
            var list = (from a in db.AuditorsRelation
                        select new
                        {
                            id = a.relate_type,
                            name = a.relate_type
                        }).Distinct().ToList();
            return Json(list); 
        }

        //获取某步骤的审核人
        public JsonResult getStepAuditors(int applyId, int step) {
            var list = (from a in db.ApplyDetails
                        where a.apply_id == applyId
                        && a.step == step
                        select new
                        {
                            detailId = a.id,
                            username = a.User.real_name,
                            department = a.User.Department1.name
                        }).ToList();
            return Json(list);
        }
        
        //获取所有出货组
        public JsonResult getAllCHZAuditors() {

            var res = new List<ResultModel>();

            foreach (var ch in db.AuditorsRelation.Where(a => a.step_name == "RED_事业部出货组").Select(r => r.relate_value).Distinct()) {
                res.Add(new ResultModel() { value = ch.ToString(), text = db.Department.Single(d=>d.dep_no==ch && d.dep_type=="退货出货组").name });
            }

            //foreach (var ch in db.ReturnDeptStepAuditor.Where(r => r.step_name == "出货组").Select(r => r.return_dept).Distinct()) {
            //    res.Add(new ResultModel() { value = ch.ToString(), text = db.ProduceDep.Single(p => p.id == ch).name });
            //}

            res = res.OrderBy(r => r.text).ToList();
            res.Insert(0, new ResultModel() { value = "0", text = "无" });
            return Json(res);
        }
       

        //获取可以上传品质报告的客退编号
        public JsonResult getUnfinishedSysNo() {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            var result = (from ad in db.ApplyDetails
                         where ad.user_id == userId
                         && ad.Apply.success == null
                         && ad.pass == true
                         select new { name = ad.Apply.sys_no }).ToList();
            return Json(result);

        }

        public JsonResult GetDotMatrix() {
            var result = (from d in db.Project_dot_matrix
                         select new { 
                            name=d.dot_matrix,
                            pixel=d.pixel
                         }).ToList();
            return Json(result);
        }

        public JsonResult GetProjectItems(string what) {
            var result = (from i in db.vw_projectItems
                          where i.name == what
                          orderby i.id
                          select new
                          {
                              value = i.value,
                              note = i.note
                          }).ToList();
            return Json(result);
        }

        //获取权限组里面的成员
        public JsonResult GetGroupMembers(string group_name)
        {
            var result = from g in db.Group
                         from gu in g.GroupAndUser
                         where g.name == group_name
                         select new
                         {
                             user_id = gu.user_id,
                             user_name = gu.User.real_name
                         };
            return Json(result);
        }

        //获取项目组和项目经理
        public JsonResult GetPjGroupAndManager()
        {
            var result = (from v in db.vw_auditor_relations
                          where v.step_name == "CM_项目经理审批"
                          orderby v.department_name
                          select new
                          {
                              pjGroup = v.department_name,
                              pjManager = v.auditor_name
                          }).ToList();
            return Json(result);
        }

        //备料单获取bom
        public JsonResult GetBom(string busDep, string productNumber)
        {
            var result = db.ExecuteQuery<BomProductModel>("exec [dbo].[getBomInfo] @bus_dep = {0},@mat_number = {1},@is_main = {2}", busDep, productNumber, 1).ToList();
            return Json(result);
        }
    }
}
