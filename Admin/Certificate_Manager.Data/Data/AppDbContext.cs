using Certificate_Manager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Certificate_Manager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Try multiple paths to locate appsettings.json at runtime
                var basePaths = new[]
                {
                    AppContext.BaseDirectory,
                    Directory.GetCurrentDirectory(),
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ""
                };

                string? configPath = null;
                foreach (var basePath in basePaths)
                {
                    var candidate = Path.Combine(basePath, "appsettings.json");
                    if (File.Exists(candidate))
                    {
                        configPath = basePath;
                        break;
                    }
                }

                if (configPath == null)
                    throw new FileNotFoundException(
                        $"Could not find appsettings.json. Searched: {string.Join(", ", basePaths)}");

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(configPath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }


        public DbSet<Entry> Entry { get; set; }
        public DbSet<EKU> EKU { get; set; }
        public DbSet<SAN> SAN { get; set; }
        public DbSet<CRL> CRL { get; set; }
        public DbSet<RevokedCert> RevokedCert { get; set; }
        public DbSet<Template> Template { get; set; }
        public DbSet<TemplateArchive> TemplatesArchiv { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entry>()
                .HasMany(e => e.EKU)
                .WithOne(s => s.Entry);

            modelBuilder.Entity<Entry>()
                .HasMany(e => e.SAN)
                .WithOne(s => s.Entry);

            modelBuilder.Entity<CRL>()
                .HasMany(c => c.RevokedCert)
                .WithOne(r => r.CRL);
        }
    }
}
