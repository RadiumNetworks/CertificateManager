using Microsoft.EntityFrameworkCore;
using WebGUICertManager.Models;

namespace WebGUICertManager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Entries> Entries { get; set; }
    }
}
