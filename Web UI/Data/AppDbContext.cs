using Microsoft.EntityFrameworkCore;
using CertificateManager.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;



namespace CertificateManager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

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

            //modelBuilder.Entity<EKUs>()
            //    .HasOne(s => s.Entries)
            //    .WithMany(e => e.EKUs);

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

//Microsoft.EntityFrameworkCore.Tools
//add-migration "DBSchemaChangeInfo"
//updata-database