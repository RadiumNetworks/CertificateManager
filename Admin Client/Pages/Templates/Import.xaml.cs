using CERTENROLLLib;
using Certificate_Manager.Data.Services;
using Certificate_Manager.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Description;
using Windows.Media.Protection.PlayReady;

namespace Certificate_Manager.Pages.Templates
{

    public sealed partial class Import : Page
    {
        public static LdapConnection _ldapConnection { get; set; } = null;
        public static DomainController _domainController { get; set; } = null;
        public static ActiveDirectorySite _site { get; set; } = null;
        public static int _maxPageSize { get; set; } 
        public static string _defaultNamingContext { get; set; }
        public static string _configurationNamingContext { get; set; }
        public static string _schemaNamingContext { get; set; }

        //https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-crtd/1192823c-d839-4bc3-9b6b-fa8c53507ae1
        public static long _CT_FLAG_ENROLLEE_SUPPLIES_SUBJECT { get; set; } = 0x1;
        public static long _CT_FLAG_ENROLLEE_SUPPLIES_SUBJECT_ALT_NAME { get; set; } = 0x10000;
        public static long _CT_FLAG_SUBJECT_ALT_REQUIRE_DOMAIN_DNS { get; set; } = 0x40000;
        public static long _CT_FLAG_SUBJECT_ALT_REQUIRE_SPN { get; set; } = 0x80000;
        public static long _CT_FLAG_SUBJECT_ALT_REQUIRE_DIRECTORY_GUID { get; set; } = 0x1000000;
        public static long _CT_FLAG_SUBJECT_ALT_REQUIRE_UPN { get; set; } = 0x2000000;
        public static long _CT_FLAG_SUBJECT_ALT_REQUIRE_EMAIL { get; set; } = 0x4000000;
        public static long _CT_FLAG_SUBJECT_ALT_REQUIRE_DNS { get; set; } = 0x8000000;
        public static long _CT_FLAG_SUBJECT_REQUIRE_DNS_AS_CN { get; set; } = 0x10000000;
        public static long _CT_FLAG_SUBJECT_REQUIRE_EMAIL { get; set; } = 0x20000000;
        public static long _CT_FLAG_SUBJECT_REQUIRE_COMMON_NAME { get; set; } = 0x40000000;
        public static long _CT_FLAG_SUBJECT_REQUIRE_DIRECTORY_PATH { get; set; } = 0x80000000;
        public static long _CT_FLAG_OLD_CERT_SUPPLIES_SUBJECT_AND_ALT_NAME { get; set; } = 0x8;

        //https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-crtd/ec71fd43-61c2-407b-83c9-b52272dec8a1
        public static long _CT_FLAG_INCLUDE_SYMMETRIC_ALGORITHMS { get; set; } = 0x1;
        public static long _CT_FLAG_PEND_ALL_REQUESTS { get; set; } = 0x2;
        public static long _CT_FLAG_PUBLISH_TO_KRA_CONTAINER { get; set; } = 0x4;
        public static long _CT_FLAG_PUBLISH_TO_DS { get; set; } = 0x8;
        public static long _CT_FLAG_AUTO_ENROLLMENT_CHECK_USER_DS_CERTIFICATE { get; set; } = 0x10;
        public static long _CT_FLAG_AUTO_ENROLLMENT { get; set; } = 0x20;
        public static long _CT_FLAG_PREVIOUS_APPROVAL_VALIDATE_REENROLLMENT { get; set; } = 0x40;
        public static long _CT_FLAG_USER_INTERACTION_REQUIRED { get; set; } = 0x100;
        public static long _CT_FLAG_REMOVE_INVALID_CERTIFICATE_FROM_PERSONAL_STORE { get; set; } = 0x400;
        public static long _CT_FLAG_ALLOW_ENROLL_ON_BEHALF_OF { get; set; } = 0x800;
        public static long _CT_FLAG_ADD_OCSP_NOCHECK { get; set; } = 0x1000;
        public static long _CT_FLAG_ENABLE_KEY_REUSE_ON_NT_TOKEN_KEYSET_STORAGE_FULL { get; set; } = 0x2000;
        public static long _CT_FLAG_NOREVOCATIONINFOINISSUEDCERTS { get; set; } = 0x4000;
        public static long _CT_FLAG_INCLUDE_BASIC_CONSTRAINTS_FOR_EE_CERTS { get; set; } = 0x8000;
        public static long _CT_FLAG_ALLOW_PREVIOUS_APPROVAL_KEYBASEDRENEWAL_VALIDATE_REENROLLMENT { get; set; } = 0x10000;
        public static long _CT_FLAG_ISSUANCE_POLICIES_FROM_REQUEST { get; set; } = 0x20000;
        public static long _CT_FLAG_SKIP_AUTO_RENEWAL { get; set; } = 0x40000;
        public static long _CT_FLAG_NO_SECURITY_EXTENSION { get; set; } = 0x80000;

        //https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-crtd/f6122d87-b999-4b92-bff8-f465e8949667
        public static long _CT_FLAG_REQUIRE_PRIVATE_KEY_ARCHIVAL { get; set; } = 0x1;
        public static long _CT_FLAG_EXPORTABLE_KEY { get; set; } = 0x10;
        public static long _CT_FLAG_STRONG_KEY_PROTECTION_REQUIRED { get; set; } = 0x20;
        public static long _CT_FLAG_REQUIRE_ALTERNATE_SIGNATURE_ALGORITHM { get; set; } = 0x40;
        public static long _CT_FLAG_REQUIRE_SAME_KEY_RENEWAL { get; set; } = 0x80;
        public static long _CT_FLAG_USE_LEGACY_PROVIDER { get; set; } = 0x100;
        public static long _CT_FLAG_ATTEST_NONE { get; set; } = 0;
        public static long _CT_FLAG_ATTEST_REQUIRED { get; set; } = 0x2000;
        public static long _CT_FLAG_ATTEST_PREFERRED { get; set; } = 0x1000;
        public static long _CT_FLAG_ATTESTATION_WITHOUT_POLICY { get; set; } = 0x4000;
        public static long _CT_FLAG_EK_TRUST_ON_USE { get; set; } = 0x200;
        public static long _CT_FLAG_EK_VALIDATE_CERT { get; set; } = 0x400;
        public static long _CT_FLAG_EK_VALIDATE_KEY { get; set; } = 0x800;
        public static long _CT_FLAG_HELLO_LOGON_KEY { get; set; } = 0x200000;

        private void AppendImportLog(string message)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                var done = new ManualResetEventSlim(false);
                DispatcherQueue.TryEnqueue(() =>
                {
                    AppendImportLog(message);
                    done.Set();
                });
                done.Wait();
                return;
            }

            var paragraph = new Paragraph();

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            if (message.Length == 0)
            {
                paragraph.Inlines.Add(new Run { Text = " " });
            }
            else
            {
                var run = new Run();
                switch (message)
                {
                    case var _ when message.Contains("Write") || message.Contains("Delete"):
                        run = new Run { Text = $"[{timestamp}] {message}" };
                        run.Foreground = new SolidColorBrush(Colors.Red);
                        paragraph.Inlines.Add(run);
                        break;
                    case var _ when message.Contains("Unsecure"):
                        run = new Run { Text = $"[{timestamp}] {message}" };
                        run.FontWeight = FontWeights.SemiBold;
                        run.Foreground = new SolidColorBrush(Colors.Red);
                        paragraph.Inlines.Add(run);
                        break;
                    default:
                        paragraph.Inlines.Add(new Run { Text = $"[{timestamp}] {message}" });
                        break;
                }
                
            }
            ImportLog.Blocks.Add(paragraph);
            ImportLogScroller.UpdateLayout();
            ImportLogScroller.ChangeView(null, ImportLogScroller.ScrollableHeight, null, true);
        }

        public Import()
        {
            InitializeComponent();

        }

        public void NewADConnection(int port = 389, string referralchasing = "none")
        {
            // Get directorycontext and connect to directory
            DirectoryContext directoryctx = new DirectoryContext(DirectoryContextType.Domain);
            _site = ActiveDirectorySite.GetComputerSite();
            LocatorOptions locatorOptions = LocatorOptions.WriteableRequired;
            if (_site != null)
            {
                try
                {
                    _domainController = DomainController.FindOne(directoryctx, _site.Name, locatorOptions);
                }
                catch (ActiveDirectoryObjectNotFoundException ex)
                {
                    _domainController = DomainController.FindOne(directoryctx, locatorOptions);
                }
            }
            else
            {
                _domainController = DomainController.FindOne(directoryctx, locatorOptions);
            }

            bool fullyQualifiedDnsHostName = true;
            bool connectionless = false;

            LdapDirectoryIdentifier ldapDirectoryIdentifier = new LdapDirectoryIdentifier(
                _domainController.Name, port, fullyQualifiedDnsHostName, connectionless);
            _ldapConnection = new LdapConnection(ldapDirectoryIdentifier);
        }

        public void GetADConfig()
        { 
            //Get namingcontexts for dynamic filters
            string searchRoot = "";
            string ldapFilter = "(objectclass=*)";
            SearchScope searchScope = SearchScope.Base;
            string[] attributelist = null;

            SearchRequest searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
            SearchResponse searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);
            SearchResultEntry entry = searchResponse.Entries[0];

            _defaultNamingContext = entry.Attributes["defaultnamingcontext"][0].ToString();
            _configurationNamingContext = entry.Attributes["configurationnamingcontext"][0].ToString();
            _schemaNamingContext = entry.Attributes["schemanamingcontext"][0].ToString();

            //Get query policy for paged queries
            string domaincontrollersite = _domainController.SiteName;
            string domaincontrollernetbiosname = _domainController.Name.Split('.')[0];
            string querypolicy = $"CN=Default Query Policy,CN=Query-Policies,CN=Directory Service,CN=Windows NT,CN=Services,{_configurationNamingContext}";

            try
            {
                searchRoot = "";
                ldapFilter = "(objectclass=ntdsdsa)";
                searchScope = SearchScope.Subtree;
                attributelist = new string[] { "Query-Policy-Object" };

                searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
                searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);
                entry = searchResponse.Entries[0];

                if (entry.Attributes["Query-Policy-Object"] != null)
                {
                    querypolicy = entry.Attributes["Query-Policy-Object"][0].ToString();
                }
            }
            catch
            {

            }

            try
            {
                searchRoot = $"CN=NTDS Site Settings,CN={_site.Name},CN=Sites,{_configurationNamingContext}";
                ldapFilter = "(objectclass=ntdsdsa)";
                searchScope = SearchScope.Subtree;
                attributelist = new string[] { "Query-Policy-Object" };

                searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
                searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);
                entry = searchResponse.Entries[0];

                if (entry.Attributes["Query-Policy-Object"] != null)
                {
                    querypolicy = entry.Attributes["Query-Policy-Object"][0].ToString();
                }
            }
            catch
            {

            }

            //Read Query Policy
            searchRoot = querypolicy;
            ldapFilter = "(objectclass=queryPolicy)";
            searchScope = SearchScope.Subtree;
            attributelist = null;

            searchRequest = new SearchRequest(searchRoot, ldapFilter, searchScope, attributelist);
            searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);
            entry = searchResponse.Entries[0];
            if (entry.Attributes["ldapadminlimits"][0] != null)
            { 
                string maxPageSize = entry.Attributes["ldapadminlimits"][0].ToString().Split("=")[1];
                _maxPageSize = int.Parse(maxPageSize);
            }
        }

        public string LoadConnectionString()
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

            return configuration.GetConnectionString("DefaultConnection");
        }

        public List<SearchResultEntryCollection> GetPagedADObjects(string searchRoot, string ldapFilter, string[] attributelist, SearchScope searchScope)
        {
            //PageResultRequestControl pagesizecontrol = new PageResultRequestControl(_maxPageSize);
            PageResultRequestControl pagesizecontrol = new PageResultRequestControl(10);
            PageResultResponseControl pageresponse = null;
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
            while(true)
            {
                SearchResponse pagedSearchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);
                DirectoryControl[] directorycontrols = pagedSearchResponse.Controls;
                foreach (DirectoryControl directorycontrol in directorycontrols)
                {
                    if (directorycontrol.Type == pagesizecontrol.Type)
                    {
                        pageresponse = (PageResultResponseControl)directorycontrol;
                    }
                }
                entries.AddRange(pagedSearchResponse.Entries);
                if (pageresponse.Cookie.Length == 0) { break; }
                pagesizecontrol.Cookie = pageresponse.Cookie;
            }
            return entries;
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportButton.IsEnabled = false;
            ImportProgressIndicator.IsActive = true;

            try
            {
                await Task.Run(DoImport);
            }
            catch (Exception ex)
            {
                AppendImportLog($"Error: {ex.Message}");
            }
            finally
            {
                ImportButton.IsEnabled = true;
                ImportProgressIndicator.IsActive = false;
            }
        }
        private void DoImport()
        {
            NewADConnection();
            GetADConfig();
            int port = 389;
            NewADConnection(port);

            string searchRoot = $"CN=Public Key Services,CN=Services,{_configurationNamingContext}";
            string ldapFilter = "(objectclass=pKICertificateTemplate)";
            string[] attributelist = new string[] { 
                "cn","distinguishedName","flags","msPKI-Certificate-Application-Policy","msPKI-Certificate-Name-Flag",
                "msPKI-Certificate-Policy","msPKI-Cert-Template-OID","msPKI-Enrollment-Flag","msPKI-Minimal-Key-Size",
                "msPKI-Private-Key-Flag","msPKI-RA-Application-Policies","msPKI-RA-Policies","msPKI-RA-Signature",
                "msPKI-Supersede-Templates","msPKI-Template-Minor-Revision","msPKI-Template-Schema-Version","objectGUID",
                "pKICriticalExtensions","pKIDefaultCSPs","pKIDefaultKeySpec","pKIExpirationPeriod","pKIExtendedKeyUsage",
                "pKIKeyUsage","pKIMaxIssuingDepth","pKIOverlapPeriod","nTSecurityDescriptor","modifyTimeStamp"};
            SearchScope searchScope = SearchScope.Subtree;

            List<SearchResultEntryCollection> entries = GetPagedADObjects(searchRoot, ldapFilter, attributelist, searchScope);

            if (entries.Count != 0)
            {
                foreach (SearchResultEntryCollection entryCollection in entries)
                {
                    foreach (SearchResultEntry entry in entryCollection)
                    {
                        string cn = "";
                        string distinguishedName = "";
                        string flags = "";
                        string msPKI_Certificate_Application_Policy = "";
                        string msPKI_Certificate_Name_Flag = "";

                        string msPKI_Certificate_Policy = "";
                        string msPKI_Cert_Template_OID = "";
                        string msPKI_Enrollment_Flag = "";
                        string msPKI_Minimal_Key_Size = "";

                        string msPKI_Private_Key_Flag = "";
                        string msPKI_RA_Application_Policies = "";
                        string msPKI_RA_Policies = "";
                        string msPKI_RA_Signature = "";

                        string msPKI_Supersede_Templates = "";
                        string msPKI_Template_Minor_Revision = "";
                        string msPKI_Template_Schema_Version = "";
                        Guid objectGUID = Guid.Empty;

                        string pKICriticalExtensions = "";
                        string pKIDefaultCSPs = "";
                        string pKIDefaultKeySpec = "";
                        string pKIExpirationPeriod = "";
                        string pKIExtendedKeyUsage = "";

                        string pKIKeyUsage = "";
                        string pKIMaxIssuingDepth = "";
                        string pKIOverlapPeriod = "";
                        string nTSecurityDescriptor = "";

                        string modifyTimeStamp = "";

                        if (entry.Attributes["cn"] != null)
                        {

                            cn = entry.Attributes["cn"][0].ToString();
                            AppendImportLog($"[Certificate Template] {cn}");
                        }
                        if (entry.Attributes["distinguishedName"] != null)
                        {
                            distinguishedName = entry.Attributes["distinguishedName"][0].ToString();
                        }
                        if (entry.Attributes["flags"] != null)
                        {
                            flags = entry.Attributes["flags"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-Certificate-Application-Policy"] != null)
                        {
                            for (int i = 0; i < entry.Attributes["msPKI-Certificate-Application-Policy"].Count; i++)
                            {
                                string apppol = "";
                                switch (entry.Attributes["msPKI-Certificate-Application-Policy"][i].ToString())
                                {
                                    case "1.3.6.1.5.5.7.3.1":
                                        apppol = "Server Authentication";
                                        break;
                                    case "1.3.6.1.5.5.7.3.2":
                                        apppol = "Client Authentication";
                                        break;
                                    case "1.3.6.1.5.5.7.3.3":
                                        apppol = "Code Signing";
                                        break;
                                    case "1.3.6.1.5.5.7.3.4":
                                        apppol = "Secure E-mail";
                                        break;
                                    case "1.3.6.1.5.5.7.3.9":
                                        apppol = "OCSP Signing";
                                        break;
                                    case "1.3.6.1.4.1.311.20.2.2":
                                        apppol = "Smartcard Logon";
                                        break;
                                    case "1.3.6.1.4.1.311.10.3.1":
                                        apppol = "Microsoft Trust List Signing";
                                        break;
                                    case "1.3.6.1.4.1.311.10.3.4":
                                        apppol = "Encrypting file system";
                                        break;
                                    case "1.3.6.1.5.2.3.5":
                                        apppol = "KDC Authentication";
                                        break;
                                    case "1.3.6.1.5.5.8.2.2":
                                        apppol = "IP Security IKE Intermediate";
                                        break;
                                    default:
                                        apppol = entry.Attributes["msPKI-Certificate-Application-Policy"][i].ToString();
                                        break;

                                }
                                if (msPKI_Certificate_Application_Policy.Length == 0)
                                {
                                    msPKI_Certificate_Application_Policy = apppol;
                                }
                                else
                                {
                                    msPKI_Certificate_Application_Policy += "; " + apppol;
                                }
                            }
                        }
                        if (entry.Attributes["msPKI-Certificate-Name-Flag"] != null)
                        {
                            var msPKICertificateNameFlag = long.Parse(entry.Attributes["msPKI-Certificate-Name-Flag"][0].ToString());

                            if ((msPKICertificateNameFlag & _CT_FLAG_ENROLLEE_SUPPLIES_SUBJECT) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "ENROLLEE_SUPPLIES_SUBJECT ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_ENROLLEE_SUPPLIES_SUBJECT_ALT_NAME) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "ENROLLEE_SUPPLIES_SAN ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_ALT_REQUIRE_DOMAIN_DNS) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SAN_DOMAIN_DNS ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_ALT_REQUIRE_SPN) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SAN_SPN ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_ALT_REQUIRE_DIRECTORY_GUID) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SAN_DIRECTORY_GUID ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_ALT_REQUIRE_UPN) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SAN_UPN ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_ALT_REQUIRE_EMAIL) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SAN_EMAIL ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_ALT_REQUIRE_DNS) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SAN_DNS ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_REQUIRE_DNS_AS_CN) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SUBJECT_DNS_AS_CN ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_REQUIRE_EMAIL) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SUBJECT_EMAIL ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_REQUIRE_COMMON_NAME) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SUBJECT_COMMON_NAME ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_SUBJECT_REQUIRE_DIRECTORY_PATH) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "SUBJECT_DIRECTORY_PATH ";
                            }
                            if ((msPKICertificateNameFlag & _CT_FLAG_OLD_CERT_SUPPLIES_SUBJECT_AND_ALT_NAME) != 0)
                            {
                                msPKI_Certificate_Name_Flag += "OLD_CERT_SUPPLIES_SUBJECT_AND_ALT_NAME ";
                            }
                            msPKI_Certificate_Name_Flag = msPKI_Certificate_Name_Flag.Trim().Replace(" ", "; ");
                        }
                        AppendImportLog($" [Subject and SAN] {msPKI_Certificate_Name_Flag}");

                        if (entry.Attributes["msPKI-Certificate-Policy"] != null)
                        {
                            msPKI_Certificate_Policy = entry.Attributes["msPKI-Certificate-Policy"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-Cert-Template-OID"] != null)
                        {
                            msPKI_Cert_Template_OID = entry.Attributes["msPKI-Cert-Template-OID"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-Enrollment-Flag"] != null)
                        {
                            var msPKIEnrollmentFlag = long.Parse(entry.Attributes["msPKI-Enrollment-Flag"][0].ToString());
                            if((msPKIEnrollmentFlag & _CT_FLAG_INCLUDE_SYMMETRIC_ALGORITHMS) != 0)
                            {
                                msPKI_Enrollment_Flag += "INCLUDE_SYMMETRIC_ALGORITHMS ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_PEND_ALL_REQUESTS) != 0)
                            {
                                msPKI_Enrollment_Flag += "PEND_ALL_REQUESTS ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_PUBLISH_TO_KRA_CONTAINER) != 0)
                            {
                                msPKI_Enrollment_Flag += "PUBLISH_TO_KRA_CONTAINER ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_PUBLISH_TO_DS) != 0)
                            {
                                msPKI_Enrollment_Flag += "PUBLISH_TO_DS ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_AUTO_ENROLLMENT_CHECK_USER_DS_CERTIFICATE) != 0)
                            {
                                msPKI_Enrollment_Flag += "AUTO_ENROLLMENT_CHECK_USER_DS_CERTIFICATE ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_AUTO_ENROLLMENT) != 0)
                            {
                                msPKI_Enrollment_Flag += "AUTO_ENROLLMENT ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_PREVIOUS_APPROVAL_VALIDATE_REENROLLMENT) != 0)
                            {
                                msPKI_Enrollment_Flag += "PREVIOUS_APPROVAL_VALIDATE_REENROLLMENT ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_USER_INTERACTION_REQUIRED) != 0)
                            {
                                msPKI_Enrollment_Flag += "USER_INTERACTION_REQUIRED ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_REMOVE_INVALID_CERTIFICATE_FROM_PERSONAL_STORE) != 0)
                            {
                                msPKI_Enrollment_Flag += "REMOVE_INVALID_CERTIFICATE_FROM_PERSONAL_STORE ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_ALLOW_ENROLL_ON_BEHALF_OF) != 0)
                            {
                                msPKI_Enrollment_Flag += "ALLOW_ENROLL_ON_BEHALF_OF ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_ADD_OCSP_NOCHECK) != 0)
                            {
                                msPKI_Enrollment_Flag += "ADD_OCSP_NOCHECK ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_ENABLE_KEY_REUSE_ON_NT_TOKEN_KEYSET_STORAGE_FULL) != 0)
                            {
                                msPKI_Enrollment_Flag += "ENABLE_KEY_REUSE_ON_NT_TOKEN_KEYSET_STORAGE_FULL ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_NOREVOCATIONINFOINISSUEDCERTS) != 0)
                            {
                                msPKI_Enrollment_Flag += "NOREVOCATIONINFOINISSUEDCERTS ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_INCLUDE_BASIC_CONSTRAINTS_FOR_EE_CERTS) != 0)
                            {
                                msPKI_Enrollment_Flag += "INCLUDE_BASIC_CONSTRAINTS_FOR_EE_CERTS ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_ALLOW_PREVIOUS_APPROVAL_KEYBASEDRENEWAL_VALIDATE_REENROLLMENT) != 0)
                            {
                                msPKI_Enrollment_Flag += "ALLOW_PREVIOUS_APPROVAL_KEYBASEDRENEWAL_VALIDATE_REENROLLMENT ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_ISSUANCE_POLICIES_FROM_REQUEST) != 0)
                            {
                                msPKI_Enrollment_Flag += "ISSUANCE_POLICIES_FROM_REQUEST ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_SKIP_AUTO_RENEWAL) != 0)
                            {
                                msPKI_Enrollment_Flag += "SKIP_AUTO_RENEWAL ";
                            }
                            if ((msPKIEnrollmentFlag & _CT_FLAG_NO_SECURITY_EXTENSION) != 0)
                            {
                                msPKI_Enrollment_Flag += "NO_SECURITY_EXTENSION ";
                            }
                            msPKI_Enrollment_Flag = msPKI_Enrollment_Flag.Trim().Replace(" ", "; ");
                        }
                        AppendImportLog($" [Enrollment Flags] {msPKI_Enrollment_Flag}");

                        if(msPKI_Certificate_Name_Flag.Contains("ENROLLEE_SUPPLIES_SUBJECT") && !msPKI_Enrollment_Flag.Contains("PEND_ALL_REQUESTS"))
                        {
                            AppendImportLog($" [Unsecure configuration] Template includes Enrollee supplies subject and does not require issuance authorization");
                        }

                        if (entry.Attributes["msPKI-Minimal-Key-Size"] != null)
                        {
                            msPKI_Minimal_Key_Size = entry.Attributes["msPKI-Minimal-Key-Size"][0].ToString();
                        }

                        if (entry.Attributes["msPKI-Private-Key-Flag"] != null)
                        {
                            var msPKIPrivateKeyFlag = long.Parse(entry.Attributes["msPKI-Private-Key-Flag"][0].ToString());
                            msPKI_Private_Key_Flag += entry.Attributes["msPKI-Private-Key-Flag"][0].ToString() + ": ";
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_REQUIRE_PRIVATE_KEY_ARCHIVAL) != 0)
                            {
                                msPKI_Private_Key_Flag += "PRIVATE_KEY_ARCHIVAL ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_EXPORTABLE_KEY) != 0)
                            {
                                msPKI_Private_Key_Flag += "EXPORTABLE_KEY ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_STRONG_KEY_PROTECTION_REQUIRED) != 0)
                            {
                                msPKI_Private_Key_Flag += "STRONG_KEY_PROTECTION_REQUIRED ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_REQUIRE_ALTERNATE_SIGNATURE_ALGORITHM) != 0)
                            {
                                msPKI_Private_Key_Flag += "ALTERNATE_SIGNATURE_ALGORITHM ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_REQUIRE_SAME_KEY_RENEWAL) != 0)
                            {
                                msPKI_Private_Key_Flag += "SAME_KEY_RENEWAL ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_USE_LEGACY_PROVIDER) != 0)
                            {
                                msPKI_Private_Key_Flag += "USE_LEGACY_PROVIDER ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_ATTEST_NONE) != 0)
                            {
                                msPKI_Private_Key_Flag += "ATTEST_NONE ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_ATTEST_REQUIRED) != 0)
                            {
                                msPKI_Private_Key_Flag += "ATTEST_REQUIRED ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_ATTEST_PREFERRED) != 0)
                            {
                                msPKI_Private_Key_Flag += "ATTEST_PREFERRED ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_ATTESTATION_WITHOUT_POLICY) != 0)
                            {
                                msPKI_Private_Key_Flag += "ATTESTATION_WITHOUT_POLICY ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_EK_TRUST_ON_USE) != 0)
                            {
                                msPKI_Private_Key_Flag += "EK_TRUST_ON_USE ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_EK_VALIDATE_CERT) != 0)
                            {
                                msPKI_Private_Key_Flag += "EK_VALIDATE_CERT ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_EK_VALIDATE_KEY) != 0)
                            {
                                msPKI_Private_Key_Flag += "EK_VALIDATE_KEY ";
                            }
                            if ((msPKIPrivateKeyFlag & _CT_FLAG_HELLO_LOGON_KEY) != 0)
                            {
                                msPKI_Private_Key_Flag += "HELLO_LOGON_KEY ";
                            }

                            msPKI_Private_Key_Flag = msPKI_Private_Key_Flag.Trim().Replace(" ", "; ");
                        }
                        if (entry.Attributes["msPKI-RA-Application-Policies"] != null)
                        {
                            msPKI_RA_Application_Policies = entry.Attributes["msPKI-RA-Application-Policies"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-RA-Policies"] != null)
                        {
                            msPKI_RA_Policies = entry.Attributes["msPKI-RA-Policies"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-RA-Signature"] != null)
                        {
                            msPKI_RA_Signature = entry.Attributes["msPKI-RA-Signature"][0].ToString();
                        }

                        if (entry.Attributes["msPKI-Supersede-Templates"] != null)
                        {
                            msPKI_Supersede_Templates = entry.Attributes["msPKI-Supersede-Templates"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-Template-Minor-Revision"] != null)
                        {
                            msPKI_Template_Minor_Revision = entry.Attributes["msPKI-Template-Minor-Revision"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-Template-Schema-Version"] != null)
                        {
                            msPKI_Template_Schema_Version = entry.Attributes["msPKI-Template-Schema-Version"][0].ToString();
                        }
                        if (entry.Attributes["objectGUID"] != null)
                        {
                            objectGUID = new Guid((byte[])entry.Attributes["objectGUID"][0]);
                        }

                        if (entry.Attributes["pKICriticalExtensions"] != null)
                        {
                            pKICriticalExtensions = entry.Attributes["pKICriticalExtensions"][0].ToString();
                        }
                        if (entry.Attributes["pKIDefaultCSPs"] != null)
                        {
                            pKIDefaultCSPs = entry.Attributes["pKIDefaultCSPs"][0].ToString();
                        }
                        if (entry.Attributes["pKIDefaultKeySpec"] != null)
                        {
                            pKIDefaultKeySpec = entry.Attributes["pKIDefaultKeySpec"][0].ToString();
                        }
                        if (entry.Attributes["pKIExpirationPeriod"] != null)
                        {
                            byte[] bytearr_pKIExpirationPeriod = (byte[])entry.Attributes["pKIExpirationPeriod"][0];
                            Array.Reverse(bytearr_pKIExpirationPeriod);
                            string littleEndianByte = "";
                            foreach (byte b in bytearr_pKIExpirationPeriod)
                            {
                                littleEndianByte += $"{b:x2}";
                            }
                            pKIExpirationPeriod = Math.Round(Convert.ToInt64(littleEndianByte, 16) * -0.0000001 / 3600 / 24, 2).ToString();
                        }
                        if (entry.Attributes["pKIExtendedKeyUsage"] != null)
                        {
                            for(int i=0; i< entry.Attributes["pKIExtendedKeyUsage"].Count; i++)
                            {
                                string eku = "";
                                switch (entry.Attributes["pKIExtendedKeyUsage"][i].ToString())
                                { 
                                    case "1.3.6.1.5.5.7.3.1":
                                        eku = "Server Authentication";
                                        break;
                                    case "1.3.6.1.5.5.7.3.2":
                                        eku = "Client Authentication";
                                        break;
                                    case "1.3.6.1.5.5.7.3.3":
                                        eku = "Code Signing";
                                        break;
                                    case "1.3.6.1.5.5.7.3.4":
                                        eku = "Secure E-mail";
                                        break;
                                    case "1.3.6.1.5.5.7.3.9":
                                        eku = "OCSP Signing";
                                        break;
                                    case "1.3.6.1.4.1.311.20.2.2":
                                        eku = "Smartcard Logon";
                                        break;
                                    case "1.3.6.1.4.1.311.10.3.1":
                                        eku = "Microsoft Trust List Signing";
                                        break;
                                    case "1.3.6.1.4.1.311.10.3.4":
                                        eku = "Encrypting file system";
                                        break;
                                    case "1.3.6.1.5.2.3.5":
                                        eku = "KDC Authentication";
                                        break;
                                    case "1.3.6.1.5.5.8.2.2":
                                        eku = "IP Security IKE Intermediate";
                                        break;
                                    default:
                                        eku = entry.Attributes["pKIExtendedKeyUsage"][i].ToString();
                                        break;

                                }
                                if(pKIExtendedKeyUsage.Length == 0)
                                {
                                    pKIExtendedKeyUsage = eku;
                                }
                                else
                                {
                                    pKIExtendedKeyUsage += "; " + eku;
                                }

                            }
                            AppendImportLog($" [ExtendedKeyUsage] {pKIExtendedKeyUsage}");
                        }

                        if (entry.Attributes["pKIKeyUsage"] != null)
                        {
                            try
                            {
                                byte[] bytearr_pKIKeyUsage = (byte[])entry.Attributes["pKIKeyUsage"][0];
                                foreach (byte b in bytearr_pKIKeyUsage)
                                {
                                    pKIKeyUsage += $"{b} ";
                                }
                            }
                            catch 
                            {
                                pKIKeyUsage = entry.Attributes["pKIKeyUsage"][0].ToString();
                            }

                        }
                        if (entry.Attributes["pKIMaxIssuingDepth"] != null)
                        {
                            pKIMaxIssuingDepth = entry.Attributes["pKIMaxIssuingDepth"][0].ToString();
                        }
                        if (entry.Attributes["pKIOverlapPeriod"] != null)
                        {
                            byte[] bytearr_pKIOverlapPeriod = (byte[])entry.Attributes["pKIOverlapPeriod"][0];
                            Array.Reverse(bytearr_pKIOverlapPeriod);
                            string littleEndianByte = "";
                            foreach (byte b in bytearr_pKIOverlapPeriod)
                            {
                                littleEndianByte += $"{b:x2}";
                            }
                            pKIOverlapPeriod = Math.Round(Convert.ToInt64(littleEndianByte, 16) * -0.0000001 / 3600 / 24, 2).ToString();
                        }
                        if (entry.Attributes["nTSecurityDescriptor"] != null)
                        {
                            
                            bool isContainer = true;
                            bool isDS = true;
                            int offset = 0;

                            byte[] bytearr_nTSecurityDescriptor = (byte[])entry.Attributes["nTSecurityDescriptor"][0];
                            System.Security.AccessControl.CommonSecurityDescriptor acl = new System.Security.AccessControl.CommonSecurityDescriptor(
                                isContainer, isDS, bytearr_nTSecurityDescriptor, offset);
                            List<string> acelist = new List<string>();
                           

                            foreach (var ace in (DiscretionaryAcl)acl.DiscretionaryAcl)
                            {
                                int accessmask = ((KnownAce)ace).AccessMask;
                                System.DirectoryServices.ActiveDirectoryRights rights = (System.DirectoryServices.ActiveDirectoryRights)accessmask;
                                string acerights = rights.ToString();
                                string acetype = ace.AceType.ToString();
                                string securityIdentifier = ((KnownAce)ace).SecurityIdentifier.Value;
                                string acetext = "";
                                try
                                {
                                    string accountlookupldapFilter = $"(objectsid={securityIdentifier})";
                                    string[] accountlookupattributelist = null;
                                    searchScope = SearchScope.Subtree;
                                    string accountlookupsearchRoot = "DC=ice,DC=corp,DC=com";//_defaultNamingContext;

                                    SearchRequest searchRequest = new SearchRequest(accountlookupsearchRoot, accountlookupldapFilter, searchScope, accountlookupattributelist);
                                    SearchResponse searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);
                                    string seciddistinguishedName = "";
                                    if (searchResponse.Entries.Count != 0)
                                    {
                                        seciddistinguishedName = searchResponse.Entries[0].DistinguishedName;
                                    }
                                    acetext = $"{acetype} {acerights} for {seciddistinguishedName}";
                                }
                                catch
                                {
                                    acetext = $"{acetype} {acerights} for {securityIdentifier}";
                                }
                                AppendImportLog($" {acetext}");
                                acelist.Add(acetext);
                            }
                            
                            nTSecurityDescriptor = acl.GetSddlForm(System.Security.AccessControl.AccessControlSections.All);
                        }
                        if (entry.Attributes["modifyTimeStamp"] != null)
                        {
                            string timestamp = entry.Attributes["modifyTimeStamp"][0].ToString();
                            string format = "yyyyMMddHHmmss.f'Z'";
                            modifyTimeStamp = DateTime.ParseExact(timestamp, format, CultureInfo.InvariantCulture).ToString("yyyyMMdd hh:mm:ss") ;
                        }

                        AppendImportLog("");

                        String sql = $@"Update Template set
                            cn = '{cn}',
                            distinguishedName = '{distinguishedName}',
                            flags = '{flags}',
                            msPKICertificateApplicationPolicy = '{msPKI_Certificate_Application_Policy}',
                            msPKICertificateNameFlag = '{msPKI_Certificate_Name_Flag}',
                            msPKICertificatePolicy = '{msPKI_Certificate_Policy}',
                            msPKICertTemplateOID = '{msPKI_Cert_Template_OID}',
                            msPKIEnrollmentFlag = '{msPKI_Enrollment_Flag}',
                            msPKIMinimalKeySize = '{msPKI_Minimal_Key_Size}',
                            msPKIPrivateKeyFlag = '{msPKI_Private_Key_Flag}',
                            msPKIRAApplicationPolicies = '{msPKI_RA_Application_Policies}',
                            msPKIRAPolicies = '{msPKI_RA_Policies}',
                            msPKIRASignature = '{msPKI_RA_Signature}',
                            msPKISupersedeTemplates = '{msPKI_Supersede_Templates}',
                            msPKITemplateMinorRevision = '{msPKI_Template_Minor_Revision}',
                            msPKITemplateSchemaVersion = '{msPKI_Template_Schema_Version}',
                            pKICriticalExtensions = '{pKICriticalExtensions}',
                            pKIDefaultCSPs = '{pKIDefaultCSPs}',
                            pKIDefaultKeySpec = '{pKIDefaultKeySpec}',
                            pKIExpirationPeriod = '{pKIExpirationPeriod}',
                            pKIExtendedKeyUsage = '{pKIExtendedKeyUsage}',
                            pKIKeyUsage = '{pKIKeyUsage}',
                            pKIMaxIssuingDepth = '{pKIMaxIssuingDepth}',
                            pKIOverlapPeriod = '{pKIOverlapPeriod}',
                            nTSecurityDescriptor = '{nTSecurityDescriptor}'
                            where GUID = '{objectGUID}' and modifyTimeStamp = '{modifyTimeStamp}'
                            If @@ROWCOUNT=0 
                            Insert into Template
                            (cn,distinguishedName,flags,msPKICertificateApplicationPolicy,msPKICertificateNameFlag,
                            msPKICertificatePolicy,msPKICertTemplateOID,msPKIEnrollmentFlag,msPKIMinimalKeySize,
                            msPKIPrivateKeyFlag,msPKIRAApplicationPolicies,msPKIRAPolicies,msPKIRASignature,
                            msPKISupersedeTemplates,msPKITemplateMinorRevision,msPKITemplateSchemaVersion,GUID,
                            pKICriticalExtensions,pKIDefaultCSPs,pKIDefaultKeySpec,pKIExpirationPeriod,pKIExtendedKeyUsage,
                            pKIKeyUsage,pKIMaxIssuingDepth,pKIOverlapPeriod,nTSecurityDescriptor,modifyTimeStamp)
                            VALUES ('{cn}','{distinguishedName}','{flags}','{msPKI_Certificate_Application_Policy}','{msPKI_Certificate_Name_Flag}',
                            '{msPKI_Certificate_Policy}','{msPKI_Cert_Template_OID}','{msPKI_Enrollment_Flag}','{msPKI_Minimal_Key_Size}',
                            '{msPKI_Private_Key_Flag}','{msPKI_RA_Application_Policies}','{msPKI_RA_Policies}','{msPKI_RA_Signature}',
                            '{msPKI_Supersede_Templates}','{msPKI_Template_Minor_Revision}','{msPKI_Template_Schema_Version}','{objectGUID}',
                            '{pKICriticalExtensions}','{pKIDefaultCSPs}','{pKIDefaultKeySpec}','{pKIExpirationPeriod}','{pKIExtendedKeyUsage}',
                            '{pKIKeyUsage}','{pKIMaxIssuingDepth}','{pKIOverlapPeriod}','{nTSecurityDescriptor}','{modifyTimeStamp}')";

                        

                        var connectionString = LoadConnectionString();

                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            using (SqlCommand command = new SqlCommand(sql, connection))
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }
    }
}
