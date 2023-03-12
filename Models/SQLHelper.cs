using System;
using System.EnterpriseServices;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Data;
using System.Data.SqlClient;

namespace NewEZLogistic.Models
{
    public class SQLHelper : System.Web.Services.WebService
    {
        public static int ExecuteNonQuery(string connString, CommandType cmdType, string cmdText, params SqlParameter[] cmdParms)
        {
            SqlCommand cmd = new SqlCommand();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParms);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static DataSet ExecuteQuery(string connString, SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] cmdParms)
        {
            SqlDataAdapter adp;
            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                try
                {
                    cmd.CommandTimeout = 100000;
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParms);
                    adp = new SqlDataAdapter(cmd);
                    adp.Fill(ds);
                    cmd.Parameters.Clear();
                    return ds;
                }
                catch
                {
                    conn.Close();
                    throw;
                }
            }
        }

        public static int ExecuteQueryReturnInt(string connString, SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] cmdParms)
        {
            SqlDataAdapter adp;
            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                try
                {
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParms);
                    adp = new SqlDataAdapter(cmd);
                    adp.Fill(ds);
                    cmd.Parameters.Clear();
                    return Convert.ToInt32(ds.Tables[0].Rows[0][0].ToString());
                }
                catch
                {
                    conn.Close();
                    throw;
                }
            }
        }

        public static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] cmdParms)
        {

            if (conn.State != ConnectionState.Open)
                conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            //cmd.
            if (trans != null)
                cmd.Transaction = trans;

            cmd.CommandType = cmdType;

            if (cmdParms != null)
            {
                foreach (SqlParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }

        }
    }
}