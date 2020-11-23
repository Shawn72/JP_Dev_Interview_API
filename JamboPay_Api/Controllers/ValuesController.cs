using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Http;
using JamboPay_Api.Models;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace JamboPay_Api.Controllers
{
    [BasicAuthentication]
    public class ValuesController : ApiController
    {
        ///for use on localhost testings
        public static string Baseurl = ConfigurationManager.AppSettings["API_LOCALHOST_URL"];
        
        /// API Authentications
        public static string ApiUsername = ConfigurationManager.AppSettings["API_USERNAME"];
        public static string ApiPassword = ConfigurationManager.AppSettings["API_PWD"];
        /// API Authentications
       
        /// MySQL Connection string
        public static readonly string ConString = @"datasource=localhost;port=3306;username=root;password=root;database=jambopay";

        /// <summary>
        /// START: Agents and Commisions functions
        /// </summary>

        [Route("api/Values")]
        [HttpPost]
        [Route("api/AddAgent")]
        public IHttpActionResult AddAgent([FromBody] SignUpModel signUpModel)
        {
            MySqlTransaction mysqlTrx = (dynamic)null;
            MySqlConnection con = (dynamic)null;
            try
            {
                if (!ModelState.IsValid)
                { return BadRequest(ModelState); }

                //now add supplied data to dB

                if (string.IsNullOrWhiteSpace(signUpModel.Email))
                    return Json("EmailEmpty");

                using ( con = new MySqlConnection(ConString))
                {
                    string insertQry =
                        "INSERT INTO agents(agent_email_id) VALUES(@agentEmailId)";

                    con.Open();
                    MySqlCommand command = new MySqlCommand(insertQry, con);
                    // Start a local transaction
                    mysqlTrx = con.BeginTransaction();
                    // assign both transaction object and connection to Command object for a pending local transaction
                    command.Connection = con;
                    command.Transaction = mysqlTrx;

                    //use parametarized queries to prevent sql injection
                    command.Parameters.AddWithValue("@agentEmailId",signUpModel.Email);

                    if (command.ExecuteNonQuery() == 1)
                    {
                        //commit the insert transaction to dB 
                        mysqlTrx.Commit();
                        return Ok("success");
                    }
                    con.Close();
                    return Ok("Error Occured!");
                }

            }
            catch (MySqlException ex)
            {
                try
                {
                    //rollback the transaction if any error occurs during the process of inserting
                    con.Open();
                    mysqlTrx.Rollback();
                    con.Close();
                }
                catch (MySqlException ex2)
                {
                    if (mysqlTrx.Connection != null)
                    {
                        return Ok("An exception of type " + ex2.GetType() + " was encountered while attempting to roll back the transaction!");
                    }
                }
                return Ok("No record was inserted due to this error: " + ex.Message);
            }
            finally
            {
                con.Close();
            }
        }


        [HttpPost]
        [Route("api/AddService")]
        public IHttpActionResult AddService([FromBody] ServiceModel serviceModel)
        {
            MySqlTransaction mysqlTrx = (dynamic)null;
            MySqlConnection con = (dynamic)null;
            try
            {
                if (!ModelState.IsValid)
                { return BadRequest(ModelState); }

                //now add supplied data to dB
                if (string.IsNullOrWhiteSpace(serviceModel.ServiceCode))
                    return Json("svcCodeEmpty");

                if (string.IsNullOrWhiteSpace(serviceModel.ServiceName))
                    return Json("svcNameEmpty");

                if (string.IsNullOrWhiteSpace(serviceModel.ServiceCommisionPercent.ToString(CultureInfo.InvariantCulture)))
                    return Json("svcCommissionEmpty");

                using ( con = new MySqlConnection(ConString))
                {
                    string insertQry =
                        "INSERT INTO services(service_name, service_code, jambo_comm_percent) VALUES(@serviceName, @serviceCode, @serviceCommission )";

                    con.Open();
                  
                    MySqlCommand command = new MySqlCommand(insertQry, con);
                    // Start a local transaction
                    mysqlTrx = con.BeginTransaction();
                    // assign both transaction object and connection to Command object for a pending local transaction
                    command.Connection = con;
                    command.Transaction = mysqlTrx;

                    //use parametarized queries to prevent sql injection
                    command.Parameters.AddWithValue("@serviceName", serviceModel.ServiceName);
                    command.Parameters.AddWithValue("@serviceCode", serviceModel.ServiceCode);
                    command.Parameters.AddWithValue("@serviceCommission", serviceModel.ServiceCommisionPercent);

                    if (command.ExecuteNonQuery() == 1)
                    {
                        //commit the insert transaction to dB 
                        mysqlTrx.Commit();
                        return Ok("success");
                    }
                    con.Close();
                    return Ok("Error Occured!");
                }
            }
            catch (MySqlException ex)
            {
                try
                {
                    //rollback the transaction if any error occurs during the process of inserting
                    con.Open();
                    mysqlTrx.Rollback();
                    con.Close();
                }
                catch (MySqlException ex2)
                {
                    if (mysqlTrx.Connection != null)
                    {
                        return Ok("An exception of type " + ex2.GetType() +" was encountered while attempting to roll back the transaction!");
                    }
                }
            
            return Ok("No record was inserted due to this error: "+ex.Message);
            }
            finally
            {
                con.Close();
            }
        }

        [HttpPost]
        [Route("api/PostTransaction")]
        public IHttpActionResult PostTransaction([FromBody] CommissionModel commissionModel)
        {
            MySqlTransaction mysqlTrx = (dynamic)null;
            MySqlConnection con = (dynamic)null;
            try
            {

                WebClient wc = new WebClient();
                wc.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(ApiUsername + ":" + ApiPassword)));
                var totalCommission = (dynamic)null;
                var agentPaidAmt = (dynamic)null;
                var jamboPaidAmt = (dynamic)null;

                //get agent Id
                string agentsjson = wc.DownloadString(Baseurl + "api/GetAgents");
                var agts = JsonConvert.DeserializeObject<List<ServiceModel>>(agentsjson);
                var agtresults = (from a in agts where a.agent_email_id == commissionModel.AgentEmailId select a.a_id).SingleOrDefault();
                int agentId = Convert.ToInt32(agtresults);


                //get service commission
                string json = wc.DownloadString(Baseurl + "api/GetServices");
                var svcs = JsonConvert.DeserializeObject<List<ServiceModel>>(json);
                var jsresult = (from a in svcs where a.s_id == commissionModel.ServiceId select a.jambo_comm_percent).SingleOrDefault();

                //get total commission % here
                decimal srvComm = jsresult;
                decimal srvFee = Convert.ToDecimal(commissionModel.TransactionCost);
                totalCommission = (srvComm / 100) * srvFee;

                //subdivide commision between JamboPay and agents

                //JamboPay gets
                jamboPaidAmt = Convert.ToDecimal(0.6) * totalCommission;

                //Agent gets
                agentPaidAmt = Convert.ToDecimal(0.4) * totalCommission;

                if (string.IsNullOrWhiteSpace(commissionModel.TransactionCost.ToString(CultureInfo.InvariantCulture)))
                    return Json("lsvcFeeEmpty");

                using ( con = new MySqlConnection(ConString))
                {
                    string insertQry =
                        "INSERT INTO agent_transactions(service_id, agent_id, agent_transaction_cost, agent_commision_amt, jambo_commission_amt) " +
                        "VALUES(@serviceId, @agentId, @trxCost, @agentCommAmt, @jamboCommAmt)";

                    con.Open();
                    MySqlCommand command = new MySqlCommand(insertQry, con);
                    // Start a local transaction
                    mysqlTrx = con.BeginTransaction();
                    // assign both transaction object and connection to Command object for a pending local transaction
                    command.Connection = con;
                    command.Transaction = mysqlTrx;

                    //use parametarized queries to prevent sql injection
                    command.Parameters.AddWithValue("@serviceId", commissionModel.ServiceId);
                    command.Parameters.AddWithValue("@agentId", agentId);
                    command.Parameters.AddWithValue("@trxCost", commissionModel.TransactionCost);
                    command.Parameters.AddWithValue("@agentCommAmt", agentPaidAmt);
                    command.Parameters.AddWithValue("@jamboCommAmt", jamboPaidAmt);

                    if (command.ExecuteNonQuery() == 1)
                    {
                        //commit the insert transaction to dB 
                        mysqlTrx.Commit();
                        return Ok("success");
                    }
                    con.Close();
                    return Ok("Error Occured!");
                }
            }
            catch (MySqlException ex)
            {
                try
                {
                    //rollback the transaction if any error occurs during the process of inserting
                    con.Open();
                    mysqlTrx.Rollback();
                    con.Close();
                }
                catch (MySqlException ex2)
                {
                    if (mysqlTrx.Connection != null)
                    {
                        return Ok("An exception of type " + ex2.GetType() + " was encountered while attempting to roll back the transaction!");
                    }
                }
                return Ok("No record was inserted due to this error: " + ex.Message);
            }
            finally
            {
                con.Close();
            }
        }
        /// <summary>
        /// END: Agents and Commisions functions
        /// </summary>


        /// <summary>
        /// Getter Functions
        /// </summary>

        [HttpGet]
        [Route("api/GetServices")]
        public IHttpActionResult GetServices()
        {
            using (MySqlConnection con = new MySqlConnection(ConString))
            {
                con.Open();
                string selectQuery = "SELECT * FROM services";
                MySqlCommand command0 = new MySqlCommand(selectQuery, con);
                command0.ExecuteNonQuery();
                DataTable dt = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(command0);
                da.Fill(dt);
                con.Close();
                return Json(dt);
            }
        }

        [HttpGet]
        [Route("api/GetAgentTransactions")]
        public IHttpActionResult GetAgentTransactions([FromBody] TransactionsModel transactionsModel)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(ConString))
                {
                    con.Open();

                    string checkifAgentExists = "SELECT * FROM agents WHERE a_id = @aid LIMIT 1";
                    //check if agent exists first
                    MySqlCommand commandX = new MySqlCommand(checkifAgentExists, con);

                    //use parametarized queries to prevent sql injection
                    commandX.Parameters.AddWithValue("@aid", transactionsModel.AgentId);

                    int agentIsThere = (int)commandX.ExecuteScalar();

                    if (agentIsThere == 1)
                    {
                        string selectQuery = "SELECT a.agent_email_id, s.service_name, t.agent_transaction_cost " +
                                             "FROM agent_transactions t, services s, agents a WHERE t.service_id = s.s_id AND t.agent_id = a.a_id AND t.agent_id = @agentId";
                        //continue here if theAgents exists
                        MySqlCommand command0 = new MySqlCommand(selectQuery, con);
                        //use parametarized queries to prevent sql injection
                        command0.Parameters.AddWithValue("@agentId", transactionsModel.AgentId);
                        command0.ExecuteNonQuery();
                        DataTable dt = new DataTable();
                        MySqlDataAdapter da = new MySqlDataAdapter(command0);
                        da.Fill(dt);
                        con.Close();
                        return Ok(dt);
                    }
                    return Ok("Agent not in Records yet!");
                }
            }
            catch (Exception e)
            {
                return Ok(e.Message);
            }
           
        }

        [HttpGet]
        [Route("api/GetAgents")]
        public IHttpActionResult GetAgents()
        {
            using (MySqlConnection con = new MySqlConnection(ConString))
            {
                con.Open();
                string selectQuery = "SELECT * FROM agents";
                MySqlCommand command0 = new MySqlCommand(selectQuery, con);
                command0.ExecuteNonQuery();
                DataTable dt = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(command0);
                da.Fill(dt);
                con.Close();
                return Json(dt);
            }
        }

        [HttpGet]
        [Route("api/GetAgentCommission")]
        public IHttpActionResult GetAgentCommission([FromBody] TransactionsModel transactionsModel)
        {
            try
            {
                var totalCommission = (dynamic)null;

                using (MySqlConnection con = new MySqlConnection(ConString))
                {
                    con.Open();

                    string checkifAgentExists = "SELECT * FROM agents WHERE a_id = @aid LIMIT 1";
                    //check if agent exists first
                    MySqlCommand commandX = new MySqlCommand(checkifAgentExists, con);

                    //use parametarized queries to prevent sql injection
                    commandX.Parameters.AddWithValue("@aid", transactionsModel.AgentId);

                    int agentIsThere = (int)commandX.ExecuteScalar();
                    if (agentIsThere == 1)
                    {
                        string selectQuery = "SELECT SUM(agent_commision_amt) AS totalComm FROM agent_transactions WHERE agent_id = @agentId";
                        //continue here if theAgents exists
                        MySqlCommand command0 = new MySqlCommand(selectQuery, con);
                        //use parametarized queries to prevent sql injection
                        command0.Parameters.AddWithValue("@agentId", transactionsModel.AgentId);
                        command0.ExecuteNonQuery();
                        DataTable dt = new DataTable();
                        MySqlDataAdapter da = new MySqlDataAdapter(command0);
                        da.Fill(dt);
                        foreach (DataRow dr in dt.Rows)
                        { totalCommission = dr["totalComm"]; }
                        con.Close();
                        return Ok("Total agent Commission: "+totalCommission);
                    }
                    return Ok("Agent not in Records yet!");
                }
            }
            catch (MySqlException ex)
            {
                return Ok(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/GetSpecificAgentTotalCommission")]
        public IHttpActionResult GetSpecificAgentTotalCommission([FromBody] TransactionsModel transactionsModel)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(ConString))
                {
                    con.Open();
                    string selectQuery = "SELECT a.agent_email_id, SUM(agent_commision_amt) AS agent_total_commission FROM agents a, agent_transactions tr WHERE a.a_id = tr.agent_id GROUP BY tr.agent_id";

                    MySqlCommand command0 = new MySqlCommand(selectQuery, con);
                    command0.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    MySqlDataAdapter da = new MySqlDataAdapter(command0);
                    da.Fill(dt);
                    return Ok(dt);

                }
            }
            catch (MySqlException ex)
            {
                return Ok(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/GetJamboPayRevenue")]
        public IHttpActionResult GetJamboPayRevenue([FromBody] TransactionsModel transactionsModel)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(ConString))
                {
                    con.Open();
                    string selectQuery = "SELECT s.service_name, SUM(jambo_commission_amt) AS jambopay_total_revenue FROM services s, agent_transactions tr WHERE s.s_id = tr.service_id GROUP BY tr.service_id";

                    MySqlCommand command0 = new MySqlCommand(selectQuery, con);
                    command0.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    MySqlDataAdapter da = new MySqlDataAdapter(command0);
                    da.Fill(dt);
                    return Ok(dt);
                   
                }
            }
            catch (MySqlException ex)
            {
                return Ok(ex.Message);
            }
        }
    }
}
