namespace APIKSA.Models
{
    public class InvoiceHelperInput
    {
        public int CompanyId { get; set; }
        public int FiscalYearId { get; set; }
        public int VoucherTypeId { get; set; }
        public int VoucherNo { get; set; }
        public string SajayaClientID { get; set; }
        public string MainSajayaClientID { get; set; }
        public string SubSajayaClientID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsStandard { get; set; }
        public bool IsWarnings { get; set; }
    }
}