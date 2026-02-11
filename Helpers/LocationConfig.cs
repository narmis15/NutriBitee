namespace NUTRIBITE.Helpers
{
    public static class LocationConfig
    {
        // Single source of truth for address + coordinates
        public static string AddressLine => "12 Greenway Lane";
        public static string City => "Bangalore";
        public static string State => "Karnataka";
        public static string Country => "India";
        public static string FullAddress => $"{AddressLine}, {City}, {State}, {Country}";

        // Latitude / Longitude for pin (use your actual coordinates)
        public static double Latitude => 12.9715987;  // example: Bangalore center
        public static double Longitude => 77.5945627;
    }
}