using Microsoft.EntityFrameworkCore;
using WebGUICertManager.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;



namespace WebGUICertManager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Entries> Entries { get; set; }
        public DbSet<EKUs> EKUs { get; set; }
        public DbSet<SANs> SANs { get; set; }
        public DbSet<CRLs> CRLs { get; set; }
        public DbSet<RevokedCerts> RevokedCerts { get; set; }
        public DbSet<Templates> Templates { get; set; }
        public DbSet<TemplatesArchive> TemplatesArchive { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<EKUs>()
            //    .HasOne(s => s.Entries)
            //    .WithMany(e => e.EKUs);

            modelBuilder.Entity<Entries>()
                .HasMany(e => e.EKUs)
                .WithOne(s => s.Entries);

            modelBuilder.Entity<Entries>()
                .HasMany(e => e.SANs)
                .WithOne(s => s.Entries);

            modelBuilder.Entity<CRLs>()
                .HasMany(c => c.RevokedCerts)
                .WithOne(r => r.CRLs);
        }

    }
}
