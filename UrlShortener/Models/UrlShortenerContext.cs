using Microsoft.EntityFrameworkCore;
using System;

namespace UrlShortener.Models
{
    public class UrlShortenerContext : DbContext
    {        
        public UrlShortenerContext(DbContextOptions<UrlShortenerContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }        

        public DbSet<UrlData> UrlData { get; set; }       
    }
}
