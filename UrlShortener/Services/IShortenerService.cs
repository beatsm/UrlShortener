using System.Threading.Tasks;
using UrlShortener.Models;

namespace UrlShortener.Services
{
    public interface IShortenerService
    {
        Task<UrlData> ShortenUrlAsync(string url, string requestPath);
        Task<string> GetRedirectionUrl(string shortUrlPath);
    }
}