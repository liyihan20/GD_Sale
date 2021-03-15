using CrystalDecisions.CrystalReports.Engine;
using Newtonsoft.Json;
using Sale_Order.Models;
using Sale_Order.Models.CMDTTableAdapters;
using Sale_Order.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sale_Order.Interfaces;

namespace Sale_Order.Services
{
    public class CMSv : BillSv,IFinishEmail
    {
        private ModelContract bill;

        public CMSv() { }
        public CMSv(string sysNo)
        {
            bill = db.ModelContract.Where(m => m.sys_no == sysNo).FirstOrDefault();
        }

        public override string BillType
        {
            get { return "CM"; }
        }

        public override string BillTypeName
        {
            get { return "开模改模单"; }
        }

        public override string CreateViewName
        {
            get { return "CreateNCM"; }
        }

        public override string CheckViewName
        {
            get { return "CheckNCM"; }
        }

        public override string CheckListViewName
        {
            get { return "CheckNBillList"; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            bill = new ModelContract();
            bill.step_version = 0;
            bill.User = db.User.Where(u => u.id == currentUser.userId).First();
            bill.sys_no = GetNextSysNo(BillType);
            bill.account = account;
            bill.bill_date = DateTime.Now;

            var dep = new K3ItemSv(account).GetK3Items("agency").Where(k => k.fname == currentUser.departmentName).FirstOrDefault();
            if (dep != null) {
                bill.agency_name = dep.fname;
                bill.agency_no = dep.fid;
            }

            return bill;
        }

        public override object GetBill(int stepVersion, int userId)
        {
            bill.step_version = stepVersion;
            return bill;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            ModelContract mc = null;
            mc = JsonConvert.DeserializeObject<ModelContract>(fc.Get("head"));


            //如已提交，则不能再保存
            if (mc.step_version == 0) {
                if (new ApplySv().ApplyHasBegan(mc.sys_no)) {
                    throw new Exception("已提交的单据不能再次保存！");
                }
            }

            //验证客户编码与客户名称是否匹配
            if (!new K3ItemSv(mc.account).IsCustomerNameAndNoMath(mc.customer_name, mc.customer_no)) {
                throw new Exception("购货单位请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(mc.account).IsCustomerNameAndNoMath(mc.plan_firm_name, mc.plan_firm_no)) {
                throw new Exception("方案公司请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(mc.account).IsCustomerNameAndNoMath(mc.oversea_customer_name, mc.oversea_customer_no)) {
                throw new Exception("海外客户请输入后按回车键搜索后在列表中选择");
            }
            if (!new K3ItemSv(mc.account).IsCustomerNameAndNoMath(mc.zz_customer_name, mc.zz_customer_no)) {
                throw new Exception("最终客户请输入后按回车键搜索后在列表中选择");
            }

            if (string.IsNullOrEmpty(mc.clerk_no)) {
                throw new Exception("业务员请输入后按回车键搜索后在列表中选择");
            }

            if (mc.step_version == 0) {
                mc.original_user_id = user.userId;
            }
            else {
                mc.update_user_id = user.userId;
            }

            ModelContract existedBill = db.ModelContract.Where(s => s.sys_no == mc.sys_no).FirstOrDefault();
            if (existedBill != null) {
                mc.original_user_id = existedBill.original_user_id;

                //备份
                BackupData bd = new BackupData();
                bd.sys_no = mc.sys_no;
                bd.main_data = new SomeUtils().ModelToString(existedBill);
                bd.op_date = DateTime.Now;
                bd.user_id = existedBill.update_user_id;
                db.BackupData.InsertOnSubmit(bd);

                //删除旧数据
                db.ModelContract.DeleteOnSubmit(existedBill);
            }

            var apply = db.Apply.Where(a => a.sys_no == mc.sys_no).FirstOrDefault();
            if (apply != null) {
                apply.p_model = mc.product_model;
            }

            db.ModelContract.InsertOnSubmit(mc);
            try {
                db.SubmitChanges();
            }
            catch (Exception ex) {
                throw new Exception("保存失败：" + ex.Message);
            }

            return "";
        }

        public override List<object> GetBillList(SalerSearchParamModel pm, int userId)
        {
            pm.toDate = pm.toDate.AddDays(1);
            pm.sysNo = pm.sysNo ?? "";

            var result = (from o in db.ModelContract
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.original_user_id == userId
                          && (o.sys_no.Contains(pm.sysNo) || o.product_model.Contains(pm.sysNo))
                          && o.bill_date >= pm.fromDate
                          && o.bill_date <= pm.toDate
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          select new OrderModel()
                          {
                              bill_id = o.id,
                              apply_status = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批当中"),
                              buy_unit = o.customer_name,
                              deal_price = o.price ?? o.cost,
                              product_model = o.product_model,
                              product_name = o.product_name,
                              qty = o.qty,
                              sys_no = o.sys_no,
                              apply_date = (Y == null ? "" : Y.start_date.ToString()),
                              account = o.account ?? "光电总部"
                          }).ToList();

            foreach (var re in result.Where(r => !string.IsNullOrEmpty(r.apply_date))) {
                re.apply_date = DateTime.Parse(re.apply_date).ToString("yyyy-MM-dd HH:mm");
            }

            return result.ToList<Object>();
        }

        public override object GetNewBillFromOld()
        {
            bill.step_version = 0;
            bill.sys_no = GetNextSysNo(BillType);
            bill.product_model = null;
            bill.account = bill.account ?? "光电总部";
            bill.bill_date = DateTime.Now;

            return bill;
        }

        public override string GetProcessNo()
        {
            if (bill.bus_dep.Contains("OLED")) return "CM_OLED";
            return "CM";
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            Dictionary<string, int?> auditorsDic = new Dictionary<string, int?>();
            auditorsDic.Add("部门NO", db.User.Single(u => u.id == bill.original_user_id).Department1.dep_no);
            auditorsDic.Add("研发项目组NO", db.Department.Single(d => d.name == bill.project_team && d.dep_type == "研发项目组").dep_no);
            if (bill.quotation_clerk_id != null) {
                auditorsDic.Add("表单报价员值NO", bill.quotation_clerk_id);
            }
            auditorsDic.Add("开模事业部NO", db.Department.Single(d => d.name == bill.bus_dep && d.dep_type == "开模事业部").dep_no);

            return auditorsDic;
        }

        public override string GetProductModel()
        {
            return bill.product_model;
        }

        public override string GetCustomerName()
        {
            return bill.customer_name;
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.ModelContract.Where(m => m.sys_no == sysNo).Count() > 0;
        }

        public override string GetSpecificBillTypeName()
        {
            return BillTypeName + "_" + bill.model_type;
        }

        public override void DoWhenBeforeApply()
        {
            if (bill.model_type.Equals("开模")) {
                //开模要判断规格型号是否重复
                if (db.Apply.Where(a => a.order_type == BillType && a.p_model == bill.product_model && (a.success == null || a.success == true)).Count() > 0) {
                    throw new Exception("存在已提交的重复的开模规格型号，提交失败");
                }
            }
        }

        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            //下单组必须填写编号
            if (stepName.Contains("下单组") && isPass) {
                if (string.IsNullOrWhiteSpace(bill.old_bill_no)) {
                    throw new Exception("下单组审核必须填写订单号");
                }
                else if (db.ModelContract.Where(m => m.sys_no != bill.sys_no && m.old_bill_no == bill.old_bill_no).Count() > 0) {
                    throw new Exception("订单号在下单系统已存在");
                }
            }
        }

        public override void DoWhenFinishAudit(bool isPass)
        {
            if (isPass) {
                MoveToFormalDir(bill.sys_no); //成功结束的申请，将附件移动到正式目录
            }
        }

        /// <summary>
        /// 导出excel数据的模型
        /// </summary>
        public class CMExcelData
        {
            public ModelContract h { get; set; }
            public string auditStatus { get; set; }
        }

        public List<CMExcelData> GetCMSalerExcelData(SalerSearchParamModel pm, int userId)
        {
            pm.toDate = pm.toDate.AddDays(1);
            pm.sysNo = pm.sysNo ?? "";

            var myData = (from o in db.ModelContract
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.original_user_id == userId
                          && (o.sys_no.Contains(pm.sysNo) || o.product_model.Contains(pm.sysNo))
                          && o.bill_date >= pm.fromDate
                          && o.bill_date <= pm.toDate
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          orderby o.bill_date
                          select new CMExcelData()
                          {
                              h = o,
                              auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批当中"),
                          }).ToList();

            return myData;
        }

        public List<CMExcelData> GetCMAuditorExcelData(AuditSearchParamModel pm, int userId)
        {
            DateTime fromDate, toDate;
            if (!DateTime.TryParse(pm.from_date, out fromDate)) {
                fromDate = DateTime.Parse("2010-01-01");
            }
            if (!DateTime.TryParse(pm.to_date, out toDate)) {
                toDate = DateTime.Parse("2099-01-01");
            }
            else {
                toDate = toDate.AddDays(1);
            }

            pm.sysNo = pm.sysNo ?? "";
            pm.proModel = pm.proModel ?? "";

            var myData = (from a in db.Apply
                          from ad in a.ApplyDetails
                          join o in db.ModelContract on a.sys_no equals o.sys_no
                          where ad.user_id == userId
                          && a.order_type == BillType
                          && a.sys_no.Contains(pm.sysNo)
                          && o.product_model.Contains(pm.proModel)
                          && a.start_date >= fromDate
                          && a.start_date <= toDate
                          && (pm.isFinish == 10
                          || (pm.isFinish == 1 && a.success == true)
                          || (pm.isFinish == 0 && a.success == null)
                          || (pm.isFinish == -1 && a.success == false))
                          && (pm.auditResult == 10
                          || (pm.auditResult == 1 && ad.pass == true)
                          || (pm.auditResult == 0 && ad.pass == null
                               && ((ad.countersign == true && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == false).Count() == 0)
                                   || ((ad.countersign == false || ad.countersign == null) && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == true).Count() == 0)
                               )
                             )
                          || (pm.auditResult == -1 && ad.pass == false)
                          )
                          && (ad.step == 1 || a.ApplyDetails.Where(ads => ads.step == ad.step - 1 && ads.pass == true).Count() > 0)
                          orderby a.start_date descending
                          select new CMExcelData()
                          {
                              h = o,
                              auditStatus = (a.success == true ? "申请成功" : a.success == false ? "申请失败" : "审批当中"),
                          }).Distinct().ToList();

            return myData;
        }

        public override void ExportSalerExcle(SalerSearchParamModel pm, int userId)
        {
            throw new NotImplementedException();
        }

        public override void ExportAuditorExcle(AuditSearchParamModel pm, int userId)
        {
            throw new NotImplementedException();
        }

        public override void BeforeRollBack(int step)
        {
            throw new NotImplementedException();
        }

        public override System.IO.Stream PrintReport(string fileFolder)
        {
            if ((from a in db.Apply
                 from ad in a.ApplyDetails
                 where a.sys_no == bill.sys_no
                 && ad.user_id == bill.original_user_id
                 select ad).Count() < 1) {
                if (!new SomeUtils().hasGotPower((int)bill.original_user_id, "chk_pdf_report")) {
                    throw new Exception("流水号不存在或没有权限查看");
                }
            }

            Stream stream = null;
            using (ReportClass rptH = new ReportClass()) {
                using (CMDT cmDt = new CMDT()) {
                    using (Sale_model_contractTableAdapter cmTa = new Sale_model_contractTableAdapter()) {
                        cmTa.Fill(cmDt.Sale_model_contract, bill.sys_no);
                    }
                    //设置办事处1、总裁办3，市场部2审核人名字
                    string agencyAuditor = "", ceoAuditor = "", marketAuditor = "", yfAdmin = "", yfManager = "", yfTopLevel = "",
                        quotationAuditor = "", busAuditor = "", marketManager = "";

                    var auditDetails = (from a in db.Apply
                                        join d in db.ApplyDetails on a.id equals d.apply_id
                                        join u in db.User on d.user_id equals u.id
                                        where a.sys_no == bill.sys_no && d.pass == true
                                        select new
                                        {
                                            d.step,
                                            d.step_name,
                                            u.real_name
                                        }).ToList();

                    agencyAuditor = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("办事处")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    marketAuditor = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("总经理")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    yfManager = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("项目经理")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    yfAdmin = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("项目管理员")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    yfTopLevel = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("项目组上级")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    quotationAuditor = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("报价员")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    busAuditor = auditDetails.Where(ad => ad.step == 1 && ad.step_name.Contains("事业部")).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    marketAuditor = auditDetails.Where(ad => ad.step == 2).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    ceoAuditor = auditDetails.Where(ad => ad.step == 3).Select(ad => ad.real_name).FirstOrDefault() ?? "";
                    
                    cmDt.ModelContract_auditor.AddModelContract_auditorRow(agencyAuditor, yfManager, yfAdmin, yfTopLevel, quotationAuditor, busAuditor, marketAuditor, ceoAuditor, marketManager);

                    rptH.FileName = fileFolder + "CMYF_A4_Report.rpt";
                    rptH.Load();
                    rptH.SetDataSource(cmDt);
                }
                stream = rptH.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            }
            return stream;
        }

        public string ccToOthers(string sysNo, bool isPass)
        {
            var emailList = (from a in db.Apply
                             join d in db.ApplyDetails on a.id equals d.apply_id
                             join u in db.User on d.user_id equals u.id
                             where a.sys_no == sysNo 
                             && d.step==1
                             && d.step_name.Contains("项目") //项目经理、项目管理员与项目组上级
                             && u.email != null 
                             && u.email != ""
                             select u.email).ToList();
            return string.Join(",", emailList);
        }

        public bool needReport(bool isPass)
        {
            return true;
        }

        public override string GetOrderNumber()
        {
            return bill.old_bill_no;
        }
    }
}