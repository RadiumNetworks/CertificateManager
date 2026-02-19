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
        internal enum PropertyType : int
        {
            PROPTYPE_LONG = 1,
            PROPTYPE_DATE = 2,
            PROPTYPE_BINARY = 3,
            PROPTYPE_STRING = 4,
            PROPTYPE_ANSI = 5
        }
        internal enum RequestType : int
        {
            CR_IN_PKCS10 = 0x100,
            CR_IN_KEYGEN = 0x200,
            CR_IN_PKCS7 = 0x300
        }

        public Exit()
        {

        }
        public class Certificate
        {
            public int RequestId { get; set; } = 0;
            public string RequesterName { get; set; } = null;
            public int PublicKeyLength { get; set; } = 0;
            public string PublicKeyAlgorithm { get; set; } = null;
            public string Issuer { get; set; } = null;
            public string SerialNumber { get; set; } = null;
            public string CertificateTemplate { get; set; } = null;
            public DateTime NotBefore { get; set; }
            public DateTime NotAfter { get; set; }
            public byte[] RawCertificate { get; set; } = null;
            public int RequestType { get; set; }
        }

        public class Request
        {
            public int RequestId { get; set; } = 0;
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
                System.IO.File.AppendAllText(@"c:\temp\log.txt", "Error reading value for " + name + Environment.NewLine);
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
                default:
                    var CertServer = new CCertServerExit();
                    var CertificateInfo = new Certificate();
                    var RequestInfo = new Request();

                    //https://docs.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-setcontext
                    CertServer.SetContext(0);
                    CertificateInfo.Issuer = (string)GetProperty(ref CertServer, "SanitizedCAName", "certificate", "string");

                    CertServer.SetContext(Context);

                    System.IO.File.AppendAllText(@"c:\temp\log.txt", Environment.NewLine + "Logging Certificate Infos" + Environment.NewLine);
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
                        try
                        {
                            System.IO.File.AppendAllText(@"c:\temp\log.txt", prop.Name + " = " + prop.GetValue(CertificateInfo).ToString() + Environment.NewLine);
                        }
                        catch
                        {
                            System.IO.File.AppendAllText(@"c:\temp\log.txt", prop.Name + " = error " + Environment.NewLine);
                        }
                        
                    }

                    System.IO.File.AppendAllText(@"c:\temp\log.txt", Environment.NewLine + "Logging Request Infos" + Environment.NewLine);
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
                        try
                        {
                            System.IO.File.AppendAllText(@"c:\temp\log.txt", prop.Name + " = " + prop.GetValue(RequestInfo).ToString() + Environment.NewLine);
                        }
                        catch
                        {
                            System.IO.File.AppendAllText(@"c:\temp\log.txt", prop.Name + " = error " + Environment.NewLine);
                        }
                    }

                    var OutputFileName = @"c:\temp\_" + CertificateInfo.RequestId.ToString() + ".cer";
                    System.IO.File.WriteAllText(OutputFileName, Convert.ToBase64String(CertificateInfo.RawCertificate, Base64FormattingOptions.None));

                    OutputFileName = @"c:\temp\_" + CertificateInfo.RequestId.ToString() + ".req";
                    System.IO.File.WriteAllText(OutputFileName, Convert.ToBase64String(RequestInfo.RawRequest, Base64FormattingOptions.None));

                    System.IO.File.AppendAllText(@"c:\temp\log.txt", Environment.NewLine);
                    break;
            }
        }
    }
}