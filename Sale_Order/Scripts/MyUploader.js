(function ($, window) {

    // 初始化插件参数并配置插件各项事件
    function init(item, options) {
        var sysNum = options.sysNum; //流水号，与最后的存放路径有关
        var uploadUser = options.uploadUser; //上传人，用于区分
        var canUpload = options.canUpload || false;  //是否有上传权限，没有上传权限的只可下载        
        
        var target = $(item);

        var html = '<table id="file_tb"></table>';
        target.append(html);

        //创建附件显示table
        $("#file_tb").datagrid({
            fit: true,
            fitColumns: true,
            rownumbers: true,
            singleSelect: true,
            idField: 'file_id',
            url: "../Files/GetUploadedFiles?sysNum="+sysNum,
            columns: [[
                        { field: 'file_id', hidden: true },
                        { field: 'file_name', title: '文件名', width: 320 },
                        { field: 'file_size', title: '大小(KB)', width: 70 },
                        { field: 'uploader', title: '上传人', width: 80 },
                        {
                            field: 'file_status', title: '状态', width: 160, styler: function (value, row, index) {
                                if (value == "已上传") {
                                    return 'color:green;';
                                } else if (value != "上传中...") {
                                    return 'color:red;';
                                }

                            }
                        }
            ]],
            toolbar: [{
                id: "picker",
                text: "上传附件",
                iconCls: 'icon-upload'
            }, {
                id:"remover",
                text: "删除选定附件",
                iconCls: 'icon-delete',
                handler: function () {
                    var row = $('#file_tb').datagrid('getSelected');
                    if (!row) {
                        tip("请先选择一行再操作");
                        return;
                    }
                    if (row.uploader != uploadUser) {
                        tip("只能删除自己上传的附件");
                        return;
                    }
                    $.messager.confirm('操作确认', '确定要删除选定的文件吗?', function (r) {
                        if (r) {
                            $.post("../Files/RemoveUploadedFile", { sysNum: sysNum, fileName: row.file_name }, function (data) {
                                if (data.suc) {
                                    tip("附件移除成功");
                                    if(row.file_id.indexOf("f_")!=0){
                                        //f_开头的fileid是后台自动生成的，不是由插件生成的
                                        uploader.removeFile(row.file_id, true);
                                    }
                                    $('#file_tb').datagrid('deleteRow', $("#file_tb").datagrid("getRowIndex", row));
                                } else {
                                    tip("移除失败：" + data.msg);
                                }
                            });
                        }
                    });
                }
            }, {
                text: "下载选定附件",
                iconCls: 'icon-download',
                handler: function () {
                    var row = $('#file_tb').datagrid('getSelected');
                    if (!row) {
                        tip("请先选择一行再操作");
                        return;
                    }
                    window.open("../Files/DownLoadFile?sysNum=" + sysNum + "&fileName=" + row.file_name);
                }
            }],
            onBeforeLoad: function (data) {
                if (!canUpload) {
                    $("#picker").hide();
                    $("#remover").hide();
                }
            }
        });        

        // 如果没有上传权限，不需要往下执行
        if (!canUpload) return;

        //上传插件默认配置
        var defaults = {
            swf: 'webupload/Uploader.swf',
            server: '../Files/BeginUpload',
            resize: false,
            auto: true,
            pick: "#picker",
            formData: { sysNum: sysNum },
            //fileNumLimit: 3,
            fileSingleSizeLimit: 10 * 1024 * 1024, //默认限制大小10M
            accept: {
                title: '*',
                extensions: '*',
                mimeTypes: '*'
            }
        };
        var opts = $.extend(defaults, options); //可用传进来的配置覆盖默认的
        var uploader = WebUploader.create(defaults); //开始创建上传插件

        //更新文件表格中的上传状态
        var UpdateFileStatus = function(fileId, fileStatus) {
            var thisRowIdx = $("#file_tb").datagrid("getRowIndex", fileId);
            $('#file_tb').datagrid('updateRow', { index: thisRowIdx, row: { file_status: fileStatus } });
            $('#file_tb').datagrid('refreshRow', thisRowIdx);
        }

        //进入上传队列后，需改名和新增一行记录
        uploader.on('fileQueued', function (file) {
            file.name = file.name.replace(/&/g, "_").replace(/ /g, ""); //将文件名中的&转化为_，空格去掉
            $('#file_tb').datagrid('appendRow', {
                file_id: file.id,
                file_name: file.name,
                file_size: (file.size / 1024).toFixed(1),
                file_status: '上传中...',
                uploader: uploadUser
            });
        });                

        uploader.on('uploadSuccess', function (file, res) {
            if (!res.suc) {
                UpdateFileStatus(file.id, res.msg);
            } else {
                UpdateFileStatus(file.id, "已上传");
            }
        });                

        uploader.on('uploadError', function (file) {
            UpdateFileStatus(file.id, '上传出错');
        });

        uploader.on("error", function (type) {
            switch (type) {
                case "Q_TYPE_DENIED":
                    tip("图片格式不正确");
                    break;
                case "F_EXCEED_SIZE":
                    tip("单个文件大小必须少于" + (maxSingleSize / (1024 * 1024)) + "M");
                    break;
                case "F_DUPLICATE":
                    tip("不能重复上传文件");
                    break;
                case "Q_EXCEED_NUM_LIMIT":
                    tip("最多上传文件数量是" + maxNum + "个");
                    break;
                default:
                    tip("上传失败：" + type);
                    break;
            }
        });
        
    }

    //对外主接口
    $.fn.myUploader = function (options) {
        var ele = this;
        init(ele, options);
    }

    //上传插件数量
    $.fn.getMyUploaderFilesNum = function (options) {        
        return $("#file_tb").datagrid("getRows").length || "0";
    }

    //获取某个操作人已上传的文件
    $.fn.GetMyFileInfo = function (sysNum,uploadUser){
        var rows = $("#file_tb").datagrid("getRows");
        var result = [];
        for (var i = 0; i < rows.length; i++) {
            if (rows[i].uploader == uploadUser && rows[i].file_status == "已上传") {
                result.push({file_id:rows[i].file_id, sys_no: sysNum, file_name: rows[i].file_name, uploader: uploadUser });
            }
        }
        return result;
    }

})(jQuery, window);