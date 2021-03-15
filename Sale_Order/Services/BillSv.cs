using Sale_Order.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sale_Order.Services
{
    public abstract class BillSv : BaseSv
    {
        public string account = "光电总部";

        // --------------------------   抽象属性   -------------------------- //

        /// <summary>
        /// 单据类型EN
        /// </summary>
        public abstract string BillType { get; }

        /// <summary>
        /// 单据类型名
        /// </summary>
        public abstract string BillTypeName { get; }

        /// <summary>
        /// 新建单据视图名
        /// </summary>
        public abstract string CreateViewName { get; }

        /// <summary>
        /// 查看单据视图名
        /// </summary>
        public abstract string CheckViewName { get; }

        /// <summary>
        /// 单据列表视图
        /// </summary>
        public abstract string CheckListViewName { get; }

        // --------------------------   抽象方法   -------------------------- //

        /// <summary>
        /// 单据列表视图
        /// </summary>
        //public virtual string CheckListViewName { get { return CHECK_VIEW_LIST_NAME; } }

        /// <summary>
        /// 新建单据需要返回的object
        /// </summary>
        /// <param name="userId">用户名</param>
        /// <returns>单据对象</returns>
        public abstract object GetNewBill(UserInfo currentUser);

        /// <summary>
        /// 获取单据对象
        /// </summary>
        /// <param name="sysNo">流水号</param>
        /// <returns>单据对象</returns>
        public abstract object GetBill(int stepVersion,int userId);

        /// <summary>
        /// 验证、保存单据
        /// </summary>
        /// <param name="fc">表单值</param>
        /// <param name="userId">用户名</param>
        /// <returns>保存结果</returns>
        public abstract string SaveBill(FormCollection fc, UserInfo user);

        /// <summary>
        /// 取得单据列表
        /// </summary>
        /// <param name="fc">查询表单值</param>
        /// <param name="userId">用户ID</param>
        /// <returns></returns>
        public abstract List<object> GetBillList(SalerSearchParamModel pm, int userId);

        /// <summary>
        /// 以旧单据为模板获取新单据
        /// </summary>
        /// <param name="oldSysNo">旧单据流水号</param>
        /// <returns>新的单据对象</returns>
        public abstract object GetNewBillFromOld();

        /// <summary>
        /// 获取单据对应的流程编号
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public abstract string GetProcessNo();

        /// <summary>
        /// 获取提交申请时，流程需要的审核人对应关系字段
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public abstract Dictionary<string, int?> GetProcessDic();

        /// <summary>
        /// 获取单据规格型号
        /// </summary>
        /// <returns></returns>
        public abstract string GetProductModel();

        /// <summary>
        /// 获取客户名称
        /// </summary>
        /// <returns></returns>
        public abstract string GetCustomerName();

        /// <summary>
        /// 获取单据编号
        /// </summary>
        /// <returns></returns>
        public abstract string GetOrderNumber();

        /// <summary>
        /// 单据是否已保存
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public abstract bool HasOrderSaved(string sysNo);

        /// <summary>
        /// 取得单据详细类型
        /// </summary>
        /// <returns></returns>
        public abstract string GetSpecificBillTypeName();

        /// <summary>
        /// 提交之前需要做的验证
        /// </summary>
        public abstract void DoWhenBeforeApply();

        /// <summary>
        /// 开始审核之前需要做的事情
        /// </summary>
        /// <param name="step">步骤</param>
        /// <param name="stepName">步骤名</param>
        /// <param name="isPass">是否通过</param>
        /// <param name="userId">审核人ID</param>
        public abstract void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId);

        /// <summary>
        /// 申请结束需要做的事情
        /// </summary>
        /// <param name="isPass">是否通过</param>
        public abstract void DoWhenFinishAudit(bool isPass);

        /// <summary>
        /// 营业员导出Excel
        /// </summary>
        /// <param name="pm">搜索参数模型</param>
        public abstract void ExportSalerExcle(SalerSearchParamModel pm, int userId);

        /// <summary>
        /// 审核人导出Excel
        /// </summary>
        /// <param name="pm">搜索参数模型</param>
        public abstract void ExportAuditorExcle(AuditSearchParamModel pm, int userId);

        /// <summary>
        /// 收回之前需要做的判断和操作
        /// </summary>
        /// <param name="sysNo"></param>
        public abstract void BeforeRollBack(int step);
        
        public abstract Stream PrintReport(string fileFolder);

        // --------------------------  虚方法  -------------------------- //

        /// <summary>
        /// 取得下一个流水号
        /// </summary>
        /// <param name="billType"></param>
        /// <returns></returns>
        public virtual string GetNextSysNo(string billType)
        {            
            string dateStr = DateTime.Now.ToString("yyMMdd");
            string result = billType + dateStr;
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == billType && sn.date_string == dateStr);
            if (maxRecord.Count() == 0) {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = billType,
                    date_string = dateStr,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                result += "001";
            }
            else {
                var firstRecord = maxRecord.First();
                firstRecord.max_num = firstRecord.max_num + 1;
                result += string.Format("{0:D3}", firstRecord.max_num);
            }
            db.SubmitChanges();
            return result;
        }

        /// <summary>
        /// 获取下一个编号
        /// </summary>
        /// <param name="prefix">前缀</param>
        /// <param name="dateStr">日期限定符，不需要传空字符串</param>
        /// <param name="digitByte">数字位数</param>
        /// <returns></returns>
        public virtual string GetNextNo(string prefix, string dateStr, int digitByte = 3)
        {
            string result = prefix;
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == prefix && sn.date_string == dateStr);
            if (maxRecord.Count() == 0) {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = prefix,
                    date_string = dateStr,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                result += dateStr + string.Format("{0:D" + digitByte + "}", 1);
            }
            else {
                var firstRecord = maxRecord.First();
                firstRecord.max_num = firstRecord.max_num + 1;
                result += dateStr + string.Format("{0:D" + digitByte + "}", firstRecord.max_num);
            }
            db.SubmitChanges();
            return result;
        }

        /// <summary>
        /// 正式附件路径
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public virtual string GetAttachmentPath(string sysNo)
        {
            string p = ConfigurationManager.AppSettings["AttachmentPath2"];
            string p1 = sysNo.Substring(0, 2);
            string p2 = sysNo.Substring(2, 2);
            string p3 = sysNo.Substring(4, 2);
            string p4 = sysNo.Substring(6, 2);
            string path = Path.Combine(p, p1, p2, p3, p4);
            return path;
        }

        /// <summary>
        /// 将附件从临时目录转移到正式目录
        /// </summary>
        /// <param name="sysNo"></param>
        public virtual void MoveToFormalDir(string sysNo)
        {
            string fileName = sysNo + ".rar";
            string oldPath = Path.Combine(ConfigurationManager.AppSettings["AttachmentPath1"], fileName);
            if (System.IO.File.Exists(oldPath)) {
                FileInfo info = new FileInfo(oldPath);
                string newPath = GetAttachmentPath(sysNo);
                if (!Directory.Exists(newPath)) {
                    Directory.CreateDirectory(newPath);
                }
                string newFile = Path.Combine(newPath, fileName);
                //如果正式目录已存在，则先删除
                if (File.Exists(newFile)) {
                    File.Delete(newFile);
                }
                info.MoveTo(newFile);
            }
        }


    }
}