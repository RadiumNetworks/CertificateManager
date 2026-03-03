using CERTCLILib;
using CERTENROLLLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Runtime.InteropServices;
using WebGUICertManager.Data;
using WebGUICertManager.Models;

namespace WebGUICertManager.Pages.Request
{
    
    public class NewModel : PageModel
    {
        private readonly AppDbContext context;

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


        public string RequestOption { get; set; }
        public string CurrentRequestOption { get; set; }
        public string RequestData { get; set; }
        public string CurrentRequestData { get; set; }
        public string action { get; set; }
        public string submitbtnactive = "false";
        public List<SelectListItem> RequestOptions { get; set; }
        

        public NewModel(AppDbContext context)
        {
            this.context = context;
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

        public void ParseRequestExtension(IX509CertificateRequestPkcs10 CX509CertificateRequestPkcs10)
        {
            var CX509ExtensionAlternativeNames = new CX509ExtensionAlternativeNames();
            var CX509ExtensionBasicConstraints = new CX509ExtensionBasicConstraints();
            var CX509ExtensionTemplate = new CX509ExtensionTemplate();
            var CX509ExtensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsage();
            var CX509ExtensionKeyUsage = new CX509ExtensionKeyUsage();
            var CX509ExtensionMSApplicationPolicies = new CX509ExtensionMSApplicationPolicies();
            var CX509ExtensionSubjectKeyIdentifier = new CX509ExtensionSubjectKeyIdentifier();
            CurrentRequestData += "Subject:" + Environment.NewLine;
            CurrentRequestData += " " + CX509CertificateRequestPkcs10.Subject.Name + Environment.NewLine;
            
            for (var i = 0; i < CX509CertificateRequestPkcs10.X509Extensions.Count; i++)
            {
                switch (CX509CertificateRequestPkcs10.X509Extensions[i].ObjectId.Value)
                {
                    case AlternativeNames:
                        try
                        {
                            string sAlternativeNames = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            CX509ExtensionAlternativeNames.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sAlternativeNames);
                            CurrentRequestData += "SubjectAlternateNames:" + Environment.NewLine;
                            foreach (CAlternativeName san in CX509ExtensionAlternativeNames.AlternativeNames)
                            {
                                CurrentRequestData += " " + san.Type + "=" + san.strValue + Environment.NewLine;
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
                            string sKeyUsage = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            CX509ExtensionKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sKeyUsage);

                            CurrentRequestData += " " + CX509ExtensionKeyUsage.KeyUsage.ToString() + Environment.NewLine;
                        }
                        catch
                        {

                        }

                        break;

                    case EnhancedKeyUsage:
                        try
                        {
                            CurrentRequestData += "EnhancedKeyUsages:" + Environment.NewLine;
                            string sEnhancedKeyUsage = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            CX509ExtensionEnhancedKeyUsage.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sEnhancedKeyUsage);
                            foreach (CObjectId objectid in CX509ExtensionEnhancedKeyUsage.EnhancedKeyUsage)
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

                            string sMSApplicationPolicies = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            CX509ExtensionMSApplicationPolicies.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sMSApplicationPolicies);
                            foreach (CCertificatePolicy certificatepolicy in CX509ExtensionMSApplicationPolicies.Policies)
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
                            string sTemplate = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            CX509ExtensionTemplate.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sTemplate);

                            CurrentRequestData += " " + CX509ExtensionTemplate.TemplateOid.Value + Environment.NewLine;

                        }
                        catch
                        {

                        }
                        break;
                    case SubjectKeyIdentifier:
                        try
                        {

                            string sSubjectKeyIdentifier = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                            CX509ExtensionSubjectKeyIdentifier.InitializeDecode(EncodingType.XCN_CRYPT_STRING_BASE64, sSubjectKeyIdentifier);
                            CObjectId oid = CX509ExtensionSubjectKeyIdentifier.ObjectId;

                        }
                        catch
                        {

                        }
                        break;
                    default:
                        try
                        {

                            string ssdef = (CX509CertificateRequestPkcs10.X509Extensions[i].RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64]);

                        }
                        catch
                        {

                        }

                        break;

                }
            }
        }



        public async Task OnGetAsync(string requestoption, string currentrequestoption, string requestdata, string currentrequestdata, string action)
        {
            
            RequestData = requestdata;
            CurrentRequestData = currentrequestdata;
            RequestOption = requestoption;
            CurrentRequestOption = currentrequestoption;
            
            RequestOptions = new List<SelectListItem>();
            RequestOptions.Add(new SelectListItem { Value = "1", Text = "INF" });
            RequestOptions.Add(new SelectListItem { Value = "2", Text = "REQ" });

            switch (RequestOption)
            {
                case "1":
                    {
                        break;
                    }
                case "2":
                    {
                        var CX509CertificateRequestCMC = new CERTENROLLLib.CX509CertificateRequestCmc();
                        try
                        {
                            CX509CertificateRequestCMC.InitializeDecode(
                                requestdata,
                                CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);
                            var CX509CertificateRequestPkcs10 = (IX509CertificateRequestPkcs10)CX509CertificateRequestCMC.GetInnerRequest(0);
                            submitbtnactive = "true";
                            ParseRequestExtension(CX509CertificateRequestPkcs10);
                            

                        }
                        catch
                        {
                            CurrentRequestData = null;
                        }

                    }
                    break;
            }
        }
    }
}
