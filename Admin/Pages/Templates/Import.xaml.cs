using CERTENROLLLib;
using Certificate_Manager.Data.Services;
using Certificate_Manager.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            if (message.Length == 0)
            {
                ImportLog.Text += $" \n";
            }
            else
            {
                ImportLog.Text += $"[{timestamp}] {message}\n";
            }
            ImportLog.Select(ImportLog.Text.Length, 0);

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
            int port = 3268;
            NewADConnection(port);
            _ldapConnection.SessionOptions.ReferralChasing = ReferralChasingOptions.All;

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
                            AppendImportLog($"Certificate Template {cn}");
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
                            msPKI_Certificate_Application_Policy = entry.Attributes["msPKI-Certificate-Application-Policy"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-Certificate-Name-Flag"] != null)
                        {
                            msPKI_Certificate_Name_Flag = entry.Attributes["msPKI-Certificate-Name-Flag"][0].ToString();
                        }

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
                            msPKI_Enrollment_Flag = entry.Attributes["msPKI-Enrollment-Flag"][0].ToString();
                        }
                        if (entry.Attributes["msPKI-Minimal-Key-Size"] != null)
                        {
                            msPKI_Minimal_Key_Size = entry.Attributes["msPKI-Minimal-Key-Size"][0].ToString();
                        }

                        if (entry.Attributes["msPKI-Private-Key-Flag"] != null)
                        {
                            msPKI_Private_Key_Flag = entry.Attributes["msPKI-Private-Key-Flag"][0].ToString();
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
                            AppendImportLog($" ExtendedKeyUsage {pKIExtendedKeyUsage}");
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
