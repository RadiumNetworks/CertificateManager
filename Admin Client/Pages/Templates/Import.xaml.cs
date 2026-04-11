using CERTENROLLLib;
using CertificateManager.Admin.Data.Services;
using CertificateManager.Admin.Models;
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

namespace CertificateManager.Admin.Pages.Templates
{

    public sealed partial class Import : Page
    {
        private readonly TemplateImportHelper _importService = new();

        public static LdapConnection _ldapConnection
        {
            get => _sharedService.LdapConnection!;
            set => _sharedService.LdapConnection = value;
        }
        public static DomainController _domainController
        {
            get => _sharedService.DomainController!;
            set => _sharedService.DomainController = value;
        }
        public static ActiveDirectorySite _site
        {
            get => _sharedService.Site!;
            set => _sharedService.Site = value;
        }
        public static int _maxPageSize
        {
            get => _sharedService.MaxPageSize;
            set => _sharedService.MaxPageSize = value;
        }
        public static string _defaultNamingContext
        {
            get => _sharedService.DefaultNamingContext!;
            set => _sharedService.DefaultNamingContext = value;
        }
        public static string _configurationNamingContext
        {
            get => _sharedService.ConfigurationNamingContext!;
            set => _sharedService.ConfigurationNamingContext = value;
        }
        public static string _schemaNamingContext
        {
            get => _sharedService.SchemaNamingContext!;
            set => _sharedService.SchemaNamingContext = value;
        }

        private static TemplateImportHelper _sharedService = new();

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
            _importService.NewADConnection(port, referralchasing);
            _sharedService = _importService;
        }

        public void GetADConfig()
        {
            _importService.GetADConfig();
        }

        public string LoadConnectionString()
        {
            return TemplateImportHelper.LoadConnectionString();
        }

        public List<SearchResultEntryCollection> GetPagedADObjects(string searchRoot, string ldapFilter, string[] attributelist, SearchScope searchScope)
        {
            return _importService.GetPagedADObjects(searchRoot, ldapFilter, attributelist, searchScope);
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
                    foreach (SearchResultEntry? entry in entryCollection)
                    {
                        if (entry is null) continue;
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

                            cn = entry.Attributes["cn"][0].ToString()!;
                            AppendImportLog($"[Certificate Template] {cn}");
                        }
                        if (entry.Attributes["distinguishedName"] is null)
                        {
                            distinguishedName = entry.Attributes["distinguishedName"][0].ToString()!;
                        }
                        if (entry.Attributes["flags"] != null)
                        {
                            flags = entry.Attributes["flags"][0].ToString()!;
                        }
                        if (entry.Attributes["msPKI-Certificate-Application-Policy"] != null)
                        {
                            var oids = new List<string>();
                            for (int i = 0; i < entry.Attributes["msPKI-Certificate-Application-Policy"].Count; i++)
                            {
                                oids.Add(entry.Attributes["msPKI-Certificate-Application-Policy"][i].ToString()!);
                            }
                            msPKI_Certificate_Application_Policy = TemplateImportHelper.ResolveOidNames(oids);
                        }
                        if (entry.Attributes["msPKI-Certificate-Name-Flag"] != null)
                        {
                            var msPKICertificateNameFlag = long.Parse(entry.Attributes["msPKI-Certificate-Name-Flag"][0].ToString()!);

                            msPKI_Certificate_Name_Flag = TemplateImportHelper.DecodeCertificateNameFlags(msPKICertificateNameFlag);
                        }
                        AppendImportLog($" [Subject and SAN] {msPKI_Certificate_Name_Flag}");

                        if (entry.Attributes["msPKI-Certificate-Policy"] != null)
                        {
                            msPKI_Certificate_Policy = entry.Attributes["msPKI-Certificate-Policy"][0].ToString()!;
                        }
                        if (entry.Attributes["msPKI-Cert-Template-OID"] != null)
                        {
                            msPKI_Cert_Template_OID = entry.Attributes["msPKI-Cert-Template-OID"][0].ToString()!;
                        }
                        if (entry.Attributes["msPKI-Enrollment-Flag"] != null)
                        {
                            var msPKIEnrollmentFlag = long.Parse(entry.Attributes["msPKI-Enrollment-Flag"][0].ToString()!);
                            msPKI_Enrollment_Flag = TemplateImportHelper.DecodeEnrollmentFlags(msPKIEnrollmentFlag);
                        }
                        AppendImportLog($" [Enrollment Flags] {msPKI_Enrollment_Flag}");

                        if (TemplateImportHelper.IsUnsecureSubjectConfiguration(msPKI_Certificate_Name_Flag, msPKI_Enrollment_Flag))
                        {
                            AppendImportLog($" [Unsecure configuration] Template includes Enrollee supplies subject and does not require issuance authorization");
                        }

                        if (entry.Attributes["msPKI-Minimal-Key-Size"] != null)
                        {
                            msPKI_Minimal_Key_Size = entry.Attributes["msPKI-Minimal-Key-Size"][0].ToString()!;
                        }

                        if (entry.Attributes["msPKI-Private-Key-Flag"] != null)
                        {
                            var msPKIPrivateKeyFlag = long.Parse(entry.Attributes["msPKI-Private-Key-Flag"][0].ToString()!);
                            msPKI_Private_Key_Flag = TemplateImportHelper.DecodePrivateKeyFlags(msPKIPrivateKeyFlag);
                        }
                        if (entry.Attributes["msPKI-RA-Application-Policies"] != null)
                        {
                            msPKI_RA_Application_Policies = entry.Attributes["msPKI-RA-Application-Policies"][0].ToString()!;
                        }
                        if (entry.Attributes["msPKI-RA-Policies"] != null)
                        {
                            msPKI_RA_Policies = entry.Attributes["msPKI-RA-Policies"][0].ToString()!;
                        }
                        if (entry.Attributes["msPKI-RA-Signature"] != null)
                        {
                            msPKI_RA_Signature = entry.Attributes["msPKI-RA-Signature"][0].ToString()!;
                        }

                        if (entry.Attributes["msPKI-Supersede-Templates"] != null)
                        {
                            msPKI_Supersede_Templates = entry.Attributes["msPKI-Supersede-Templates"][0].ToString()!;
                        }
                        if (entry.Attributes["msPKI-Template-Minor-Revision"] != null)
                        {
                            msPKI_Template_Minor_Revision = entry.Attributes["msPKI-Template-Minor-Revision"][0].ToString()!;
                        }
                        if (entry.Attributes["msPKI-Template-Schema-Version"] != null)
                        {
                            msPKI_Template_Schema_Version = entry.Attributes["msPKI-Template-Schema-Version"][0].ToString()!;
                        }
                        if (entry.Attributes["objectGUID"] != null)
                        {
                            objectGUID = new Guid((byte[])entry.Attributes["objectGUID"][0]);
                        }

                        if (entry.Attributes["pKICriticalExtensions"] != null)
                        {
                            pKICriticalExtensions = entry.Attributes["pKICriticalExtensions"][0].ToString()!;
                        }
                        if (entry.Attributes["pKIDefaultCSPs"] != null)
                        {
                            pKIDefaultCSPs = entry.Attributes["pKIDefaultCSPs"][0].ToString()!;
                        }
                        if (entry.Attributes["pKIDefaultKeySpec"] != null)
                        {
                            pKIDefaultKeySpec = entry.Attributes["pKIDefaultKeySpec"][0].ToString()!;
                        }
                        if (entry.Attributes["pKIExpirationPeriod"] != null)
                        {
                            byte[] bytearr_pKIExpirationPeriod = (byte[])entry.Attributes["pKIExpirationPeriod"][0];
                            pKIExpirationPeriod = TemplateImportHelper.ConvertPkiPeriodToDays(bytearr_pKIExpirationPeriod).ToString();
                        }
                        if (entry.Attributes["pKIExtendedKeyUsage"] != null)
                        {
                            var oids = new List<string>();
                            for (int i=0; i< entry.Attributes["pKIExtendedKeyUsage"].Count; i++)
                            {
                                oids.Add(entry.Attributes["pKIExtendedKeyUsage"][i].ToString()!);
                            }
                            pKIExtendedKeyUsage = TemplateImportHelper.ResolveOidNames(oids);
                            AppendImportLog($" [ExtendedKeyUsage] {pKIExtendedKeyUsage}");
                        }

                        if (entry.Attributes["pKIKeyUsage"] != null)
                        {
                            try
                            {
                                byte[] bytearr_pKIKeyUsage = (byte[])entry.Attributes["pKIKeyUsage"][0];
                                pKIKeyUsage = TemplateImportHelper.FormatKeyUsageBytes(bytearr_pKIKeyUsage);
                            }
                            catch 
                            {
                                pKIKeyUsage = entry.Attributes["pKIKeyUsage"][0].ToString()!;
                            }

                        }
                        if (entry.Attributes["pKIMaxIssuingDepth"] != null)
                        {
                            pKIMaxIssuingDepth = entry.Attributes["pKIMaxIssuingDepth"][0].ToString()!;
                        }
                        if (entry.Attributes["pKIOverlapPeriod"] != null)
                        {
                            byte[] bytearr_pKIOverlapPeriod = (byte[])entry.Attributes["pKIOverlapPeriod"][0];
                            pKIOverlapPeriod = TemplateImportHelper.ConvertPkiPeriodToDays(bytearr_pKIOverlapPeriod).ToString();
                        }
                        if (entry.Attributes["nTSecurityDescriptor"] != null)
                        {

                            byte[] bytearr_nTSecurityDescriptor = (byte[])entry.Attributes["nTSecurityDescriptor"][0];

                            Func<string, string?> resolveSid = (sid) =>
                            {
                                try
                                {
                                    string accountlookupldapFilter = $"(objectsid={sid})";
                                    string[]? accountlookupattributelist = null;
                                    var accountSearchScope = SearchScope.Subtree;
                                    string accountlookupsearchRoot = _defaultNamingContext;

                                    SearchRequest searchRequest = new SearchRequest(accountlookupsearchRoot, accountlookupldapFilter, accountSearchScope, accountlookupattributelist);
                                    SearchResponse searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);
                                    if (searchResponse.Entries.Count != 0)
                                        return searchResponse.Entries[0].DistinguishedName;
                                }
                                catch { }
                                return null;
                            };

                            var (sddl, aceList) = TemplateImportHelper.ParseSecurityDescriptor(bytearr_nTSecurityDescriptor, resolveSid);
                            foreach (var acetext in aceList)
                            {
                                AppendImportLog($" {acetext}");
                            }
                            nTSecurityDescriptor = sddl;
                        }
                        if (entry.Attributes["modifyTimeStamp"] != null)
                        {
                            string? timestamp = entry.Attributes["modifyTimeStamp"][0].ToString();
                            if(!(timestamp is null))
                            {
                                modifyTimeStamp = TemplateImportHelper.ParseModifyTimestamp(timestamp);
                            }
                            
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
