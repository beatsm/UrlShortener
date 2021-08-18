using System;

namespace UrlShortener.Services
{
    public class ShortUrlPathGenerator : IShortUrlPathGenerator
    {
        public string GenerateShortUrlPath()
        {
            var safeUrlChars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789";
            return safeUrlChars.Substring(new Random().Next(0, safeUrlChars.Length), new Random().Next(2, 6));
        }
    }
}