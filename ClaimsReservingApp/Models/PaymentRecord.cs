using CsvHelper.Configuration.Attributes;

namespace ClaimsReservingApp.Models
{
    public class PaymentRecord
    {
        [Index(0)]
        public string Product { get; set; }

        [Index(1)]
        public int? OriginYear { get; set; }

        [Index(2)]
        public int DevelopmentYear { get; set; }

        [Index(3)]
        public decimal? IncrementalValue { get; set; }
    }
}