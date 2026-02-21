using CERTEXITLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SendToSQL
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("SendToSQL.ExitManage")]
    [Guid("d6f2e993-980b-4a02-bea5-f7a34804c8ef")]

    //https://learn.microsoft.com/en-us/windows/win32/api/certmod/nn-certmod-icertmanagemodule
    public class ExitManage : CERTEXITLib.ICertManageModule, CERTEXITLib.CCertManageExitModule
    {
        public ExitManage()
        {

        }

        //https://learn.microsoft.com/en-us/windows/win32/api/certmod/nf-certmod-icertmanagemodule-configure
        public void Configure(string strConfig, string strStorageLocation, int Flags)
        {

        }

        //https://learn.microsoft.com/en-us/windows/win32/api/certmod/nf-certmod-icertmanagemodule-getproperty
        public object GetProperty(string strConfig, string strStorageLocation, string strPropertyName, int Flags)
        {
            string strMsg = null;
            switch (strPropertyName)
            {
                case "Name":
                    strMsg = "SendToSQL";
                    break;

                case "Description":
                    strMsg = "Exit module to send certificate data to SQL server";
                    break;

                case "Copyright":
                    strMsg = "Copyright © 2026 René Mau";
                    break;

                case "File Version":
                    strMsg = "1.0.0.3";
                    break;

                case "Product Version":
                    strMsg = "1.0.0.3";
                    break;

                default:
                    strMsg = "Unknown Property: " + strPropertyName;
                    break;
            }

            return (object)strMsg;
        }

        //https://learn.microsoft.com/en-us/windows/win32/api/certmod/nf-certmod-icertmanagemodule-setproperty
        public void SetProperty(string strConfig, string strStorageLocation, string strPropertyName, int Flags, ref object pvarProperty)
        {
            //Not needed here
        }
    }
}

