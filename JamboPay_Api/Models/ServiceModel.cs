namespace JamboPay_Api.Models
{
    public class ServiceModel
    {
        //for posting
        public string ServiceName { get; set; }
        public string ServiceCode { get; set; }
        public string ServiceId { get; set; }
        public decimal ServiceCommisionPercent { get; set; }

        //for getting agents
        public string agent_email_id { get; set; }
        public int a_id { get; set; }

        //for getting services
        public int s_id { get; set; }
        public string service_name { get; set; }
        public string service_code { get; set; }
        public decimal jambo_comm_percent { get; set; }

    }
}