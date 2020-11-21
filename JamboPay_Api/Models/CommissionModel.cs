namespace JamboPay_Api.Models
{
    public class CommissionModel
    {
        public string AgentEmailId { get; set; }
        public int ServiceId { get; set; }
        public decimal TransactionCost { get; set; }
        public decimal JamboCommPaid { get; set; }
        public decimal AgentCommPaid { get; set; }
    }
}