## Welcome to the Certificate Management project

The UI has different pages

Here for filtering, exporting, editing owner information or revocation
![Header](./certificateview.png)

Here for approval or to deny new requests
![Header](./certificaterequestview.png)
Important

To run the tool with the certificate revocation / request approval function on a new system
1. the RSAT Remote Server Administration Tool for ADCS must be installed
2. the certadm.dll from c:\Windows\system32 must be converted using tlbimp certadm.dll to certadminlib.dll
3. the certadminlib.dll file must be placed in the project folder
4. Additionally, the wwwroot directory must be restored (e.g. from a new asp.net razor project)
