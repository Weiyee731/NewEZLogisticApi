﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace EZLogistic.Controllers
{
    public class EZLogisticController : ApiController
    {
        public string constr_tour = ConfigurationManager.ConnectionStrings["EZLogistic"].ConnectionString;
        public string no_data_msg = "Sorry, there is no datas found!";
        public string error_msg = "Sorry, there is an error!";
        public string DataTableToJSONWithJavaScriptSerializer(DataTable table)
        {
            JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
            jsSerializer.MaxJsonLength = Int32.MaxValue;
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;
            foreach (DataRow row in table.Rows)
            {
                childRow = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    childRow.Add(col.ColumnName, row[col]);
                }
                parentRow.Add(childRow);
            }

            return jsSerializer.Serialize(parentRow);
        }

        public string EncryptString(string strToEncrypt)
        {
            System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();

            byte[] bytes = ue.GetBytes(strToEncrypt);
            // encrypt bytes
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);

            // Convert the encrypted bytes back to a string (base 16)
            string hashString = "";
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashString += Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
            }

            return hashString.PadLeft(32, '0');
        }

        public string ReturnDataSet(DataSet ds)
        {
            string Result = "";
            DataSet dsReturn = new DataSet();
            dsReturn.Tables.Add(new DataTable());
            dsReturn.Tables[0].Columns.Add("ReturnVal", typeof(System.Int32));
            dsReturn.Tables[0].Columns.Add("ReturnMsg");
            dsReturn.Tables[0].Columns.Add("ReturnID");
            dsReturn.Tables[0].Columns.Add("ReturnData");
            dsReturn.Tables[0].Columns.Add("ReturnSqlError");
            if (ds.Tables[0].Rows[0]["ReturnVal"].ToString() == "1")
            {
                if (ds.Tables[1].Rows.Count > 0)
                {
                    DataRow dr = dsReturn.Tables[0].NewRow();
                    dr["ReturnVal"] = Convert.ToInt32(ds.Tables[0].Rows[0]["ReturnVal"]);
                    dr["ReturnMsg"] = ds.Tables[0].Rows[0]["ReturnMsg"].ToString();
                    dr["ReturnID"] = ds.Tables[1].Rows.Count.ToString();
                    dr["ReturnData"] = DataTableToJSONWithJavaScriptSerializer(ds.Tables[1]);
                    dr["ReturnSqlError"] = "";
                    dsReturn.Tables[0].Rows.Add(dr);
                    Result = DataTableToJSONWithJavaScriptSerializer(dsReturn.Tables[0]);
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\",\"ReturnID\":0,\"ReturnData\":\"[]\",\"ReturnSqlError\":\"\"}]";
                }
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + error_msg + "\",\"ReturnID\":\"0\",\"ReturnData\":\"[]\",\"ReturnSqlError\":\"" + ds.Tables[0].Rows[0]["ReturnMsg"].ToString() + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/SendPushWhatapps")]
        public string SendPushWhatapps(string USERPHONE, string CONTENT)
        {
            string str = "";
            try
            {
                var url = "https://graph.facebook.com/v15.0/103624799223277/messages";

                var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                httpRequest.Method = "POST";
                httpRequest.ContentType = "application/json";
                httpRequest.Headers["Authorization"] = "Bearer EAAQytsHsLBMBAPEkbooHkIhBDR425X7XQwELaxSHXuSZAhfaHYvhSZAYqF6OYeAQbj2dKxVGGKgCbuyfJUWZBfZCzHO9R1bSZBhyJbrHPnIOWGlLVaa3A09wpgKT8icTJppfTAHHxFZAsEbT9UcJknBx9BI7BYgFLTNMKbdwgLFammKBnEGAkw8IzinXZBACzPequy4mlYjyvdMZAbcFuSqy";

                var data = @"{ ""messaging_product"": ""whatsapp"",""recipient_type"": ""individual"", ""to"": """ + USERPHONE + @""", ""type"": ""image"", ""image"": {""link"": ""https://upload.wikimedia.org/wikipedia/commons/thumb/b/b6/Image_created_with_a_mobile_phone.png/220px-Image_created_with_a_mobile_phone.png""} }";
                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }

                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                str = ex.Message;
            }
            return str;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_Login")]
        public string User_Login(string username, string password)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@username", username),
                                       new SqlParameter("@password", EncryptString(password))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_Login", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_Register")]
        public string User_Register(string USERAREAID, string USERNAME, string FULLNAME, string PASSWORD, string CONTACTNO, string USEREMAIL, string USERNICKNAME, string USERWECHATID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERAREAID", USERAREAID),
                                       new SqlParameter("@USERNAME", USERNAME),
                                       new SqlParameter("@FULLNAME", FULLNAME),
                                       new SqlParameter("@PASSWORD", EncryptString(PASSWORD)),
                                       new SqlParameter("@CONTACTNO", CONTACTNO),
                                       new SqlParameter("@USEREMAIL", USEREMAIL),
                                       new SqlParameter("@USERNICKNAME", USERNICKNAME),
                                       new SqlParameter("@USERWECHATID", USERWECHATID)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_Register", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_Register_WithReferal")]
        public string User_Register_WithReferal(string USERAREAID, string USERNAME, string FULLNAME, string PASSWORD, string CONTACTNO, string USEREMAIL, string USERNICKNAME, string USERWECHATID, string REFERALCODE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERAREAID", USERAREAID),
                                       new SqlParameter("@USERNAME", USERNAME),
                                       new SqlParameter("@FULLNAME", FULLNAME),
                                       new SqlParameter("@PASSWORD", EncryptString(PASSWORD)),
                                       new SqlParameter("@CONTACTNO", CONTACTNO),
                                       new SqlParameter("@USEREMAIL", USEREMAIL),
                                       new SqlParameter("@USERNICKNAME", USERNICKNAME),
                                       new SqlParameter("@REFERALCODE", REFERALCODE),
                                       new SqlParameter("@USERWECHATID", USERWECHATID)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_Register_WithReferal", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_ForgetPassword")]
        public string User_ForgetPassword(string USEREMAIL)
        {
            SqlParameter[] cmdParm = { new SqlParameter("@USEREMAIL", USEREMAIL) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ForgetPassword", cmdParm);
            return ReturnDataSet(ds);
        }

        public class User
        {
            public string USERCODE { get; set; }
            public string AREACODE { get; set; }
            public string FULLNAME { get; set; }
            public string USERCONTACTNO { get; set; }
            public string USEREMAILADDRESS { get; set; }
            public string USERADDRESS { get; set; }
            public string MINSELFPICKUPPRICE { get; set; }
            public string CUBICSELFPICKUPPRICE { get; set; }
            public string CONSOLIDATEPRICE { get; set; }
            public string DELIVERYCARGO { get; set; }
            public string DELIVERYFIRSTPRICE { get; set; }
            public string DELIVERYSUBPRICE { get; set; }
        }

        [HttpPost]
        [Route("api/EZLogistic/User_RegisterUsersByPost")]
        public HttpResponseMessage User_RegisterUsersByPost([FromBody] User User)
        {
            try
            {
                string Result = "";
                string[] USERCODELIST = User.USERCODE.Split(';');
                string[] AREACODELIST = User.AREACODE.Split(';');
                string[] FULLNAMELIST = User.FULLNAME.Split(';');
                string[] USERCONTACTNOLIST = User.USERCONTACTNO.Split(';');
                string[] USEREMAILADDRESSLIST = User.USEREMAILADDRESS.Split(';');
                string[] USERADDRESSLIST = User.USERADDRESS.Split(';');
                string[] MINSELFPICKUPPRICELIST = User.MINSELFPICKUPPRICE.Split(';');
                string[] CUBICSELFPICKUPPRICELIST = User.CUBICSELFPICKUPPRICE.Split(';');
                string[] CONSOLIDATEPRICELIST = User.CONSOLIDATEPRICE.Split(';');
                string[] DELIVERYCARGOLIST = User.DELIVERYCARGO.Split(';');
                string[] DELIVERYFIRSTPRICELIST = User.DELIVERYFIRSTPRICE.Split(';');
                string[] DELIVERYSUBPRICELIST = User.DELIVERYSUBPRICE.Split(';');
                for (int i = 0; i < USERCODELIST.Length; i++)
                {
                    SqlParameter[] cmdParm = { new SqlParameter("@USERCODE", USERCODELIST[i]),
                                               new SqlParameter("@USERAREA", AREACODELIST[i]),
                                               new SqlParameter("@FULLNAME", FULLNAMELIST[i]),
                                               new SqlParameter("@USERPASSWORD", EncryptString(USERCODELIST[i])),
                                               new SqlParameter("@USERCONTACTNO", USERCONTACTNOLIST[i]),
                                               new SqlParameter("@USEREMAILADDRESS", USEREMAILADDRESSLIST[i]),
                                               new SqlParameter("@USERADDRESS", USERADDRESSLIST[i]),
                                               new SqlParameter("@MINSELFPICKUPPRICE", Convert.ToDecimal(MINSELFPICKUPPRICELIST[i])),
                                               new SqlParameter("@CUBICSELFPICKUPPRICE", Convert.ToDecimal(CUBICSELFPICKUPPRICELIST[i])),
                                               new SqlParameter("@CONSOLIDATEPRICE", Convert.ToDecimal(CONSOLIDATEPRICELIST[i])),
                                               new SqlParameter("@DELIVERYCARGO", Convert.ToDecimal(DELIVERYCARGOLIST[i])),
                                               new SqlParameter("@DELIVERYFIRSTPRICE", Convert.ToDecimal(DELIVERYFIRSTPRICELIST[i])),
                                               new SqlParameter("@DELIVERYSUBPRICE", Convert.ToDecimal(DELIVERYSUBPRICELIST[i])) };
                    DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_RegisterUsers", cmdParm);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                    }

                    else
                    {
                        Result = "fail";
                    }
                }
                var response = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(Result)

                };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        [Route("api/EZLogistic/User_RegisterSimplify")]
        public string User_RegisterSimplify(string USERCODE, string USERAREAID, string USERNAME, string PASSWORD, string CONTACTNO)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERCODE", USERCODE),
                                       new SqlParameter("@USERAREAID", USERAREAID),
                                       new SqlParameter("@USERNAME", USERNAME),
                                       new SqlParameter("@PASSWORD", EncryptString(PASSWORD)),
                                       new SqlParameter("@CONTACTNO", CONTACTNO)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_RegisterSimplify", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_ViewPage")]
        public string User_ViewPage(string ROLEGROUPID, string USERID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@ROLEGROUPID", Convert.ToInt32(ROLEGROUPID)),
                                       new SqlParameter("@USERID", Convert.ToInt32(USERID))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ViewPage", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].Columns.Add("submenus");

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {

                    SqlParameter[] cmdParm2 = { new SqlParameter("@PAGEID", Convert.ToInt32(ds.Tables[0].Rows[i]["PageID"])),
                                                new SqlParameter("@ROLEGROUPID", Convert.ToInt32(ROLEGROUPID))};
                    DataSet dsDetail = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ViewPageByPageID", cmdParm2);
                    if (dsDetail.Tables[0].Rows.Count > 0)
                    {
                        ds.Tables[0].Rows[i]["submenus"] = DataTableToJSONWithJavaScriptSerializer(dsDetail.Tables[0]);
                    }

                }
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_ViewAreaCode")]
        public string User_ViewAreaCode()
        {
            string Result = "";
            SqlParameter[] cmdParm = { };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ViewAreaCode", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }


        [HttpGet]
        [Route("api/EZLogistic/User_ViewProfile")]
        public string User_Profile()
        {
            string Result = "";
            SqlParameter[] cmdParm = { };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ViewProfile", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_UpdateUserProfile")]
        public string User_UpdateUserProfile(string USERID, string USERNAME, string USERCODE, string USERAREAID, string FULLNAME, string USERWECHATID, string USERNICKNAME, string CONTACTNO, string USEREMAIL, string USERADDRESS, string MINSELFPICKUPPRICE, string CUBICSELFPICKUPPRICE, string CONSOLIDATEPRICE, string DELIVERYCARGO, string DELIVERYFIRSTPRICE, string DELIVERYSUBPRICE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERID", Convert.ToInt32(USERID)),
                                       new SqlParameter("@USERCODE", USERCODE),
                                       new SqlParameter("@USERAREAID", USERAREAID),
                                       new SqlParameter("@FULLNAME", FULLNAME),
                                       new SqlParameter("@USERNAME", USERNAME),
                                       new SqlParameter("@USERNICKNAME", USERNICKNAME),
                                       new SqlParameter("@USERWECHATID", USERWECHATID),
                                       new SqlParameter("@USERCONTACTNO", CONTACTNO),
                                       new SqlParameter("@USEREMAILADDRESS", USEREMAIL),
                                       new SqlParameter("@USERADDRESS", USERADDRESS),
                                       new SqlParameter("@MINSELFPICKUPPRICE", Convert.ToDecimal(MINSELFPICKUPPRICE)),
                                       new SqlParameter("@CUBICSELFPICKUPPRICE", Convert.ToDecimal(CUBICSELFPICKUPPRICE)),
                                       new SqlParameter("@CONSOLIDATEPRICE", Convert.ToDecimal(CONSOLIDATEPRICE)),
                                       new SqlParameter("@DELIVERYCARGO", Convert.ToDecimal(DELIVERYCARGO)),
                                       new SqlParameter("@DELIVERYFIRSTPRICE", Convert.ToDecimal(DELIVERYFIRSTPRICE)),
                                       new SqlParameter("@DELIVERYSUBPRICE", Convert.ToDecimal(DELIVERYSUBPRICE)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_UpdateUserProfile", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_UpdateUserPassword")]
        public string User_UpdateUserPassword(string USERID, string USERPASSWORD)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERID", Convert.ToInt32(USERID)),
                                       new SqlParameter("@USERPASSWORD", EncryptString(USERPASSWORD)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_UpdateUserPassword", cmdParm);
            return ReturnDataSet(ds);
        }

        [HttpGet]
        [Route("api/EZLogistic/User_DeleteUserProfile")]
        public string User_DeleteUserProfile(string USERID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERID", Convert.ToInt32(USERID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_DeleteUserProfile", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_ViewProfileByID")]
        public string User_ProfileByID(string USERID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERID", Convert.ToInt32(USERID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ViewProfileByID", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].Columns.Add("Transaction");
                ds.Tables[0].Columns.Add("Payment");
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {

                    SqlParameter[] cmdParm2 = { new SqlParameter("@USERID", Convert.ToInt32(USERID)) };
                    DataSet dsDetail = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_ViewUserTransaction", cmdParm2);
                    if (dsDetail.Tables[0].Rows.Count > 0)
                    {
                        ds.Tables[0].Rows[i]["Transaction"] = DataTableToJSONWithJavaScriptSerializer(dsDetail.Tables[0]);
                    }
                    else
                    {
                        ds.Tables[0].Rows[i]["Transaction"] = "[]";
                    }

                    SqlParameter[] cmdParm3 = { new SqlParameter("@USERID", Convert.ToInt32(USERID)) };
                    DataSet dsDetail2 = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Payment_ViewUserPayment", cmdParm3);
                    if (dsDetail2.Tables[0].Rows.Count > 0)
                    {
                        ds.Tables[0].Rows[i]["Payment"] = DataTableToJSONWithJavaScriptSerializer(dsDetail2.Tables[0]);
                    }
                    else
                    {
                        ds.Tables[0].Rows[i]["Payment"] = "[]";
                    }

                }
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_ViewCommissionList")]
        public string User_ViewCommissionList(string USERCODE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERCODE", USERCODE) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ViewCommissionList", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_ViewProfileByUserCode")]
        public string User_ViewProfileByUserCode(string USERCODE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERCODE", USERCODE) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ViewProfileByUserCode", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Container_InsertNewContainer")]
        public string Container_InsertNewContainer(string CONTAINERNAME, string CONTAINERDATE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@CONTAINERNAME", CONTAINERNAME),
                                       new SqlParameter("@CONTAINERDATE", CONTAINERDATE)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Container_InsertNewContainer", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        //[HttpGet]
        //[Route("api/EZLogistic/Notification_AddNotification")]
        //public string Notification_AddNotification(string NOTIFICATIONTITLE, string NOTIFICATIONDESC, string NOTIFICATIONSTATUSID, string MODIFY)
        //{
        //    string Result = "";
        //    SqlParameter[] cmdParm = { new SqlParameter("@NOTIFICATIONTITLE", NOTIFICATIONTITLE),
        //                               new SqlParameter("@NOTIFICATIONDESC", NOTIFICATIONDESC),
        //                               new SqlParameter("@NOTIFICATIONSTATUSID", NOTIFICATIONSTATUSID),
        //                               new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
        //    DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Notification_AddNotification", cmdParm);
        //    if (ds.Tables[0].Rows.Count > 0)
        //    {
        //        if (ds.Tables[0].Rows[0]["SendInd"].ToString() == "1")
        //        {
        //            for(int i = 0; i < ds.Tables[1].Rows.Count; i++)
        //            {
        //                General_TriggerEmail(ds.Tables[0].Rows[0]["Title"].ToString(), ds.Tables[0].Rows[0]["Content"].ToString(), ds.Tables[1].Rows[i]["UserEmailAddress"].ToString());
        //            }
        //        }
        //        Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
        //    }
        //    else
        //    {
        //        Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
        //    }
        //    return Result;
        //}

        public class NotificationData
        {
            public string NOTIFICATIONTITLE { get; set; }
            public string NOTIFICATIONDESC { get; set; }
            public string NOTIFICATIONSTATUSID { get; set; }
            public string MODIFY { get; set; }
            public string NOTIFICATIONID { get; set; }
        }

        [HttpPost]
        [Route("api/EZLogistic/Notification_AddNotificationByPost")]
        public HttpResponseMessage Notification_AddNotificationByPost([FromBody] NotificationData NotificationData)
        {
            try
            {
                string Result = "";
                SqlParameter[] cmdParm = { new SqlParameter("@NOTIFICATIONTITLE",  NotificationData.NOTIFICATIONTITLE),
                                                new SqlParameter("@NOTIFICATIONDESC",  NotificationData.NOTIFICATIONDESC),
                                                new SqlParameter("@NOTIFICATIONSTATUSID",  NotificationData.NOTIFICATIONSTATUSID),
                                             new SqlParameter("@MODIFY", Convert.ToInt32(NotificationData.MODIFY))};
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Notification_AddNotification", cmdParm);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    if (ds.Tables[0].Rows[0]["SendInd"].ToString() == "1")
                    {
                        for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
                        {
                            General_TriggerEmail(ds.Tables[0].Rows[0]["Title"].ToString(), ds.Tables[0].Rows[0]["Content"].ToString(), ds.Tables[1].Rows[i]["UserEmailAddress"].ToString());
                        }
                    }
                    Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                }

                var response = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(Result)

                };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }

            //return Result;
        }

        [HttpPost]
        [Route("api/EZLogistic/Notification_UpdateNotificationByPost")]
        public HttpResponseMessage Notification_UpdateNotificationByPost([FromBody] NotificationData NotificationData)
        {
            try
            {
                string Result = "";
                SqlParameter[] cmdParm = { new SqlParameter("@NOTIFICATIONTITLE",  NotificationData.NOTIFICATIONTITLE),
                                                new SqlParameter("@NOTIFICATIONDESC",  NotificationData.NOTIFICATIONDESC),
                                                new SqlParameter("@NOTIFICATIONSTATUSID",  NotificationData.NOTIFICATIONSTATUSID),
                                               new SqlParameter("@NOTIFICATIONID", Convert.ToInt32(NotificationData.NOTIFICATIONID)),
                                             new SqlParameter("@MODIFY", Convert.ToInt32(NotificationData.MODIFY))};
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Notification_UpdateNotification", cmdParm);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    if (ds.Tables[0].Rows[0]["SendInd"].ToString() == "1")
                    {
                        for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
                        {
                            General_TriggerEmail(ds.Tables[0].Rows[0]["Title"].ToString(), ds.Tables[0].Rows[0]["Content"].ToString(), ds.Tables[1].Rows[i]["UserEmailAddress"].ToString());
                        }
                    }
                    Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                }

                var response = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(Result)

                };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }

            //return Result;
        }

        //[HttpGet]
        // [Route("api/EZLogistic/Notification_UpdateNotification")]
        // public string Notification_UpdateNotification(string NOTIFICATIONID, string NOTIFICATIONTITLE, string NOTIFICATIONDESC, string NOTIFICATIONSTATUSID, string MODIFY)
        // {
        //     string Result = "";
        //     SqlParameter[] cmdParm = { new SqlParameter("@NOTIFICATIONID", Convert.ToInt32(NOTIFICATIONID)),
        //                                new SqlParameter("@NOTIFICATIONTITLE", NOTIFICATIONTITLE),
        //                                new SqlParameter("@NOTIFICATIONDESC", NOTIFICATIONDESC),
        //                                new SqlParameter("@NOTIFICATIONSTATUSID", NOTIFICATIONSTATUSID),
        //                                new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
        //     DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Notification_UpdateNotification", cmdParm);
        //     if (ds.Tables[0].Rows.Count > 0)
        //     {
        //         if (ds.Tables[0].Rows[0]["SendInd"].ToString() == "1")
        //         {
        //             for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
        //             {
        //                 General_TriggerEmail(ds.Tables[0].Rows[0]["Title"].ToString(), ds.Tables[0].Rows[0]["Content"].ToString(), ds.Tables[1].Rows[i]["UserEmailAddress"].ToString());
        //             }
        //         }
        //         Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
        //     }
        //     else
        //     {
        //         Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
        //     }
        //     return Result;
        // }

        [HttpGet]
        [Route("api/EZLogistic/Notification_UpdateNotificationStatus")]
        public string Notification_UpdateNotificationStatus(string NOTIFICATIONID, string NOTIFICATIONSTATUSID, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@NOTIFICATIONID", Convert.ToInt32(NOTIFICATIONID)),
                                       new SqlParameter("@NOTIFICATIONSTATUSID", NOTIFICATIONSTATUSID),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Notification_UpdateNotificationStatus", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Notification_DeleteNotification")]
        public string Notification_DeleteNotification(string NOTIFICATIONID, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@NOTIFICATIONID", Convert.ToInt32(NOTIFICATIONID)),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Notification_DeleteNotification", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Notification_ViewNotification")]
        public string Notification_ViewNotification(string NOTIFICATIONSTATUSID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@NOTIFICATIONSTATUSID", Convert.ToInt32(NOTIFICATIONSTATUSID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Notification_ViewNotification", cmdParm);
            return ReturnDataSet(ds);
        }

        [HttpGet]
        [Route("api/EZLogistic/Notification_ViewNotification2")]
        public string Notification_ViewNotification2(string NOTIFICATIONSTATUSID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@NOTIFICATIONSTATUSID", Convert.ToInt32(NOTIFICATIONSTATUSID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Notification_ViewNotification", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[1]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Container_AddContainer")]
        public string Container_AddContainer(string CONTAINERNAME, string CONTAINERDATE, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@CONTAINERNAME", CONTAINERNAME),
                                       new SqlParameter("@CONTAINERDATE", CONTAINERDATE),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Container_AddContainer", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Container_UpdateContainer")]
        public string Container_UpdateContainer(string CONTAINERID, string CONTAINERNAME, string CONTAINERREMARK, string CONTAINERDATE, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@CONTAINERID", Convert.ToInt32(CONTAINERID)),
                                       new SqlParameter("@CONTAINERNAME", CONTAINERNAME),
                                       new SqlParameter("@CONTAINERDATE", CONTAINERDATE),
                                       new SqlParameter("@CONTAINERREMARK", CONTAINERREMARK),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Container_UpdateContainer", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Container_UpdateContainerStatus")]
        public string Container_UpdateContainerStatus(string CONTAINERID, string CONTAINERSTATUSID, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@CONTAINERID", Convert.ToInt32(CONTAINERID)),
                                       new SqlParameter("@CONTAINERSTATUSID", Convert.ToInt32(CONTAINERSTATUSID)),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Container_UpdateContainerStatus", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Container_DeleteContainer")]
        public string Container_DeleteContainer(string CONTAINERID, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@CONTAINERID", Convert.ToInt32(CONTAINERID)),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Container_DeleteContainer", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Container_ViewContainer")]
        public string Container_ViewContainer()
        {
            string Result = "";
            SqlParameter[] cmdParm = { };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Container_ViewContainer", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Container_ViewContainerStatus")]
        public string Container_ViewContainerStatus()
        {
            string Result = "";
            SqlParameter[] cmdParm = { };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Container_ViewContainerStatus", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Courier_ViewCourier")]
        public string Courier_ViewCourier()
        {
            string Result = "";
            SqlParameter[] cmdParm = { };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Courier_ViewCourier", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }



        [HttpGet]
        [Route("api/EZLogistic/Inventory_ViewStockList")]
        public string Inventory_ViewStockList(string TRACKINGSTATUSID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@TRACKINGSTATUSID", Convert.ToInt32(TRACKINGSTATUSID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_ViewStockList", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_ViewStockListByFilter")]
        public string Inventory_ViewStockListByFilter(string FILTERCOLUMN, string FILTERKEYWORD)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@FILTERCOLUMN", FILTERCOLUMN),
                                       new SqlParameter("@FILTERKEYWORD", FILTERKEYWORD)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_ViewStockListByFilter", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_ViewStockByFilter")]
        public string Inventory_ViewStockByFilter(string FILTERCOLUMN)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@FILTERCOLUMN", FILTERCOLUMN) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_ViewStockByFilter", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_ViewStockByFilter2")]
        public string Inventory_ViewStockByFilter2(string FILTERCOLUMN)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@FILTERCOLUMN", FILTERCOLUMN) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_ViewStockByFilter2", cmdParm);
            return ReturnDataSet(ds);
        }


        [HttpGet]
        [Route("api/EZLogistic/Inventory_ViewStockListByDate")]
        public string Inventory_ViewStockListByDate(string STARTDATE, string ENDDATE)
        {
            string Result = "";
            STARTDATE = String.IsNullOrEmpty(STARTDATE) ? "-" : STARTDATE;
            ENDDATE = String.IsNullOrEmpty(ENDDATE) ? "-" : ENDDATE;
            SqlParameter[] cmdParm = { new SqlParameter("@STARTDATE", STARTDATE),
                                       new SqlParameter("@ENDDATE", ENDDATE)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_ViewStockListByDate", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_UpdateStockDetail")]
        public string Inventory_UpdateStockDetail(string STOCKID, string USERCODE, string TRACKINGNUMBER, string PRODUCTWEIGHT, string PRODUCTHEIGHT, string PRODUCTWIDTH, string PRODUCTDEEP, string AREACODE, string ITEM, string TRACKINGSTATUSID, string CONTAINERNAME, string CONTAINERDATE, string REMARK, string EXTRACHARGE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@STOCKID", Convert.ToInt32(STOCKID)),
                                       new SqlParameter("@USERCODE", USERCODE),
                                       new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBER),
                                       new SqlParameter("@AREACODE", AREACODE),
                                       new SqlParameter("@PRODUCTWEIGHT", Convert.ToDecimal(PRODUCTWEIGHT)),
                                       new SqlParameter("@PRODUCTHEIGHT", Convert.ToDecimal(PRODUCTHEIGHT)),
                                       new SqlParameter("@PRODUCTWIDTH", Convert.ToDecimal(PRODUCTWIDTH)),
                                       new SqlParameter("@PRODUCTDEEP", Convert.ToDecimal(PRODUCTDEEP)),
                                       new SqlParameter("@ITEM", ITEM),
                                       new SqlParameter("@TRACKINGSTATUSID", Convert.ToInt32(TRACKINGSTATUSID)),
                                       new SqlParameter("@CONTAINERNAME", CONTAINERNAME),
                                       new SqlParameter("@CONTAINERDATE", CONTAINERDATE),
                                       new SqlParameter("@REMARK", REMARK),
                                       new SqlParameter("@EXTRACHARGE", EXTRACHARGE)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_UpdateStockDetail", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_UpdateStock")]
        public string Inventory_UpdateStock(string STOCKID, string USERCODE, string TRACKINGNUMBER, string PRODUCTWEIGHT, string PRODUCTHEIGHT, string PRODUCTWIDTH, string PRODUCTDEEP, string ITEM, string REMARK, string COURIERID, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@STOCKID", Convert.ToInt32(STOCKID)),
                                       new SqlParameter("@USERCODE", USERCODE),
                                       new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBER),
                                       new SqlParameter("@PRODUCTWEIGHT", Convert.ToDecimal(PRODUCTWEIGHT)),
                                       new SqlParameter("@PRODUCTHEIGHT", Convert.ToDecimal(PRODUCTHEIGHT)),
                                       new SqlParameter("@PRODUCTWIDTH", Convert.ToDecimal(PRODUCTWIDTH)),
                                       new SqlParameter("@PRODUCTDEEP", Convert.ToDecimal(PRODUCTDEEP)),
                                       new SqlParameter("@ITEM", ITEM),
                                       new SqlParameter("@REMARK", REMARK),
                                       new SqlParameter("@COURIERID", Convert.ToInt32(COURIERID)),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_UpdateStock", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_DeleteStock")]
        public string Inventory_DeleteStock(string STOCKID, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@STOCKID", Convert.ToInt32(STOCKID)),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_DeleteStock", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpPost]
        [Route("api/EZLogistic/Inventory_UpdateStockDetailByPost")]
        public HttpResponseMessage Inventory_UpdateStockDetailByPost([FromBody] Stock Stock)
        {
            try
            {
                string Result = "";
                string[] USERCODELIST = Stock.USERCODE.Split(',');
                string[] STOCKIDLIST = Stock.STOCKID.Split(',');
                string[] TRACKINGNUMBERLIST = Stock.TRACKINGNUMBER.Split(',');
                string[] AREACODELIST = Stock.AREACODE.Split(',');
                string[] PRODUCTWEIGHTLIST = Stock.PRODUCTWEIGHT.Split(',');
                string[] PRODUCTHEIGHTLIST = Stock.PRODUCTHEIGHT.Split(',');
                string[] PRODUCTWIDTHLIST = Stock.PRODUCTWIDTH.Split(',');
                string[] PRODUCTDEEPLIST = Stock.PRODUCTDEEP.Split(',');
                string[] ITEMLIST = Stock.ITEM.Split(',');
                string[] TRACKINGSTATUSIDLIST = Stock.TRACKINGSTATUSID.Split(',');
                string[] CONTAINERNAMELIST = Stock.CONTAINERNAME.Split(',');
                string[] CONTAINERDATELIST = Stock.CONTAINERDATE.Split(',');
                string[] REMARKLIST = Stock.REMARK.Split(',');
                string[] EXTRACHARGELIST = Stock.EXTRACHARGE.Split(',');
                for (int i = 0; i < USERCODELIST.Length; i++)
                {
                    SqlParameter[] cmdParm = { new SqlParameter("@STOCKID", Convert.ToInt32(STOCKIDLIST[i])),
                                               new SqlParameter("@USERCODE", USERCODELIST[i]),
                                               new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBERLIST[i]),
                                               new SqlParameter("@AREACODE",AREACODELIST[i]),
                                               new SqlParameter("@PRODUCTWEIGHT", Convert.ToDecimal(PRODUCTWEIGHTLIST[i])),
                                               new SqlParameter("@PRODUCTHEIGHT", Convert.ToDecimal(PRODUCTHEIGHTLIST[i])),
                                               new SqlParameter("@PRODUCTWIDTH", Convert.ToDecimal(PRODUCTWIDTHLIST[i])),
                                               new SqlParameter("@PRODUCTDEEP", Convert.ToDecimal(PRODUCTDEEPLIST[i])),
                                               new SqlParameter("@ITEM", ITEMLIST[i]),
                                               new SqlParameter("@TRACKINGSTATUSID", Convert.ToInt32(TRACKINGSTATUSIDLIST[i])),
                                               new SqlParameter("@CONTAINERNAME", CONTAINERNAMELIST[i]),
                                               new SqlParameter("@CONTAINERDATE", CONTAINERDATELIST[i]),
                                               new SqlParameter("@REMARK", REMARKLIST[i]),
                                               new SqlParameter("@EXTRACHARGE", EXTRACHARGELIST[i])};
                    DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_UpdateStockDetail", cmdParm);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                    }
                    else
                    {
                        Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                    }
                }
                var response = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(Result)

                };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_UpdateStockStatus")]
        public string Inventory_UpdateStockStatus(string STOCKID, string CONTAINERID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@STOCKID", STOCKID),
                                       new SqlParameter("@CONTAINERID", CONTAINERID)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_UpdateStockStatus", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }



        [HttpGet]
        [Route("api/EZLogistic/Inventory_UpdateStockContainer")]
        public string Inventory_UpdateStockContainer(string TRACKINGNUMBER, string CONTAINERID)
        {
            string Result = "";
            string[] TRACKINGNUMBERs = TRACKINGNUMBER.Split(',');
            DataSet dsReturn = new DataSet();
            dsReturn.Tables.Add(new DataTable());
            dsReturn.Tables[0].Columns.Add("StockID");
            dsReturn.Tables[0].Columns.Add("TrackingNumber");
            dsReturn.Tables[0].Columns.Add("ReturnVal", typeof(System.Int32));
            dsReturn.Tables[0].Columns.Add("ReturnMsg");
            for (int i = 0; i < TRACKINGNUMBERs.Length; i++)
            {
                SqlParameter[] cmdParm = { new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBERs[i]),
                                           new SqlParameter("@CONTAINERID", CONTAINERID)};
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_UpdateStockContainer", cmdParm);
                DataRow dr = dsReturn.Tables[0].NewRow();
                dr["StockID"] = ds.Tables[0].Rows[0]["StockID"].ToString();
                dr["TrackingNumber"] = TRACKINGNUMBERs[i];
                dr["ReturnVal"] = Convert.ToInt32(ds.Tables[0].Rows[0]["ReturnVal"].ToString());
                dr["ReturnMsg"] = ds.Tables[0].Rows[0]["ReturnMsg"].ToString();
                dsReturn.Tables[0].Rows.Add(dr);
            }
            Result = DataTableToJSONWithJavaScriptSerializer(dsReturn.Tables[0]);
            return Result;
        }

        public class TRACKINGLIST
        {
            public string CONTAINERID { get; set; }
            public string TRACKINGNUMBER { get; set; }
        }

        [HttpPost]
        [Route("api/EZLogistic/Inventory_UpdateStockContainerByPost")]
        public HttpResponseMessage Inventory_UpdateStockContainerByPost([FromBody] TRACKINGLIST TRACKINGLIST)
        {
            try
            {
                string Result = "";
                string[] TRACKINGNUMBERs = TRACKINGLIST.TRACKINGNUMBER.Split(',');
                DataSet dsReturn = new DataSet();
                dsReturn.Tables.Add(new DataTable());
                dsReturn.Tables[0].Columns.Add("StockID");
                dsReturn.Tables[0].Columns.Add("TrackingNumber");
                dsReturn.Tables[0].Columns.Add("ReturnVal", typeof(System.Int32));
                dsReturn.Tables[0].Columns.Add("ReturnMsg");
                for (int i = 0; i < TRACKINGNUMBERs.Length; i++)
                {
                    SqlParameter[] cmdParm = { new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBERs[i]),
                                           new SqlParameter("@CONTAINERID", TRACKINGLIST.CONTAINERID)};
                    DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_UpdateStockContainer", cmdParm);
                    DataRow dr = dsReturn.Tables[0].NewRow();
                    dr["StockID"] = ds.Tables[0].Rows[0]["StockID"].ToString();
                    dr["TrackingNumber"] = TRACKINGNUMBERs[i];
                    dr["ReturnVal"] = Convert.ToInt32(ds.Tables[0].Rows[0]["ReturnVal"].ToString());
                    dr["ReturnMsg"] = ds.Tables[0].Rows[0]["ReturnMsg"].ToString();
                    dsReturn.Tables[0].Rows.Add(dr);
                }
                Result = DataTableToJSONWithJavaScriptSerializer(dsReturn.Tables[0]);
                var response = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(Result)

                };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_AddStock")]
        public string Inventory_AddStock(string USERCODE, string TRACKINGNUMBER, string PRODUCTWEIGHT, string PRODUCTHEIGHT, string PRODUCTWIDTH, string PRODUCTDEEP, string ITEM, string REMARK, string COURIERID, string MODIFY)
        {
            string Result = "";
            string[] USERCODEs = USERCODE.Split(',');
            string[] TRACKINGNUMBERs = TRACKINGNUMBER.Split(',');
            string[] PRODUCTWEIGHTs = PRODUCTWEIGHT.Split(',');
            string[] PRODUCTHEIGHTs = PRODUCTHEIGHT.Split(',');
            string[] PRODUCTWIDTHs = PRODUCTWIDTH.Split(',');
            string[] PRODUCTDEEPs = PRODUCTDEEP.Split(',');
            string[] ITEMs = ITEM.Split(',');
            string[] REMARKs = REMARK.Split(',');
            string[] COURIERIDs = COURIERID.Split(',');
            for (int i = 0; i < USERCODEs.Length; i++)
            {
                SqlParameter[] cmdParm = { new SqlParameter("@USERCODE", USERCODEs[i]),
                                       new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBERs[i]),
                                       new SqlParameter("@PRODUCTWEIGHT", Convert.ToDecimal(PRODUCTWEIGHTs[i])),
                                       new SqlParameter("@PRODUCTHEIGHT", Convert.ToDecimal(PRODUCTHEIGHTs[i])),
                                       new SqlParameter("@PRODUCTWIDTH", Convert.ToDecimal(PRODUCTWIDTHs[i])),
                                       new SqlParameter("@PRODUCTDEEP", Convert.ToDecimal(PRODUCTDEEPs[i])),
                                       new SqlParameter("@ITEM", ITEMs[i]),
                                       new SqlParameter("@REMARK", REMARKs[i]),
                                       new SqlParameter("@COURIERID", COURIERIDs[i]),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_AddStock", cmdParm);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                }
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_InsertStock")]
        public string Inventory_InsertStock(string USERCODE, string TRACKINGNUMBER, string PRODUCTWEIGHT, string PRODUCTHEIGHT, string PRODUCTWIDTH, string PRODUCTDEEP, string AREACODE, string ITEM, string STOCKDATE, string PACKAGINGDATE, string REMARK, string EXTRACHARGE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERCODE", USERCODE),
                                       new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBER),
                                       new SqlParameter("@AREACODE", Convert.ToInt32(AREACODE)),
                                       new SqlParameter("@PRODUCTWEIGHT", Convert.ToDecimal(PRODUCTWEIGHT)),
                                       new SqlParameter("@PRODUCTHEIGHT", Convert.ToDecimal(PRODUCTHEIGHT)),
                                       new SqlParameter("@PRODUCTWIDTH", Convert.ToDecimal(PRODUCTWIDTH)),
                                       new SqlParameter("@PRODUCTDEEP", Convert.ToDecimal(PRODUCTDEEP)),
                                       new SqlParameter("@ITEM", ITEM),
                                       new SqlParameter("@STOCKDATE", STOCKDATE),
                                       new SqlParameter("@PACKAGINGDATE", PACKAGINGDATE),
                                       new SqlParameter("@REMARK", REMARK),
                                       new SqlParameter("@EXTRACHARGE", EXTRACHARGE)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_InsertStock", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }


        public class ParcelData
        {
            public string USERCODE { get; set; }
            public string TRACKINGNUMBER { get; set; }
            public string PRODUCTWEIGHT { get; set; }
            public string PRODUCTHEIGHT { get; set; }
            public string PRODUCTWIDTH { get; set; }
            public string PRODUCTDEEP { get; set; }
            public string ITEM { get; set; }
            public string REMARK { get; set; }
            public string COURIERID { get; set; }
            public string MODIFY { get; set; }
        }

        [HttpPost]
        [Route("api/EZLogistic/Inventory_AddStockByPost")]
        public HttpResponseMessage Inventory_InsertStockByPost([FromBody] ParcelData ParcelData)

        //  [Route("api/EZLogistic/Inventory_AddStock")]
        //  public string Inventory_AddStock(string USERCODE, string TRACKINGNUMBER, string PRODUCTWEIGHT, string PRODUCTHEIGHT, string PRODUCTWIDTH, string PRODUCTDEEP, string ITEM, string REMARK, string COURIERID, string MODIFY)
        {
            try
            {
                string Result = "";
                string[] USERCODEs = ParcelData.USERCODE.Split(',');
                string[] TRACKINGNUMBERs = ParcelData.TRACKINGNUMBER.Split(',');
                string[] PRODUCTWEIGHTs = ParcelData.PRODUCTWEIGHT.Split(',');
                string[] PRODUCTHEIGHTs = ParcelData.PRODUCTHEIGHT.Split(',');
                string[] PRODUCTWIDTHs = ParcelData.PRODUCTWIDTH.Split(',');
                string[] PRODUCTDEEPs = ParcelData.PRODUCTDEEP.Split(',');
                string[] ITEMs = ParcelData.ITEM.Split(',');
                string[] REMARKs = ParcelData.REMARK.Split(',');
                string[] COURIERIDs = ParcelData.COURIERID.Split(',');
                for (int i = 0; i < USERCODEs.Length; i++)
                {
                    SqlParameter[] cmdParm = { new SqlParameter("@USERCODE", USERCODEs[i]),
                                       new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBERs[i]),
                                       new SqlParameter("@PRODUCTWEIGHT", Convert.ToDecimal(PRODUCTWEIGHTs[i])),
                                       new SqlParameter("@PRODUCTHEIGHT", Convert.ToDecimal(PRODUCTHEIGHTs[i])),
                                       new SqlParameter("@PRODUCTWIDTH", Convert.ToDecimal(PRODUCTWIDTHs[i])),
                                       new SqlParameter("@PRODUCTDEEP", Convert.ToDecimal(PRODUCTDEEPs[i])),
                                       new SqlParameter("@ITEM", ITEMs[i]),
                                       new SqlParameter("@REMARK", REMARKs[i]),
                                       new SqlParameter("@COURIERID", COURIERIDs[i]),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(ParcelData.MODIFY))};
                    DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_AddStock", cmdParm);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                    }
                    else
                    {
                        Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                    }
                }
                var response = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(Result)

                };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }

            //return Result;
        }


        public class Stock
        {
            public string STOCKID { get; set; }
            public string USERCODE { get; set; }
            public string TRACKINGNUMBER { get; set; }
            public string AREACODE { get; set; }
            public string PRODUCTWEIGHT { get; set; }
            public string PRODUCTHEIGHT { get; set; }
            public string PRODUCTWIDTH { get; set; }
            public string PRODUCTDEEP { get; set; }
            public string ITEM { get; set; }
            public string STOCKDATE { get; set; }
            public string PACKAGINGDATE { get; set; }
            public string REMARK { get; set; }
            public string EXTRACHARGE { get; set; }
            public string TRACKINGSTATUSID { get; set; }
            public string CONTAINERNAME { get; set; }
            public string CONTAINERDATE { get; set; }



        }

        [HttpPost]
        [Route("api/EZLogistic/Inventory_InsertStockByPost")]
        public HttpResponseMessage Inventory_InsertStockByPost([FromBody] Stock Stock)
        {
            try
            {
                string Result = "";
                string[] USERCODELIST = Stock.USERCODE.Split(',');
                string[] TRACKINGNUMBERLIST = Stock.TRACKINGNUMBER.Split(',');
                string[] AREACODELIST = Stock.AREACODE.Split(',');
                string[] PRODUCTWEIGHTLIST = Stock.PRODUCTWEIGHT.Split(',');
                string[] PRODUCTHEIGHTLIST = Stock.PRODUCTHEIGHT.Split(',');
                string[] PRODUCTWIDTHLIST = Stock.PRODUCTWIDTH.Split(',');
                string[] PRODUCTDEEPLIST = Stock.PRODUCTDEEP.Split(',');
                string[] ITEMLIST = Stock.ITEM.Split(',');
                string[] STOCKDATELIST = Stock.STOCKDATE.Split(',');
                string[] PACKAGINGDATELIST = Stock.PACKAGINGDATE.Split(',');
                string[] REMARKLIST = Stock.REMARK.Split(',');
                string[] EXTRACHARGELIST = Stock.EXTRACHARGE.Split(',');
                SqlParameter[] cmdParm = { new SqlParameter("@CONTAINERNAME", Stock.CONTAINERNAME),
                                           new SqlParameter("@CONTAINERDATE", Stock.CONTAINERDATE)};
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Container_InsertNewContainer", cmdParm);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    if (ds.Tables[0].Rows[0]["ReturnVal"].ToString() == "0")
                    {
                        Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + ds.Tables[0].Rows[0]["ReturnMsg"].ToString() + "\"}]";
                    }
                    else
                    {
                        for (int i = 0; i < USERCODELIST.Length; i++)
                        {
                            SqlParameter[] cmdParm2 = { new SqlParameter("@USERCODE", USERCODELIST[i]),
                                       new SqlParameter("@TRACKINGNUMBER", TRACKINGNUMBERLIST[i]),
                                       new SqlParameter("@AREACODE", AREACODELIST[i]),
                                       new SqlParameter("@PRODUCTWEIGHT", Convert.ToDecimal(PRODUCTWEIGHTLIST[i])),
                                       new SqlParameter("@PRODUCTHEIGHT", Convert.ToDecimal(PRODUCTHEIGHTLIST[i])),
                                       new SqlParameter("@PRODUCTWIDTH", Convert.ToDecimal(PRODUCTWIDTHLIST[i])),
                                       new SqlParameter("@PRODUCTDEEP", Convert.ToDecimal(PRODUCTDEEPLIST[i])),
                                       new SqlParameter("@ITEM", ITEMLIST[i]),
                                       new SqlParameter("@STOCKDATE", STOCKDATELIST[i]),
                                       new SqlParameter("@PACKAGINGDATE", PACKAGINGDATELIST[i]),
                                       new SqlParameter("@REMARK", REMARKLIST[i]),
                                       new SqlParameter("@EXTRACHARGE", EXTRACHARGELIST[i]),
                                       new SqlParameter("@ESTCONTAINERID", ds.Tables[0].Rows[0]["ContainerID"]),
                                       new SqlParameter("@ESTCONTAINERNAME", Stock.CONTAINERNAME),
                                       new SqlParameter("@ESTCONTAINERDATE", Stock.CONTAINERDATE)};
                            DataSet ds2 = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_InsertStock", cmdParm2);
                            if (ds2.Tables[0].Rows.Count > 0)
                            {
                                Result = DataTableToJSONWithJavaScriptSerializer(ds2.Tables[0]);
                            }
                            else
                            {
                                Result = "fail";
                            }
                        }
                    }
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                }
                var response = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(Result)

                };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_InsertTransaction")]
        public string Transaction_InsertTransaction(string USERID, string CALCULATIONTYPE, string DELIVERYFEE, string ORDERTOTALMOUNT, string ORDERSUBTOTALMOUNT, string ORDERPAIDMOUNT, string FIRSTKG, string SUBSEQUENCEKG, string STOCKID, string PRODUCTPRICE, string PRODUCTQUANTITY, string PRODUCTDIMENSION, string PRODUCTUNITPRICE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERID", Convert.ToInt32(USERID)),
                                       new SqlParameter("@CALCULATIONTYPE", CALCULATIONTYPE),
                                       new SqlParameter("@DELIVERYFEE", Convert.ToDecimal(DELIVERYFEE)),
                                       new SqlParameter("@ORDERSUBTOTALMOUNT", Convert.ToDecimal(ORDERSUBTOTALMOUNT)),
                                       new SqlParameter("@ORDERTOTALMOUNT", Convert.ToDecimal(ORDERTOTALMOUNT)),
                                       new SqlParameter("@ORDERPAIDMOUNT", Convert.ToDecimal(ORDERPAIDMOUNT)),
                                       new SqlParameter("@FIRSTKG", Convert.ToDecimal(FIRSTKG)),
                                       new SqlParameter("@SUBSEQUENCEKG", Convert.ToDecimal(SUBSEQUENCEKG))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_InsertTransaction", cmdParm);
            string[] StockIdList = STOCKID.Split(',');
            string[] ProductPriceList = PRODUCTPRICE.Split(',');
            string[] ProductQuantityList = PRODUCTQUANTITY.Split(',');
            string[] ProductDimensionList = PRODUCTDIMENSION.Split(',');
            string[] ProductUnitPriceList = PRODUCTUNITPRICE.Split(',');
            for (int i = 0; i < StockIdList.Length; i++)
            {
                SqlParameter[] cmdParm2 = { new SqlParameter("@TRANSACTIONID", Convert.ToInt32(ds.Tables[0].Rows[0]["TransactionID"])),
                                            new SqlParameter("@STOCKID", Convert.ToInt32(StockIdList[i])),
                                            new SqlParameter("@PRODUCTPRICE", Convert.ToDecimal(ProductPriceList[i])),
                                            new SqlParameter("@PRODUCTQUANTITY", Convert.ToDecimal(ProductQuantityList[i])),
                                            new SqlParameter("@PRODUCTDIMENSION", Convert.ToDecimal(ProductDimensionList[i])),
                                            new SqlParameter("@PRODUCTUNITPRICE", Convert.ToDecimal(ProductUnitPriceList[i]))};
                DataSet ds2 = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_InsertTransactionDetail", cmdParm2);
                if (ds2.Tables[0].Rows[0]["AdditionalCharges"].ToString() != "-" && ds2.Tables[0].Rows[0]["AdditionalCharges"].ToString() != "[]" && ds2.Tables[0].Rows[0]["AdditionalCharges"].ToString() != "null" && ds2.Tables[0].Rows[0]["AdditionalCharges"].ToString() != null)
                {
                    string[] AdditionalChargesList = ds2.Tables[0].Rows[0]["AdditionalCharges"].ToString().Split(';');
                    foreach (string AdditionalCharge in AdditionalChargesList)
                    {
                        string[] AdditionalChargePriceandDetail = AdditionalCharge.Split('=');
                        SqlParameter[] cmdParm3 = { new SqlParameter("@TRANSACTIONDETAILID", Convert.ToInt32(ds2.Tables[0].Rows[0]["TransactionDetailID"])),
                                                    new SqlParameter("@TRANSACTIONID", Convert.ToInt32(ds.Tables[0].Rows[0]["TransactionID"])),
                                                    new SqlParameter("@STOCKID", Convert.ToInt32(StockIdList[i])),
                                                    new SqlParameter("@ITEMDESCRIPTION", AdditionalChargePriceandDetail[0]),
                                                    new SqlParameter("@PRODUCTPRICE", Convert.ToDecimal(AdditionalChargePriceandDetail[1]))};
                        DataSet ds3 = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_InsertTransactionDetailItem", cmdParm3);
                    }
                }
            }
            //SqlParameter[] cmdParm4 = { new SqlParameter("@TRANSACTIONID", Convert.ToInt32(ds.Tables[0].Rows[0]["TransactionID"]))};
            //DataSet ds4 = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_UpdateTransactionAmount", cmdParm4);


            Result = "[{\"ReturnVal\":\"1\",\"ReturnMsg\":\"had successfully added transaction\",\"TransactionID\":\"" + ds.Tables[0].Rows[0]["TransactionID"] + "\"}]";
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_ViewTransaction")]
        public string Transaction_ViewTransaction(string TrackingStatusID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@TRACKINGSTATUSID", Convert.ToInt32(TrackingStatusID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_ViewTransaction", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].Columns.Add("TransactionDetail");
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    SqlParameter[] cmdParm2 = { new SqlParameter("@TRANSACTIONID", Convert.ToInt32(ds.Tables[0].Rows[i]["TransactionID"])) };
                    DataSet dsDetail = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_ViewTransactionDetailByID", cmdParm2);
                    if (dsDetail.Tables[0].Rows.Count > 0)
                    {

                        ds.Tables[0].Rows[i]["TransactionDetail"] = DataTableToJSONWithJavaScriptSerializer(dsDetail.Tables[0]);
                    }
                }
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_ViewTransactionByUserID")]
        public string Transaction_ViewTransactionByUserID(string USERID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERID", Convert.ToInt32(USERID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_ViewTransactionByUserID", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_ViewTransactionByID")]
        public string Transaction_ViewTransactionByID(string TRANSACTIONID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@TRANSACTIONID", Convert.ToInt32(TRANSACTIONID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_ViewTransactionByID", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].Columns.Add("TransactionDetail");

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {

                    SqlParameter[] cmdParm2 = { new SqlParameter("@TRANSACTIONID", Convert.ToInt32(TRANSACTIONID)) };
                    DataSet dsDetail = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_ViewTransactionDetailByID", cmdParm2);
                    if (dsDetail.Tables[0].Rows.Count > 0)
                    {
                        dsDetail.Tables[0].Columns.Add("TransactionDetailCharges");
                        for (int j = 0; j < dsDetail.Tables[0].Rows.Count; j++)
                        {
                            SqlParameter[] cmdParm3 = { new SqlParameter("@TRANSACTIONDETAILID", Convert.ToInt32(dsDetail.Tables[0].Rows[j]["TransactionDetailID"])) };
                            DataSet dsDetail2 = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_ViewTransactionDetailChargeByID", cmdParm3);
                            if (dsDetail.Tables[0].Rows.Count > 0)
                            {
                                dsDetail.Tables[0].Rows[j]["TransactionDetailCharges"] = DataTableToJSONWithJavaScriptSerializer(dsDetail2.Tables[0]);
                            }
                        }
                        ds.Tables[0].Rows[i]["TransactionDetail"] = DataTableToJSONWithJavaScriptSerializer(dsDetail.Tables[0]);
                    }

                }
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_UpdateTransactionStatus")]
        public string Transaction_UpdateTransactionStatus(string TRANSACTIONID, string TRANSPORTATIONTYPE, string DELIVERYFEE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@TRANSACTIONID", Convert.ToInt32(TRANSACTIONID)),
                                       new SqlParameter("@TRANSPORTATIONTYPE", Convert.ToInt32(TRANSPORTATIONTYPE)),
                                       new SqlParameter("@DELIVERYFEE", Convert.ToDecimal(DELIVERYFEE))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_UpdateTransactionStatus", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_UpdateTransactionPayment")]
        public string Transaction_UpdateTransactionPayment(string TRANSACTIONID, string COMMISSIONID, string PAYMENTAMMOUNT, string PAYMENTMETHOD, string REFERENCENO, string DATETIME)
        {
            string Result = "";
            string[] TRANSACTIONIDList = TRANSACTIONID.Split(';');
            string[] PAYMENTAMMOUNTList = PAYMENTAMMOUNT.Split(';');
            string[] COMMISSIONIDList = COMMISSIONID.Split(';');

            for (int i = 0; i < TRANSACTIONIDList.Length; i++)
            {
                SqlParameter[] cmdParm = { new SqlParameter("@TRANSACTIONID", Convert.ToInt32(TRANSACTIONIDList[i])),
                                           new SqlParameter("@PAYMENTAMMOUNT", Convert.ToDecimal(PAYMENTAMMOUNTList[i])),
                                            new SqlParameter("@COMMISSIONID", COMMISSIONIDList[i]),
                                           new SqlParameter("@PAYMENTMETHOD", PAYMENTMETHOD),
                                           new SqlParameter("@REFERENCENO", REFERENCENO),
                                           new SqlParameter("@DATETIME", DATETIME)};
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_UpdateTransactionPayment", cmdParm);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                }
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_UpdateTransactionDetailHandling")]
        public string Transaction_UpdateTransactionDetailHandling(string TRANSACTIONDETAILID, string PRODUCTHANDLINGPRICE)
        {
            string Result = "";
            string[] TTRANSACTIONDETAILIDList = TRANSACTIONDETAILID.Split(',');
            string[] PRODUCTHANDLINGPRICEList = PRODUCTHANDLINGPRICE.Split(',');
            for (int i = 0; i < TTRANSACTIONDETAILIDList.Length; i++)
            {
                SqlParameter[] cmdParm = { new SqlParameter("@TRANSACTIONDETAILID", Convert.ToInt32(TTRANSACTIONDETAILIDList[i])),
                                           new SqlParameter("@PRODUCTHANDLINGPRICE", Convert.ToDecimal(PRODUCTHANDLINGPRICEList[i]))};
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_UpdateTransactionDetailHandling", cmdParm);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                }
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_ViewArchiveTransaction")]
        public string Transaction_ViewArchiveTransaction(string STARTDATE, string ENDDATE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@STARTDATE", STARTDATE),
                                       new SqlParameter("@ENDDATE", ENDDATE)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_ViewArchiveTransaction", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_ViewArchiveStockListByDate")]
        public string Inventory_ViewArchiveStockListByDate(string STARTDATE, string ENDDATE)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@STARTDATE", STARTDATE),
                                       new SqlParameter("@ENDDATE", ENDDATE)};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_ViewArchiveStockListByDate", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Dashboard_View")]
        public string Dashboard_View()
        {
            string Result = "";
            DataSet ds = new DataSet();
            ds.Tables.Add(new DataTable());
            ds.Tables[0].Columns.Add("CardView");
            ds.Tables[0].Columns.Add("SalesSummary");
            ds.Tables[0].Columns.Add("Sales");
            ds.Tables[0].Columns.Add("SalesByContainerSummary");
            ds.Tables[0].Columns.Add("SalesByContainer");

            DataRow dr = ds.Tables[0].NewRow();
            SqlParameter[] cmdParm = { };
            DataSet dsCard = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Dashboard_ViewCard", cmdParm);
            dr["CardView"] = DataTableToJSONWithJavaScriptSerializer(dsCard.Tables[0]);

            SqlParameter[] cmdParm2 = { new SqlParameter("@STARTDATE", '-'),
                                       new SqlParameter("@ENDDATE", '-')};
            DataSet dsSaleSummary = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Dashboard_SaleSummary", cmdParm2);
            dr["SalesSummary"] = DataTableToJSONWithJavaScriptSerializer(dsSaleSummary.Tables[0]);

            SqlParameter[] cmdParm3 = { new SqlParameter("@STARTDATE", '-'),
                                        new SqlParameter("@ENDDATE", '-')};
            DataSet dsSale = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Dashboard_Sale", cmdParm3);
            dr["Sales"] = DataTableToJSONWithJavaScriptSerializer(dsSale.Tables[0]);

            SqlParameter[] cmdParm4 = { new SqlParameter("@STARTDATE", '-'),
                                       new SqlParameter("@ENDDATE", '-')};
            DataSet dsSaleByContainerSummary = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Dashboard_SaleByContainerSummary", cmdParm4);
            dr["SalesByContainerSummary"] = DataTableToJSONWithJavaScriptSerializer(dsSaleByContainerSummary.Tables[0]);

            SqlParameter[] cmdParm5 = { new SqlParameter("@STARTDATE", '-'),
                                       new SqlParameter("@ENDDATE", '-')};
            DataSet dsSaleByContainer = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Dashboard_SaleByContainer", cmdParm5);
            dr["SalesByContainer"] = DataTableToJSONWithJavaScriptSerializer(dsSaleByContainer.Tables[0]);
            ds.Tables[0].Rows.Add(dr);
            Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Transaction_DeleteTransaction")]
        public string Transaction_DeleteTransaction(string TRANSACTIONID)
        {
            string Result = "";
            string[] TRANSACTIONIDList = TRANSACTIONID.Split(',');

            for (int i = 0; i < TRANSACTIONIDList.Length; i++)
            {
                SqlParameter[] cmdParm = { new SqlParameter("@TRANSACTIONID", Convert.ToInt32(TRANSACTIONIDList[i])) };
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Transaction_DeleteTransaction", cmdParm);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                }
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_AddUserAreaCode")]
        public string User_AddUserAreaCode(string AREACODE, string AREANAME, string MODIFY, string AGENDIND, string USERCODE, string MINIMUMCUBIC, string AREACHARGES,
                                            string SELFPICKSMALL, string CONSOLIDATE, string LARGEDELIVERY, string SMALLDELIVERYFIRST, string SMALLDELIVERYSUB)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@AREACODE", AREACODE),
                                       new SqlParameter("@AREANAME", AREANAME),
                                       new SqlParameter("@MODIFYBY", Convert.ToInt32(MODIFY)),
                                       new SqlParameter("@AGENDIND", Convert.ToInt32(AGENDIND)),
                                       new SqlParameter("@MINIMUMCUBIC", Convert.ToDecimal(MINIMUMCUBIC)),
                                       new SqlParameter("@USERCODE", USERCODE),
                                       new SqlParameter("@AREACHARGES", Convert.ToDecimal(AREACHARGES)),
                                       new SqlParameter("@SELFPICKSMALL", Convert.ToDecimal(SELFPICKSMALL)),
                                       new SqlParameter("@CONSOLIDATE", Convert.ToDecimal(CONSOLIDATE)),
                                       new SqlParameter("@LARGEDELIVERY", Convert.ToDecimal(LARGEDELIVERY)),
                                       new SqlParameter("@SMALLDELIVERYSUB", Convert.ToDecimal(SMALLDELIVERYSUB)),
                                       new SqlParameter("@SMALLDELIVERYFIRST", Convert.ToDecimal(SMALLDELIVERYFIRST))
            };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_AddUserAreaCode", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }


        [HttpGet]
        [Route("api/EZLogistic/User_UpdateUserAreaCode")]
        public string User_UpdateUserAreaCode(string USERAREAID, string AREACODE, string AREANAME, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERAREAID", Convert.ToInt32(USERAREAID)),
                                       new SqlParameter("@AREACODE", AREACODE),
                                       new SqlParameter("@AREANAME", AREANAME),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_UpdateUserAreaCode", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_DeleteUserAreaCode")]
        public string User_DeleteUserAreaCode(string USERAREAID, string MODIFY)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERAREAID", Convert.ToInt32(USERAREAID)),
                                       new SqlParameter("@MODIFY", Convert.ToInt32(MODIFY))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_DeleteUserAreaCode", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }


        [HttpGet]
        [Route("api/EZLogistic/User_UpdateAreaCode")]
        public string User_UpdateAreaCode(string USERAREAID, string AREACODE, string AREANAME, string AGENDIND, string USERCODE, string MINIMUMCUBIC, string AREACHARGES, string MODIFY,
          string SELFPICKSMALL, string CONSOLIDATE, string LARGEDELIVERY, string SMALLDELIVERYFIRST, string SMALLDELIVERYSUB)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERAREAID", Convert.ToInt32(USERAREAID)),
                                       new SqlParameter("@AREACODE", AREACODE),
                                       new SqlParameter("@AREANAME", AREANAME),
                                       new SqlParameter("@AGENDIND", Convert.ToInt32(AGENDIND)),
                                       new SqlParameter("@USERCODE", USERCODE),
                                       new SqlParameter("@MINIMUMCUBIC", Convert.ToDecimal(MINIMUMCUBIC)),
                                       new SqlParameter("@MODIFYBY", Convert.ToInt32(MODIFY)),
                                       new SqlParameter("@AREACHARGES", Convert.ToDecimal(AREACHARGES)),
                                       new SqlParameter("@SELFPICKSMALL", Convert.ToDecimal(SELFPICKSMALL)),
                                       new SqlParameter("@CONSOLIDATE", Convert.ToDecimal(CONSOLIDATE)),
                                       new SqlParameter("@LARGEDELIVERY", Convert.ToDecimal(LARGEDELIVERY)),
                                       new SqlParameter("@SMALLDELIVERYSUB", Convert.ToDecimal(SMALLDELIVERYSUB)),
                                       new SqlParameter("@SMALLDELIVERYFIRST", Convert.ToDecimal(SMALLDELIVERYFIRST))


                };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_UpdateAreaCode", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_DeleteAreaCode")]
        public string User_DeleteAreaCode(string USERAREAID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERAREAID", Convert.ToInt32(USERAREAID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_DeleteAreaCode", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/Report_ViewStockSummary")]
        public string Report_ViewStockSummary()
        {
            string Result = "";
            SqlParameter[] cmdParm = { };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Report_ViewStockSummary", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_CalculateParcelPrice")]
        public string User_CalculateParcelPrice(string SELECTEDAREAID, string USERID, string PARCELWEIGHT, string PARCELWIDTH, string PARCELHEIGHT, string PARCELLENGTH)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@SELECTEDAREAID", Convert.ToInt32(SELECTEDAREAID)),
                                    new SqlParameter("@USERID", Convert.ToInt32(USERID)),
                                    new SqlParameter("@PARCELWEIGHT", Convert.ToDecimal(PARCELWEIGHT)),
                                    new SqlParameter("@PARCELWIDTH", Convert.ToDecimal(PARCELWIDTH)),
                                    new SqlParameter("@PARCELHEIGHT", Convert.ToDecimal(PARCELHEIGHT)),
                                    new SqlParameter("@PARCELLENGTH", Convert.ToDecimal(PARCELLENGTH))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_CalculateParcelPrice", cmdParm);
            return ReturnDataSet(ds);
        }

        [HttpGet]
        [Route("api/EZLogistic/Inventory_UpdateStockUser")]
        public string Inventory_UpdateStockUser(string STOCKID, string USERID)
        {
            string Result = "";
            string[] STOCKIDs = STOCKID.Split(',');
            for (int i = 0; i < STOCKIDs.Length; i++)
            {
                SqlParameter[] cmdParm = { new SqlParameter("@STOCKID", STOCKIDs[i]),
                                           new SqlParameter("@USERID", Convert.ToInt32(USERID))};
                DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.Inventory_UpdateStockUser", cmdParm);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
                }
                else
                {
                    Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
                }
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/User_ViewGeneralSetting")]
        public string User_ViewGeneralSetting(string USERID)
        {
            string Result = "";
            SqlParameter[] cmdParm = { new SqlParameter("@USERID", Convert.ToInt32(USERID)) };
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.User_ViewGeneralSetting", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = DataTableToJSONWithJavaScriptSerializer(ds.Tables[0]);
            }
            else
            {
                Result = "[{\"ReturnVal\":0,\"ReturnMsg\":\"" + no_data_msg + "\"}]";
            }
            return Result;
        }

        [HttpGet]
        [Route("api/EZLogistic/General_TriggerEmail")]
        public string General_TriggerEmail(string SUBJECT, string BODY, string RECIPIENTS)
        {
            DataSet ds = new DataSet();
            SqlParameter[] cmdParm = { new SqlParameter("@SUBJECT", SUBJECT),
                                       new SqlParameter("@BODY", BODY),
                                       new SqlParameter("@RECIPIENTS", RECIPIENTS)};
            ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.General_TriggerEmail", cmdParm);
            return ReturnDataSet(ds);
        }

    }
}
