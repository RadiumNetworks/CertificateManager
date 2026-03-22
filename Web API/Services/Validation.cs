using CERTCLILib;
using CERTENROLLLib;
using CertificateManager.Controllers;
using CertificateManager.Models;
using DnsClient;
using System.Runtime.InteropServices;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace CertificateManager.Services
{
    public class Validation
    {
        private readonly CertificateService _certificateService;
        public Validation(CertificateService certificateService)
        {
            _certificateService = certificateService;
        }

        const int CR_IN_ANY = 0;
        const int CR_IN_BASE64 = 0x1;
        const int CR_IN_PKCS10 = 0x100;

        const int CR_OUT_BASE64 = 0x1;

        const int CR_DISP_ISSUED = 0x3;
        const int CR_DISP_PENDING = 0x5;

        private Entry _entry = new Entry();

        public string CAConfig { get; set; } = string.Empty;

        public int selectedAction = 0;
        private bool _btn1hidden = true;
        private bool _btn2hidden = true;

        public List<string> _challenges = new List<string>();

        //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
        internal enum PropertyType : int
        {
            PROPTYPE_LONG = 1,
            PROPTYPE_DATE = 2,
            PROPTYPE_BINARY = 3,
            PROPTYPE_STRING = 4,
            PROPTYPE_ANSI = 5
        }

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

        public string ChallengeData { get; set; } = string.Empty;
        public string CurrentRequestData { get; set; } = string.Empty;
        public string output { get; set; } = string.Empty;

        public class ParseResponse
        {
            public string Status { get; set; } = string.Empty;
            public string? ParsedData { get; set; }
            public string? ChallengeData { get; set; }
            public string? Message { get; set; }
        }

        public class SubmitRequest
        {
            public string Base64Request { get; set; } = string.Empty;
            public string CAConfig { get; set; } = string.Empty;
        }

        public class SubmitResponse
        {
            public string Status { get; set; } = string.Empty;
            public string? ParsedData { get; set; }
            public string? ChallengeData { get; set; }
            public string? Output { get; set; }
            public string? Message { get; set; }
        }


        //https://learn.microsoft.com/en-us/archive/blogs/alejacma/how-to-modify-an-interop-assembly-to-change-the-return-type-of-a-method-vb-net
        [DllImport(@"oleaut32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern Int32 VariantClear(IntPtr pvarg);
        private object GetProperty(ref CCertServerExit server, string name, string propertytype, string returntype)
        {
            IntPtr variantObjectPtr = Marshal.AllocHGlobal(2048);

            try
            {
                switch (returntype)
                {

                    case "date":
                        if (propertytype == "certificate")
                        {
                            //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
                            server.GetCertificateProperty(name, (int)PropertyType.PROPTYPE_DATE, variantObjectPtr);
                        }
                        else if (propertytype == "request")
                        {
                            //https://docs.microsoft.com/de-de/windows/win32/api/certif/nf-certif-icertserverexit-getrequestproperty
                            server.GetRequestProperty(name, (int)PropertyType.PROPTYPE_DATE, variantObjectPtr);
                        }

                        var dateresult = (DateTime)(Marshal.GetObjectForNativeVariant(variantObjectPtr));
                        return dateresult;
                    case "int":
                        if (propertytype == "certificate")
                        {
                            server.GetCertificateProperty(name, (int)PropertyType.PROPTYPE_LONG, variantObjectPtr);
                        }
                        else if (propertytype == "request")
                        {
                            server.GetRequestProperty(name, (int)PropertyType.PROPTYPE_LONG, variantObjectPtr);
                        }

                        var intresult = (int)(Marshal.GetObjectForNativeVariant(variantObjectPtr));
                        return intresult;
                    case "string":
                        if (propertytype == "certificate")
                        {
                            server.GetCertificateProperty(name, (int)PropertyType.PROPTYPE_STRING, variantObjectPtr);
                        }
                        else if (propertytype == "request")
                        {
                            server.GetRequestProperty(name, (int)PropertyType.PROPTYPE_STRING, variantObjectPtr);
                        }

                        var stringresult = (string)(Marshal.GetObjectForNativeVariant(variantObjectPtr));
                        return stringresult;
                    default:
                        IntPtr bstrPtr;
                        int bstrLen;

                        if (propertytype == "certificate")
                        {
                            server.GetCertificateProperty(name, (int)PropertyType.PROPTYPE_BINARY, variantObjectPtr);
                        }
                        else if (propertytype == "request")
                        {
                            server.GetRequestProperty(name, (int)PropertyType.PROPTYPE_BINARY, variantObjectPtr);
                        }

                        bstrPtr = Marshal.ReadIntPtr(variantObjectPtr, 8);
                        bstrLen = Marshal.ReadInt32(bstrPtr, -4);
                        byte[] bytearrresult = new byte[bstrLen];
                        Marshal.Copy(bstrPtr, bytearrresult, 0, bstrLen);
                        return bytearrresult;
                }
            }
            catch (Exception e)
            {
                switch (returntype)
                {
                    case "date":
                        return new DateTime();
                    default:
                        return null;
                }
            }
            finally
            {
                VariantClear(variantObjectPtr);
                Marshal.FreeHGlobal(variantObjectPtr);
            }
        }

        public void ParseRequestExtension(IX509CertificateRequestPkcs10 cX509CertificateRequestPkcs10)
        {
            var extensionAlternativeNames = new CX509ExtensionAlternativeNames();
            var extensionBasicConstraints = new CX509ExtensionBasicConstraints();
            var extensionTemplate = new CX509ExtensionTemplate();
            var extensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsage();
            var extensionKeyUsage = new CX509ExtensionKeyUsage();
            var extensionMSApplicationPolicies = new CX509ExtensionMSApplicationPolicies();
            var extensionSubjectKeyIdentifier = new CX509ExtensionSubjectKeyIdentifier();


            try
            {
                CurrentRequestData += "Subject:" + Environment.NewLine;
                CurrentRequestData += " " + cX509CertificateRequestPkcs10.Subject.Name + Environment.NewLine;
                foreach (string subjectinfo in cX509CertificateRequestPkcs10.Subject.Name.Split(","))
                {
                    if (subjectinfo.StartsWith("CN="))
                    {
                        string cn = subjectinfo.Split("=")[1];
                        Guid token = Guid.NewGuid();
                        Guid content = Guid.NewGuid();
                        _challenges.Add($"HTTP http://{cn}/{token.ToString()} {content.ToString()}");
                    }
                }
            }
            catch
            {
                CurrentRequestData += " Could not read subject" + Environment.NewLine;
            }


            for (var i = 0; i < cX509CertificateRequestPkcs10.X509Extensions.Count; i++)
            {
                switch (cX509CertificateRequestPkcs10.X509Extensions[i].ObjectId.Value)
                {
                    case AlternativeNames:
                        try
                        {
                            string sAlternativeNames = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            extensionAlternativeNames.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sAlternativeNames);
                            CurrentRequestData += "SubjectAlternateNames:" + Environment.NewLine;
                            foreach (CAlternativeName san in extensionAlternativeNames.AlternativeNames)
                            {
                                switch (san.Type)
                                {
                                    case AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME:
                                        CurrentRequestData += " DNS = " + san.strValue + Environment.NewLine;
                                        Guid token = Guid.NewGuid();
                                        Guid content = Guid.NewGuid();
                                        if (san.strValue.StartsWith("*."))
                                        {

                                            _challenges.Add($"DNS_TXT {san.strValue.Remove(0, 2)} {token.ToString()}");
                                        }
                                        else
                                        {
                                            _challenges.Add($"HTTP http://{san.strValue}/{token.ToString()} {content.ToString()}");
                                        }

                                        break;
                                    case AlternativeNameType.XCN_CERT_ALT_NAME_DIRECTORY_NAME:
                                        CurrentRequestData += " DN = " + san.strValue + Environment.NewLine;
                                        break;
                                    case AlternativeNameType.XCN_CERT_ALT_NAME_GUID:
                                        CurrentRequestData += " GUID = " + san.strValue + Environment.NewLine;
                                        break;
                                    case AlternativeNameType.XCN_CERT_ALT_NAME_IP_ADDRESS:
                                        CurrentRequestData += " IP = " + san.strValue + Environment.NewLine;
                                        break;
                                    case AlternativeNameType.XCN_CERT_ALT_NAME_RFC822_NAME:
                                        CurrentRequestData += " RFC822 = " + san.strValue + Environment.NewLine;
                                        break;
                                    case AlternativeNameType.XCN_CERT_ALT_NAME_OTHER_NAME:
                                        CurrentRequestData += " Other = " + san.strValue + Environment.NewLine;
                                        break;
                                    case AlternativeNameType.XCN_CERT_ALT_NAME_UNKNOWN:
                                        CurrentRequestData += " UNKNOWN = " + san.strValue + Environment.NewLine;
                                        break;
                                    default:
                                        CurrentRequestData += " " + san.Type + "=" + san.strValue + Environment.NewLine;
                                        break;
                                }

                            }

                        }
                        catch
                        {
                            CurrentRequestData = null;
                        }


                        break;
                    case KeyUsage:
                        try
                        {
                            CurrentRequestData += "KeyUsages:" + Environment.NewLine;
                            string sKeyUsage = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            extensionKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sKeyUsage);

                            CurrentRequestData += " " + extensionKeyUsage.KeyUsage.ToString() + Environment.NewLine;
                        }
                        catch
                        {

                        }

                        break;

                    case EnhancedKeyUsage:
                        try
                        {
                            CurrentRequestData += "EnhancedKeyUsages:" + Environment.NewLine;
                            string sEnhancedKeyUsage = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            extensionEnhancedKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sEnhancedKeyUsage);
                            foreach (CObjectId objectid in extensionEnhancedKeyUsage.EnhancedKeyUsage)
                            {
                                CurrentRequestData += " " + objectid.Name + "=" + objectid.Value + Environment.NewLine;
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

                            extensionMSApplicationPolicies.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sMSApplicationPolicies);
                            foreach (CCertificatePolicy certificatepolicy in extensionMSApplicationPolicies.Policies)
                            {

                            }
                        }
                        catch
                        {

                        }

                        break;

                    case Template:
                        try
                        {
                            CurrentRequestData += "Template:" + Environment.NewLine;
                            string sTemplate = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            extensionTemplate.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sTemplate);

                            CurrentRequestData += " " + extensionTemplate.TemplateOid.Value + Environment.NewLine;

                        }
                        catch
                        {

                        }
                        break;
                    case SubjectKeyIdentifier:
                        try
                        {

                            string sSubjectKeyIdentifier = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            extensionSubjectKeyIdentifier.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sSubjectKeyIdentifier);
                            CObjectId oid = extensionSubjectKeyIdentifier.ObjectId;

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
            if (_challenges.Count != 0)
            {
                ChallengeData = @"For automatic enrollment, the following files or entries must be created." + Environment.NewLine;
            }
            foreach (string challenge in _challenges)
            {
                ChallengeData += challenge + Environment.NewLine;
            }

        }

        async Task<bool> CheckChallenge(string type, string location, string content)
        {
            if (type == "HTTP")
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        HttpResponseMessage response = await client.GetAsync(location);
                        response.EnsureSuccessStatusCode();
                        string filecontent = await response.Content.ReadAsStringAsync();
                        if (filecontent == content)
                        {
                            return true;
                        }
                    }
                }
                catch
                {

                }
            }
            else if (type == "DNS_TXT")
            {
                try
                {
                    var lookup = new LookupClient();
                    var result = await lookup.QueryAsync(location, QueryType.TXT);
                    var txtRecords = result.Answers.TxtRecords();
                    foreach (var txt in txtRecords)
                    {
                        string txtValue = string.Join("", txt.Text);
                        if (txtValue == content)
                        {
                            return true;
                        }
                    }
                }
                catch
                {

                }
            }
            return false;
        }


        public ParseResponse ParseRequest(string request)
        {
            ParseResponse parseResponse = new ParseResponse();
            try
            {
                var cX509CertificateRequestCmc = new CERTENROLLLib.CX509CertificateRequestCmc();
                cX509CertificateRequestCmc.InitializeDecode(
                    request,
                    CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);
                var cX509CertificateRequestPkcs10 = (CERTENROLLLib.IX509CertificateRequestPkcs10)cX509CertificateRequestCmc.GetInnerRequest(0);
                ParseRequestExtension(cX509CertificateRequestPkcs10);

                
                parseResponse.Status = "Parsed";
                parseResponse.ParsedData = CurrentRequestData;
                parseResponse.ChallengeData = ChallengeData;
                return parseResponse;
            }
            catch (Exception ex)
            {
                parseResponse.Status = "Error";
                parseResponse.Message = $"Failed to parse request: {ex.Message}";
                return parseResponse;
            }


        }
    }
}
