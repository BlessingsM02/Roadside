

namespace Roadside.Models
{
    internal class Request
    {
        public string RequestId { get; set; }
        public string MobileNumber { get; set; }       
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }
}
