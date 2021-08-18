using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UrlShortener.Models;
using UrlShortener.Services;

namespace UrlShortener.Controllers
{
    public class UrlShortenerController : Controller
    {
        private readonly IShortenerService _shortenerService;

        public UrlShortenerController(IShortenerService shortenerService)
        {
            _shortenerService = shortenerService;
        }

        [HttpGet, Route("/")]
        public IActionResult Index()
        {
            return View("Index");
        }

        [HttpPost, Route("/")]
        public async Task<IActionResult> PostURLAsync([FromBody] string url)
        {
            string requestPath = GetRequestPath();
            try
            {
                var urlData = await _shortenerService.ShortenUrlAsync(url, requestPath);               
                var response = new UrlShortenerResponse
                {
                    Result = true,
                    Url = urlData.ShortenedURL
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new UrlShortenerResponse { Result = false, Error = ex.Message });
            }            
        }

        [HttpGet, Route("/{shortUrlPath}")]
        public async Task<IActionResult> UrlRedirectAsync([FromRoute] string shortUrlPath)
        {
            string redirectUrl = string.Empty;
            try
            {
                redirectUrl = await _shortenerService.GetRedirectionUrl(shortUrlPath);
            }
            catch (Exception)
            {
                return View("NotFound");
            }            

            return Redirect(redirectUrl);
        }

        private string GetRequestPath()
        {
            return $"{Request.Scheme}://{Request.Host}/";
        }

        public IActionResult Privacy()
        {
            return View("Privacy");
        }        
    }
}
