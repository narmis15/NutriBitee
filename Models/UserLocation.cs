using System;

namespace NUTRIBITE.Models
{
    /// <summary>
    /// Structured user location returned by geocoding service and stored in session.
    /// </summary>
    public class UserLocation
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? City { get; set; }
        public string? Area { get; set; }         // neighbourhood / locality / suburb
        public string? State { get; set; }
        public string? Pincode { get; set; }      // postal code
        public string? FullAddress { get; set; }  // formatted address
    }
}