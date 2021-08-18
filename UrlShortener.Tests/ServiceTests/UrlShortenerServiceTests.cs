using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UrlShortener.Models;
using UrlShortener.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace UrlShortener.Tests.ServiceTests
{
    public class UrlShortenerServiceTests
    {                     
        private Mock<IShortUrlPathGenerator> mockShortUrlPathGenerator;        

        [SetUp]
        public void Setup()
        {            
            mockShortUrlPathGenerator = new Mock<IShortUrlPathGenerator>();
            mockShortUrlPathGenerator.Setup(c => c.GenerateShortUrlPath())
                .Returns(new Queue<string>(new[] { "YyZz0", "11", "baz" }).Dequeue);         
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
        public async Task ShortenerService_ShortenUrl_StartsWithHttp_DoNotPrependHttpAsync()
        {           
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var urlData = await service.ShortenUrlAsync("http://www.bing.com", "http://localhost:80/");
            Assert.AreEqual("http://www.bing.com", urlData.Url);
        }

        [Test]
        public async Task ShortenerService_ShortenUrl_MissingHttp_PrependHttp()
        {         
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var urlData = await service.ShortenUrlAsync("www.bing.com", "http://localhost:80/");
            Assert.AreEqual("http://www.bing.com", urlData.Url);
        }

        [Test]
        public async Task ShortenerService_ShortenUrl_ReturnsShortenedUrlLessThanOrEqualTo25Chars()
        {         
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var urlData = await service.ShortenUrlAsync(url: "http://www.bing.com", requestPath: "http://localhost:80/");
            Assert.LessOrEqual(urlData.ShortUrlPath.Length, 25);
        }

        [Test]
        public async Task ShortenerService_ShortenUrl_ReturnsShortenedUrlGreaterThanOrEqualTo21Chars()
        {         
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var urlData = await service.ShortenUrlAsync(url: "http://www.bing.com", requestPath: "http://localhost:80/");
            Assert.GreaterOrEqual(urlData.ShortenedURL.Length, 21);
        }

        [Test]
        public async Task ShortenerService_GenerateShortUrlPath_IfShortUrlPathAlreadyExists_GenerateNewShortUrlPath()
        {               
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

            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var urlData = await service.ShortenUrlAsync(url: "http://www.bing.com", requestPath: "http://localhost:80/");
            Assert.AreNotEqual(urlData.ShortUrlPath, "YyZz0");
        }

        [Test]
        public async Task ShortenerService_ShortenUrl_IfUrlAlreadyExists_ReturnPreviousShortenedUrl()
        {
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var one = new UrlData
            {
                ID = 1,
                Url = "http://www.google.com",
                ShortenedURL = "http://localhost:80/YyZz0",
                ShortUrlPath = "YyZz0"
            };

            context.UrlData.Add(one);
            context.SaveChanges();

            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var urlData = await service.ShortenUrlAsync(url: "http://www.google.com", requestPath: "http://localhost:80/");
            Assert.AreEqual("http://localhost:80/YyZz0", urlData.ShortenedURL);
        }

        [Test]
        public void ShortenerService_ShortenUrl_ThrowIfUrlNull()
        {         
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);            
            ArgumentException ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await service.ShortenUrlAsync(url: null, requestPath: "http://localhost:80/"));
            Assert.That(ex.Message, Is.EqualTo("Url to shorten cannot be empty"));
        }

        [Test]
        public void ShortenerService_ShortenUrl_ThrowIfUrlEmpty()
        {         
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            ArgumentException ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await service.ShortenUrlAsync(url: string.Empty, requestPath: "http://localhost:80/"));
            Assert.That(ex.Message, Is.EqualTo("Url to shorten cannot be empty"));
        }

        [Test]
        public void ShortenerService_ShortenUrl_ThrowIfUrlIsNotValid()
        {
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            ArgumentException ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await service.ShortenUrlAsync(url: "https://this-shouldn't.match@example.com", requestPath: "http://localhost:80/"));
            Assert.That(ex.Message, Is.EqualTo("This is not a valid Url"));
        }        

        [Test]
        public void ShortenerService_GenerateShortUrlPath_ReturnsShortenedUrlLengthBetween2And6Chars()
        {            
            var shortUrlPathGenerator = new ShortUrlPathGenerator();
            var shortUrlPath = shortUrlPathGenerator.GenerateShortUrlPath();
            Assert.LessOrEqual(shortUrlPath.Length, 6);
            Assert.GreaterOrEqual(shortUrlPath.Length, 2);
        }

        [Test]
        public async Task ShortenerService_GetRedirectionUrl_ReturnsFullUrlAsync()
        {
            using var context = new UrlShortenerContext(CreateNewContextOptions());
            var one = new UrlData
            {
                ID = 1,
                Url = "http://www.google.com",
                ShortenedURL = "http://localhost/YyZz0",
                ShortUrlPath = "YyZz0"
            };

            await context.AddAsync(one);
            await context.SaveChangesAsync();

            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var fullUrl = await service.GetRedirectionUrl("YyZz0");

            Assert.AreEqual("http://www.google.com", fullUrl);
        }

        [Test]
        public void ShortenerService_GetRedirectionUrl_ThrowsIfUrlNotFound()
        {
            using var context = new UrlShortenerContext(CreateNewContextOptions());

            var service = new ShortenerService(context, mockShortUrlPathGenerator.Object);
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.GetRedirectionUrl("http://localhost/YyZz0"));
            Assert.That(ex.Message, Is.EqualTo("Url Not Found"));
        }
    }    
}