using CERTCLILib;
using CERTEXITLib;
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
using System.Xml.Serialization;
using CERTENROLLLib;

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
        private string SQLConfig = null;
        private string DebugFlag = null;
        private string DebugLog = null;
        private string CertificateFolder = null;
        private string RequestFolder = null;

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

        //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
        public class CAInfo
        {
            public byte[] RawCACertificate { get; set; } = null;
            public byte[] RawCRL { get; set; } = null;
            public string SanitizedCAName { get; set; } = null;
        }

            //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
            public class Certificate
        {
            public int RequestId { get; set; } = 0;
            public int PublicKeyLength { get; set; } = 0;
            public string PublicKeyAlgorithm { get; set; } = null;
            public string SerialNumber { get; set; } = null;
            public string CertificateTemplate { get; set; } = null;
            public DateTime NotBefore { get; set; }
            public DateTime NotAfter { get; set; }
            public byte[] RawCertificate { get; set; } = null;
            public byte[] RawPublicKey { get; set; } = null;
            public byte[] RawPublicKeyAlgorithmParameters { get; set; } = null;
            public int RequestType { get; set; }
        }

        //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getrequestproperty
        public class Request
        {
            public int RequestId { get; set; } = 0;
            public int RequestType { get; set; } = 0;
            public int StatusCode { get; set; } = 0;
            public int Disposition { get; set; } = 0;
            public string DispositionMessage { get; set; } = null;
            public DateTime ResolvedWhen { get; set; }
            public DateTime SubmittedWhen { get; set; }
            public byte[] RawCertificate { get; set; } = null;
            public string RequesterName { get; set; } = null;
            public string RequestAttributes { get; set; } = null;
            public string CommonName { get; set; } = null;
            public string Organization { get; set; } = null;
            public string OrgUnit { get; set; } = null;
            public string EMail { get; set; } = null;
            public string Locality { get; set; } = null;
            public string State { get; set; } = null;
            public byte[] RawRequest { get; set; } = null;
        }

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
                SQLConfig = (string)configRegistryKey.GetValue("SQLConfig");
                DebugFlag = (string)configRegistryKey.GetValue("DebugFlag");
                DebugLog = (string)configRegistryKey.GetValue("DebugLog");
                CertificateFolder = (string)configRegistryKey.GetValue("CertificateFolder");
                RequestFolder = (string)configRegistryKey.GetValue("CertificateFolder");

            }
            
            // Subscribe to the Events we want to process
            return (Int32)(
                ExitEvents.CertIssued | ExitEvents.CertPending | ExitEvents.CertDenied | ExitEvents.CertRevoked | ExitEvents.CertRetrievePending
            );


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
            catch
            {
                if(DebugFlag != null && DebugLog != null)
                {
                    System.IO.File.AppendAllText(DebugLog, "Error reading value for " + name + Environment.NewLine);
                }
                
                switch (returntype)
                {
                    case "date":
                        return new DateTime();
                    case "int":
                        return 0;
                    case "string":
                        return "";
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

        public void Notify(int ExitEvent, int Context)
        {
            //https://learn.microsoft.com/en-us/windows/win32/api/certexit/nf-certexit-icertexit-notify
            switch (ExitEvent)
            {
                //case (int)ExitEvents.CertIssued:
                //case (int)ExitEvents.CertPending:
                //case (int)ExitEvents.CertRevoked:
                //case (int)ExitEvents.CertRetrievePending:
                //case (int)ExitEvents.CertDenied:
                default:
                    var CertServer = new CCertServerExit();
                    var CAInfo = new CAInfo();
                    var CertificateInfo = new Certificate();
                    var RequestInfo = new Request();


                    //https://docs.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-setcontext
                    //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
                    CertServer.SetContext(0);
                    if (DebugFlag != null && DebugLog != null)
                    {
                        System.IO.File.AppendAllText(DebugLog, Environment.NewLine + "Seen eventtype " + ExitEvent + Environment.NewLine);
                        System.IO.File.AppendAllText(DebugLog, "Logging CA Infos" + Environment.NewLine);
                    }
                    foreach (PropertyInfo prop in typeof(CAInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.PropertyType == typeof(string))
                        {
                            try
                            {
                                prop.SetValue(CAInfo, (string)GetProperty(ref CertServer, prop.Name, "certificate", "string"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(int))
                        {
                            try
                            {
                                prop.SetValue(CAInfo, (int)GetProperty(ref CertServer, prop.Name, "certificate", "int"));

                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(DateTime))
                        {
                            try
                            {
                                prop.SetValue(CAInfo, (DateTime)GetProperty(ref CertServer, prop.Name, "certificate", "date"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(byte[]))
                        {
                            try
                            {
                                prop.SetValue(CAInfo, (byte[])GetProperty(ref CertServer, prop.Name, "certificate", "bytearr"));
                            }
                            catch
                            {

                            }
                        }
                        if (DebugFlag != null && DebugLog != null)
                        {
                            try
                            {
                                System.IO.File.AppendAllText(DebugLog, prop.Name + " = " + prop.GetValue(CAInfo).ToString() + Environment.NewLine);
                            }
                            catch
                            {
                                System.IO.File.AppendAllText(DebugLog, prop.Name + " = error " + Environment.NewLine);
                            }
                        }

                    }

                    CertServer.SetContext(Context);

                    if (DebugFlag != null && DebugLog != null)
                    {
                        System.IO.File.AppendAllText(DebugLog, Environment.NewLine + "Logging Certificate Infos" + Environment.NewLine);
                    }
                    foreach (PropertyInfo prop in typeof(Certificate).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.PropertyType == typeof(string))
                        {
                            try
                            {
                                prop.SetValue(CertificateInfo, (string)GetProperty(ref CertServer, prop.Name, "certificate", "string"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(int))
                        {
                            try
                            {
                                prop.SetValue(CertificateInfo, (int)GetProperty(ref CertServer, prop.Name, "certificate", "int"));
                                
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(DateTime))
                        {
                            try
                            {
                                prop.SetValue(CertificateInfo, (DateTime)GetProperty(ref CertServer, prop.Name, "certificate", "date"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(byte[]))
                        {
                            try
                            {
                                prop.SetValue(CertificateInfo, (byte[])GetProperty(ref CertServer, prop.Name, "certificate", "bytearr"));
                            }
                            catch
                            {

                            }
                        }
                        if (DebugFlag != null && DebugLog != null)
                        {
                            try
                            {

                                System.IO.File.AppendAllText(DebugLog, prop.Name + " = " + prop.GetValue(CertificateInfo).ToString() + Environment.NewLine);

                            }
                            catch
                            {

                                System.IO.File.AppendAllText(DebugLog, prop.Name + " = error " + Environment.NewLine);
                            }
                        }
                        
                    }

                    if (DebugFlag != null && DebugLog != null)
                    {
                        System.IO.File.AppendAllText(DebugLog, Environment.NewLine + "Logging Request Infos" + Environment.NewLine);
                    }
                    foreach (PropertyInfo prop in typeof(Request).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.PropertyType == typeof(string))
                        {
                            try
                            {
                                prop.SetValue(RequestInfo, (string)GetProperty(ref CertServer, prop.Name, "request", "string"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(int))
                        {
                            try
                            {
                                prop.SetValue(RequestInfo, (int)GetProperty(ref CertServer, prop.Name, "request", "int"));

                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(DateTime))
                        {
                            try
                            {
                                prop.SetValue(RequestInfo, (DateTime)GetProperty(ref CertServer, prop.Name, "request", "date"));
                            }
                            catch
                            {

                            }
                        }
                        else if (prop.PropertyType == typeof(byte[]))
                        {
                            try
                            {
                                prop.SetValue(RequestInfo, (byte[])GetProperty(ref CertServer, prop.Name, "request", "bytearr"));
                            }
                            catch
                            {

                            }
                        }
                        if (DebugFlag != null && DebugLog != null)
                        {
                            try
                            {
                                System.IO.File.AppendAllText(DebugLog, prop.Name + " = " + prop.GetValue(RequestInfo).ToString() + Environment.NewLine);
                            }
                            catch
                            {
                                System.IO.File.AppendAllText(DebugLog, prop.Name + " = error " + Environment.NewLine);
                            }
                        }
                    }
                    string OutputFileName = null;
                    if (CertificateFolder != null)
                    {
                        try
                        {
                            OutputFileName = CertificateFolder + CertificateInfo.RequestId.ToString() + ".cer";
                            System.IO.File.WriteAllText(OutputFileName, Convert.ToBase64String(CertificateInfo.RawCertificate, Base64FormattingOptions.None));
                        }
                        catch
                        { 
                        
                        }
                        
                    }
                    if (RequestFolder != null)
                    {
                        try
                        {
                            OutputFileName = CertificateFolder + CertificateInfo.RequestId.ToString() + ".req";
                            System.IO.File.WriteAllText(OutputFileName, Convert.ToBase64String(RequestInfo.RawRequest, Base64FormattingOptions.None));
                        }
                        catch
                        {

                        }
                        
                    }

                    switch (RequestInfo.RequestType )
                    {
                        case 263168:

                            if (DebugFlag != null && DebugLog != null)
                            {
                                var CX509CertificateRequestCMC = new CERTENROLLLib.CX509CertificateRequestCmc();
                                try
                                {
                                    string request = Convert.ToBase64String(RequestInfo.RawRequest, Base64FormattingOptions.None);
                                    CX509CertificateRequestCMC.InitializeDecode(
                                        request, 
                                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);
                                    var CX509CertificateRequestPkcs10 = (IX509CertificateRequestPkcs10)CX509CertificateRequestCMC.GetInnerRequest(0);

                                    var CX509ExtensionAlternativeNames = new CX509ExtensionAlternativeNames();
                                    var CX509ExtensionBasicConstraints = new CX509ExtensionBasicConstraints();
                                    var CX509ExtensionTemplate = new CX509ExtensionTemplate();
                                    var CX509ExtensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsage();
                                    var CX509ExtensionKeyUsage = new CX509ExtensionKeyUsage();
                                    var CX509ExtensionMSApplicationPolicies = new CX509ExtensionMSApplicationPolicies();
                                    var CX509ExtensionSubjectKeyIdentifier = new CX509ExtensionSubjectKeyIdentifier();

                                    for (var i = 0; i < CX509CertificateRequestPkcs10.X509Extensions.Count; i++)
                                    {
                                        switch(CX509CertificateRequestPkcs10.X509Extensions[i].ObjectId.Value)
                                        {
                                            case AlternativeNames:
                                                try
                                                {
                                                    System.IO.File.AppendAllText(DebugLog, "AlternativeNames" + Environment.NewLine);
                                                    string sAlternativeNames = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                                                    CX509ExtensionAlternativeNames.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sAlternativeNames);
                                                    foreach (CAlternativeName san in CX509ExtensionAlternativeNames.AlternativeNames)
                                                    {
                                                        System.IO.File.AppendAllText(DebugLog, " " + san.Type + Environment.NewLine);
                                                        System.IO.File.AppendAllText(DebugLog, "  " + san.strValue + Environment.NewLine);
                                                    }
                                                }
                                                catch
                                                {

                                                }
                                                

                                                break;
                                            case KeyUsage:
                                                try
                                                {
                                                    System.IO.File.AppendAllText(DebugLog, "KeyUsage:" + Environment.NewLine);
                                                    string sKeyUsage = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                                                    CX509ExtensionKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64 , sKeyUsage);
                                                    System.IO.File.AppendAllText(DebugLog, " " + CX509ExtensionKeyUsage.KeyUsage.ToString() + Environment.NewLine); 
                                                }
                                                catch
                                                {

                                                }

                                                break;

                                            case EnhancedKeyUsage:
                                                try
                                                {
                                                    System.IO.File.AppendAllText(DebugLog, "EnhancedKeyUsage:" + Environment.NewLine);
                                                    string sEnhancedKeyUsage = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                                                    CX509ExtensionEnhancedKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sEnhancedKeyUsage);
                                                    foreach (CObjectId objectid in CX509ExtensionEnhancedKeyUsage.EnhancedKeyUsage)
                                                    {
                                                        System.IO.File.AppendAllText(DebugLog, " " + objectid.Name + Environment.NewLine);
                                                        System.IO.File.AppendAllText(DebugLog, "  " + objectid.Value + Environment.NewLine);
                                                    }
                                                }
                                                catch
                                                {

                                                }

                                                break;

                                            case MSApplicationPolicies:
                                                try
                                                {
                                                    System.IO.File.AppendAllText(DebugLog, "MSApplicationPolicies:" + Environment.NewLine);
                                                    string sMSApplicationPolicies = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                                                    CX509ExtensionMSApplicationPolicies.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sMSApplicationPolicies);
                                                    foreach (CCertificatePolicy certificatepolicy in CX509ExtensionMSApplicationPolicies.Policies)
                                                    {
                                                        System.IO.File.AppendAllText(DebugLog, " " + certificatepolicy.ObjectId.Name + Environment.NewLine);
                                                        System.IO.File.AppendAllText(DebugLog, "  " + certificatepolicy.ObjectId.Value + Environment.NewLine);
                                                    }
                                                }
                                                catch
                                                {

                                                }

                                                break;

                                            case Template:
                                                try
                                                {
                                                    System.IO.File.AppendAllText(DebugLog, "Template:" + Environment.NewLine);
                                                    string sTemplate = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                                                    CX509ExtensionTemplate.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sTemplate);
                                                    System.IO.File.AppendAllText(DebugLog, " " + CX509ExtensionTemplate.TemplateOid.Value + Environment.NewLine);
                                                    System.IO.File.AppendAllText(DebugLog, "  " + CX509ExtensionTemplate.MajorVersion + Environment.NewLine);
                                                    System.IO.File.AppendAllText(DebugLog, "  " + CX509ExtensionTemplate.MinorVersion + Environment.NewLine);

                                                }
                                                catch
                                                {

                                                }
                                                break;
                                            case SubjectKeyIdentifier:
                                                try
                                                {
                                                    System.IO.File.AppendAllText(DebugLog, "SubjectKeyIdentifier" + Environment.NewLine);
                                                    string sSubjectKeyIdentifier = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                                                    CX509ExtensionSubjectKeyIdentifier.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sSubjectKeyIdentifier);
                                                    System.IO.File.AppendAllText(DebugLog, " " + CX509ExtensionSubjectKeyIdentifier.SubjectKeyIdentifier[EncodingType.XCN_CRYPT_STRING_BASE64] + Environment.NewLine);
                                                }
                                                catch
                                                {

                                                }
                                                break;
                                            default:
                                                try
                                                {
                                                    System.IO.File.AppendAllText(DebugLog, "Default:" + Environment.NewLine);
                                                    System.IO.File.AppendAllText(DebugLog, CX509CertificateRequestPkcs10.X509Extensions[i].ObjectId.Value + Environment.NewLine);
                                                    string ssdef = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);
                                                    System.IO.File.AppendAllText(DebugLog, " " + ssdef + Environment.NewLine);
                                                }
                                                catch
                                                {

                                                }

                                                break;

                                        }
                                    }  


                                    System.IO.File.AppendAllText(DebugLog, CX509CertificateRequestPkcs10.RawData + Environment.NewLine);
                                    System.IO.File.AppendAllText(DebugLog, "Cmc successfully parsed" + Environment.NewLine);
                                }
                                catch
                                {
                                    System.IO.File.AppendAllText(DebugLog, "Error reading Cmc" + Environment.NewLine);
                                }
                            }
                            break;
                        case 262912:
                            if (DebugFlag != null && DebugLog != null)
                            {
                                var CX509CertificateRequestPkcs7 = new CERTENROLLLib.CX509CertificateRequestPkcs7();
                                try
                                {
                                    string request = Convert.ToBase64String(RequestInfo.RawRequest, Base64FormattingOptions.None);
                                    CX509CertificateRequestPkcs7.InitializeDecode(
                                        Convert.ToBase64String(RequestInfo.RawRequest, Base64FormattingOptions.None), 
                                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64HEADER);
                                    System.IO.File.AppendAllText(DebugLog, "PKCS7 successfully parsed" + Environment.NewLine);
                                }
                                catch
                                {
                                    System.IO.File.AppendAllText(DebugLog, "Error reading PKCS7" + Environment.NewLine);
                                }
                            }
                            break;
                        case 262400:
                            if (DebugFlag != null && DebugLog != null)
                            {
                                var CX509CertificateRequestPkcs10 = new CERTENROLLLib.CX509CertificateRequestPkcs10();
                                try
                                {
                                    string request = Convert.ToBase64String(RequestInfo.RawRequest, Base64FormattingOptions.None);
                                    CX509CertificateRequestPkcs10.InitializeDecode(
                                        request,
                                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64HEADER);
                                    System.IO.File.AppendAllText(DebugLog, "PKCS10 successfully parsed" + Environment.NewLine);
                                }
                                catch
                                {
                                    System.IO.File.AppendAllText(DebugLog, "Error reading PKCS10" + Environment.NewLine);
                                }
                            }
                            break;
                    }
                    if (DebugFlag != null && DebugLog != null)
                    {
                        System.IO.File.AppendAllText(DebugLog, Environment.NewLine);
                    }
                    break;
            }
        }
    }
}