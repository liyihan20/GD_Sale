using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    //订单临时表列表model
    public class OrderModel
    {
        public OrderModel()
        {
            this.account = "光电总部";
        }
        public int bill_id { get; set; }
        public string sys_no { get; set; }
        public string buy_unit { get; set; }
        public string product_name { get; set; }
        public string product_model { get; set; }
        public decimal? qty { get; set; }
        public decimal? deal_price { get; set; }
        public string apply_status { get; set; }
        public string apply_date { get; set; }
        public string account { get; set; }
    }

    //附件信息model
    public class AttachmentModel
    {
        public string file_name { get; set; }
        public string file_size { get; set; }
        public string upload_time { get; set; }
    }

    //变更订单信息model
    public class BillChangeInfoModel {
        public int update_id { get; set; }
        public string bill_no { get; set; }
        public string apply_date { get; set; }
        public string change_content { get; set; }
        public string apply_status { get; set; }
        public string bill_type { get; set; }
    }

    //营业员比例model
    public class SalerPerModel{
        public int salerId { get; set; }
        public string cardNumber { get; set; }
        public string salerName { get; set; }
        public string zt { get; set; }
        public float percent { get; set; }
    }

    public class SOModel
    {
        public Sale_SO head { get; set; }
        public List<Sale_SO_details> entrys { get; set; }
    }

    public class CHModel
    {
        public CH_bill head { get; set; }
        public List<CHEntrys> entrys { get; set; }
        public List<CH_package> packages { get; set; }
    }

    public class CHEntrys
    {
        public int entry_no { get; set; }
        public int order_id { get; set; }
        public string order_no { get; set; }
        public DateTime? order_date { get; set; }
        public int order_entry_no { get; set; }
        public int item_id { get; set; }
        public string item_number { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public string unit_name { get; set; }
        public string unit_no { get; set; }
        public decimal? order_qty { get; set; }
        public decimal? relate_qty { get; set; }
        public decimal? apply_qty { get; set; }
        public decimal? real_qty { get; set; }
        public decimal inv_qty { get; set; }
        public decimal? can_apply_qty { get; set; }
        public string customer_po { get; set; }
        public string customer_pn { get; set; }
        public string entry_comment { get; set; }
        public decimal? unit_price { get; set; }
        public decimal total_price { get; set; }
        public string contract_no { get; set; }
        public decimal tax_rate { get; set; }
        public string sale_style_name { get; set; }
        public string sale_style_no { get; set; }
        public string clerk_name { get; set; }
        public string clerk_no { get; set; }
        public string department_name { get; set; }
        public string department_no { get; set; }

    }

    public class CHK3Qtys
    {
        public decimal order_qty { get; set; }
        public decimal relate_qty { get; set; }
        public decimal inv_qty { get; set; }
        public decimal? can_apply_qty { get; set; }
    }

    public class CHPakcageModel
    {
        public string comanyName { get; set; }
        public string depName { get; set; }
        public string sysNo { get; set; }
        public string customerName { get; set; }
        public int packId { get; set; }
        public string order_no { get; set; }
        public int order_entry_no { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public int pack_num { get; set; }
        public decimal every_qty { get; set; }
        public string pack_size { get; set; }
        public decimal every_gross_weight { get; set; }
        public decimal every_net_weight { get; set; }
    }

    public class MXModel
    {
        public Sale_MX head { get; set; }
        public List<Sale_MX_details> entrys { get; set; }
    }

    public class CHList4LogParam
    {
        public DateTime beginDate { get; set; }
        public DateTime toDate { get; set; }
        public string stockNo { get; set; }
        public string orderNo { get; set; }
        public string itemModel { get; set; }
        public string customerName { get; set; }
        public string exName { get; set; }
        public string exNo { get; set; }
        public string deliveryAddr { get; set; }
        public string isPrinted { get; set; }
        public string exSeted { get; set; }

    }

    public class CHList4Log
    {
        public string account { get; set; }
        public string bill_type { get; set; }
        public string stock_addr { get; set; }
        public int ch_id { get; set; }
        public string sysNo { get; set; }
        public DateTime? k3_import_date { get; set; }
        public string stock_no { get; set; }
        public int entry_no { get; set; }
        public string order_no { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public string is_print { get; set; }
        public decimal? real_qty { get; set; }
        public decimal order_qty { get; set; }
        public int? pack_num { get; set; }
        public string pack_size { get; set; }
        public decimal? every_qty { get; set; }
        public decimal? every_gross_weight { get; set; }
        public decimal? every_net_weight { get; set; }
        public string ex_name { get; set; }
        public string ex_no { get; set; }
        public decimal? ex_fee { get; set; }
        public string ex_type { get; set; }
        public string ex_comment { get; set; }
        public DateTime log_out_date { get; set; }
        public string log_out_span { get; set; }
        public string unit_name { get; set; }
        public string customer_name { get; set; }
        public string delivery_unit { get; set; }
        public string delivery_addr { get; set; }
    }

    public class CHExInfo
    {
        public string FName { get; set; }
        public string FDelivery { get; set; }
        public string FProvince { get; set; }
        public string FCity { get; set; }
        public decimal? FReed { get; set; }
        public decimal? FDocQty { get; set; }
    }

    public class CHBackSignInfo
    {
        public string account { get; set; }
        public string sys_no { get; set; }
        public string order_no { get; set; }
        public int order_entry_no { get; set; }
        public string k3_stock_no { get; set; }
        public DateTime? k3_import_date { get; set; }
        public DateTime? back_sig_date { get; set; }
        public DateTime? back_sig_acc_date { get; set; }
        public string is_back_sig { get; set; }
        public string customer_name { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public decimal order_qty { get; set; }
        public decimal? real_qty { get; set; }
        public decimal price { get; set; }
        public decimal? total_price { get; set; }
    }

    public class K3SOModel
    {
        public string account { get; set; }
        public string billType { get; set; }
        public string orderNumber { get; set; }
        public int orderId { get; set; }
        public DateTime orderDate { get; set; }
        public string customerName { get; set; }
        public string customerNumber { get; set; }
        public int orderEntry { get; set; }
        public string itemModel { get; set; }
        public string itemName { get; set; }
        public string itemNumber { get; set; }
        public decimal qty { get; set; }
        public decimal relateQty { get; set; }
        public string unitName { get; set; }
        public string empName { get; set; }
        public bool isclosed { get; set; }
        public string saleStyle { get; set; }
        public string contractNo { get; set; }

    }

    public class K3SOStockModel
    {
        public string stockDate { get; set; }
        public string stockNo { get; set; }
        public int stockEntryNo { get; set; }
        public decimal qty { get; set; }
        public string unitName { get; set; }
    }

    public class CHOutBillModel
    {
        public string outNo { get; set; }
        public string stockNos { get; set; }
        public List<CHOutBillEntryModel> entrys { get; set; }
        public string printer { get; set; }
        public int numPerPage { get; set; }
    }

    public class CHOutBillEntryModel
    {
        public string itemModel { get; set; }
        public decimal? qty { get; set; }
    }

}