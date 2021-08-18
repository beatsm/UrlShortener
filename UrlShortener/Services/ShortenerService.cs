using System;
using UrlShortener.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace UrlShortener.Services
{
    public class ShortenerService : IShortenerService
    {
        private readonly UrlShortenerContext _context;
        private readonly IShortUrlPathGenerator _shortUrlPathGenerator;

        public ShortenerService(UrlShortenerContext context, IShortUrlPathGenerator shortUrlPathGenerator)
        {
            _context = context;
            _shortUrlPathGenerator = shortUrlPathGenerator;
        }

        private bool IsUrlValid(string url)
        {
            string pattern = @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()!@:%_\+.~#?&\/\/=]*)";
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(url);
        }

        public async Task<UrlData> ShortenUrlAsync(string url, string requestPath)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Url to shorten cannot be empty");
            }

            if (!url.StartsWith("http"))
            {
                url = "http://" + url;
            }

            if (!IsUrlValid(url))
                throw new ArgumentException("This is not a valid Url");            

            var urlData = ReturnAlreadyShortenedUrl(url);
            if (urlData != null)
                return urlData;

            var shortUrlPath = await GenerateUniqueShortUrlPathAsync();

            urlData = new UrlData
            {
                ShortUrlPath = shortUrlPath,
                Url = url,
                ShortenedURL = requestPath + shortUrlPath,
            };

            await _context.UrlData.AddAsync(urlData);
            await _context.SaveChangesAsync();
            
            return urlData;
        }

        private UrlData ReturnAlreadyShortenedUrl(string url)
        {
            return (from d in _context.UrlData
                    where d.Url == url
                    select d).FirstOrDefault();
        }

        private async Task<string> GenerateUniqueShortUrlPathAsync()
        {
            var shortUrlPathExists = true;
            string tempUrlPath = string.Empty;

            while (shortUrlPathExists)
            {
                tempUrlPath = _shortUrlPathGenerator.GenerateShortUrlPath();

                var shortUrlNotUsed = await _context.UrlData.Where(b => b.ShortUrlPath == tempUrlPath)
                    .FirstOrDefaultAsync() == null;

                if (shortUrlNotUsed)
                {
                    shortUrlPathExists = false;
                }
            }
            return tempUrlPath;
        }

        public async Task<string> GetRedirectionUrl(string shortUrl)
        {
            var urlData = await _context.UrlData.Where(b => b.ShortUrlPath == shortUrl)
                    .FirstOrDefaultAsync();

            if (urlData == null)
                throw new ArgumentException("Url Not Found");

            return urlData.Url;
        }     
    }
}
