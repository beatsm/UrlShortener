using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UrlShortener.Controllers;
using UrlShortener.Models;
using UrlShortener.Services;

namespace UrlShortener.Tests.ControllerTests
{
    public class UrlShortenerControllerTests
    {
        [Test]
        public void UrlShortenerController_Index_ReturnIndexView()
        {
            var shortenerService = new Mock<IShortenerService>();
            var controller = new UrlShortenerController(shortenerService.Object);
            var result = controller.Index() as ViewResult;
            Assert.AreEqual("Index", result.ViewName);
        }

        private static DbContextOptions<UrlShortenerContext> CreateNewContextOptions()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var builder = new DbContextOptionsBuilder<UrlShortenerContext>();
            builder.UseInMemoryDatabase("UrlShortener")
                .UseInternalServiceProvider(serviceProvider);

            return builder.Options;
        }

        [Test]
        public async Task UrlShortenerController_PostURLAsync_ReturnsShortenedUrlAsync()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");

            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var mockShortUrlPathGenerator = new Mock<IShortUrlPathGenerator>();
            mockShortUrlPathGenerator.Setup(c => c.GenerateShortUrlPath())
                .Returns("YyZz0");

            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);            
            var controller = new UrlShortenerController(service)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext
                }
            };

            var result = await controller.PostURLAsync("www.google.com") as JsonResult;

            var o = (UrlShortenerResponse)result.Value;        
            Assert.AreEqual("http://localhost/YyZz0", o.Url);
        }

        [Test]
        public async Task UrlShortenerController_PostURLAsync_ExceptionThrown_ReturnsErrorJson()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");

            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var mockShortUrlPathGenerator = new Mock<IShortUrlPathGenerator>();
            mockShortUrlPathGenerator.Setup(c => c.GenerateShortUrlPath())
                .Throws(new Exception("Unable to generate shortUrl"));

            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var controller = new UrlShortenerController(service)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext
                }
            };

            var result = await controller.PostURLAsync("www.google.com") as JsonResult;

            var urlShortenerResponse = (UrlShortenerResponse)result.Value;
            Assert.AreEqual("Unable to generate shortUrl", urlShortenerResponse.Error);
        }

        [Test]
        public void UrlShortenerController_Privacy_ReturnPrivacyView()
        {
            var shortenerService = new Mock<IShortenerService>();
            var controller = new UrlShortenerController(shortenerService.Object);
            var result = controller.Privacy() as ViewResult;
            Assert.AreEqual("Privacy", result.ViewName);
        }

        [Test]
        public async Task UrlShortenerController_UrlRedirectAsync_ReturnsStoredFullUrl()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");

            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var one = new UrlData
            {
                ID = 1,
                Url = "http://www.google.com",
                ShortenedURL = "http://localhost/YyZz0",
                ShortUrlPath = "YyZz0"
            };

            context.UrlData.Add(one);
            context.SaveChanges();

            var mockShortUrlPathGenerator = new Mock<IShortUrlPathGenerator>();
            mockShortUrlPathGenerator.Setup(c => c.GenerateShortUrlPath())
                .Returns("YyZz0");

            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);            
            var controller = new UrlShortenerController(service)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext,
                }
            };

            var response = await controller.UrlRedirectAsync("YyZz0") as RedirectResult;

            Assert.AreEqual("http://www.google.com", response.Url);
        }

        [Test]
        public async Task UrlShortenerController_UrlRedirectAsync_UrlNotFound_ReturnsNotFoundView()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");

            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var mockShortUrlPathGenerator = new Mock<IShortUrlPathGenerator>();
            mockShortUrlPathGenerator.Setup(c => c.GenerateShortUrlPath())
                .Returns("YyZz0");

            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);

            // Todo why does this not work?
            //var service = new Mock<IShortenerService>()
            //    .Setup(c => c.GetRedirectionUrl("YyZz0"))
            //    .ThrowsAsync(new ArgumentException("Url Not Found"));
                

            var controller = new UrlShortenerController(service)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext,
                }
            };

            var result = await controller.UrlRedirectAsync("YyZz0") as ViewResult;
            Assert.AreEqual("NotFound", result.ViewName);           
        }
    }
}
