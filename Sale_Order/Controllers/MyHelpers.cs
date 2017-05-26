using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System.Web.Mvc
{
    public static class MyHelpers
    {
        public static string getLang(this HtmlHelper helper, string key) {
            return key + ",yes";
        }

        public static string getLang(this Controller controler, string key) {
            return key + ",no";
        }

        public static string getRand(this HtmlHelper helper, int bits) {
            if (bits < 0 || bits > 20) {
                return string.Empty;
            }
            string result = "";
            Random ran = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 1; i <= bits; i++) {
                result += ran.Next(1, 10).ToString();
            }
            return result;
        }

        public static string myRound(this HtmlHelper helper, decimal? value,short? dec) {

            if (value == null)
            {
                return "";
            }
            else if (dec != null)
            {
                return Math.Round(((decimal)value), (int)dec).ToString();
            }
            else
                return value.ToString();
        }
    }
}