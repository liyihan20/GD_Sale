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
            _account=account;
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

    }
}