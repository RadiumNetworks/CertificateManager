Um das exit modul nutzen zu können werden 2 dlls benötigt die auf dem client unter c:\Windows\system32 gefunden werden können.

certxds.dll 

certcli.dll

certenroll.dll

Um die Funktionen im Programcode einzubetten müssen die runtime metadaten über tlbimp in einem definierten Format extrahiert werden.

https://learn.microsoft.com/en-us/dotnet/framework/tools/tlbimp-exe-type-library-importer

*tlbimp certxds.dll*

*tlbimp certcli.dll*

*tlbimp certenroll.dll*

Die Konvertierung erzeugt jedoch Fehler da die benötigten Funktionen keine out Parameter haben

Laut Doku https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
sollte es so sein

*HRESULT GetCertificateProperty(*

  *[in]  const BSTR strPropertyName,*
  
  *[in]  LONG       PropertyType,*
  
  *[out] VARIANT    *pvarPropertyValue*
  
);


jedoch fehlt pvarPropertyValue in der erzeugten Datei

Um das zu korrigieren muss die Dateiinformation angepasst werden. Hierfür wird die Datei mit ildasm
https://learn.microsoft.com/de-de/dotnet/framework/tools/ildasm-exe-il-disassembler umgewandelt, angepasst und mit ilasm https://learn.microsoft.com/de-de/dotnet/framework/tools/ilasm-exe-il-assembler wieder kompiliert werden. 

*"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe" CERTCLILib.dll /out:CERTCLILIB.il*

*"C:\Windows\Microsoft.NET\Framework\v4.0.30319\ilasm.exe" /DLL CERTCLILIB.il /res:CERTCLILIB.res /out=CERTCLILIB.dll*

In der IL Datei müssen die Funktionen GetRequestProperty und GetCertificateProperty wie folgt korrigiert werden

*GetRequestProperty([in] string  marshal( bstr) strPropertyName,[in] int32 PropertyType,*

*[out] native int pvarPropertyValue) runtime managed internalcall*


*GetCertificateProperty([in] string  marshal( bstr) strPropertyName,[in] int32 PropertyType,*

*[out] native int pvarPropertyValue) runtime managed internalcall*


Ausführlicher ist es hier "noch" als archivierter Artikel geschrieben. https://learn.microsoft.com/en-us/archive/blogs/alejacma/how-to-modify-an-interop-assembly-to-change-the-return-type-of-a-method-vb-net



**Registry Values for the Exit Module**

To configure the Exit module on the certificate authority to current Settings are

HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CertSvc\Configuration\<CASanitizedName>\ExitModules\SendToSQL
SQLConfig <ConfigurationString>
DebugFlag <Value>
DebugLog <Log Path e.g. c:\temp\debug.log>
CertificateFolder <Folder to place issued certificates e.g. c:\temp\>
RequestFolder <Folder to place requests e.g. c:\temp\>

