namespace UrlShortener.Models
{
    public class UrlData
    {
        public int ID { get; set; }
        public string Url { get; set; }
        public string ShortenedURL { get; set; }
        public string ShortUrlPath { get; set; }
    }
}
