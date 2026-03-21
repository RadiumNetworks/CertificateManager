## Welcome to the Certificate Management project

The project is created to provide a solution for the following pain points related to Certificate Services Management:</br>
-easier approval or denial of certificate requests so that the certificate template configuration can be more secure</br>
-implementation of automatic approval of custom certificate requests if they agree with company rules</br>
-easier revocation of certificates</br>
-better reporting of expiring certificates / open certificate requests / failed requests to see the need for troubleshooting</br>
-easier logging/exporting of information to allow point in time restore after a disaster recovery</br>

The architecture can be outlined as follows

![Header](./media/architecture.png)

The UI has different pages

The main page shows some guidance of what the user can do (role based access still on todo)
![Header](./media/blazor.png)

Here for filtering, exporting, editing owner information or revocation
![Header](./media/listcertificates.png)

Here for approval or to deny new requests
![Header](./media/certificaterequestview.png)

For submitting new requests
![Header](./media/newrequest.png)

The backend consists of an exit module where changes on the certificate authority
are written to a SQL database

The Admin interface is shown below
![Header](./media/admin.png)
![Header](./media/admin2.png)
![Header](./media/winui_listcertificates.png)
![Header](./media/signscript.png)
![Header](./media/importcertificatetemplates.png)
![Header](./media/templatestatistics.png)

The used database has the following tables
![Header](./media/database.png)

