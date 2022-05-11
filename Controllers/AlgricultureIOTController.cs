using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Net.Http;
using System.Net;

namespace AlgricultureIOTApi.Controllers
{
    public class AlgricultureIOTController : ApiController
    {
        public string constr_tour = ConfigurationManager.ConnectionStrings["EZLOGISTIC"].ConnectionString;
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

        [HttpGet]
        [Route("api/A/D")]
        public int User_Login(string TEMP)
        {
            int Result = 0;
            SqlParameter[] cmdParm = { new SqlParameter("@DATAVALUETEMP", Convert.ToDecimal(TEMP)),
                                       new SqlParameter("@DATAVALUEHUM",  Convert.ToDecimal("1.0"))};
            DataSet ds = Models.SQLHelper.ExecuteQuery(constr_tour, null, CommandType.StoredProcedure, "dbo.DataCollection_Environmental", cmdParm);
            if (ds.Tables[0].Rows.Count > 0)
            {
                Result = 1;
            }
            else
            {
                Result = 0;
            }
            return Result;
        }
    }
}
