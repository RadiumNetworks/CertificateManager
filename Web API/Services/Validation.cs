using CERTADMINLib;
using CERTCLILib;
using CERTENROLLLib;
using CertificateManager.Controllers;
using CertificateManager.Models;
using DnsClient;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace CertificateManager.Services
{
    public class Validation
    {
        private readonly CertificateService _certificateService;
        private readonly IHttpClientFactory _httpClientFactory;
        public Validation(CertificateService certificateService, IHttpClientFactory httpClientFactory)
        {
            _certificateService = certificateService;
            _httpClientFactory = httpClientFactory;
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

        private static string ComputeChallengeHash(string name)
        {
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd:00:00:00");
            string challengeContent = $"{name}:{date}";

            byte[] bytes = Encoding.UTF8.GetBytes(challengeContent);
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
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
                        string challengeHash = ComputeChallengeHash(cn);

                        _challenges.Add($"HTTP http://{cn}/{challengeHash}.html");
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
                                        
                                        string challengeHash = ComputeChallengeHash(san.strValue);
                                        
                                        if (san.strValue.StartsWith("*."))
                                        {
                                            _challenges.Add($"DNS_TXT {san.strValue.Remove(0, 2)} {challengeHash}");
                                        }
                                        else
                                        {
                                            if(_challenges.Contains($"HTTP http://{san.strValue}/{challengeHash}.html"))
                                            {

                                            }
                                            else
                                            {
                                                _challenges.Add($"HTTP http://{san.strValue}/{challengeHash}.html");
                                            }
                                            
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

        async Task<(bool Success, string Message)> CheckChallenge(string type, string location, string content)
        {
            if (type == "HTTP")
            {
                try
                {
                    using (HttpClient client = _httpClientFactory.CreateClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        HttpResponseMessage response = await client.GetAsync(location);
                        if (!response.IsSuccessStatusCode)
                        {
                            return (false, $"HTTP challenge failed for {location}: server returned {(int)response.StatusCode} {response.ReasonPhrase}");
                        }
                        string filecontent = await response.Content.ReadAsStringAsync();
                        if (filecontent == content)
                        {
                            return (true, $"HTTP challenge succeeded for {location}");
                        }
                        else
                        {
                            return (false, $"HTTP challenge failed for {location}: content does not match expected hash");
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    return (false, $"HTTP challenge failed for {location}: request timed out");
                }
                catch (HttpRequestException ex)
                {
                    return (false, $"HTTP challenge failed for {location}: {ex.Message}");
                }
            }
            else if (type == "DNS_TXT")
            {
                try
                {
                    var lookup = new LookupClient();
                    var result = await lookup.QueryAsync(location, QueryType.TXT);
                    var txtRecords = result.Answers.TxtRecords().ToList();
                    if (txtRecords.Count == 0)
                    {
                        return (false, $"DNS_TXT challenge failed for {location}: no TXT records found");
                    }
                    foreach (var txt in txtRecords)
                    {
                        string txtValue = string.Join("", txt.Text);
                        if (txtValue == content)
                        {
                            return (true, $"DNS_TXT challenge succeeded for {location}");
                        }
                    }
                    return (false, $"DNS_TXT challenge failed for {location}: no TXT record matches expected hash");
                }
                catch (Exception ex)
                {
                    return (false, $"DNS_TXT challenge failed for {location}: {ex.Message}");
                }
            }
            return (false, $"Unknown challenge type: {type}");
        }

        public async Task<SubmitResponse> VerifyAllChallenges(string request)
        {
            var parseResult = ParseRequest(request);
            if (parseResult.Status != "Parsed")
            {
                return new SubmitResponse
                {
                    Status = "Error",
                    Message = parseResult.Message
                };
            }

            var results = new List<string>();
            bool allPassed = true;

            foreach (string challenge in _challenges)
            {
                string[] parts = challenge.Split(' ', 3);
                string type = parts[0];
                string location = parts[1];
                string content = parts.Length > 2 ? parts[2] : string.Empty;

                // For HTTP challenges the hash is part of the URL, content to check is the hash
                if (type == "HTTP")
                {
                    // URL format: http://host/hash — the hash is what should be served at that URL
                    string hash = location.Substring(location.LastIndexOf('/') + 1);
                    var (success, message) = await CheckChallenge(type, location, "HTTP");
                    if (!success) allPassed = false;
                    results.Add(message);
                }
                else if (type == "DNS_TXT")
                {
                    // Format: DNS_TXT <domain> <hash>
                    var (success, message) = await CheckChallenge(type, location, content);
                    if (!success) allPassed = false;
                    results.Add(message);
                }
            }

            return new SubmitResponse
            {
                Status = allPassed ? "Success" : "Failed",
                ParsedData = parseResult.ParsedData,
                ChallengeData = parseResult.ChallengeData,
                Output = string.Join(Environment.NewLine, results),
                Message = allPassed
                    ? "All challenges verified successfully"
                    : "One or more challenges failed verification"
            };
        }

        public SubmitResponse SubmitToCA(string base64Request, string caConfig, bool allChallengesPassed)
        {
            var submitResponse = new SubmitResponse();
            try
            {
                var certRequest = new CCertRequest();
                int disposition = certRequest.Submit(
                    CR_IN_BASE64 | CR_IN_ANY,
                    base64Request,
                    null,
                    caConfig);

                int requestId = certRequest.GetRequestId();

                if (disposition == CR_DISP_ISSUED)
                {
                    string base64Cert = certRequest.GetCertificate(CR_OUT_BASE64);
                    submitResponse.Status = "Issued" + Environment.NewLine;
                    submitResponse.Output = base64Cert + Environment.NewLine;
                    submitResponse.Message = $"Certificate issued successfully (RequestId: {requestId})";
                }
                else if (disposition == CR_DISP_PENDING)
                {
                    if (allChallengesPassed)
                    {
                        try
                        {
                            var certAdmin = new CCertAdmin();
                            certAdmin.ResubmitRequest(caConfig, requestId);

                            // Retrieve the certificate after approval
                            disposition = certRequest.RetrievePending(requestId, caConfig);
                            if (disposition == CR_DISP_ISSUED)
                            {
                                string base64Cert = certRequest.GetCertificate(CR_OUT_BASE64);
                                submitResponse.Status = "Issued" + Environment.NewLine;
                                submitResponse.Output = base64Cert + Environment.NewLine;
                                submitResponse.Message = $"Certificate approved and issued successfully (RequestId: {requestId})";
                            }
                            else
                            {
                                submitResponse.Status = "Pending" + Environment.NewLine;
                                submitResponse.Output = $"RequestId: {requestId}" + Environment.NewLine;
                                submitResponse.Message = $"Certificate request was approved but is still pending (RequestId: {requestId})";
                            }
                        }
                        catch (Exception ex)
                        {
                            submitResponse.Status = "Pending" + Environment.NewLine;
                            submitResponse.Output = $"RequestId: {requestId}" + Environment.NewLine;
                            submitResponse.Message = $"Certificate request is pending; auto-approve failed: {ex.Message} (RequestId: {requestId})";
                        }
                    }
                    else
                    {
                        submitResponse.Status = "Pending" + Environment.NewLine;
                        submitResponse.Output = $"RequestId: {requestId}" + Environment.NewLine;
                        submitResponse.Message = $"Certificate request is pending approval, not all challenges were verified (RequestId: {requestId})";
                    }
                }
                else
                {
                    string dispositionMessage = certRequest.GetDispositionMessage();
                    submitResponse.Status = "Denied" + Environment.NewLine;
                    submitResponse.Message = $"Certificate request was denied (RequestId: {requestId}): {dispositionMessage}";
                }
            }
            catch (Exception ex)
            {
                submitResponse.Status = "Error";
                submitResponse.Message = $"Failed to submit request to CA: {ex.Message}";
            }
            return submitResponse;
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
