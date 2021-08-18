namespace UrlShortener.Models
{
    public class UrlShortenerResponse
    {
        public bool Result { get; set; }
        public string Url { get; set; }
        public string Error { get; set; }
    }
}
