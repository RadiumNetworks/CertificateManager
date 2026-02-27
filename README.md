## Welcome to the Certificate Management project

The architecture can be outlined as follows

![Header](./architecture.png)

The UI has different pages

The main page shows some guidance of what the user can do (role based access still on todo)
![Header](./startuppage.png)

Here for filtering, exporting, editing owner information or revocation
![Header](./certificateview.png)

Here for approval or to deny new requests
![Header](./certificaterequestview.png)

For submitting new requests
![Header](./newrequest.png)

The backend consists of an exit module where changes on the certificate authority
are written to a SQL database

The used database has the following tables
![Header](./database.png)
