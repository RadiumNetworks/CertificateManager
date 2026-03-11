using CERTCLILib;
using CERTENROLLLib;
using CERTEXITLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using SendToSQL.Models;

namespace SendToSQL
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("SendToSQL.Exit")]
    [Guid("78fdc18d-4d5d-46a4-8a06-c9b5e6ed99b7")]

    //https://learn.microsoft.com/en-us/windows/win32/api/certexit/nn-certexit-icertexit
    //https://learn.microsoft.com/en-us/windows/win32/api/certexit/nn-certexit-icertexit2
    public class Exit : CERTEXITLib.ICertExit2, CERTEXITLib.ICertExit
    {
        private string _sqlConfig = null;
        private string _debugFlag = null;
        private string _debugLog = null;
        private string _certificateFolder = null;
        private string _requestFolder = null;
        private string _caConfig = null;

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

        //https://learn.microsoft.com/en-us/windows/win32/api/certexit/nf-certexit-icertexit-notify
        internal enum ExitEvents : int
        {
            Invalid = 0x0,
            CertIssued = 0x1,
            CertPending = 0x2,
            CertDenied = 0x4,
            CertRevoked = 0x8,
            CertRetrievePending = 0x10,
            CRLIssued = 0x20,
            Shutdown = 0x40
        }
        //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
        internal enum PropertyType : int
        {
            PROPTYPE_LONG = 1,
            PROPTYPE_DATE = 2,
            PROPTYPE_BINARY = 3,
            PROPTYPE_STRING = 4,
            PROPTYPE_ANSI = 5
        }
        //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getrequestproperty
        internal enum RequestType : int
        {
            CR_IN_PKCS10 = 0x100,
            CR_IN_KEYGEN = 0x200,
            CR_IN_PKCS7 = 0x300
        }

        public Exit()
        {

        }

        public List<string> SubjectAlternativeNames { get; set; } = new List<string>();
        public List<string> EKUs { get; set; } = new List<string>();

        //https://learn.microsoft.com/en-us/windows/win32/api/certexit/nf-certexit-icertexit-getdescription
        public string GetDescription()
        {
            return "Exit Module to send data to SQL instance";
        }

        //https://learn.microsoft.com/en-us/windows/win32/api/certexit/nf-certexit-icertexit2-getmanagemodule
        public CCertManageExitModule GetManageModule()
        {
            return new ExitManage();
        }

        //https://learn.microsoft.com/en-us/windows/win32/api/certexit/nf-certexit-icertexit-initialize
        public int Initialize(string strConfig)
        {
            RegistryKey configRegistryKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\CertSvc\Configuration\" + strConfig + @"\ExitModules\SendToSQL");
            if (configRegistryKey != null)
            {
                _sqlConfig = (string)configRegistryKey.GetValue("SQLConfig");
                _debugFlag = (string)configRegistryKey.GetValue("DebugFlag");
                _debugLog = (string)configRegistryKey.GetValue("DebugLog");
                _certificateFolder = (string)configRegistryKey.GetValue("CertificateFolder");
                _requestFolder = (string)configRegistryKey.GetValue("CertificateFolder");
            }

            configRegistryKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\CertSvc\Configuration\" + strConfig);
            {
                _caConfig = (string)configRegistryKey.GetValue("CAServerName") + @"\" + (string)configRegistryKey.GetValue("CommonName");
            }

            // Subscribe to the Events we want to process
            return (Int32)(
                ExitEvents.CertIssued | ExitEvents.CertPending | ExitEvents.CertDenied | ExitEvents.CertRevoked | ExitEvents.CertRetrievePending
            );

        }

        private void Log(string debugFlag, string debugLog, string message)
        {
            if (debugFlag == "Debug")
            {
                try
                {
                    System.IO.File.AppendAllText(debugLog, message + Environment.NewLine);
                }
                catch
                {

                }
            }
        }

        private void SendToSQLDB(CAInfo caInfo, CACertificate certificate, CARequest request)
        {
            Log(_debugFlag, _debugLog, _sqlConfig);
            Log(_debugFlag, _debugLog, _caConfig);
            Log(_debugFlag, _debugLog, certificate.RequestId.ToString());
            Log(_debugFlag, _debugLog, Convert.ToBase64String(request.RawRequest, Base64FormattingOptions.None));
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
            try { certificateTemplate = certificate.CertificateTemplate.ToString(); }
            catch { }
            try { certificateHash = certificate.CertificateHash.ToString(); }
            catch { }
            try { publicKeyLength = certificate.PublicKeyLength.ToString(); }
            catch { }
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
                where RequestID='{requestid}' and CAConfig='{_caConfig}' 
                If @@ROWCOUNT=0 
                Insert into Entry (Base64Request, RequestId, 
                CAConfig, Base64Certificate, SerialNumber, RequestDisposition, RequesterName,
                RequestType, IssuedEmailAddress, IssuedCommonName, IssuedCountryRegion, IssuedOrganization, 
                IssuedOrganizationUnit, CallerName, CertificateHash, CertificateTemplate,
                CertificateExpirationDate, CertificateEffectiveDate,
                PublicKeyLength,PublicKeyAlgorithm, RequestCountryRegion, RequestOrganization, 
                RequestOrganizationUnit, RequestCommonName, RequestCity, RequestEmailAddress) 
                VALUES ('{base64Request}','{requestid}',
                '{_caConfig}','{base64Certificate}','{serialNumber}','{disposition}','{requesterName}',
                '{requestType}','{issuedEMailAddress}','{issuedCommonName}','{issuedCountryRegion}','{issuedOrganization}',
                '{issuedOrganizationUnit}','{callerName}','{certificateHash}','{certificateTemplate}',
                '{notAfter}','{notBefore}',
                '{publicKeyLength}','{publicKeyAlgorithm}','{requestCountryRegion}','{requestOrganization}',
                '{requestOrganizationUnit}','{requestCommonName}','{requestCity}','{requestEMailAddress}')";
                Log(_debugFlag, _debugLog, sql);

                using (SqlConnection connection = new SqlConnection(_sqlConfig))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    Log(_debugFlag, _debugLog, "Update or Import successful");
                }
            }
            catch (Exception e)
            {
                Log(_debugFlag, _debugLog, e.Message);
            }

            try
            {
                foreach (var eku in EKUs)
                {
                    String ekusql = $@"Update EKU set Name='{eku}' 
                    where RequestID='{requestid}' and CAConfig='{_caConfig}' and Name = '{eku}'
                    If @@ROWCOUNT=0 
                    Insert into EKU (Name, RequestId, CAConfig) 
                    VALUES ('{eku}','{requestid}','{_caConfig}')";

                    Log(_debugFlag, _debugLog, ekusql);

                    using (SqlConnection connection = new SqlConnection(_sqlConfig))
                    {
                        using (SqlCommand command = new SqlCommand(ekusql, connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                        Log(_debugFlag, _debugLog, "Update or Import successful");
                    }
                }
            }
            catch (Exception e)
            {
                Log(_debugFlag, _debugLog, e.Message);
            }

            try
            {
                foreach (var san in SubjectAlternativeNames)
                {
                    String sansql = $@"Update SAN set SubjectAlternativeName='{san}' 
                    where RequestID='{requestid}' and CAConfig='{_caConfig}' and SubjectAlternativeName='{san}'
                    If @@ROWCOUNT=0 
                    Insert into SAN (SubjectAlternativeName, RequestId, CAConfig) 
                    VALUES ('{san}','{requestid}','{_caConfig}')";
                    Log(_debugFlag, _debugLog, sansql);

                    using (SqlConnection connection = new SqlConnection(_sqlConfig))
                    {
                        using (SqlCommand command = new SqlCommand(sansql, connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                        Log(_debugFlag, _debugLog, "Update or Import successful");
                    }
                }
            }
            catch (Exception e)
            {
                Log(_debugFlag, _debugLog, e.Message);
            }

            
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
                Log(_debugFlag, _debugLog, e.Message);

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
            var cX509ExtensionAlternativeNames = new CX509ExtensionAlternativeNames();
            var cX509ExtensionBasicConstraints = new CX509ExtensionBasicConstraints();
            var cX509ExtensionTemplate = new CX509ExtensionTemplate();
            var cX509ExtensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsage();
            var cX509ExtensionKeyUsage = new CX509ExtensionKeyUsage();
            var cX509ExtensionMSApplicationPolicies = new CX509ExtensionMSApplicationPolicies();
            var cX509ExtensionSubjectKeyIdentifier = new CX509ExtensionSubjectKeyIdentifier();

            for (var i = 0; i < cX509CertificateRequestPkcs10.X509Extensions.Count; i++)
            {
                switch (cX509CertificateRequestPkcs10.X509Extensions[i].ObjectId.Value)
                {
                    case AlternativeNames:
                        try
                        {
                            Log(_debugFlag, _debugLog, "AlternativeNames");
                            string sAlternativeNames = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            cX509ExtensionAlternativeNames.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sAlternativeNames);
                            foreach (CAlternativeName san in cX509ExtensionAlternativeNames.AlternativeNames)
                            {
                                if(san.Type == AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME)
                                {
                                    SubjectAlternativeNames.Add("DNS Name=" + san.strValue);
                                }else if (san.Type == AlternativeNameType.XCN_CERT_ALT_NAME_DIRECTORY_NAME)
                                {
                                    SubjectAlternativeNames.Add("Directory Name=" + san.strValue);
                                }else if (san.Type == AlternativeNameType.XCN_CERT_ALT_NAME_USER_PRINCIPLE_NAME)
                                {
                                    SubjectAlternativeNames.Add("UPN=" + san.strValue);
                                }
                                else 
                                {
                                    SubjectAlternativeNames.Add(san.Type + "=" + san.strValue);
                                }

                                Log(_debugFlag, _debugLog, " " + san.Type);
                                Log(_debugFlag, _debugLog, "  " + san.strValue);
                            }
                        }
                        catch
                        {

                        }


                        break;
                    case KeyUsage:
                        try
                        {
                            Log(_debugFlag, _debugLog, "KeyUsage:");
                            string sKeyUsage = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            cX509ExtensionKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sKeyUsage);
                            Log(_debugFlag, _debugLog, " " + cX509ExtensionKeyUsage.KeyUsage.ToString());
                        }
                        catch
                        {

                        }

                        break;

                    case EnhancedKeyUsage:
                        try
                        {
                            Log(_debugFlag, _debugLog, "EnhancedKeyUsage:");
                            string sEnhancedKeyUsage = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            cX509ExtensionEnhancedKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sEnhancedKeyUsage);
                            foreach (CObjectId objectid in cX509ExtensionEnhancedKeyUsage.EnhancedKeyUsage)
                            {
                                if (objectid.Value == "1.3.6.1.5.5.7.3.1")
                                {
                                    EKUs.Add("Server Authentication");
                                } else if (objectid.Value == "1.3.6.1.5.5.7.3.2")
                                {
                                    EKUs.Add("Client Authentication");
                                } else 
                                {
                                    EKUs.Add(objectid.Name.ToString());
                                }
                                
                                Log(_debugFlag, _debugLog, " " + objectid.Name);
                                Log(_debugFlag, _debugLog, "  " + objectid.Value);
                            }
                        }
                        catch
                        {

                        }

                        break;

                    case MSApplicationPolicies:
                        try
                        {
                            Log(_debugFlag, _debugLog, "MSApplicationPolicies:");
                            string sMSApplicationPolicies = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            cX509ExtensionMSApplicationPolicies.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sMSApplicationPolicies);
                            foreach (CCertificatePolicy certificatepolicy in cX509ExtensionMSApplicationPolicies.Policies)
                            {
                                Log(_debugFlag, _debugLog, " " + certificatepolicy.ObjectId.Name);
                                Log(_debugFlag, _debugLog, "  " + certificatepolicy.ObjectId.Value);
                            }
                        }
                        catch
                        {

                        }

                        break;

                    case Template:
                        try
                        {
                            Log(_debugFlag, _debugLog, "Template:");
                            string sTemplate = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            cX509ExtensionTemplate.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sTemplate);
                            Log(_debugFlag, _debugLog, " " + cX509ExtensionTemplate.TemplateOid.Value);
                            Log(_debugFlag, _debugLog, "  " + cX509ExtensionTemplate.MajorVersion);
                            Log(_debugFlag, _debugLog, "  " + cX509ExtensionTemplate.MinorVersion);

                        }
                        catch
                        {

                        }
                        break;
                    case SubjectKeyIdentifier:
                        try
                        {
                            Log(_debugFlag, _debugLog, "SubjectKeyIdentifier");
                            string sSubjectKeyIdentifier = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            cX509ExtensionSubjectKeyIdentifier.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sSubjectKeyIdentifier);
                            CObjectId oid = cX509ExtensionSubjectKeyIdentifier.ObjectId;
                            Log(_debugFlag, _debugLog, " " + oid.Value);
                        }
                        catch
                        {

                        }
                        break;
                    default:
                        try
                        {
                            Log(_debugFlag, _debugLog, "Default:");
                            Log(_debugFlag, _debugLog, cX509CertificateRequestPkcs10.X509Extensions[i].ObjectId.Value);
                            string ssdef = (cX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);
                            Log(_debugFlag, _debugLog, " " + ssdef);
                        }
                        catch
                        {

                        }

                        break;

                }
            }
        }

        public void Notify(int exitEvent, int context)
        {
            //https://learn.microsoft.com/en-us/windows/win32/api/certexit/nf-certexit-icertexit-notify
            switch (exitEvent)
            {
                //case (int)ExitEvents.CertIssued:
                //case (int)ExitEvents.CertPending:
                //case (int)ExitEvents.CertRevoked:
                //case (int)ExitEvents.CertRetrievePending:
                //case (int)ExitEvents.CertDenied:
                default:
                    var certConfig = new CCertConfig();
                    var certGetConfig = new CCertGetConfig();
                    var certServer = new CCertServerExit();
                    var caInfo = new CAInfo();
                    var certificateInfo = new CACertificate();
                    var requestInfo = new CARequest();
                    
                    //otherwise read CAServerName / CommonName from registry
                    

                    //https://docs.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-setcontext
                    //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
                    certServer.SetContext(0);

                    if (_debugFlag != null && _debugLog != null)
                    {
                        Log(_debugFlag, _debugLog, Environment.NewLine + DateTime.Now);
                        Log(_debugFlag, _debugLog, Environment.NewLine + "Seen eventtype " + exitEvent);
                        Log(_debugFlag, _debugLog, Environment.NewLine + "Logging CA Infos");
                    }
                    foreach (PropertyInfo prop in typeof(CAInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.PropertyType == typeof(string))
                        {
                            try
                            {
                                prop.SetValue(caInfo, (string)GetProperty(ref certServer, prop.Name, "certificate", "string"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                        {
                            try
                            {
                                prop.SetValue(caInfo, (int)GetProperty(ref certServer, prop.Name, "certificate", "int"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(DateTime))
                        {
                            try
                            {
                                prop.SetValue(caInfo, (DateTime)GetProperty(ref certServer, prop.Name, "certificate", "date"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(byte[]))
                        {
                            try
                            {
                                prop.SetValue(caInfo, (byte[])GetProperty(ref certServer, prop.Name, "certificate", "bytearr"));
                            }
                            catch
                            {

                            }
                        }
                        if (_debugFlag != null && _debugLog != null && prop.GetValue(caInfo) != null)
                        {
                            try
                            {
                                if (prop.GetValue(caInfo).ToString() == "System.Byte[]")
                                {
                                    Log(_debugFlag, _debugLog, prop.Name + " = " + Convert.ToBase64String((byte[])prop.GetValue(caInfo), Base64FormattingOptions.None));
                                }
                                else
                                {
                                    Log(_debugFlag, _debugLog, prop.Name + " = " + prop.GetValue(caInfo).ToString());
                                }

                            }
                            catch
                            {

                            }
                        }
                    }

                    certServer.SetContext(context);

                    if (_debugFlag != null && _debugLog != null)
                    {
                        Log(_debugFlag, _debugLog, Environment.NewLine + "Logging Certificate Infos");
                    }
                    foreach (PropertyInfo prop in typeof(CACertificate).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.PropertyType == typeof(string))
                        {
                            try
                            {
                                prop.SetValue(certificateInfo, (string)GetProperty(ref certServer, prop.Name, "certificate", "string"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                        {
                            try
                            {
                                prop.SetValue(certificateInfo, (int)GetProperty(ref certServer, prop.Name, "certificate", "int"));

                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(DateTime))
                        {
                            try
                            {
                                prop.SetValue(certificateInfo, (DateTime)GetProperty(ref certServer, prop.Name, "certificate", "date"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(byte[]))
                        {
                            try
                            {
                                prop.SetValue(certificateInfo, (byte[])GetProperty(ref certServer, prop.Name, "certificate", "bytearr"));
                            }
                            catch
                            {

                            }
                        }
                        if (_debugFlag != null && _debugLog != null && prop.GetValue(certificateInfo) != null)
                        {
                            try
                            {
                                if (prop.GetValue(certificateInfo).ToString() == "System.Byte[]")
                                {
                                    Log(_debugFlag, _debugLog, prop.Name + " = " + Convert.ToBase64String((byte[])prop.GetValue(certificateInfo), Base64FormattingOptions.None));
                                }
                                else
                                {
                                    Log(_debugFlag, _debugLog, prop.Name + " = " + prop.GetValue(certificateInfo).ToString());
                                }
                            }
                            catch
                            {

                            }
                        }

                    }

                    if (_debugFlag != null && _debugLog != null)
                    {
                        Log(_debugFlag, _debugLog, Environment.NewLine + "Logging Request Infos");
                    }
                    foreach (PropertyInfo prop in typeof(CARequest).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.PropertyType == typeof(string))
                        {
                            try
                            {
                                prop.SetValue(requestInfo, (string)GetProperty(ref certServer, prop.Name, "request", "string"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                        {
                            try
                            {
                                prop.SetValue(requestInfo, (int)GetProperty(ref certServer, prop.Name, "request", "int"));

                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(DateTime))
                        {
                            try
                            {
                                prop.SetValue(requestInfo, (DateTime)GetProperty(ref certServer, prop.Name, "request", "date"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(byte[]))
                        {
                            try
                            {
                                prop.SetValue(requestInfo, (byte[])GetProperty(ref certServer, prop.Name, "request", "bytearr"));
                            }
                            catch
                            {

                            }
                        }
                        if (_debugFlag != null && _debugLog != null && prop.GetValue(requestInfo) != null)
                        {
                            try
                            {
                                if (prop.GetValue(requestInfo).ToString() == "System.Byte[]")
                                {
                                    Log(_debugFlag, _debugLog, prop.Name + " = " + Convert.ToBase64String((byte[])prop.GetValue(requestInfo), Base64FormattingOptions.None));
                                }
                                else
                                {
                                    Log(_debugFlag, _debugLog, prop.Name + " = " + prop.GetValue(requestInfo).ToString());
                                }
                            }
                            catch
                            {

                            }
                        }
                    }
                    string outputFileName = null;
                    if (_certificateFolder != null)
                    {
                        try
                        {
                            outputFileName = _certificateFolder + certificateInfo.RequestId.ToString() + ".cer";
                            System.IO.File.WriteAllText(outputFileName, Convert.ToBase64String(certificateInfo.RawCertificate, Base64FormattingOptions.None));
                        }
                        catch
                        {

                        }

                    }
                    if (_requestFolder != null)
                    {
                        try
                        {
                            outputFileName = _requestFolder + certificateInfo.RequestId.ToString() + ".req";
                            System.IO.File.WriteAllText(outputFileName, Convert.ToBase64String(requestInfo.RawRequest, Base64FormattingOptions.None));
                        }
                        catch
                        {

                        }

                    }

                    switch (requestInfo.RequestType)
                    {
                        case 263168:

                            if (_debugFlag != null && _debugLog != null)
                            {
                                var cX509CertificateRequestCMC = new CERTENROLLLib.CX509CertificateRequestCmc();
                                try
                                {
                                    string request = Convert.ToBase64String(requestInfo.RawRequest, Base64FormattingOptions.None);
                                    cX509CertificateRequestCMC.InitializeDecode(
                                        request,
                                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);
                                    var cX509CertificateRequestPkcs10 = (IX509CertificateRequestPkcs10)cX509CertificateRequestCMC.GetInnerRequest(0);

                                    ParseRequestExtension(cX509CertificateRequestPkcs10);

                                    Log(_debugFlag, _debugLog, "Cmc successfully parsed" + Environment.NewLine);
                                }
                                catch
                                {
                                    Log(_debugFlag, _debugLog, "Error reading Cmc" + Environment.NewLine);
                                }
                            }
                            break;
                        case 262912:
                            if (_debugFlag != null && _debugLog != null)
                            {
                                var cX509CertificateRequestPkcs7 = new CERTENROLLLib.CX509CertificateRequestPkcs7();
                                try
                                {
                                    string request = Convert.ToBase64String(requestInfo.RawRequest, Base64FormattingOptions.None);
                                    cX509CertificateRequestPkcs7.InitializeDecode(
                                        request,
                                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);
                                    
                                    var cX509CertificateRequestPkcs10 = (IX509CertificateRequestPkcs10)cX509CertificateRequestPkcs7.GetInnerRequest(0);

                                    ParseRequestExtension(cX509CertificateRequestPkcs10);

                                    Log(_debugFlag, _debugLog, "PKCS7 successfully parsed");
                                }
                                catch
                                {
                                    Log(_debugFlag, _debugLog, "Error reading PKCS7");
                                }
                            }
                            break;
                        case 262400:
                            if (_debugFlag != null && _debugLog != null)
                            {
                                var cX509CertificateRequestPkcs10 = new CERTENROLLLib.CX509CertificateRequestPkcs10();
                                try
                                {
                                    string request = Convert.ToBase64String(requestInfo.RawRequest, Base64FormattingOptions.None);
                                    cX509CertificateRequestPkcs10.InitializeDecode(
                                        request,
                                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);

                                    Log(_debugFlag, _debugLog, "PKCS10 successfully parsed");

                                    ParseRequestExtension(cX509CertificateRequestPkcs10);

                                }
                                catch
                                {
                                    Log(_debugFlag, _debugLog, "Error reading PKCS10");
                                }
                            }
                            break;
                    }
                    if (_debugFlag != null && _debugLog != null)
                    {
                        Log(_debugFlag, _debugLog, Environment.NewLine);
                    }
                    SendToSQLDB(caInfo, certificateInfo, requestInfo);

                    
                    break;
            }
            EKUs = new List<string>();
            SubjectAlternativeNames = new List<string>();
        }
    }
}