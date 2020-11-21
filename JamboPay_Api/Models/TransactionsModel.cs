namespace JamboPay_Api.Models
{
    public class TransactionsModel
    {
        //for getting
        public string agent_id { get; set; }
        public decimal agent_transaction_cost { get; set; }
        public decimal agent_commision_amt { get; set; }
        public decimal jambo_commission_amt { get; set; }

        //for fetching
        public string AgentId { get; set; }
    }
}