using Certificate_Manager.Data;
using Certificate_Manager.Models;
using Certificate_Manager.Models.Views;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Certificate_Manager.Data.Services
{
    public class DatabaseSvc
    {
        public AppDbContext CreateDbContext()
        {
            return _dbContextFactory.CreateDbContext();
        }

        public IQueryable<ExtendedEntry> GetCertificateEntries(AppDbContext context, string searchstring, string columnname, Type table)
        {
            IQueryable<Entry> query = context.Entry
                .Include(e => e.SAN)
                .Include(e => e.EKU);

            var parameter = Expression.Parameter(typeof(Entry), "e");
            Expression? combinedFilter = null;

            Expression CombineAnd(Expression? existing, Expression addition)
                => existing == null ? addition : Expression.AndAlso(existing, addition);

            if (!string.IsNullOrEmpty(columnname) && table != null && !string.IsNullOrEmpty(searchstring))
            {
                var innerParameter = Expression.Parameter(table, "k");
                var property = Expression.Property(innerParameter, columnname);
                var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
                var contains = Expression.Call(
                    property,
                    typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!,
                    Expression.Constant(searchstring));
                var predicate = Expression.AndAlso(notNull, contains);
                
                var anyMethod = typeof(Enumerable)
                    .GetMethods()
                    .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
                    .MakeGenericMethod(table);

                var collection = Expression.Property(parameter, table.Name);
                if(table.Name == "EKU")
                {
                    var lambda = Expression.Lambda<Func<EKU, bool>>(predicate, innerParameter);
                    var anyCall = Expression.Call(anyMethod, collection, lambda);
                    combinedFilter = CombineAnd(combinedFilter, anyCall);
                }
                else
                {
                    var lambda = Expression.Lambda<Func<SAN, bool>>(predicate, innerParameter);
                    var anyCall = Expression.Call(anyMethod, collection, lambda);
                    combinedFilter = CombineAnd(combinedFilter, anyCall);
                }

            }

            if (combinedFilter != null)
            {
                var lambda = Expression.Lambda<Func<Entry, bool>>(combinedFilter, parameter);
                query = query.Where(lambda);
            }

            return query.Select(e => new ExtendedEntry
            {
                RequestId = e.RequestId,
                CAConfig = e.CAConfig,
                SerialNumber = e.SerialNumber,
                RequestCommonName = e.RequestCommonName,
                RequestCountryRegion = e.RequestCountryRegion,
                RequestCity = e.RequestCity,
                RequestOrganization = e.RequestOrganization,
                RequestOrganizationUnit = e.RequestOrganizationUnit,
                RequestEMailAddress = e.RequestEmailAddress,
                Owner = e.Owner,
                Notes = e.Notes,
                CertificateExpirationDate = e.CertificateExpirationDate,
                SubjectAlternativeNames = string.Join(", ", e.SAN.Select(s => s.SubjectAlternativeName)),
                EKUNames = string.Join(", ", e.EKU.Select(s => s.Name))
            });

        }

        public IQueryable<ExtendedEntry> GetCertificateEntries(AppDbContext context, double? requestId, DateTime? expirationDate, System.Collections.Hashtable filterht)
        {
            IQueryable<Entry> query = context.Entry
                .Include(e => e.SAN)
                .Include(e => e.EKU);

            var parameter = Expression.Parameter(typeof(Entry), "e");
            Expression? combinedFilter = null;

            Expression CombineAnd(Expression? existing, Expression addition)
                => existing == null ? addition : Expression.AndAlso(existing, addition);

            if (requestId.HasValue)
            {
                var property = Expression.Property(parameter, "RequestId");
                var equals = Expression.Equal(property, Expression.Constant((int)requestId.Value));
                combinedFilter = CombineAnd(combinedFilter, equals);
            }

            if (expirationDate.HasValue)
            {
                var property = Expression.Property(parameter, "CertificateExpirationDate");
                var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(DateTime?)));
                var value = Expression.Property(property, nameof(Nullable<DateTime>.Value));
                var lessOrEqual = Expression.LessThanOrEqual(value, Expression.Constant(expirationDate.Value));
                combinedFilter = CombineAnd(combinedFilter, Expression.AndAlso(notNull, lessOrEqual));
            }

            foreach (string key in filterht.Keys)
            {
                if (filterht[key] != null && filterht[key] != "")
                {
                    var property = Expression.Property(parameter, key);
                    var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
                    var contains = Expression.Call(
                        property,
                        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!,
                        Expression.Constant(filterht[key].ToString()));
                    combinedFilter = CombineAnd(combinedFilter, Expression.AndAlso(notNull, contains));
                }
            }
            

            if (combinedFilter != null)
            {
                var lambda = Expression.Lambda<Func<Entry, bool>>(combinedFilter, parameter);
                query = query.Where(lambda);
            }

            return query.Select(e => new ExtendedEntry
            {
                RequestId = e.RequestId,
                CAConfig = e.CAConfig,
                SerialNumber = e.SerialNumber,
                RequestCommonName = e.RequestCommonName,
                RequestCountryRegion = e.RequestCountryRegion,
                RequestCity = e.RequestCity,
                RequestOrganization = e.RequestOrganization,
                RequestOrganizationUnit = e.RequestOrganizationUnit,
                RequestEMailAddress = e.RequestEmailAddress,
                Owner = e.Owner,
                Notes = e.Notes,
                CertificateExpirationDate = e.CertificateExpirationDate,
                SubjectAlternativeNames = string.Join(", ", e.SAN.Select(s => s.SubjectAlternativeName)),
                EKUNames = string.Join(", ", e.EKU.Select(s => s.Name))
            });
        }

        public IQueryable<ExtendedEntry> GetCertificateEntries(AppDbContext context)
        {
            return context.Entry
                .Include(e => e.SAN)
                .Include(e => e.EKU)
                .Select(e => new ExtendedEntry
                {
                    RequestId = e.RequestId,
                    CAConfig = e.CAConfig,
                    SerialNumber = e.SerialNumber,
                    RequestCommonName = e.RequestCommonName,
                    RequestCountryRegion = e.RequestCountryRegion,
                    RequestCity = e.RequestCity,
                    RequestOrganization = e.RequestOrganization,
                    RequestOrganizationUnit = e.RequestOrganizationUnit,
                    RequestEMailAddress = e.RequestEmailAddress,
                    Owner = e.Owner,
                    Notes = e.Notes,
                    CertificateExpirationDate = e.CertificateExpirationDate,
                    SubjectAlternativeNames = string.Join(", ", e.SAN.Select(s => s.SubjectAlternativeName)),
                    EKUNames = string.Join(", ", e.EKU.Select(s => s.Name))
                });
        }

        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public DatabaseSvc(IDbContextFactory<AppDbContext> dbContextFactory)
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

        public Entry GetCertificate(int requestId, string caConfig)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var entry = context.Entry.SingleOrDefault(x => (x.RequestId == requestId) && (x.CAConfig == caConfig));
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

        public void UpdateCertificate(int requestId, string caConfig, string owner, string notes)
        {
            var entry = GetCertificate(requestId, caConfig);
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
