using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.IO;
using System.Security.AccessControl;
using System.Text;

namespace CertificateManager.Admin.Pages.Templates
{
    internal class TemplateImportHelper
    {
        public LdapConnection? LdapConnection { get; set; }
        public DomainController? DomainController { get; set; }
        public ActiveDirectorySite? Site { get; set; }
        public int MaxPageSize { get; set; }
        public string? DefaultNamingContext { get; set; }
        public string? ConfigurationNamingContext { get; set; }
        public string? SchemaNamingContext { get; set; }

        //https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-crtd/1192823c-d839-4bc3-9b6b-fa8c53507ae1
        public const long CT_FLAG_ENROLLEE_SUPPLIES_SUBJECT = 0x1;
        public const long CT_FLAG_ENROLLEE_SUPPLIES_SUBJECT_ALT_NAME = 0x10000;
        public const long CT_FLAG_SUBJECT_ALT_REQUIRE_DOMAIN_DNS = 0x40000;
        public const long CT_FLAG_SUBJECT_ALT_REQUIRE_SPN = 0x80000;
        public const long CT_FLAG_SUBJECT_ALT_REQUIRE_DIRECTORY_GUID = 0x1000000;
        public const long CT_FLAG_SUBJECT_ALT_REQUIRE_UPN = 0x2000000;
        public const long CT_FLAG_SUBJECT_ALT_REQUIRE_EMAIL = 0x4000000;
        public const long CT_FLAG_SUBJECT_ALT_REQUIRE_DNS = 0x8000000;
        public const long CT_FLAG_SUBJECT_REQUIRE_DNS_AS_CN = 0x10000000;
        public const long CT_FLAG_SUBJECT_REQUIRE_EMAIL = 0x20000000;
        public const long CT_FLAG_SUBJECT_REQUIRE_COMMON_NAME = 0x40000000;
        public const long CT_FLAG_SUBJECT_REQUIRE_DIRECTORY_PATH = unchecked((long)0x80000000);
        public const long CT_FLAG_OLD_CERT_SUPPLIES_SUBJECT_AND_ALT_NAME = 0x8;

        public static string DecodeCertificateNameFlags(long flagValue)
        {
            var flags = new List<string>();

            if ((flagValue & CT_FLAG_ENROLLEE_SUPPLIES_SUBJECT) != 0) flags.Add("ENROLLEE_SUPPLIES_SUBJECT");
            if ((flagValue & CT_FLAG_ENROLLEE_SUPPLIES_SUBJECT_ALT_NAME) != 0) flags.Add("ENROLLEE_SUPPLIES_SAN");
            if ((flagValue & CT_FLAG_SUBJECT_ALT_REQUIRE_DOMAIN_DNS) != 0) flags.Add("SAN_DOMAIN_DNS");
            if ((flagValue & CT_FLAG_SUBJECT_ALT_REQUIRE_SPN) != 0) flags.Add("SAN_SPN");
            if ((flagValue & CT_FLAG_SUBJECT_ALT_REQUIRE_DIRECTORY_GUID) != 0) flags.Add("SAN_DIRECTORY_GUID");
            if ((flagValue & CT_FLAG_SUBJECT_ALT_REQUIRE_UPN) != 0) flags.Add("SAN_UPN");
            if ((flagValue & CT_FLAG_SUBJECT_ALT_REQUIRE_EMAIL) != 0) flags.Add("SAN_EMAIL");
            if ((flagValue & CT_FLAG_SUBJECT_ALT_REQUIRE_DNS) != 0) flags.Add("SAN_DNS");
            if ((flagValue & CT_FLAG_SUBJECT_REQUIRE_DNS_AS_CN) != 0) flags.Add("SUBJECT_DNS_AS_CN");
            if ((flagValue & CT_FLAG_SUBJECT_REQUIRE_EMAIL) != 0) flags.Add("SUBJECT_EMAIL");
            if ((flagValue & CT_FLAG_SUBJECT_REQUIRE_COMMON_NAME) != 0) flags.Add("SUBJECT_COMMON_NAME");
            if ((flagValue & CT_FLAG_SUBJECT_REQUIRE_DIRECTORY_PATH) != 0) flags.Add("SUBJECT_DIRECTORY_PATH");
            if ((flagValue & CT_FLAG_OLD_CERT_SUPPLIES_SUBJECT_AND_ALT_NAME) != 0) flags.Add("OLD_CERT_SUPPLIES_SUBJECT_AND_ALT_NAME");

            return string.Join("; ", flags);
        }

        //https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-crtd/ec71fd43-61c2-407b-83c9-b52272dec8a1
        public const long CT_FLAG_INCLUDE_SYMMETRIC_ALGORITHMS = 0x1;
        public const long CT_FLAG_PEND_ALL_REQUESTS = 0x2;
        public const long CT_FLAG_PUBLISH_TO_KRA_CONTAINER = 0x4;
        public const long CT_FLAG_PUBLISH_TO_DS = 0x8;
        public const long CT_FLAG_AUTO_ENROLLMENT_CHECK_USER_DS_CERTIFICATE = 0x10;
        public const long CT_FLAG_AUTO_ENROLLMENT = 0x20;
        public const long CT_FLAG_PREVIOUS_APPROVAL_VALIDATE_REENROLLMENT = 0x40;
        public const long CT_FLAG_USER_INTERACTION_REQUIRED = 0x100;
        public const long CT_FLAG_REMOVE_INVALID_CERTIFICATE_FROM_PERSONAL_STORE = 0x400;
        public const long CT_FLAG_ALLOW_ENROLL_ON_BEHALF_OF = 0x800;
        public const long CT_FLAG_ADD_OCSP_NOCHECK = 0x1000;
        public const long CT_FLAG_ENABLE_KEY_REUSE_ON_NT_TOKEN_KEYSET_STORAGE_FULL = 0x2000;
        public const long CT_FLAG_NOREVOCATIONINFOINISSUEDCERTS = 0x4000;
        public const long CT_FLAG_INCLUDE_BASIC_CONSTRAINTS_FOR_EE_CERTS = 0x8000;
        public const long CT_FLAG_ALLOW_PREVIOUS_APPROVAL_KEYBASEDRENEWAL_VALIDATE_REENROLLMENT = 0x10000;
        public const long CT_FLAG_ISSUANCE_POLICIES_FROM_REQUEST = 0x20000;
        public const long CT_FLAG_SKIP_AUTO_RENEWAL = 0x40000;
        public const long CT_FLAG_NO_SECURITY_EXTENSION = 0x80000;

        public static string DecodeEnrollmentFlags(long flagValue)
        {
            var flags = new List<string>();

            if ((flagValue & CT_FLAG_INCLUDE_SYMMETRIC_ALGORITHMS) != 0) flags.Add("INCLUDE_SYMMETRIC_ALGORITHMS");
            if ((flagValue & CT_FLAG_PEND_ALL_REQUESTS) != 0) flags.Add("PEND_ALL_REQUESTS");
            if ((flagValue & CT_FLAG_PUBLISH_TO_KRA_CONTAINER) != 0) flags.Add("PUBLISH_TO_KRA_CONTAINER");
            if ((flagValue & CT_FLAG_PUBLISH_TO_DS) != 0) flags.Add("PUBLISH_TO_DS");
            if ((flagValue & CT_FLAG_AUTO_ENROLLMENT_CHECK_USER_DS_CERTIFICATE) != 0) flags.Add("AUTO_ENROLLMENT_CHECK_USER_DS_CERTIFICATE");
            if ((flagValue & CT_FLAG_AUTO_ENROLLMENT) != 0) flags.Add("AUTO_ENROLLMENT");
            if ((flagValue & CT_FLAG_PREVIOUS_APPROVAL_VALIDATE_REENROLLMENT) != 0) flags.Add("PREVIOUS_APPROVAL_VALIDATE_REENROLLMENT");
            if ((flagValue & CT_FLAG_USER_INTERACTION_REQUIRED) != 0) flags.Add("USER_INTERACTION_REQUIRED");
            if ((flagValue & CT_FLAG_REMOVE_INVALID_CERTIFICATE_FROM_PERSONAL_STORE) != 0) flags.Add("REMOVE_INVALID_CERTIFICATE_FROM_PERSONAL_STORE");
            if ((flagValue & CT_FLAG_ALLOW_ENROLL_ON_BEHALF_OF) != 0) flags.Add("ALLOW_ENROLL_ON_BEHALF_OF");
            if ((flagValue & CT_FLAG_ADD_OCSP_NOCHECK) != 0) flags.Add("ADD_OCSP_NOCHECK");
            if ((flagValue & CT_FLAG_ENABLE_KEY_REUSE_ON_NT_TOKEN_KEYSET_STORAGE_FULL) != 0) flags.Add("ENABLE_KEY_REUSE_ON_NT_TOKEN_KEYSET_STORAGE_FULL");
            if ((flagValue & CT_FLAG_NOREVOCATIONINFOINISSUEDCERTS) != 0) flags.Add("NOREVOCATIONINFOINISSUEDCERTS");
            if ((flagValue & CT_FLAG_INCLUDE_BASIC_CONSTRAINTS_FOR_EE_CERTS) != 0) flags.Add("INCLUDE_BASIC_CONSTRAINTS_FOR_EE_CERTS");
            if ((flagValue & CT_FLAG_ALLOW_PREVIOUS_APPROVAL_KEYBASEDRENEWAL_VALIDATE_REENROLLMENT) != 0) flags.Add("ALLOW_PREVIOUS_APPROVAL_KEYBASEDRENEWAL_VALIDATE_REENROLLMENT");
            if ((flagValue & CT_FLAG_ISSUANCE_POLICIES_FROM_REQUEST) != 0) flags.Add("ISSUANCE_POLICIES_FROM_REQUEST");
            if ((flagValue & CT_FLAG_SKIP_AUTO_RENEWAL) != 0) flags.Add("SKIP_AUTO_RENEWAL");
            if ((flagValue & CT_FLAG_NO_SECURITY_EXTENSION) != 0) flags.Add("NO_SECURITY_EXTENSION");

            return string.Join("; ", flags);
        }

        //https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-crtd/f6122d87-b999-4b92-bff8-f465e8949667
        public const long CT_FLAG_REQUIRE_PRIVATE_KEY_ARCHIVAL = 0x1;
        public const long CT_FLAG_EXPORTABLE_KEY = 0x10;
        public const long CT_FLAG_STRONG_KEY_PROTECTION_REQUIRED = 0x20;
        public const long CT_FLAG_REQUIRE_ALTERNATE_SIGNATURE_ALGORITHM = 0x40;
        public const long CT_FLAG_REQUIRE_SAME_KEY_RENEWAL = 0x80;
        public const long CT_FLAG_USE_LEGACY_PROVIDER = 0x100;
        public const long CT_FLAG_ATTEST_NONE = 0;
        public const long CT_FLAG_ATTEST_REQUIRED = 0x2000;
        public const long CT_FLAG_ATTEST_PREFERRED = 0x1000;
        public const long CT_FLAG_ATTESTATION_WITHOUT_POLICY = 0x4000;
        public const long CT_FLAG_EK_TRUST_ON_USE = 0x200;
        public const long CT_FLAG_EK_VALIDATE_CERT = 0x400;
        public const long CT_FLAG_EK_VALIDATE_KEY = 0x800;
        public const long CT_FLAG_HELLO_LOGON_KEY = 0x200000;

        public static string DecodePrivateKeyFlags(long flagValue)
        {
            var flags = new List<string>();
            string prefix = flagValue.ToString() + ": ";

            if ((flagValue & CT_FLAG_REQUIRE_PRIVATE_KEY_ARCHIVAL) != 0) flags.Add("PRIVATE_KEY_ARCHIVAL");
            if ((flagValue & CT_FLAG_EXPORTABLE_KEY) != 0) flags.Add("EXPORTABLE_KEY");
            if ((flagValue & CT_FLAG_STRONG_KEY_PROTECTION_REQUIRED) != 0) flags.Add("STRONG_KEY_PROTECTION_REQUIRED");
            if ((flagValue & CT_FLAG_REQUIRE_ALTERNATE_SIGNATURE_ALGORITHM) != 0) flags.Add("ALTERNATE_SIGNATURE_ALGORITHM");
            if ((flagValue & CT_FLAG_REQUIRE_SAME_KEY_RENEWAL) != 0) flags.Add("SAME_KEY_RENEWAL");
            if ((flagValue & CT_FLAG_USE_LEGACY_PROVIDER) != 0) flags.Add("USE_LEGACY_PROVIDER");
            if ((flagValue & CT_FLAG_ATTEST_NONE) != 0) flags.Add("ATTEST_NONE");
            if ((flagValue & CT_FLAG_ATTEST_REQUIRED) != 0) flags.Add("ATTEST_REQUIRED");
            if ((flagValue & CT_FLAG_ATTEST_PREFERRED) != 0) flags.Add("ATTEST_PREFERRED");
            if ((flagValue & CT_FLAG_ATTESTATION_WITHOUT_POLICY) != 0) flags.Add("ATTESTATION_WITHOUT_POLICY");
            if ((flagValue & CT_FLAG_EK_TRUST_ON_USE) != 0) flags.Add("EK_TRUST_ON_USE");
            if ((flagValue & CT_FLAG_EK_VALIDATE_CERT) != 0) flags.Add("EK_VALIDATE_CERT");
            if ((flagValue & CT_FLAG_EK_VALIDATE_KEY) != 0) flags.Add("EK_VALIDATE_KEY");
            if ((flagValue & CT_FLAG_HELLO_LOGON_KEY) != 0) flags.Add("HELLO_LOGON_KEY");

            return prefix + string.Join("; ", flags);
        }

        private static readonly Dictionary<string, string> OidFriendlyNames = new()
        {
            ["1.3.6.1.5.5.7.3.1"] = "Server Authentication",
            ["1.3.6.1.5.5.7.3.2"] = "Client Authentication",
            ["1.3.6.1.5.5.7.3.3"] = "Code Signing",
            ["1.3.6.1.5.5.7.3.4"] = "Secure E-mail",
            ["1.3.6.1.5.5.7.3.9"] = "OCSP Signing",
            ["1.3.6.1.4.1.311.20.2.2"] = "Smartcard Logon",
            ["1.3.6.1.4.1.311.10.3.1"] = "Microsoft Trust List Signing",
            ["1.3.6.1.4.1.311.10.3.4"] = "Encrypting file system",
            ["1.3.6.1.5.2.3.5"] = "KDC Authentication",
            ["1.3.6.1.5.5.8.2.2"] = "IP Security IKE Intermediate",
        };

        public static string ResolveOidName(string oid)
        {
            return OidFriendlyNames.TryGetValue(oid, out var name) ? name : oid;
        }

        public static string ResolveOidNames(IEnumerable<string> oids)
        {
            var sb = new StringBuilder();
            foreach (var oid in oids)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(ResolveOidName(oid));
            }
            return sb.ToString();
        }

        public static double ConvertPkiPeriodToDays(byte[] periodBytes)
        {
            byte[] copy = (byte[])periodBytes.Clone();
            Array.Reverse(copy);
            var sb = new StringBuilder();
            foreach (byte b in copy)
            {
                sb.Append($"{b:x2}");
            }
            long ticks = Convert.ToInt64(sb.ToString(), 16);
            return Math.Round(ticks * -0.0000001 / 3600 / 24, 2);
        }

        public static string FormatKeyUsageBytes(byte[] keyUsageBytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in keyUsageBytes)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(b);
            }
            return sb.ToString();
        }

        public static string ParseModifyTimestamp(string timestamp)
        {
            string format = "yyyyMMddHHmmss.f'Z'";
            return DateTime.ParseExact(timestamp, format, CultureInfo.InvariantCulture).ToString("yyyyMMdd hh:mm:ss");
        }

        public static bool IsUnsecureSubjectConfiguration(string certificateNameFlags, string enrollmentFlags)
        {
            return certificateNameFlags.Contains("ENROLLEE_SUPPLIES_SUBJECT") &&
                   !enrollmentFlags.Contains("PEND_ALL_REQUESTS");
        }

        public static (string sddl, List<string> aceList) ParseSecurityDescriptor(
        byte[] descriptorBytes,
        Func<string, string?>? resolveSid = null)
        {
            var acl = new CommonSecurityDescriptor(isContainer: true, isDS: true, descriptorBytes, 0);
            var aceList = new List<string>();

            foreach (var ace in (DiscretionaryAcl)acl.DiscretionaryAcl!)
            {
                int accessmask = ((KnownAce)ace).AccessMask;
                var rights = (System.DirectoryServices.ActiveDirectoryRights)accessmask;
                string acerights = rights.ToString();
                string acetype = ace.AceType.ToString();
                string sid = ((KnownAce)ace).SecurityIdentifier.Value;

                string identity = sid;
                if (resolveSid != null)
                {
                    var resolved = resolveSid(sid);
                    if (resolved != null)
                        identity = resolved;
                }

                aceList.Add($"{acetype} {acerights} for {identity}");
            }

            string sddl = acl.GetSddlForm(AccessControlSections.All);
            return (sddl, aceList);
        }

        public void NewADConnection(int port = 389, string referralchasing = "none")
        {
            DirectoryContext directoryctx = new DirectoryContext(DirectoryContextType.Domain);
            Site = ActiveDirectorySite.GetComputerSite();
            LocatorOptions locatorOptions = LocatorOptions.WriteableRequired;
            if (Site != null)
            {
                try
                {
                    DomainController = DomainController.FindOne(directoryctx, Site.Name, locatorOptions);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    DomainController = DomainController.FindOne(directoryctx, locatorOptions);
                }
            }
            else
            {
                DomainController = DomainController.FindOne(directoryctx, locatorOptions);
            }

            bool fullyQualifiedDnsHostName = true;
            bool connectionless = false;

            LdapDirectoryIdentifier ldapDirectoryIdentifier = new LdapDirectoryIdentifier(
                DomainController.Name, port, fullyQualifiedDnsHostName, connectionless);
            LdapConnection = new LdapConnection(ldapDirectoryIdentifier);
        }

        public void GetADConfig()
        {
            string searchRoot = "";
            string ldapFilter = "(objectclass=*)";
            SearchScope searchScope = SearchScope.Base;
            string[]? attributelist = null;

            SearchRequest searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
            SearchResponse searchResponse = (SearchResponse)LdapConnection!.SendRequest(searchRequest);
            SearchResultEntry entry = searchResponse.Entries[0];

            DefaultNamingContext = entry.Attributes["defaultnamingcontext"][0].ToString();
            ConfigurationNamingContext = entry.Attributes["configurationnamingcontext"][0].ToString();
            SchemaNamingContext = entry.Attributes["schemanamingcontext"][0].ToString();

            string querypolicy = $"CN=Default Query Policy,CN=Query-Policies,CN=Directory Service,CN=Windows NT,CN=Services,{ConfigurationNamingContext}";

            try
            {
                searchRoot = "";
                ldapFilter = "(objectclass=ntdsdsa)";
                searchScope = SearchScope.Subtree;
                attributelist = new string[] { "Query-Policy-Object" };

                searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
                searchResponse = (SearchResponse)LdapConnection.SendRequest(searchRequest);
                entry = searchResponse.Entries[0];

                if (entry.Attributes["Query-Policy-Object"] != null)
                {
                    querypolicy = entry.Attributes["Query-Policy-Object"][0].ToString()!;
                }
            }
            catch { }

            try
            {
                searchRoot = $"CN=NTDS Site Settings,CN={Site!.Name},CN=Sites,{ConfigurationNamingContext}";
                ldapFilter = "(objectclass=ntdsdsa)";
                searchScope = SearchScope.Subtree;
                attributelist = new string[] { "Query-Policy-Object" };

                searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
                searchResponse = (SearchResponse)LdapConnection.SendRequest(searchRequest);
                entry = searchResponse.Entries[0];

                if (entry.Attributes["Query-Policy-Object"] != null)
                {
                    querypolicy = entry.Attributes["Query-Policy-Object"][0].ToString()!;
                }
            }
            catch { }

            searchRoot = querypolicy;
            ldapFilter = "(objectclass=queryPolicy)";
            searchScope = SearchScope.Subtree;
            attributelist = null;

            searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
            searchResponse = (SearchResponse)LdapConnection.SendRequest(searchRequest);
            entry = searchResponse.Entries[0];
            if (entry.Attributes["ldapadminlimits"][0] != null)
            {
                string maxPageSize = entry.Attributes["ldapadminlimits"][0].ToString()!.Split("=")[1];
                MaxPageSize = int.Parse(maxPageSize);
            }
        }

        public static string LoadConnectionString()
        {
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

            return configuration.GetConnectionString("DefaultConnection")!;
        }

        public List<SearchResultEntryCollection> GetPagedADObjects(string searchRoot, string ldapFilter, string[] attributelist, SearchScope searchScope)
        {
            PageResultRequestControl pagesizecontrol = new PageResultRequestControl(10);
            PageResultResponseControl? pageresponse = null;
            List<SearchResultEntryCollection> entries = new List<SearchResultEntryCollection>();

            SearchRequest searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
            searchRequest.Controls.Add(pagesizecontrol);
            if (attributelist != null)
            {
                foreach (string attribute in attributelist)
                {
                    searchRequest.Attributes.Add(attribute);
                }
            }
            while (true)
            {
                SearchResponse pagedSearchResponse = (SearchResponse)LdapConnection!.SendRequest(searchRequest);
                DirectoryControl[] directorycontrols = pagedSearchResponse.Controls;
                foreach (DirectoryControl directorycontrol in directorycontrols)
                {
                    if (directorycontrol.Type == pagesizecontrol.Type)
                    {
                        pageresponse = (PageResultResponseControl)directorycontrol;
                    }
                }
                entries.AddRange(pagedSearchResponse.Entries);
                if (pageresponse!.Cookie.Length == 0) { break; }
                pagesizecontrol.Cookie = pageresponse.Cookie;
            }
            return entries;
        }
    }
}
