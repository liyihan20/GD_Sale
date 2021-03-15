using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_Order.Models;

namespace Sale_Order.Interfaces
{
    interface IFinishEmail
    {
        string ccToOthers(string sysNo, bool isPass);

        bool needReport(bool isPass);

    }
}