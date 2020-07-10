using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;

namespace Sale_Order.Services
{
    public class BaseSv
    {
        public SaleDBDataContext db;

        public BaseSv()
        {
            db = new SaleDBDataContext();
        }
    }
}