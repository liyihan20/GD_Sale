using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_Order.Models
{
    //权限枚举
    public enum Powers
    {
        new_order,//新建销售订单
        edit_order,//变更销售订单
        cancel_order,//取消销售订单
        new_fix,//新建修理单
        edit_fix,//变更修理单
        cancel_fix,//取消修理单
        new_model,//新建开模改模单
        edit_model,//变更开模改模单
        cancel_model,//取消开模改模单
        new_sample,//新建样品单
        edit_sample,//变更样品单
        cancel_sample,//取消样品单
        user_manage,//用户管理
        dep_manage,//部门管理
        group_manage,//组管理
        process_manage,//流程管理
        cur_manage,//汇率管理
        bak_bill,//后台查看订单信息
        agency_rel_auditor,//办事处与一审
        reset_password,//重置密码
        ceo_excel,//总裁办报表
        chk_process,//监控流程时间
        edit_proc_auditor,//生产部门审核人设置
        not_contract_price,//不能查看合同价
        not_all_price,//不能查看所有价格
        chk_dep_bills,  //事业部人员查看本事业部的已审核的订单
        order_number_manage, //订单编号管理
        new_TH, //新增退换货申请
        new_HH, //新增换货申请
        chk_red, //搜索红字单
        new_CH, //新增出货申请
        TH_manage, //后台查询红字申请记录
        print_THQS, //打印签收红字退货单
        uploadQualityReport, //上传品质报告
        new_PJ, //新增客户立项
        Chk_all_PJ, //查看所有的客户立项申请
        depHasAudit_notInK3, //事业部已审未入K3
        chk_pdf_report, //查看pdf报表
        agency_sb_excel, //样品办事处Excel
        test_module, //模块测试
        modify_bill_no, //后台修改单号
        ceo_batch_audit, //总裁办批量审批单据
        new_BL, //新建备料单
    }
}
