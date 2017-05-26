using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_Order.Models;
using Sale_Order.Filter;
using Sale_Order.Utils;

namespace Sale_Order.Controllers
{
    public class AuthorityController : Controller
    {

        SaleDBDataContext db = new SaleDBDataContext();
        SomeUtils utl = new SomeUtils();

        //部门管理
        [SessionTimeOutFilter()]
        public ActionResult Departments()
        {
            return View();
        }

        public JsonResult getDepartments(int page, int rows, string name, string value)
        {
            List<DepList> list = new List<DepList>();
            string charger = "", th_charger = "";
            foreach (var d in db.Department)
            {
                charger = "";
                th_charger = "";
                if (d.charge != null)
                    charger = db.User.Single(u => u.id == d.charge).real_name;
                if (d.th_charge != null)
                    th_charger = db.User.Single(u => u.id == d.th_charge).real_name;
                list.Add(new DepList()
                {
                    id = d.id,
                    name = d.name,
                    charger = charger,
                    th_charger = th_charger,
                    description = d.description,
                    //exam = examInfo
                });
            }
            if (!string.IsNullOrEmpty(value))
            {
                if (name.Equals("name"))
                {
                    list = list.Where(d => d.name.Contains(value)).ToList();
                }
                else if (name.Equals("description"))
                {
                    list = list.Where(d => d.description.Contains(value)).ToList();
                }
                else if (name.Equals("charger"))
                {
                    list = list.Where(d => d.charger.Contains(value) || d.th_charger.Contains(value)).ToList();
                }
            }
            int total = list.Count();
            list = list.Skip((page - 1) * rows).Take(rows).ToList();
            return Json(new { rows = list, total = total });
        }

        public JsonResult saveDepartment(FormCollection col)
        {
            string name = col.Get("name");
            string description = col.Get("description");
            string charger = col.Get("charger");
            string th_charger = col.Get("th_charger");
            int? chargerId = null, th_chargerId = null;
            if (!string.IsNullOrWhiteSpace(charger))
            {
                chargerId = db.User.Where(u => u.real_name == charger).First().id;
            }
            if (!string.IsNullOrEmpty(th_charger))
            {
                th_chargerId = db.User.Where(u => u.real_name == th_charger).First().id;
            }
            try
            {
                db.Department.InsertOnSubmit(new Department()
                {
                    name = name,
                    charge = chargerId,
                    th_charge = th_chargerId,
                    description = description
                });
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");

        }

        public JsonResult updateDepartment(int id, FormCollection col)
        {
            string name = col.Get("name");
            string description = col.Get("description");
            string charger = col.Get("charger");
            string th_charger = col.Get("th_charger");
            int? chargerId = null, th_chargerId = null;
            if (!string.IsNullOrWhiteSpace(charger))
            {
                chargerId = db.User.Where(u => u.real_name == charger).First().id;
            }
            if (!string.IsNullOrWhiteSpace(th_charger))
            {
                th_chargerId = db.User.Where(u => u.real_name == th_charger).First().id;
            }
            try
            {
                var dep = db.Department.Single(d => d.id == id);
                dep.name = name;
                dep.charge = chargerId;
                dep.th_charge = th_chargerId;
                dep.description = description;
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }

        public JsonResult getDepartmentList()
        {
            var list = from d in db.Department
                       select new
                       {
                           id = d.id,
                           name = d.name
                       };
            return Json(list);
        }

        public JsonResult removeDep(int depId)
        {
            var dep = db.Department.Single(d => d.id == depId);
            if (dep.User.Count() > 0)
            {
                return Json(new { suc = false, msg = "该部门下有用户，不能删除" });
            }
            db.Department.DeleteOnSubmit(dep);
            db.SubmitChanges();
            return Json(new { suc = true, msg = "删除成功" });
        }

        //部门&审核人
        //public JsonResult getExaminerByDep(int id)
        //{
        //    List<Examiner> list = new List<Examiner>();
        //    Utils.SomeUtils utl = new Utils.SomeUtils();
        //    string stepName;
        //    foreach (var c in (db.Department.Single(de => de.id == id).Charger.OrderBy(de => de.step)))
        //    {
        //        stepName = utl.getStepName((int)c.step);
        //        list.Add(new Examiner()
        //        {
        //            id = c.id,
        //            step = (int)c.step,
        //            name = stepName,
        //            department = c.User.Department1.name,
        //            examiner = c.User.real_name
        //        });
        //    }
        //    return Json(new { rows = list });
        //}

        //public JsonResult removeChargesInDep(string chas)
        //{
        //    string[] chaIds = chas.Split(',');
        //    try
        //    {
        //        foreach (string cha in chaIds)
        //        {
        //            db.Charger.DeleteOnSubmit(db.Charger.Single(ch => ch.id == Int32.Parse(cha)));
        //        }
        //        db.SubmitChanges();
        //    }
        //    catch
        //    {
        //        return Json(new { msg = "can not remove, a failure occured." }, "text/html");
        //    }
        //    return Json(new { success = true }, "text/html");
        //}

        public JsonResult getPeopleInADepTree(int depId, int step)
        {
            int vid = 0;
            string id = Request.Form.Get("id");
            if (!string.IsNullOrEmpty(id))
            {
                vid = Int32.Parse(id);
            }
            IQueryable result;
            if (vid == 0)
            {
                if (step <= 2) //办事处一二审，只取该办事处以下的人
                {
                    result = from d in db.Department
                             where d.id == depId
                             select new
                             {
                                 id = d.id,
                                 text = d.name,
                                 state = "closed",
                                 iconCls = "icon-home"
                             };
                }
                else
                { //市场部一二审，取市场部的人
                    result = from d in db.Department
                             where d.name.Contains("市场部")
                             select new
                             {
                                 id = d.id,
                                 text = d.name,
                                 state = "closed",
                                 iconCls = "icon-home"
                             };
                }
            }
            else
            {
                result = from u in db.User
                         where u.department == vid
                         select new
                         {
                             id = u.id,
                             text = u.real_name,
                             state = "open",
                             iconCls = "icon-user"
                         };
            }

            return Json(result);
        }

        //public JsonResult addExaminerInDep(int depId, string users, int step)
        //{
        //    string[] userIds = users.Split(',');
        //    try
        //    {
        //        foreach (string uid in userIds)
        //        {
        //            db.Charger.InsertOnSubmit(new Charger()
        //            {
        //                dep_id = depId,
        //                step = step,
        //                user_id = Int32.Parse(uid)
        //            });
        //        }
        //        db.SubmitChanges();
        //    }
        //    catch
        //    {
        //        return Json(new { msg = "can not save, a failure occured." }, "text/html");
        //    }
        //    return Json(new { success = true }, "text/html");
        //}

        //用户管理
        [SessionTimeOutFilter()]
        public ActionResult Users()
        {
            return View();
        }

        public JsonResult getUsers(int page, int rows, string searchValue, string searchName)
        {
            var users = from u in db.User
                        select new
                        {
                            id = u.id,
                            username = u.username,
                            real_name = u.real_name,
                            department = u.Department1.name,
                            job = u.job,
                            email = u.email,
                            is_forbit = u.is_forbit ? "Y" : "N",
                            in_date = ((DateTime)u.in_date).ToShortDateString(),
                            can_check_deps = u.can_check_deps
                        };
            if (!string.IsNullOrEmpty(searchValue))
            {
                if (searchName.Equals("realname"))
                {
                    users = users.Where(u => u.real_name.Contains(searchValue));
                }
                if (searchName.Equals("department"))
                {
                    users = users.Where(u => u.department.Contains(searchValue));
                }
            }
            int total = users.Count();
            users = users.Skip((page - 1) * rows).Take(rows);
            return Json(new { rows = users, total = total });
        }

        public JsonResult saveUser(FormCollection col)
        {
            string chValue = col.Get("is_forbit");
            bool is_forbit = string.IsNullOrEmpty(chValue) ? false : true;
            Department department;
            string dep = col.Get("department");
            string canCheckDeps = col.Get("can_check_deps");
            var deps = db.Department.Where(d => d.name == dep);
            if (deps.Count() > 0)
            {
                department = deps.First();
            }
            else
            {
                return Json(new { success = false, msg = "can not save, department should be selected in the list." }, "text/html");
            }
            if (db.User.Where(u => u.real_name == col.Get("real_name")).Count() > 0)
            {
                return Json(new { success = false, msg = "can not save, username is existed." }, "text/html");
            }
            User user = new User()
            {
                username = col.Get("username"),
                password = utl.getMD5("000000"),
                real_name = col.Get("real_name"),
                Department1 = department,
                email = col.Get("email"),
                job = col.Get("job"),
                is_first_use = true,
                is_forbit = is_forbit,
                in_date = DateTime.Now,
                can_check_deps = canCheckDeps
            };
            try
            {
                db.User.InsertOnSubmit(user);
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { success = false, msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }

        public JsonResult updateUser(int id, FormCollection col)
        {
            string chValue = col.Get("is_forbit");
            bool is_forbit = string.IsNullOrEmpty(chValue) ? false : true;
            Department department;
            string dep = col.Get("department");
            string canCheckDeps = col.Get("can_check_deps");
            var deps = db.Department.Where(d => d.name == dep);
            if (deps.Count() > 0)
            {
                department = deps.First();
            }
            else
            {
                return Json(new { msg = "can not save, department should be selected in the list." }, "text/html");
            }
            try
            {
                User user = db.User.Single(u => u.id == id);
                user.username = col.Get("username");
                user.real_name = col.Get("real_name");
                user.Department1 = department;
                user.email = col.Get("email");
                user.job = col.Get("job");
                user.is_forbit = is_forbit;
                user.can_check_deps = canCheckDeps;
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }

        //组管理
        [SessionTimeOutFilter()]
        public ActionResult Groups()
        {
            return View();
        }

        public JsonResult getGroups()
        {
            var groups = from g in db.Group
                         select new
                         {
                             id = g.id,
                             name = g.name,
                             description = g.description
                         };
            return Json(new { rows = groups });
        }

        public JsonResult saveGroup(FormCollection col)
        {
            string name = col.Get("name");
            string description = col.Get("description");
            try
            {
                db.Group.InsertOnSubmit(new Group()
                {
                    name = name,
                    description = description
                });
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }

        public JsonResult updateGroup(int id, FormCollection col)
        {
            string name = col.Get("name");
            string description = col.Get("description");
            try
            {
                Group g = db.Group.Single(gr => gr.id == id);
                g.name = name;
                g.description = description;
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }

        public JsonResult getPeopleByGroup(int id)
        {
            var people = from gu in db.GroupAndUser
                         where gu.group_id == id
                         select new
                         {
                             id = gu.id,
                             name = gu.User.real_name,
                             department = gu.User.Department1.name
                         };
            return Json(new { rows = people });
        }

        public JsonResult getPeopleAndDepartmentTree()
        {
            int vid = 0;
            string id = Request.Form.Get("id");
            if (!string.IsNullOrEmpty(id))
            {
                vid = Int32.Parse(id);
            }
            IQueryable result;
            if (vid == 0)
            {
                result = from d in db.Department
                         where d.User.Count() > 0
                         select new
                         {
                             id = d.id,
                             text = d.name,
                             state = "closed",
                             iconCls = "icon-home"
                         };
            }
            else
            {
                result = from u in db.User
                         where u.department == vid
                         select new
                         {
                             id = u.id,
                             text = u.real_name,
                             state = "open",
                             iconCls = "icon-user"
                         };
            }

            return Json(result);
        }

        public JsonResult addUsersInGroup(int groupId, string users)
        {
            string[] userArr = users.Split(',');
            List<GroupAndUser> list = new List<GroupAndUser>();
            try
            {
                foreach (string user in userArr)
                {
                    int userid = Int32.Parse(user);
                    if (db.GroupAndUser.Where(gu => gu.group_id == groupId && gu.user_id == userid).Count() < 1)
                    {
                        list.Add(new GroupAndUser()
                        {
                            user_id = userid,
                            group_id = groupId
                        });
                    }
                }
                db.GroupAndUser.InsertAllOnSubmit(list);
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }

        public JsonResult removeUsersInGroup(int groupAndUserId)
        {
            try
            {
                GroupAndUser gau = db.GroupAndUser.Single(g => g.id == groupAndUserId);
                db.GroupAndUser.DeleteOnSubmit(gau);
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }

        public JsonResult getAuthByGroup(int id)
        {
            var auths = from ga in db.GroupAndAuth
                        where ga.group_id == id
                        select new
                        {
                            id = ga.id,
                            name = ga.Authority.name,
                            description = ga.Authority.description
                        };
            return Json(new { rows = auths });
        }

        public JsonResult getAllAthority()
        {
            var auth = from a in db.Authority
                       select new
                       {
                           id = a.id,
                           name = a.name,
                           description = a.description
                       };
            return Json(new { rows = auth });
        }

        public JsonResult addAuthInGroup(int groupId, string auth)
        {
            string[] auths = auth.Split(',');
            List<GroupAndAuth> list = new List<GroupAndAuth>();
            try
            {
                foreach (string au in auths)
                {
                    int auv = Int32.Parse(au);
                    if (db.GroupAndAuth.Where(g => g.group_id == groupId && g.auth_id == auv).Count() < 1)
                    {
                        list.Add(new GroupAndAuth()
                        {
                            auth_id = auv,
                            group_id = groupId
                        });
                    }
                }
                db.GroupAndAuth.InsertAllOnSubmit(list);
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }

            return Json(new { success = true }, "text/html");
        }

        public JsonResult removeAuthInGroup(int groupAndAuthId)
        {
            try
            {
                db.GroupAndAuth.DeleteOnSubmit(db.GroupAndAuth.Single(g => g.id == groupAndAuthId));
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { msg = "can not save, a failure occured." }, "text/html");
            }
            return Json(new { success = true }, "text/html");
        }

        //流程管理
        [SessionTimeOutFilter()]
        public ActionResult Processes()
        {
            return View();
        }

        public JsonResult getProcesses()
        {
            SomeUtils utl = new SomeUtils();
            var result = (from p in db.Process
                          orderby p.is_finish, p.bill_type
                          select new
                          {
                              id = p.id,
                              order_type = p.bill_type,
                              info = p.info,
                              order_type_name = utl.getBillType(p.bill_type),
                              modify_time = DateTime.Parse(p.modify_time.ToString()).ToString("yyyy-MM-dd HH:mm"),
                              is_finish = p.is_finish == true ? "已启用" : "未启用",
                              begin_time = DateTime.Parse(p.begin_time.ToString()).ToString("yyyy-MM-dd"),
                              end_time = DateTime.Parse(p.end_time.ToString()).ToString("yyyy-MM-dd")
                          }).ToList();
            return Json(result);
        }

        //启用-禁用流程
        public JsonResult toggleProc(int id)
        {
            Process pro = db.Process.Single(p => p.id == id);
            if (pro.is_finish == false)
            {
                if (pro.ProcessDetail.Count() == 0)
                {
                    return Json(new { suc = false, msg = "还没有设置流程，不能启用" });
                }
                for (int i = 1; i <= pro.ProcessDetail.Max(p => p.step); i++)
                {
                    if (pro.ProcessDetail.Where(p => p.step == i).Count() == 0)
                    {
                        return Json(new { suc = false, msg = "步骤" + i.ToString() + "缺失，不能启用" });
                    }
                }
                if (pro.ProcessDetail.Where(p => p.step_type == 0 && p.user_id == null).Count() > 0)
                {
                    return Json(new { suc = false, msg = "存在步骤类型为固定人员审核但是没有设置审核人的情况，不能启用。" });
                }
                pro.is_finish = true;
            }
            else
                pro.is_finish = false;
            db.SubmitChanges();
            return Json(new { suc = true });
        }

        //获取流程明细
        public JsonResult getProDets(int id)
        {
            var pro = db.Process.Single(p => p.id == id);
            var dets = from d in pro.ProcessDetail
                       orderby d.step
                       select new
                       {
                           id = d.id,
                           step = d.step,
                           step_name = d.step_name,
                           step_type = d.step_type,
                           auditor = d.user_id != null ? d.User.real_name : "",
                           can_modify = d.can_modify == true ? "Y" : "N",
                           can_be_null = d.can_be_null == true ? "Y" : "N",
                           can_select_next = d.can_select_next == true ? "Y" : "N",
                           is_countersign = d.countersign == true ? "Y" : "N",
                       };
            return Json(dets, "text/html");
        }

        //保存流程的修改
        public JsonResult saveProcess(FormCollection fcl)
        {
            int proId = Int32.Parse(fcl.Get("pro_id"));
            string proType = fcl.Get("proc_type");
            string info = fcl.Get("info");
            DateTime begin_time = string.IsNullOrEmpty(fcl.Get("begin_time")) ? DateTime.Parse("2000-1-1") : DateTime.Parse(fcl.Get("begin_time"));
            DateTime end_time = string.IsNullOrEmpty(fcl.Get("end_time")) ? DateTime.Parse("2050-1-1") : DateTime.Parse(fcl.Get("end_time"));
            string[] step = fcl.GetValues("step[]");
            string[] stepName = fcl.GetValues("step_name[]");
            string[] stepType = fcl.GetValues("step_type[]");
            string[] auditor = fcl.GetValues("auditor[]");
            string[] canModify = fcl.GetValues("can_modify[]");
            string[] canBeNull = fcl.GetValues("can_be_null[]");
            string[] canSelectNext = fcl.GetValues("can_select_next[]");
            string[] countersign = fcl.GetValues("is_countersign[]");

            Process pro = new Process();
            pro.bill_type = proType;
            pro.modify_time = DateTime.Now;
            pro.is_finish = true;
            pro.begin_time = begin_time;
            pro.end_time = end_time;
            pro.info = info;
            db.Process.InsertOnSubmit(pro);

            List<ProcessDetail> details = new List<ProcessDetail>();
            for (int i = 0; i < step.Count(); i++)
            {
                ProcessDetail pd = new ProcessDetail();
                pd.Process = pro;
                pd.step = Int32.Parse(step[i]);
                pd.step_name = stepName[i];
                pd.step_type = Int32.Parse(stepType[i]);
                if (!string.IsNullOrEmpty(auditor[i]))
                {
                    pd.User = db.User.Where(u => u.real_name == auditor[i]).First();
                }
                pd.can_modify = canModify[i].Equals("Y") ? true : false;
                pd.can_be_null = canBeNull[i].Equals("Y") ? true : false;
                pd.can_select_next = canSelectNext[i].Equals("Y") ? true : false;
                pd.countersign = countersign[i].Equals("Y") ? true : false;
                details.Add(pd);
            }
            db.ProcessDetail.InsertAllOnSubmit(details);

            if (proId > 0)
            {
                //表示更新的，要将旧的流程信息删除
                db.ProcessDetail.DeleteAllOnSubmit(db.Process.Single(p => p.id == proId).ProcessDetail);
                db.Process.DeleteOnSubmit(db.Process.Single(p => p.id == proId));
            }

            try
            {
                db.SubmitChanges();
            }
            catch (Exception)
            {
                return Json(new { suc = false, msg = "保存流程失败" });
            }

            return Json(new { suc = true });
        }

        //汇率管理
        [SessionTimeOutFilter()]
        public ActionResult Currency()
        {
            return View();
        }

        public JsonResult getCurrency()
        {
            var result = from c in db.Currency

                         select new
                         {
                             id = c.id,
                             currency = c.currency,
                             exchange = decimal.Round((decimal)c.exchange, 4),
                             begin_date = ((DateTime)c.begin_date).ToShortDateString(),
                             end_date = ((DateTime)c.end_date).ToShortDateString()
                         };
            return Json(result);
        }

        public JsonResult addCurrency(FormCollection fc)
        {
            string curencyName = fc.Get("currency");
            decimal exchange = decimal.Parse(fc.Get("exchange"));
            DateTime beginDate = DateTime.Parse(fc.Get("begin_date"));
            DateTime endDate = DateTime.Parse(fc.Get("end_date"));
            try
            {
                db.Currency.InsertOnSubmit(new Models.Currency()
                {
                    currency = curencyName,
                    exchange = exchange,
                    begin_date = beginDate,
                    end_date = endDate
                });
                db.SubmitChanges();
            }
            catch (Exception)
            {
                return Json(new { success = true, msg = "新增条目失败" }, "text/html");
            }

            return Json(new { success = true }, "text/html");
        }

        public JsonResult updateCurrency(int id, FormCollection fc)
        {
            string curencyName = fc.Get("currency");
            decimal exchange = decimal.Parse(fc.Get("exchange"));
            DateTime beginDate = DateTime.Parse(fc.Get("begin_date"));
            DateTime endDate = DateTime.Parse(fc.Get("end_date"));
            try
            {
                Currency cur = db.Currency.Single(c => c.id == id);
                cur.currency = curencyName;
                cur.exchange = exchange;
                cur.begin_date = beginDate;
                cur.end_date = endDate;
                db.SubmitChanges();
            }
            catch (Exception)
            {
                return Json(new { success = true, msg = "更新条目失败" }, "text/html");
            }

            return Json(new { success = true }, "text/html");
        }

        public JsonResult removeCurrency(int id)
        {
            try
            {
                db.Currency.DeleteOnSubmit(db.Currency.Single(c => c.id == id));
                db.SubmitChanges();
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }

            return Json(new { success = true });

        }

        //后台查看订单
        [SessionTimeOutFilter()]
        public ActionResult CheckBillsBackground()
        {
            return BackgroundSearchBills("", "", DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"), "", "10");
        }

        [HttpPost]
        public ActionResult BackgroundSearchBills(string sys_no, string saler, string fromDate, string toDate, string auditResult)
        {
            ViewData["sys_no"] = sys_no;
            ViewData["saler"] = saler;
            ViewData["from_date"] = fromDate;
            ViewData["to_date"] = toDate;
            ViewData["audit_result"] = auditResult;
            fromDate = string.IsNullOrWhiteSpace(fromDate) ? "1901-1-1" : fromDate;
            toDate = string.IsNullOrWhiteSpace(toDate) ? "2099-9-9" : toDate;
            var res = from ap in db.Apply
                      where ap.sys_no.Contains(sys_no)
                      && ap.User.real_name.Contains(saler)
                      && ap.start_date >= DateTime.Parse(fromDate)
                      && ap.start_date <= DateTime.Parse(toDate)
                      select ap;
            switch (auditResult)
            {
                case "1":
                    res = res.Where(r => r.success == true);
                    break;
                case "0":
                    res = res.Where(r => r.success == null);
                    break;
                case "-1":
                    res = res.Where(r => r.success == false);
                    break;
            }
            List<backBills> list = new List<backBills>();
            foreach (var a in res.OrderByDescending(r => r.id).Take(200))
            {
                string orderType = "";
                db.getOrderTypeBySysNo(a.sys_no, ref orderType);
                list.Add(new backBills()
                {
                    applyId = a.id,
                    apply_date = a.start_date.ToString(),
                    depName = a.User.Department1.name,
                    orderType = orderType,
                    salerName = a.User.real_name,
                    status = a.success == true ? "PASS" : (a.success == false ? "NG" : "----"),
                    sysNum = a.sys_no,
                    encryptNo = utl.myEncript(a.sys_no)
                });
            }
            ViewData["result"] = list;
            utl.writeEventLog("后台订单查看", "查看条数：" + list.Count(), "", Request);
            return View("CheckBillsBackground");
        }

        //办事处---市场部一审审核人
        [SessionTimeOutFilter()]
        public ActionResult AgencyAndMarketAuditor()
        {
            return View();
        }

        public JsonResult getAgencyAndMaketAuditors(string value)
        {
            var res = from a in db.AgencyAndMarket
                      select new
                      {
                          id = a.id,
                          billType = a.bill_type,
                          depName = a.agency_name,
                          auditor = a.User.real_name
                      };
            if (!string.IsNullOrWhiteSpace(value))
            {
                res = res.Where(r => r.billType.Contains(value) || r.depName.Contains(value) || r.auditor.Contains(value));
            }
            return Json(res.OrderBy(r => r.billType).OrderBy(r => r.auditor));
        }

        public JsonResult saveAgencyAndMarketAuditor(FormCollection fcl)
        {
            string billType = fcl.Get("billType");
            string depName = fcl.Get("depName");
            string auditor = fcl.Get("auditor");

            SomeUtils utl = new SomeUtils();
            try
            {
                int? depId = utl.getItemsId("agency", depName);
                int auditorId = db.User.Where(u => u.real_name == auditor).First().id;
                db.AgencyAndMarket.InsertOnSubmit(new AgencyAndMarket()
                {
                    agency_id = depId,
                    agency_name = depName,
                    bill_type = billType,
                    market_auditor = auditorId
                });
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { suc = false, msg = "保存失败" }, "text/html");
            }
            return Json(new { suc = true }, "text/html");
        }

        public JsonResult updateAgencyAndMarketAuditor(int id, FormCollection fcl)
        {
            string billType = fcl.Get("billType");
            string depName = fcl.Get("depName");
            string auditor = fcl.Get("auditor");

            try
            {
                var am = db.AgencyAndMarket.Single(a => a.id == id);
                if (!am.agency_name.Equals(depName))
                {
                    SomeUtils utl = new SomeUtils();
                    int? depId = utl.getItemsId("agency", depName);
                    am.agency_id = depId;
                    am.agency_name = depName;
                }
                int auditorId = db.User.Where(u => u.real_name == auditor).First().id;
                am.bill_type = billType;
                am.market_auditor = auditorId;
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { suc = false, msg = "保存失败" }, "text/html");
            }
            return Json(new { suc = true }, "text/html");
        }

        public JsonResult RemoveAgencyAndMarketAuditor(int id)
        {
            try
            {
                var am = db.AgencyAndMarket.Single(a => a.id == id);
                db.AgencyAndMarket.DeleteOnSubmit(am);
                db.SubmitChanges();
            }
            catch
            {
                return Json(new { suc = false, msg = "删除失败" });
            }
            return Json(new { suc = true, msg = "删除成功" });
        }

        //重置用户密码
        [SessionTimeOutFilter()]
        public ActionResult ResetPassword()
        {
            return View();
        }

        public JsonResult BeginReset(string userID, string newPass)
        {
            if (string.IsNullOrWhiteSpace(userID))
            {
                return Json(new { suc = false, msg = "保存失败，请选择需要重置密码的用户名" });
            }
            var user = db.User.Single(u => u.id == Int32.Parse(userID));
            SomeUtils utl = new SomeUtils();
            user.password = utl.getMD5(newPass);
            db.SubmitChanges();
            return Json(new { suc = true });
        }

        //流程执行时间监控
        public ActionResult ProcExcuteTime()
        {

            return GetProcExcuteTimeList("", "", DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd"), "", "0");
        }

        public ActionResult GetProcExcuteTimeList(string sys_no, string saler, string fromDate, string toDate, string auditResult)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            ViewData["sys_no"] = sys_no;
            ViewData["saler"] = saler;
            ViewData["from_date"] = fromDate;
            ViewData["to_date"] = toDate;
            ViewData["audit_result"] = auditResult;
            fromDate = string.IsNullOrWhiteSpace(fromDate) ? "1901-1-1" : fromDate;
            toDate = string.IsNullOrWhiteSpace(toDate) ? "2099-9-9" : toDate;

            //测试有没有SO和TH的查看权限
            var billTypeArr = db.ProcessAuthority.Where(p => p.user_id == userId).Select(p => p.bill_type).Distinct().ToArray();
            
            var res = from ap in db.Apply
                      where ap.sys_no.Contains(sys_no)
                      && billTypeArr.Contains(ap.order_type)
                      && ap.User.real_name.Contains(saler)
                      && ap.start_date >= DateTime.Parse(fromDate)
                      && ap.start_date <= DateTime.Parse(toDate)                      
                      select ap;

            //如果是TH，则要进一步判断退货部门的权限。为null表示可以查看所有退货部门。
            if (billTypeArr.Contains("TH")) {
                var returnDepIds = db.ProcessAuthority.Where(p => p.user_id == userId && p.bill_type == "TH").Select(p => p.return_dept_id);
                if (!returnDepIds.Contains(null)) {
                    res = from re in res
                          join rb in db.ReturnBill on re.sys_no equals rb.sys_no
                          where re.order_type == "SO" || (re.order_type == "TH" && returnDepIds.Contains(rb.return_dept))
                          select re;
                }
            }
            switch (auditResult)
            {
                case "1":
                    res = res.Where(r => r.success == true);
                    break;
                case "0":
                    res = res.Where(r => r.success == null);
                    break;
                case "-1":
                    res = res.Where(r => r.success == false);
                    break;
            }

            List<ProcTimeModel> list = new List<ProcTimeModel>();
            //最多审核步骤，加1表示加上结束步骤
            int maxStep = 6;
            if (res.Count() > 0)
            {
                maxStep = (int)((from re in res
                                 join ad in db.ApplyDetails on re.id equals ad.apply_id
                                 select ad.step).Max()) + 1;
            }
            //只取前200条记录
            foreach (var a in res.Take(200))
            {
                ProcTimeModel proc = new ProcTimeModel();
                proc.applyId = a.id;
                proc.sysNo = a.sys_no;
                proc.ProduceDep = "";
                proc.applier = a.User.real_name;
                proc.agency = a.User.Department1.name;
                proc.applyDate = ((DateTime)a.start_date).ToString("yyyy-MM-dd");
                proc.applyTime = ((DateTime)a.start_date).ToString("HH:mm");
                proc.orderType = a.order_type;
                List<AuditTimeModel> atmList = new List<AuditTimeModel>();
                DateTime previous_time = (DateTime)a.start_date;
                string nextAuditors = "";
                int? nextStep = 0;
                AuditTimeModel atm;
                foreach (var ad in a.ApplyDetails.Where(d => d.pass != null).OrderBy(d=>d.step))
                {
                    atm = new AuditTimeModel();
                    atm.step = (int)ad.step;
                    atm.stepName = ad.step_name;
                    atm.pass = ad.pass;
                    atm.auditor = ad.User.real_name;
                    atm.auditTime = ((DateTime)ad.finish_date).ToString("MM-dd HH:mm");
                    TimeSpan ts = (((DateTime)ad.finish_date) - previous_time);
                    atm.timeCost = ts.Days * 24 + ts.Hours + "时" + ts.Minutes + "分";
                    previous_time = (DateTime)ad.finish_date;
                    atmList.Add(atm);
                }
                if (a.success == true)
                {
                    atm = new AuditTimeModel();
                    atm.step = (int)a.ApplyDetails.Max(d => d.step) + 1;
                    atm.stepName = "完成申请";
                    atm.pass = true;
                    atm.auditor = "";
                    atm.auditTime = ((DateTime)a.finish_date).ToString("MM-dd HH:mm");
                    TimeSpan ts = (((DateTime)a.finish_date) - (DateTime)a.start_date);
                    atm.timeCost = ts.Days * 24 + ts.Hours + "时" + ts.Minutes + "分";
                    atmList.Add(atm);
                }
                else if (a.success == null)
                {
                    atm = new AuditTimeModel();
                    TimeSpan ts;
                    nextStep = a.ApplyDetails.Where(d => d.pass == true).Max(d => d.step);
                    nextStep = nextStep == null ? 1 : nextStep + 1;
                    //挂起的
                    var blocks = db.BlockOrder.Where(b => b.sys_no == a.sys_no && b.step == nextStep);
                    if (blocks.Count() > 0)
                    {
                        nextAuditors = blocks.First().User.real_name;
                        atm.stepName = blocks.First().step_name;
                        atm.auditTime = ((DateTime)blocks.First().block_time).ToString("MM-dd HH:mm");
                        atm.blocking = true;
                        ts = (DateTime)blocks.First().block_time - previous_time;
                    }
                    else
                    {
                        //待审核的
                        foreach (var ad in a.ApplyDetails.Where(d => d.step == nextStep))
                        {
                            if (string.IsNullOrEmpty(nextAuditors))
                            {
                                nextAuditors = ad.User.real_name;
                                atm.stepName = ad.step_name;
                            }
                            else
                            {
                                nextAuditors += "/" + ad.User.real_name;
                            }
                        }
                        ts = DateTime.Now - previous_time;
                        atm.auditTime = "";
                        atm.blocking = false;
                    }

                    atm.step = (int)nextStep;
                    atm.pass = null;
                    atm.auditor = nextAuditors;
                    atm.timeCost = ts.Days * 24 + ts.Hours + "时" + ts.Minutes + "分";
                    atmList.Add(atm);
                }
                proc.auditList = atmList;
                list.Add(proc);
            }
            ViewData["result"] = list;
            ViewData["maxStep"] = maxStep;
            utl.writeEventLog("后台流程监控", "查看条数：" + list.Count(), "", Request);
            return View("ProcExcuteTime");
        }

        //事业部审核人管理
        public ActionResult ProcDepAuditor()
        {
            return View();
        }

        //public JsonResult getProcDepAuditors(string value)
        //{
            
        //    var res = from d in db.ProduceAuditor
        //              select new
        //              {
        //                  id = d.id,
        //                  auditor_id = d.User.id,
        //                  proc_dep_id = d.produce_dep_id,
        //                  billType = d.bill_type,
        //                  procDep = d.ProduceDep.name,
        //                  auditType = d.audit_type,
        //                  auditor = d.User.real_name
        //              };
        //    if (!string.IsNullOrWhiteSpace(value))
        //    {
        //        res = res.Where(r => r.auditor.Contains(value) || r.procDep.Contains(value) || r.billType.Contains(value));
        //    }
        //    return Json(res.OrderBy(r => r.auditType).OrderBy(r => r.proc_dep_id).OrderBy(r => r.billType));
        //}

        //public JsonResult saveProcDepAuditor(FormCollection fcl)
        //{
        //    string billType = fcl.Get("billType");
        //    int depId = Int32.Parse(fcl.Get("procDep"));
        //    int auditorId = Int32.Parse(fcl.Get("auditor"));
        //    int auditType = Int32.Parse(fcl.Get("auditType"));

        //    if (db.ProduceAuditor.Where(p => p.auditor_id == auditorId && p.produce_dep_id == depId && p.bill_type == billType && p.audit_type == auditType).Count() > 0)
        //    {
        //        return Json(new { suc = false, msg = "对应关系已存在，保存失败" });
        //    }

        //    db.ProduceAuditor.InsertOnSubmit(new ProduceAuditor()
        //    {
        //        bill_type = billType,
        //        auditor_id = auditorId,
        //        produce_dep_id = depId,
        //        audit_type = auditType
        //    });

        //    db.SubmitChanges();

        //    return Json(new { suc = true }, "text/html");
        //}

        //public JsonResult updateProcDepAuditor(int id, FormCollection fcl)
        //{
        //    string billType = fcl.Get("billType");
        //    int depId = Int32.Parse(fcl.Get("procDep"));
        //    int auditorId = Int32.Parse(fcl.Get("auditor"));

        //    if (db.ProduceAuditor.Where(p => p.id == id).Count() < 1)
        //    {
        //        return Json(new { suc = false, msg = "旧对应关系不存在，保存失败" });
        //    }

        //    if (db.ProduceAuditor.Where(p => p.auditor_id == auditorId && p.produce_dep_id == depId && p.bill_type == billType).Count() > 0)
        //    {
        //        return Json(new { suc = false, msg = "新对应关系已存在，保存失败" });
        //    }

        //    ProduceAuditor pa = db.ProduceAuditor.Single(p => p.id == id);
        //    pa.bill_type = billType;
        //    pa.auditor_id = auditorId;
        //    pa.produce_dep_id = depId;

        //    db.SubmitChanges();

        //    return Json(new { suc = true }, "text/html");
        //}

        //public JsonResult RemoveProcDepAuditor(int id)
        //{

        //    var pd = db.ProduceAuditor.Single(p => p.id == id);
        //    db.ProduceAuditor.DeleteOnSubmit(pd);
        //    db.SubmitChanges();
        //    return Json(new { suc = true, msg = "删除成功" });
        //}

        //查看事业部订单
        [SessionTimeOutFilter()]
        public ActionResult CheckProcBills()
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            ViewData["userid"] = userId;
            ViewData["proc_dep"] = "all";
            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd"];
            if (queryData != null && queryData.Values.Get("pro_so") != null)
            {
                ViewData["sys_no"] = utl.DecodeToUTF8(queryData.Values.Get("pro_so"));
                ViewData["bill_no"] = utl.DecodeToUTF8(queryData.Values.Get("pro_bo"));
                ViewData["saler"] = utl.DecodeToUTF8(queryData.Values.Get("pro_sa"));
                ViewData["from_date"] = queryData.Values.Get("pro_fd");
                ViewData["to_date"] = queryData.Values.Get("pro_td");
                ViewData["proc_dep"] = utl.DecodeToUTF8(queryData.Values.Get("pro_nm"));
            }

            return View();
        }

        [SessionTimeOutFilter()]
        public JsonResult GetDepBills(FormCollection fcl)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            string sysNo = fcl.Get("sys_no");
            string billNo = fcl.Get("bill_no");
            string saler = fcl.Get("saler");
            string fromDate = fcl.Get("fromDate");
            string toDate = fcl.Get("toDate");
            string procDep = fcl.Get("proc_dep");

            //查询参数保存在Cookie，方便下次继续查询
            var queryData = Request.Cookies["op_qd"];
            if (queryData == null) queryData = new HttpCookie("op_qd");
            queryData.Values.Set("pro_so", utl.EncodeToUTF8(sysNo));
            queryData.Values.Set("pro_bo", utl.EncodeToUTF8(billNo)); 
            queryData.Values.Set("pro_sa", utl.EncodeToUTF8(saler));
            queryData.Values.Set("pro_fd", fromDate);
            queryData.Values.Set("pro_td", toDate);
            queryData.Values.Set("pro_nm", utl.EncodeToUTF8(procDep));
            queryData.Expires = DateTime.Now.AddDays(7);
            Response.AppendCookie(queryData);

            if (string.IsNullOrWhiteSpace(fromDate)) fromDate = "2000-1-1";
            if (string.IsNullOrWhiteSpace(toDate)) toDate = "2099-9-9";
            if ("all".Equals(procDep))
            {
                procDep = db.User.Single(u => u.id == userId).can_check_deps;
            }

            var depArr = procDep.Split(new Char[] { ',', '，' });

            var result = (from so in db.Order
                          join ap in db.Apply on so.sys_no equals ap.sys_no
                          join h in db.HasAttachment on so.sys_no equals h.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where so.sys_no.Contains(sysNo)
                          && so.order_no != null && so.order_no != ""
                          && so.order_no.Contains(billNo)
                          && ap.User.real_name.Contains(saler)
                          && ap.success == true
                          && depArr.Contains(so.ProduceDep.name)
                          && so.order_date >= DateTime.Parse(fromDate)
                          && so.order_date <= DateTime.Parse(toDate)
                          orderby ap.start_date
                          select new
                          {
                              applyId = ap.id,
                              encryptNo = utl.myEncript(ap.sys_no),
                              orderDate = so.order_date,
                              finishDate = ap.finish_date,
                              orderType = utl.getContantType(ap.sys_no),
                              procDep = so.ProduceDep.name,
                              billNo = so.order_no,
                              sysNum = ap.sys_no,
                              depName = ap.User.Department1.name,
                              salerName = ap.User.real_name,
                              hasAtt = Y != null
                          }).Take(300).ToList();

            utl.writeEventLog("查看事业部订单", string.Format("sys_no:{0},bill_no:{1},saler:{2},f_date:{3},t_date:{4},proc:{5}:", sysNo, billNo, saler, fromDate, toDate, procDep), "", Request);

            return Json(result, "text/html");
        }

        //订单编号管理
        [SessionTimeOutFilter()]
        public ActionResult OrderNumberManage()
        {
            return View();
        }

        public JsonResult getNextOrderNumber(string big_type, string account, string product_type, string in_out)
        {
            string result = "";
            try
            {
                db.getNextOrderNumber(account, in_out, big_type, product_type, "", ref result);
                utl.writeEventLog("手动获取订单编号", "获取成功:" + result, "", Request);
            }
            catch (Exception ex)
            {
                utl.writeEventLog("手动获取订单编号", "获取失败:" + ex.Message, "", Request, 100);
                return Json(new { suc = false, result = ex.Message });
            }
            return Json(new { suc = true, result = result });
        }

        public JsonResult returnOrderNumber(string return_number)
        {
            string result = "订单编号返还成功";
            try
            {
                db.put_number_in_recycle(return_number);
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            return Json(result);
        }

        //各级审核步骤与人员管理        
        public JsonResult getAuditorRelations(string value)
        {
            var list = db.vw_auditor_relations.Where(a => a.id >= 0);
            if (!string.IsNullOrEmpty(value))
            {
                list = list.Where(l => l.auditor_name.Contains(value.Trim()) || l.department_name.Contains(value.Trim()) || l.step_name.Contains(value.Trim()));
            }
            return Json(list.OrderBy(l => l.step_value).ToList());
        }

        public JsonResult saveAuditorRelation(FormCollection fcl)
        {

            string stepName = fcl.Get("stepName");
            string stepValueStr = fcl.Get("stepValue");
            string relateType = fcl.Get("relateType");
            string relateValueStr = fcl.Get("relateValue");
            string auditorStr = fcl.Get("auditor");

            //关联部门的值和审核人是可以为空的。
            int? relateValue = string.IsNullOrEmpty(relateValueStr) ? (int?)null : Int32.Parse(relateValueStr);
            int? auditor = string.IsNullOrEmpty(auditorStr) ? (int?)null : Int32.Parse(auditorStr);

            //检验这个步骤是否已存在数据库，如果没有，顺便可以新增步骤
            int stepValue = 0;
            if (!Int32.TryParse(stepValueStr, out stepValue))
            {
                stepValue = (int)db.AuditorsRelation.Max(a => a.step_value) + 1;
            }

            //检查关系是否已经存在
            var existsRel = db.AuditorsRelation.Where(a => a.auditor_id == auditor && a.step_value == stepValue && a.relate_type == relateType && a.relate_value == relateValue);
            if (existsRel.Count() > 0)
            {
                return Json(new { suc = false, msg = "对应关系已存在，保存失败" });
            }

            var rel = new AuditorsRelation()
            {
                auditor_id = auditor,
                relate_type = relateType,
                relate_value = relateValue,
                step_value = stepValue,
                step_name = stepName
            };

            db.AuditorsRelation.InsertOnSubmit(rel);
            db.SubmitChanges();

            return Json(new { suc = true, msg = "保存成功" });

        }

        public JsonResult updateAuditorRelation(int id, FormCollection fcl)
        {
            string stepName = fcl.Get("stepName");
            string stepValueStr = fcl.Get("stepValue");
            string relateType = fcl.Get("relateType");
            string relateValueStr = fcl.Get("relateValue");
            string auditorStr = fcl.Get("auditor");

            //关联部门的值和审核人是可以为空的。
            int? relateValue = string.IsNullOrEmpty(relateValueStr) ? (int?)null : Int32.Parse(relateValueStr);
            int? auditor = string.IsNullOrEmpty(auditorStr) ? (int?)null : Int32.Parse(auditorStr);

            //检验这个步骤是否已存在数据库，如果没有，顺便可以新增步骤
            int stepValue = 0;
            if (!Int32.TryParse(stepValueStr, out stepValue))
            {
                stepValue = (int)db.AuditorsRelation.Max(a => a.step_value) + 1;
            }

            if (db.AuditorsRelation.Where(a => a.id == id).Count() < 1) {
                return Json(new { suc = false, msg = "旧对应关系不存在，保存失败" });
            }

            var existsRel = db.AuditorsRelation.Where(a => a.auditor_id == auditor && a.step_value == stepValue && a.relate_type == relateType && a.relate_value == relateValue);
            if (existsRel.Count() > 0)
            {
                return Json(new { suc = false, msg = "新对应关系已存在，保存失败" });
            }

            AuditorsRelation rel = db.AuditorsRelation.Single(a => a.id == id);
            rel.auditor_id = auditor;
            rel.relate_type = relateType;
            rel.relate_value = relateValue;
            rel.step_name = stepName;
            rel.step_value = stepValue;
            db.SubmitChanges();

            return Json(new { suc = true, msg = "保存成功" });            
        }

        public JsonResult RemoveAuditorRelation(int id) {
            var rel = db.AuditorsRelation.Single(a => a.id == id);
            db.AuditorsRelation.DeleteOnSubmit(rel);
            db.SubmitChanges();
            return Json(new { suc = true, msg = "删除成功" });
        }

        //后台查看红字申请单
        [SessionTimeOutFilter()]
        public ActionResult CheckReturnBillsBackground()
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            ViewData["userid"] = userId;
            ViewData["proc_dep"] = "all";
            return View();
        }

        [HttpPost]
        public JsonResult BackgroundSearchReturnBills(FormCollection fcl)
        {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            string custNo = fcl.Get("cust_no");
            string sysNo = fcl.Get("sys_no");
            string billNo = fcl.Get("bill_no");
            string fromDate = fcl.Get("fromDate");
            string toDate = fcl.Get("toDate");
            string procDep = fcl.Get("proc_dep");
            fromDate = string.IsNullOrWhiteSpace(fromDate) ? "1901-1-1" : fromDate;
            toDate = string.IsNullOrWhiteSpace(toDate) ? "2099-9-9" : toDate;
            if ("all".Equals(procDep))
            {
                procDep = db.User.Single(u => u.id == userId).can_check_deps;
            }

            var depArr = procDep.Split(new Char[] { ',', '，' });

            var result = (from re in db.ReturnBill
                          join red in db.ReturnBillDetail on re.id equals red.bill_id
                          join ap in db.Apply on re.sys_no equals ap.sys_no
                          where (re.sys_no.Contains(sysNo) || red.product_model.Contains(sysNo))
                          && (re.customer_name.Contains(custNo) || re.customer_number.Contains(custNo))
                          && (red.seorder_no.Contains(billNo) || red.stock_no.Contains(billNo))                         
                          && ap.success == true
                          && depArr.Contains(re.ProduceDep.name)
                          && ap.start_date >= DateTime.Parse(fromDate)
                          && ap.start_date <= DateTime.Parse(toDate)
                          orderby ap.start_date
                          select new
                          {
                              applyId = ap.id,
                              returnId = re.id,
                              customerName = re.customer_name,
                              orderDate = ap.start_date,                              
                              procDep = re.ProduceDep.name,
                              seorderNo = red.seorder_no,
                              stockNo = red.stock_no,
                              sysNum = ap.sys_no,
                              depName = ap.User.Department1.name,
                              salerName = ap.User.real_name,
                              qty=red.real_return_qty,
                              model=red.product_model
                          }).Take(500).ToList();

            utl.writeEventLog("查看红字退货单", string.Format("sys_no:{0},bill_no:{1},f_date:{2},t_date:{3},proc:{4}:", sysNo, billNo,  fromDate, toDate, procDep), "", Request);

            return Json(result, "text/html");
        }

        public ActionResult UploadQualityReport() {
            int userId = Int32.Parse(Request.Cookies["order_cookie"]["userid"]);
            if (!utl.hasGotPower(userId, "uploadQualityReport"))
            {
                ViewBag.tip = " 没有权限";
                return View("Tip");
            }
            return View();
        }

        //事业部客服或者出货组已经OK，但还未导入K3的查询界面
        public ActionResult CheckDepHasAuditNotInK3() {
            utl.writeEventLog("统计报表", "查看事业部已审核但未导入K3客退列表", "", Request);
            return View();
        }
        
        //获取上面方法的数据
        public JsonResult LoadDepHasAuditNotInK3() {

            return Json(db.VwDepHasAuditNoInK3.OrderBy(v=>v.sys_no).OrderBy(v=>v.dep_name).ToList());
        }
    }
}
