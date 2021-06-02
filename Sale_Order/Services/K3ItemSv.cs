using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;

namespace Sale_Order.Services
{
    public class K3ItemSv:BaseSv
    {
        private string _account;

        public K3ItemSv(string account)
        {
            _account = account;
        }

        private string GetDataBaseStr()
        {
            //return "[192.168.100.209].[cjl]"; //光电总部测试帐套
            return db.Sale_companies.Where(s => s.short_name == _account).Select(s => s.database_name).FirstOrDefault();
        }
        
        public List<K3Emp> GetK3Emp(string empInfo)
        {
            return db.ExecuteQuery<K3Emp>("exec dbo.getK3Emp @empInfo={0},@account={1}", empInfo, _account).ToList();
        }

        public List<K3Customer> GetK3Customer(string customerInfo)
        {
            return db.ExecuteQuery<K3Customer>("exec dbo.getK3Customer @customerInfo={0},@account={1}", customerInfo, _account).ToList();
        }

        public List<K3Product> GetK3ProductByModel(string itemModel)
        {
            return db.ExecuteQuery<K3Product>("exec dbo.getK3ProductByModel @itemModel={0},@account={1}", itemModel, _account).ToList();
        }

        public List<K3Product> GetK3ProductByInfo(string itemInfo)
        {
            return db.ExecuteQuery<K3Product>("exec dbo.getK3ProductByInfo @itemInfo={0},@account={1}", itemInfo, _account).ToList();
        }
        public List<K3Items> GetK3Items(string what)
        {
            return db.ExecuteQuery<K3Items>("exec dbo.getK3Items @what={0},@account={1}", what, _account).ToList();
        }
        public decimal GetK3ExchangeRate(string currencyNo, string currencyName)
        {
            return db.getK3ExchangeRate(currencyNo, currencyName, _account).FirstOrDefault().exchange_rate ?? 0m;
        }
        public decimal GetK3CommissionRate(string proType, double MU)
        {
            return db.ExecuteQuery<decimal?>("exec dbo.getK3CommissionRate @proType={0},@MU={1},@account={2}", proType, MU, _account).FirstOrDefault()??0m;            
        }

        public bool IsCustomerNameAndNoMath(string customerName, string customerNumber)
        {
            string sql = string.Format("select count(1) from {0}.dbo.t_Organization where fname = '{1}' and fnumber = '{2}'", GetDataBaseStr(), customerName, customerNumber);
            return db.ExecuteQuery<int>(sql).FirstOrDefault() > 0;
            //return db.ExecuteQuery<bool?>("exec dbo.isCustomerNameAndNoMath @name={0},@no={1},@account={2}", customerName, customerNumber, _account).FirstOrDefault() ?? false;
        }

        public CH_delivery_info GetK3CustomerInfo(string customerNo)
        {
            return db.ExecuteQuery<CH_delivery_info>("exec " + GetDataBaseStr() + ".dbo.CH_GetCustomerInfo @customer_no = {0}", customerNo).FirstOrDefault();
        }

        public List<CHEntrys> GetK3Order4CH(string billType, string customerNo, DateTime fromDate, DateTime toDate, string orderNo, string itemModel)
        {
            var entrys = db.ExecuteQuery<CHEntrys>("exec " + GetDataBaseStr() + ".dbo.CH_GetSOs4Apply @bill_type = {0},@customer_no = {1}, @from_date = {2}, @to_date = {3}, @order_no = {4},@item_model = {5}",
                billType, customerNo, fromDate.ToString(), toDate.ToString(), orderNo, itemModel).ToList();
            entrys.ForEach(e => e.can_apply_qty = e.order_qty - e.relate_qty);

            return entrys;
        }

        public CHK3Qtys GetK3OrderQtys(string billType, int orderId, int orderEntryId)
        {
            var result = db.ExecuteQuery<CHK3Qtys>("exec " + GetDataBaseStr() + ".dbo.CH_GetSOQtys @bill_type = {0},@order_id = {1}, @entry_id = {2}",
                billType, orderId, orderEntryId).First();
            result.can_apply_qty = result.order_qty - result.relate_qty;

            return result;
        }

        public SimpleResultModel GenStockbill(string sysNo)
        {
            return db.ExecuteQuery<SimpleResultModel>("exec " + GetDataBaseStr() + ".dbo.CH_GenBill @sys_no = {0}", sysNo).FirstOrDefault();
        }

        //更新回签日期到k3出库单
        public void UpdateStockbillSignDate(string stockNo, string day)
        {
            db.ExecuteCommand("exec " + GetDataBaseStr() + ".dbo.CH_UpdateSignBackDate @stock_no = {0},@day = {1}", stockNo, day);
        }

        //读取客户对应的K3销售订单
        public List<K3SOModel> GetK3SOList(string billType, string customerNo, DateTime fromDate, DateTime toDate, string orderNo, string itemModel)
        {
            var list = db.ExecuteQuery<K3SOModel>("exec " + GetDataBaseStr() + ".dbo.CH_GetOrderInfo @bill_type = {0},@customer_no = {1}, @from_date = {2}, @to_date = {3}, @order_no = {4},@item_model = {5}",
                billType, customerNo, fromDate.ToString(), toDate.ToString(), orderNo, itemModel).ToList();
            list.ForEach(l => l.account = _account);

            return list;
        }

        //读取销售订单行对应的出货记录
        public List<K3SOStockModel> GetK3SOStockDetail(string billType, int orderId,int entryId)
        {
            return db.ExecuteQuery<K3SOStockModel>("exec " + GetDataBaseStr() + ".dbo.CH_GetStockInfo @bill_type = {0},@order_id = {1}, @entry_id = {2}", billType, orderId, entryId).ToList();
        }

    }
}