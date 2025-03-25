namespace APIKSA.Models
{
    public class ReportInvoiceInput
    {
        public int CompanyId { get; set; }
        public string VoucherId { get; set; }
        public string SajayaClientID { get; set; }
        //public string MainSajayaClientID { get; set; }
        public string SubSajayaClientID { get; set; }
        public string Username { get; set; }
     //   public string Password { get; set; }
    }
}