using CertificateManager.Data;
using CertificateManager.Models;
using CertificateManager.Models.Views;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CertificateManager.Services
{
    public class CertificateService
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CertificateService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public void AddCertificateEntry(Entry entry)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                context.Entry.Add(entry);
                context.SaveChanges();
            }
        }

        public Entry GetCertificate(int requestId, string cAConfig)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var entry = context.Entry.SingleOrDefault(x => (x.RequestId == requestId) && (x.CAConfig == cAConfig));
                if (entry == null)
                {
                    return null;
                }
                else
                {
                    return entry;
                }

            }
        }

        public void UpdateCertificate(int requestId, string cAConfig, string owner, string notes)
        {
            var entry = GetCertificate(requestId, cAConfig);
            if (entry == null)
            {
                throw new Exception("Certificate entry does not exist");
            }
            else
            {
                entry.Owner = owner;
                entry.Notes = notes;

            }
            using (var context = _dbContextFactory.CreateDbContext())
            {
                context.Update(entry);
                context.SaveChanges();
            }  
        }
    }
}
