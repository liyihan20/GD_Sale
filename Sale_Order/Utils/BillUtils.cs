using System;
using System.Reflection;

namespace Sale_Order.Utils
{
    public class BillUtils
    {
        //取得单据类型名
        public string GetBillType(string typ)
        {
            object obj = GetBillSvInstance(typ);
            if (obj == null) return "";

            Type type = obj.GetType();
            return (string)type.GetProperty("BillTypeName").GetValue(obj, null);
        }

        //从流水号获得单据类型
        public string GetBillEnType(string sysNo)
        {
            if (sysNo.Contains("TH")) return "TH";//TH类型的在中间
            return sysNo.Substring(0, 2);
        }

        //取得单据实体，返回空对象
        public object GetBillSvInstance(string billType)
        {
            string ty = billType.Length >= 2 ? billType.Substring(0, 2) : "";
            if (!string.IsNullOrEmpty(ty)) {
                Type t = Type.GetType(string.Format("Sale_Order.Services.{0}Sv", ty));
                if (t != null && t.IsClass) {
                    return Activator.CreateInstance(t);
                }
            }
            return null;
        }

        //取得单据实体,并且使用sysNo当作构造函数的参数返回实体对象
        public object GetBillSvInstanceBySysNo(string sysNo)
        {
            string billType = GetBillEnType(sysNo);
            string ty = billType.Length >= 2 ? billType.Substring(0, 2) : "";
            if (!string.IsNullOrEmpty(ty)) {
                Type t = Type.GetType(string.Format("Sale_Order.Services.{0}Sv", ty));
                if (t != null && t.IsClass) {
                    return Activator.CreateInstance(t, new object[] { sysNo });
                }
            }
            return null;
        }

    }
}