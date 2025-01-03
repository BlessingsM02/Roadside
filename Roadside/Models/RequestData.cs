﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadside.Models
{
    internal class RequestData
    {
        public string ServiceProviderId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double ServiceProviderLatitude { get; set; }
        public double ServiceProviderLongitude { get; set; }
        public string DriverId { get; set; }
        public string Status { get; set; }
        public double Price { get; set; }
        public DateTime Date { get; set; }
        public int RatingId { get; set; }
        public string CancellationReason { get; internal set; }
        public object ServiceProviderName { get; internal set; }
    }
}
