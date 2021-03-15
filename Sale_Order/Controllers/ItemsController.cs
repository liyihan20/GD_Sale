using System;
using System.Linq;
using System.Web.Mvc;
using Sale_Order.Models;
using Sale_Order.Filter;
using Sale_Order.Utils;
using System.Collections.Generic;
using Sale_Order.Services;

namespace Sale_Order.Controllers
{
    public class ItemsController : BaseController
    {             
        SomeUtils utl = new SomeUtils();

        //获取各个字段的选择列表
        public JsonResult getItems(string what,string account="光电总部")
        {            
            return Json(new K3ItemSv(account).GetK3Items(what).Select(k => new
            {
                no = k.fid,
                name = k.fname
            }).ToList());
        }

        //获取汇率
        public JsonResult getExchangeRate(string currencyNo,string currencyName,string account="光电总部")
        {            
            return Json(new K3ItemSv(account).GetK3ExchangeRate(currencyNo,currencyName));
        }

        //获取业务员
        public JsonResult getClerks(string q, string account = "光电总部")
        {
            return Json(new K3ItemSv(account).GetK3Emp(q).Select(e => new { number = e.emp_card_number, name = e.emp_name }));
        }

        //获取客户
        public JsonResult getCostomers(string q, string account = "光电总部")
        {
            return Json(new K3ItemSv(account).GetK3Customer(q).Select(e => new { number = e.customer_number, name = e.customer_name }));
        }

        public JsonResult GetCustomerInfo(string customerNumber, string account = "光电总部")
        {
            return Json(new K3ItemSv(account).GetK3CustomerInfo(customerNumber));
        }

        //获取产品信息
        public JsonResult getProductInfo(string q, string account = "光电总部")
        {
            return Json(new K3ItemSv(account).GetK3ProductByInfo(q).Select(k => new
            {
                number = k.item_no,
                name = k.item_name,
                model = k.item_model,
                unit_name = k.unit_name,
                id = k.item_id,
                unit_number = k.unit_number
            }).ToList());
        }

        //获取项目编号（2013-7-19更新）
        //buy_unit:供货客户（用于国内单）；oversea_client：海外客户（用于国外单）
        public JsonResult getProjectNumbers(string buy_unit = null, string oversea_client = null)
        {
            //id为467的表示无指定编号，number为无客户机型
            var result = from v in db.VwProjectNumber
                         where v.id == 467
                         || v.client_number == buy_unit
                         || v.client_number == oversea_client
                         orderby v.id
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
            var deps = (from d in db.Department
                        join a in db.AuditorsRelation on d.dep_no equals a.relate_value
                        where d.dep_type == "退货事业部"
                        && a.step_name == "RED_事业部客服"
                        && a.relate_type == "退货事业部"
                        orderby d.name
                        select new
                        {
                            id = d.dep_no,
                            name = d.name
                        }).Distinct().ToList();
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
               

        //获取自己可以查看的事业部订单
        public JsonResult GetMyCheckingDep(string orderType) {            
            String[] orderTypeDepts = new String[]{};
            if (orderType.Equals("SO")) {
                orderTypeDepts = db.Department.Where(d => d.dep_type == "销售事业部").Select(d => d.name).ToArray();
            }
            else if (orderType.Equals("TH")) {
                orderTypeDepts = db.Department.Where(d => d.dep_type == "退货事业部").Select(d => d.name).ToArray();
            }
            List<ResultModel> list = new List<ResultModel>();
            list.Add(new ResultModel() { value = "all", text = "所有" });
            List<string> userCanCheckDeps = db.Sale_user_can_check_deps.Where(s => s.username == currentUser.userName && s.bill_type == orderType).Select(s => s.dep_name).Distinct().ToList();
            if (userCanCheckDeps.Contains("*")) {
                foreach (string proc in orderTypeDepts) {
                    list.Add(new ResultModel() { value = proc, text = proc });
                }
            }
            else {
                foreach (var proc in userCanCheckDeps) {
                    if (orderTypeDepts.Contains(proc)) {
                        list.Add(new ResultModel() { value = proc, text = proc });
                    }
                }
            }
            return Json(list);
        }

        //摘要：
        //获取用户配置文件，sel为1包括默认模板，为0只包括用户模板
        public JsonResult getExcelTemplate(int sel) {
            if (sel == 1)
            {
                var result = from ex in db.UserExcelTemplate
                             where ex.user_id == 0
                             || ex.user_id == currentUser.userId
                             select new
                             {
                                 value = ex.id,
                                 label = ex.short_name
                             };
                return Json(result);
            }
            else {
                var result = from ex in db.UserExcelTemplate
                             where ex.user_id == currentUser.userId
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
            var result = db.UserExcelTemplate.Where(u => u.user_id == currentUser.userId);
            return Json(result);
        }

        //获取默认模板
        public JsonResult getDefaultTemplate() {
            string info = db.UserExcelTemplate.Single(u => u.user_id == 0).seg_info;
            return Json(new { seg = info });
        }

        //新增模板
        public JsonResult addTemplate(FormCollection fc) {            
            string shortName = fc.Get("short_name");
            string segInfo = fc.Get("seg_info");
            segInfo = segInfo.Replace('，', ',').Replace(",,", ",");
            if (segInfo.LastIndexOf(',') == segInfo.Length - 1) {
                segInfo = segInfo.Substring(0, segInfo.Length - 1);
            }
            if (db.UserExcelTemplate.Where(u => u.user_id == currentUser.userId && u.short_name == shortName).Count() > 0)
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
            tem.user_id = currentUser.userId;
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


            res = res.OrderBy(r => r.text).ToList();
            res.Insert(0, new ResultModel() { value = "0", text = "无" });
            return Json(res);
        }
       

        //获取可以上传品质报告的客退编号
        public JsonResult getUnfinishedSysNo() {
            var result = (from ad in db.ApplyDetails
                          join a in db.Apply on ad.apply_id equals a.id
                          where ad.user_id == currentUser.userId
                          && ad.pass == true
                          && a.success == null
                          && a.order_type == "TH"
                          select new { name = ad.Apply.sys_no }).Distinct().ToList();
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
            var result = (from g in db.Group
                         from gu in g.GroupAndUser
                         where g.name == group_name
                         select new ResultModel()
                         {
                             value = gu.user_id.ToString(),
                             text = gu.User.real_name
                         }).ToList();
            result.Insert(0, new ResultModel() { value = "", text = "" });
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
            var result = db.ExecuteQuery<BomProductModel>("exec [dbo].[getBomInfo] @bus_dep = {0},@mat_number = {1}", busDep, productNumber).ToList();
            return Json(result);
        }

        //获取备料单的计划和订料
        public JsonResult GetAuditorsWithStep(string stepName, string depName)
        {
            var result = (from v in db.vw_auditor_relations
                          where v.step_name == stepName
                          && v.department_name.Contains(depName)
                          select new
                          {
                              auditorId = v.auditor_id,
                              auditorName = v.auditor_name
                          }).ToList();
            return Json(result);
        }

        //获取PIS系统的产品用途
        public JsonResult GetPisProductUsage(string model)
        {
            var result = db.vw_modelUsage.Where(m => m.model == model).ToList();
            if (result.Count() > 0) {
                return Json(new { suc = true, usage = result.First().usage });
            }
            else {
                return Json(new { suc = false });
            }
        }
        
        public ActionResult ProjectGroupAuditor()
        {
            ViewData["list"] = db.vw_project_group_auditor.ToList();
            return View();
        }

        //获取k3的客户型号和客户料号 -2018-4-12
        public JsonResult GetK3CustomerModel(int customerId, int productId)
        {
            string customerItemNumber = "", customerItemModel = "";
            var list = db.getK3CustomerModel(customerId, productId).ToList();
            if (list.Count() > 0) {
                customerItemNumber = list.First().FMapNumber;
                customerItemModel = list.First().FMapName;
            }
            return Json(new { customerItemNumber = customerItemNumber, customerItemModel = customerItemModel });
        }

        //更新客户料号和型号到k3系统 -2018-4-12
        public JsonResult SynchroToK3CustomerModel(int? customerId, int? productId, string customerItemNumber, string customerItemModel)
        {
            if (customerId != null && productId != null) {
                try {
                    db.synchroK3CustomerModel(customerId, productId, customerItemNumber, customerItemModel);
                }
                catch (Exception ex) {
                    return Json(new { suc = false, msg = ex.Message });                    
                }
            }
            return Json(new { suc = true });
        }

        public JsonResult GetK3CommissionRate(string proType, double MU, string account)
        {
            return Json(new K3ItemSv(account).GetK3CommissionRate(proType, MU));
        }


    }
}
