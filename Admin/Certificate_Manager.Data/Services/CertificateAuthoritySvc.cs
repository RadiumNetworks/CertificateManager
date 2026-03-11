using CERTADMINLib;
using CERTCLILib;
using CERTENROLLLib;
using Certificate_Manager.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Certificate_Manager.Data.Services.CertificateAuthoritySvc;

namespace Certificate_Manager.Data.Services
{
    public class CertificateAuthoritySvc
    {

        //ExtensionOIDs
        private const string AlternativeNames = "2.5.29.17";
        private const string AuthorityInformationAccess = "1.3.6.1.5.5.7.1.1";
        private const string AuthorityKeyIdentifier = "2.5.29.35";
        private const string BasicConstraints = "2.5.29.19";
        private const string CertificatePolicies = "2.5.29.32";
        private const string CRLDistributionPoints = "2.5.29.31";
        private const string EnhancedKeyUsage = "2.5.29.37";
        private const string FreshestCRL = "2.5.29.46";
        private const string KeyUsage = "2.5.29.15";
        private const string MSApplicationPolicies = "1.3.6.1.4.1.311.21.10";
        private const string NameConstraints = "2.5.29.30";
        private const string PolicyConstraints = "2.5.29.36";
        private const string PolicyMappings = "2.5.29.33";
        private const string PrivateKeyUsagePeriod = "2.5.29.16";
        private const string SMimeCapabilities = "1.2.840.113549.1.9.15";
        private const string SubjectDirectoryAttributes = "2.5.29.9";
        private const string SubjectKeyIdentifier = "2.5.29.14";
        private const string Template = "1.3.6.1.4.1.311.21.7";
        private const string TemplateName = "1.3.6.1.4.1.311.20.2";

        private const int CV_OUT_BASE64HEADER = 0;
        private const int CV_OUT_BASE64 = 1;

        public List<string> SubjectAlternativeNames { get; set; } = new List<string>();
        public List<string> EKUs { get; set; } = new List<string>();

        internal enum PropertyType : int
        {
            PROPTYPE_LONG = 1,
            PROPTYPE_DATE = 2,
            PROPTYPE_BINARY = 3,
            PROPTYPE_STRING = 4,
            PROPTYPE_ANSI = 5
        }

        private static readonly HashSet<string> BinaryColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "Request.RawRequest", "RawCertificate"
        };

        private static readonly string[] ResultColumns = new[]
        {
            "RequestID",
            "Request.RequesterName",
            "Request.SubmittedWhen",
            "Request.ResolvedWhen",
            "Request.RawRequest",
            "Request.RequestType",
            "Request.StatusCode",
            "Request.Disposition",
            "Request.DispositionMessage",
            "Request.CallerName",
            "Request.DistinguishedName",
            "Request.RequestAttributes",
            "CommonName",
            "Organization",
            "OrgUnit",
            "EMail",
            "Locality",
            "Country",
            "State",
            "SerialNumber",
            "NotBefore",
            "NotAfter",
            "CertificateHash",
            "CertificateTemplate",
            "RawCertificate",
            "PublicKeyLength",
            "PublicKeyAlgorithm",
            "Request.RevokedWhen",
            "Request.RevokedReason"
        };

        //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
        public class Certificate
        {
            public int RequestId { get; set; } = 0;
            public int? PublicKeyLength { get; set; } = null;
            public string PublicKeyAlgorithm { get; set; } = null;
            public string SerialNumber { get; set; } = null;
            public string CertificateTemplate { get; set; } = null;
            public string CertificateHash { get; set; } = null;
            public DateTime NotBefore { get; set; }
            public DateTime NotAfter { get; set; }
            public byte[] RawCertificate { get; set; } = null;
            public byte[] RawPublicKey { get; set; } = null;
            public byte[] RawPublicKeyAlgorithmParameters { get; set; } = null;
            public int? RequestType { get; set; } = null;
            public string CommonName { get; set; } = null;
            public string Organization { get; set; } = null;
            public string OrgUnit { get; set; } = null;
            public string EMail { get; set; } = null;
            public string Country { get; set; } = null;
            public string Locality { get; set; } = null;
            public string State { get; set; } = null;
            public string CallerName { get; set; } = null;
            public string TemplateEnrollmentFlags { get; set; } = null;
            public string TemplateGeneralFlags { get; set; } = null;
            public string TemplatePrivateKeyFlags { get; set; } = null;
            public string PublicKeyAlgorithmParameters { get; set; } = null;
            public DateTime RevocationDate { get; set; }
            public string RevocationReason { get; set; } = null;
        }

        //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getrequestproperty
        public class Request
        {
            public int RequestId { get; set; } = 0;
            public int? RequestType { get; set; } = null;
            public int? StatusCode { get; set; } = null;
            public int? Disposition { get; set; } = null;
            public string DispositionMessage { get; set; } = null;
            public DateTime ResolvedWhen { get; set; }
            public DateTime SubmittedWhen { get; set; }
            public byte[] RawCertificate { get; set; } = null;
            public string RequesterName { get; set; } = null;
            public string DistinguishedName { get; set; } = null;
            public string RequestAttributes { get; set; } = null;
            public string CommonName { get; set; } = null;
            public string Organization { get; set; } = null;
            public string OrgUnit { get; set; } = null;
            public string EMail { get; set; } = null;
            public string Locality { get; set; } = null;
            public string Country { get; set; } = null;
            public string State { get; set; } = null;
            public byte[] RawRequest { get; set; } = null;
            public string CallerName { get; set; } = null;
        }



        public void ParseRequestExtension(IX509CertificateRequestPkcs10 cX509CertificateRequestPkcs10)
        {
            var cX509ExtensionAlternativeNames = new CX509ExtensionAlternativeNames();
            var cX509ExtensionBasicConstraints = new CX509ExtensionBasicConstraints();
            var cX509ExtensionTemplate = new CX509ExtensionTemplate();
            var cX509ExtensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsage();
            var cX509ExtensionKeyUsage = new CX509ExtensionKeyUsage();
            var cX509ExtensionMSApplicationPolicies = new CX509ExtensionMSApplicationPolicies();
            var cX509ExtensionSubjectKeyIdentifier = new CX509ExtensionSubjectKeyIdentifier();
            var cX509ExtensionTemplateName = new CX509ExtensionTemplateName();
            var cX509ExtensionSmimeCapabilities = new CX509ExtensionSmimeCapabilities();

            for (var i = 0; i < cX509CertificateRequestPkcs10.X509Extensions.Count; i++)
            {
                switch (cX509CertificateRequestPkcs10.X509Extensions[i].ObjectId.Value)
                {
                    case AlternativeNames:
                        try
                        {
                            string sAlternativeNames = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            cX509ExtensionAlternativeNames.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sAlternativeNames);
                            foreach (CAlternativeName san in cX509ExtensionAlternativeNames.AlternativeNames)
                            {
                                if (san.Type == AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME)
                                {
                                    SubjectAlternativeNames.Add("DNS = " + san.strValue);
                                }
                                else if (san.Type == AlternativeNameType.XCN_CERT_ALT_NAME_DIRECTORY_NAME)
                                {
                                    SubjectAlternativeNames.Add("DN = " + san.strValue);
                                }
                                else if (san.Type == AlternativeNameType.XCN_CERT_ALT_NAME_USER_PRINCIPLE_NAME)
                                {
                                    SubjectAlternativeNames.Add("UPN = " + san.strValue);
                                }
                                else
                                {
                                    SubjectAlternativeNames.Add(san.Type + " = " + san.strValue);
                                }
                            }
                        }
                        catch
                        {

                        }


                        break;
                    case KeyUsage:
                        try
                        {
                            string sKeyUsage = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);
                            cX509ExtensionKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sKeyUsage);
                        }
                        catch
                        {

                        }

                        break;

                    case EnhancedKeyUsage:
                        try
                        {
                            string sEnhancedKeyUsage = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            cX509ExtensionEnhancedKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sEnhancedKeyUsage);
                            foreach (CObjectId objectid in cX509ExtensionEnhancedKeyUsage.EnhancedKeyUsage)
                            {
                                if (objectid.Value == "1.3.6.1.5.5.7.3.1")
                                {
                                    EKUs.Add("Server Authentication");
                                }
                                else if (objectid.Value == "1.3.6.1.5.5.7.3.2")
                                {
                                    EKUs.Add("Client Authentication");
                                }
                                else
                                {
                                    EKUs.Add(objectid.Name.ToString());
                                }
                            }
                        }
                        catch
                        {

                        }

                        break;

                    case MSApplicationPolicies:
                        try
                        {
                            string sMSApplicationPolicies = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);
                            cX509ExtensionMSApplicationPolicies.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sMSApplicationPolicies);
                        }
                        catch
                        {

                        }

                        break;

                    case Template:
                        try
                        {
                            string sTemplate = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);
                            cX509ExtensionTemplate.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sTemplate);
                        }
                        catch
                        {

                        }
                        break;
                    case SubjectKeyIdentifier:
                        try
                        {
                            string sSubjectKeyIdentifier = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);
                            cX509ExtensionSubjectKeyIdentifier.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sSubjectKeyIdentifier);
                            CObjectId oid = cX509ExtensionSubjectKeyIdentifier.ObjectId;
                        }
                        catch
                        {

                        }
                        break;
                    case TemplateName:
                        try
                        {
                            string sTemplateName = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);
                            cX509ExtensionTemplateName.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sTemplateName);
                            string templatename = cX509ExtensionTemplateName.TemplateName;
                        }
                        catch
                        {

                        }
                        break;
                    case SMimeCapabilities:
                        try
                        {
                            string sSmimeCapabilities = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);
                            cX509ExtensionSmimeCapabilities.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sSmimeCapabilities);
                            CSmimeCapabilities capabilities = cX509ExtensionSmimeCapabilities.SmimeCapabilities;
                        }
                        catch
                        {

                        }
                        break;
                    default:
                        try
                        {
                            string ssdef = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                        }
                        catch
                        {

                        }

                        break;

                }
            }
        }

        public (List<Request> Requests, List<Certificate> Certificates) ReadCADbEntries(string caConfig, Action<string> log = null)
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

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var requests = new List<Request>();
            var certificates = new List<Certificate>();

            ICertView certView = new CCertView();

            certView.OpenConnection(caConfig);

            certView.SetResultColumnCount(ResultColumns.Length);
            foreach (string col in ResultColumns)
            {
                int colIndex = certView.GetColumnIndex(0, col);
                certView.SetResultColumn(colIndex);
            }

            IEnumCERTVIEWROW enumRow = certView.OpenView();

            int rowCount = 0;

            while (enumRow.Next() != -1)
            {
                rowCount++;
                var request = new Request();
                var certificate = new Certificate();
                bool hasCertData = false;

                IEnumCERTVIEWCOLUMN enumCol = enumRow.EnumCertViewColumn();

                while (enumCol.Next() != -1)
                {
                    IntPtr variantNamePtr = Marshal.AllocHGlobal(2048);
                    string colName = enumCol.GetName();
                    try
                    {

                        int flags = BinaryColumns.Contains(colName) ? CV_OUT_BASE64 : CV_OUT_BASE64HEADER;
                        object val = enumCol.GetValue(flags);
                        if (val == null) continue;

                        switch (colName)
                        {
                            case "RequestID":
                                int id = Convert.ToInt32(val);
                                request.RequestId = id;
                                certificate.RequestId = id;
                                break;
                            case "Request.RequesterName":
                                request.RequesterName = val.ToString();
                                break;
                            case "Request.SubmittedWhen":
                                request.SubmittedWhen = (DateTime)val;
                                break;
                            case "Request.ResolvedWhen":
                                request.ResolvedWhen = (DateTime)val;
                                break;
                            case "Request.RawRequest":
                                request.RawRequest = Convert.FromBase64String(val.ToString());
                                break;
                            case "Request.RequestType":
                                request.RequestType = Convert.ToInt32(val);
                                break;
                            case "Request.StatusCode":
                                request.StatusCode = Convert.ToInt32(val);
                                break;
                            case "Request.Disposition":
                                request.Disposition = Convert.ToInt32(val);
                                break;
                            case "Request.DispositionMessage":
                                request.DispositionMessage = val.ToString();
                                break;
                            case "Request.CallerName":
                                request.CallerName = val.ToString();
                                certificate.CallerName = val.ToString();
                                break;
                            case "Request.DistinguishedName":
                                request.DistinguishedName = val.ToString();
                                break;
                            case "Request.RequestAttributes":
                                request.RequestAttributes = val.ToString();
                                break;
                            case "CommonName":
                                request.CommonName = val.ToString();
                                certificate.CommonName = val.ToString();
                                break;
                            case "Organization":
                                request.Organization = val.ToString();
                                certificate.Organization = val.ToString();
                                break;
                            case "OrgUnit":
                                request.OrgUnit = val.ToString();
                                certificate.OrgUnit = val.ToString();
                                break;
                            case "EMail":
                                request.EMail = val.ToString();
                                certificate.EMail = val.ToString();
                                break;
                            case "Locality":
                                request.Locality = val.ToString();
                                certificate.Locality = val.ToString();
                                break;
                            case "Country":
                                request.Country = val.ToString();
                                certificate.Country = val.ToString();
                                break;
                            case "State":
                                request.State = val.ToString();
                                certificate.State = val.ToString();
                                break;
                            case "SerialNumber":
                                certificate.SerialNumber = val.ToString();
                                hasCertData = true;
                                break;
                            case "NotBefore":
                                certificate.NotBefore = (DateTime)val;
                                break;
                            case "NotAfter":
                                certificate.NotAfter = (DateTime)val;
                                break;
                            case "CertificateHash":
                                certificate.CertificateHash = val.ToString();
                                break;
                            case "CertificateTemplate":
                                certificate.CertificateTemplate = val.ToString();
                                break;
                            case "RawCertificate":
                                byte[] certBytes = Convert.FromBase64String(val.ToString());
                                request.RawCertificate = certBytes;
                                certificate.RawCertificate = certBytes;
                                hasCertData = true;
                                break;
                            case "PublicKeyLength":
                                certificate.PublicKeyLength = Convert.ToInt32(val);
                                break;
                            case "PublicKeyAlgorithm":
                                certificate.PublicKeyAlgorithm = val.ToString();
                                break;
                            case "Request.RevokedWhen":
                                certificate.RevocationDate = (DateTime)val;
                                break;
                            case "Request.RevokedReason":
                                certificate.RevocationReason = val.ToString();
                                break;
                        }
                    }
                    catch
                    {

                    }
                }

                requests.Add(request);
                if (hasCertData)
                    certificates.Add(certificate);

                switch (request.RequestType)
                {
                    case 263168: //CMC

                        var cX509CertificateRequestCMC = new CERTENROLLLib.CX509CertificateRequestCmc();
                        try
                        {
                            string base64request = Convert.ToBase64String(request.RawRequest, Base64FormattingOptions.None);
                            cX509CertificateRequestCMC.InitializeDecode(
                                base64request,
                                CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);
                            var cX509CertificateRequestCMCtoPkcs10 = (IX509CertificateRequestPkcs10)cX509CertificateRequestCMC.GetInnerRequest(0);

                            ParseRequestExtension(cX509CertificateRequestCMCtoPkcs10);

                        }
                        catch
                        {

                        }

                        break;
                    case 262912: //PKCS7

                        var cX509CertificateRequestPkcs7 = new CERTENROLLLib.CX509CertificateRequestPkcs7();
                        try
                        {
                            string base64request = Convert.ToBase64String(request.RawRequest, Base64FormattingOptions.None);
                            cX509CertificateRequestPkcs7.InitializeDecode(
                                base64request,
                                CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);

                            var cX509CertificateRequestPkcs7toPkcs10 = (IX509CertificateRequestPkcs10)cX509CertificateRequestPkcs7.GetInnerRequest(0);

                            ParseRequestExtension(cX509CertificateRequestPkcs7toPkcs10);

                        }
                        catch
                        {

                        }

                        break;
                    case 262400: //PKCS10

                        var cX509CertificateRequestPkcs10 = new CERTENROLLLib.CX509CertificateRequestPkcs10();
                        try
                        {
                            string base64request = Convert.ToBase64String(request.RawRequest, Base64FormattingOptions.None);
                            cX509CertificateRequestPkcs10.InitializeDecode(
                                base64request,
                                CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);

                            ParseRequestExtension(cX509CertificateRequestPkcs10);

                        }
                        catch
                        {

                        }

                        break;
                }
                log?.Invoke($"Processed {rowCount} rows — " +
                    $"Last: RequestID = {request.RequestId}, " +
                    $"CN = {request.CommonName ?? "(none)"}, " +
                    $"Disposition = {request.DispositionMessage ?? "(none)"}");

                string requestid = "";
                string serialNumber = "";
                string notBefore = "";
                string notAfter = "";
                string certificateTemplate = "";
                string publicKeyLength = "";
                string publicKeyAlgorithm = "";
                string base64Certificate = "";
                string base64PublicKey = "";
                string base64PublicKeyAlgorithmParameters = "";
                string requestType = "";
                string base64Request = "";
                string disposition = "";
                string dispositionMessage = "";
                string requesterName = "";
                string distinguishedName = "";
                string requestCommonName = "";
                string requestOrganization = "";
                string requestOrganizationUnit = "";
                string requestEMailAddress = "";
                string requestCity = "";
                string requestCountryRegion = "";
                string requestState = "";
                string issuedCommonName = "";
                string issuedOrganization = "";
                string issuedOrganizationUnit = "";
                string issuedEMailAddress = "";
                string issuedCity = "";
                string issuedCountryRegion = "";
                string issuedState = "";
                string callerName = "";
                string certificateHash = "";


                try { requestid = certificate.RequestId.ToString(); }
                catch { }
                try { serialNumber = certificate.SerialNumber; }
                catch { }
                try { notBefore = certificate.NotBefore.ToString("yyyy-MM-dd hh:mm:ss"); }
                catch { }
                try { notAfter = certificate.NotAfter.ToString("yyyy-MM-dd hh:mm:ss"); }
                catch { }
                if (certificate.CertificateTemplate != null)
                {
                    try { certificateTemplate = certificate.CertificateTemplate.ToString(); }
                    catch { }
                }

                if (certificate.CertificateHash != null)
                {
                    try { certificateHash = certificate.CertificateHash.ToString(); }
                    catch { }
                }

                if (certificate.PublicKeyLength != null)
                {
                    try { publicKeyLength = certificate.PublicKeyLength.ToString(); }
                    catch { }
                }

                try { publicKeyAlgorithm = certificate.PublicKeyAlgorithm.ToString(); }
                catch { }
                try { base64Certificate = Convert.ToBase64String(certificate.RawCertificate, Base64FormattingOptions.None); }
                catch { }
                try { base64PublicKey = Convert.ToBase64String(certificate.RawPublicKey, Base64FormattingOptions.None); }
                catch { }
                try { base64PublicKeyAlgorithmParameters = Convert.ToBase64String(certificate.RawPublicKeyAlgorithmParameters, Base64FormattingOptions.None); }
                catch { }
                try { requestType = request.RequestType.ToString(); }
                catch { }
                try { base64Request = Convert.ToBase64String(request.RawRequest, Base64FormattingOptions.None); }
                catch { }
                try { disposition = request.Disposition.ToString(); }
                catch { }
                try { dispositionMessage = request.DispositionMessage; }
                catch { }
                try { requesterName = request.RequesterName; }
                catch { }
                try { distinguishedName = request.DistinguishedName; }
                catch { }
                try { requestCommonName = request.CommonName; }
                catch { }
                try { requestOrganization = request.Organization; }
                catch { }
                try { requestOrganizationUnit = request.OrgUnit; }
                catch { }
                try { requestEMailAddress = request.EMail; }
                catch { }
                try { requestCity = request.Locality; }
                catch { }
                try { requestCountryRegion = request.Country; }
                catch { }
                try { requestState = request.State; }
                catch { }
                try { issuedCommonName = certificate.CommonName; }
                catch { }
                try { issuedOrganization = certificate.Organization; }
                catch { }
                try { issuedOrganizationUnit = certificate.OrgUnit; }
                catch { }
                try { issuedEMailAddress = certificate.EMail; }
                catch { }
                try { issuedCity = certificate.Locality; }
                catch { }
                try { issuedCountryRegion = certificate.Country; }
                catch { }
                try { issuedState = certificate.State; }
                catch { }
                try { callerName = request.CallerName; }
                catch { }

                try
                {
                    String sql = $@"Update Entry set 
                    Base64Request='{base64Request}',
                    Base64Certificate='{base64Certificate}',
                    SerialNumber='{serialNumber}',
                    RequestDisposition='{disposition}',
                    RequesterName='{requesterName}',
                    RequestType='{requestType}',
                    IssuedEmailAddress='{issuedEMailAddress}',
                    IssuedCommonName='{issuedCommonName}',
                    IssuedCountryRegion='{issuedCountryRegion}',
                    IssuedOrganization='{issuedOrganization}',
                    IssuedOrganizationUnit='{issuedOrganizationUnit}',
                    CallerName='{callerName}',
                    CertificateHash='{certificateHash}',
                    CertificateTemplate='{certificateTemplate}',
                    CertificateEffectiveDate='{notBefore}',
                    CertificateExpirationDate='{notAfter}',
                    PublicKeyLength='{publicKeyLength}',
                    PublicKeyAlgorithm='{publicKeyAlgorithm}',
                    RequestCountryRegion='{requestCountryRegion}',
                    RequestOrganization='{requestOrganization}',
                    RequestOrganizationUnit='{requestOrganizationUnit}',
                    RequestCommonName='{requestCommonName}',
                    RequestCity='{requestCity}',
                    RequestEmailAddress='{requestEMailAddress}'
                    where RequestID='{requestid}' and CAConfig='{caConfig}' 
                    If @@ROWCOUNT=0 
                    Insert into Entry (Base64Request, RequestId, 
                    CAConfig, Base64Certificate, SerialNumber, RequestDisposition, RequesterName,
                    RequestType, IssuedEmailAddress, IssuedCommonName, IssuedCountryRegion, IssuedOrganization, 
                    IssuedOrganizationUnit, CallerName, CertificateHash, CertificateTemplate,
                    CertificateExpirationDate, CertificateEffectiveDate,
                    PublicKeyLength,PublicKeyAlgorithm, RequestCountryRegion, RequestOrganization, 
                    RequestOrganizationUnit, RequestCommonName, RequestCity, RequestEmailAddress) 
                    VALUES ('{base64Request}','{requestid}',
                    '{caConfig}','{base64Certificate}','{serialNumber}','{disposition}','{requesterName}',
                    '{requestType}','{issuedEMailAddress}','{issuedCommonName}','{issuedCountryRegion}','{issuedOrganization}',
                    '{issuedOrganizationUnit}','{callerName}','{certificateHash}','{certificateTemplate}',
                    '{notAfter}','{notBefore}',
                    '{publicKeyLength}','{publicKeyAlgorithm}','{requestCountryRegion}','{requestOrganization}',
                    '{requestOrganizationUnit}','{requestCommonName}','{requestCity}','{requestEMailAddress}')";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                        log?.Invoke("Update or Import successful");
                    }
                }
                catch (Exception e)
                {
                    log?.Invoke(e.Message);
                }



                foreach (string san in SubjectAlternativeNames)
                {
                    String sansql = $@"Update SAN set SubjectAlternativeName='{san}' 
                    where RequestID='{requestid}' and CAConfig='{caConfig}' and SubjectAlternativeName='{san}'
                    If @@ROWCOUNT=0 
                    Insert into SAN (SubjectAlternativeName, RequestId, CAConfig) 
                    VALUES ('{san}','{requestid}','{caConfig}')";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(sansql, connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                        log?.Invoke($"SAN Update or Import successful");
                    }
                }

                foreach (string eku in EKUs)
                {
                    String ekusql = $@"Update EKU set Name='{eku}' 
                    where RequestID='{requestid}' and CAConfig='{caConfig}' and Name = '{eku}'
                    If @@ROWCOUNT=0 
                    Insert into EKU (Name, RequestId, CAConfig) 
                    VALUES ('{eku}','{requestid}','{caConfig}')";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(ekusql, connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                        log?.Invoke($"EKU Update or Import successful");
                    }
                }

                SubjectAlternativeNames = new List<string>();
                EKUs = new List<string>();

                if (rowCount % 10 == 0)
                {
                    log?.Invoke($"Processed {rowCount} rows so far...");
                }

            }

            return (requests, certificates);
        }
    }
}
