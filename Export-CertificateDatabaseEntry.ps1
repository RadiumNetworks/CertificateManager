function Export-CertificateDatabaseEntry
{
    param(
        $Table,
        $IndexColumn,
        $Filter,
        $SQLConnection,
        $Columns,
        $ColumnInformationType
    )

    #https://msdn.microsoft.com/en-us/library/windows/desktop/aa380253(v=vs.85).aspx
    #https://msdn.microsoft.com/en-us/library/windows/desktop/aa383268(v=vs.85).aspx
    #Get the Config String to access to local CA
    $CaConfig = New-Object -Com CertificateAuthority.Config.1
    #$CaConfig.GetConfig(0)

    #Create a ComObject for the ICertView Interface to access the Database
    #https://msdn.microsoft.com/en-us/library/windows/desktop/aa388193(v=vs.85).aspx
    $CaView = New-Object -Com CertificateAuthority.View.1
    $CaView.OpenConnection($Config.CAConfig)
    $CaView.SetTable($Table)

    #We need this reversed hashtable to create the final export object 
    $ReverseColumns = @{}
    $Columns.Keys | % {
        $ColumnName = $_
        $ReverseColumns.Add($Columns.$ColumnName.ColumnName,$ColumnName)
    }

    #Define the View on the database set to the amount of given columns and add the internal indices to an array
    $CaView.SetResultColumnCount($Columns.Keys.Count)
    #The Hashtable $Indices will have the same keys as the Input Hashtable $Columns
    $Indices = @{}
    $Columns.Keys | % {
        $SimpleColumnName = $_
        $ColumnName = $Columns.$_.ColumnName
   
        #GetColumnIndex(CVRC_COLUMN_SCHEMA,<ColumnName>)
        $Indices.Add($SimpleColumnName,$CAView.GetColumnIndex($ColumnInformationType, $ColumnName))
    }
    $Indices.Keys | % {
        $Index = $Indices.$_
        $CAView.SetResultColumn($Index)
    }

    $Filter | ForEach-Object {
        $SeekQualifier = $_.SeekQualifier
        $SortQualifier = $_.SortQualifier
        $IndexColumn = $_.IndexColumn
        $QueryValue = $_.QueryValue
        #Set Restrictions on <IndexColumn>,CVR_SEEK_EQ,CVR_SORT_NONE,<QueryValue>
        $CaView.SetRestriction($Indices.$IndexColumn,$SeekQualifier,$SortQualifier,$QueryValue)
    }

    #Open the defined view on the CA database
    $RowObj= $CAView.OpenView()
    #Get the pointer to the first row
    while($RowObj.Next() -ne -1)
    {
        $GUID = [System.Guid]::NewGuid().Guid
        #Open a view for a single column
        $ColObj = $RowObj.EnumCertViewColumn()

        #Iterate through the different colums and get the values
        #Write to HashTable $Row
        $Row=@{}
        for($i=0;$i -lt $Columns.Keys.Count;$i++)
        {
            $Column = $_
            [void]$ColObj.Next()
            $Key = $ReverseColumns[$ColObj.GetDisplayName()]
            $Row.Add($Key,$ColObj.GetValue($Columns[$ColObj.GetName()].Format))
        }
        if ($Row.BinaryCertificate)
        {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($Row.BinaryCertificate)
            $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($bytes)
            $Collection = New-Object -TypeName System.Security.Cryptography.X509Certificates.X509Certificate2Collection -ArgumentList $certificate
            for ($i=0;$i -lt $Collection.Count;$i++)
            {
                $EnhancedKeyUsages = $Collection[0].Extensions | Where-Object { $_.Oid.Value -eq $EnhancedKeyUsage }
            }
        }
        #Most certificate request from the database have an outer structure of a CMC request and a nested pkcs7 structure
        #The certificate request with its x509extensions for the subject alternate names are within the nested pkcs7 structure
        #However, within these structures we can only see the names and oid's for certain extensions
        #If the cmc parsing fails we try to parse it directly as a pkcs7 and following pkcs10 format
        if($Row.BinaryRequest)
        {
            $Row.RequestType
            switch($Row.RequestType)
            {
                263168 {
                    try 
                    {
                        $CMCRequest = New-Object -ComObject X509enrollment.CX509CertificateRequestCmc
                        $CMCRequest.InitializeDecode($Row.BinaryRequest,6)
                        $PKCS7Request = $CMCRequest.GetInnerRequest(0)
                        $SANExtension = $PKCS7Request.X509Extensions | ? { $_.objectid.value -eq $AlternativeNames}
                        "CMC"
                    } catch 
                    { 
                        "NO CMC" 
                    }
                }
                262912 {
                    try {
                        $PKCS7Request = New-Object -ComObject X509enrollment.CX509CertificateRequestPKCS7
                        $PKCS7Request.InitializeDecode($Row.BinaryRequest,0)
                        $SANExtension = $PKCS7Request.X509Extensions | ? { $_.objectid.value -eq $AlternativeNames}
                        "PKCS7"
                    } catch 
                    {
                        "NO PKCS7"
                    }
                }
                262400 {
                    try {
                        $PKCS10Request = New-Object -ComObject X509enrollment.CX509CertificateRequestPKCS10
                        $PKCS10Request.InitializeDecode($Row.BinaryRequest,0)
                        $SANExtension = $PKCS10Request.X509Extensions | ? { $_.objectid.value -eq $AlternativeNames}
                        "PKCS10"

                    } catch {
                        "NO PKCS10"
                    }
                }
            }
            
            #In the next we create a X509Extension object with the data to be able to parse it
            $SANList = $null
            if($SANExtension)
            {
                $RawData = [Convert]::FromBase64String($SANExtension.RawData(1))
                $NativeExtension = New-Object Security.Cryptography.X509Certificates.X509Extension $AlternativeNames, $RawData, $false
                $SANList = ($NativeExtension.Format(0) -split ",") | % { $_.Trim() }
            }
        }
        if ($Row.RequestID)
        {
            $Statement = "
            Update Entries set Base64Certificate='{1}',Base64Request='{2}',SerialNumber='{3}',RequestDisposition='{4}',RequesterName='{5}',
            CallerName='{6}',CertificateHash='{7}',CertificateTemplate='{8}',CertificateEffectiveDate='{9}',CertificateExpirationDate='{10}',
            IssuedEmailAddress='{11}',IssuedCommonName='{12}',IssuedCountryRegion='{13}',IssuedOrganization='{14}',IssuedOrganizationUnit='{15}',
            RequestType='{16}',PublicKeyLength='{17}',PublicKeyAlgorithm='{18}',RequestCountryRegion='{21}',RequestOrganization='{22}',
            RequestOrganizationUnit='{23}',RequestCommonName='{24}',RequestCity='{25}',RequestTitle='{26}',RequestEmailAddress='{27}',
            TemplateEnrollmentFlags='{28}',TemplateGeneralFlags='{29}',TemplatePrivateKeyFlags='{30}',PublicKeyAlgorithmParameters='{31}',
            RevocationDate='{32}',RevocationReason='{33}' where RequestID='{0}' and CAConfig='{19}'
            IF @@ROWCOUNT=0
            INSERT INTO Entries
            ( RequestID,Base64Certificate,Base64Request,SerialNumber,RequestDisposition,RequesterName,CallerName,
            CertificateHash,CertificateTemplate,CertificateEffectiveDate,CertificateExpirationDate,IssuedEmailAddress,
            IssuedCommonName,IssuedCountryRegion,IssuedOrganization,IssuedOrganizationUnit,RequestType,PublicKeyLength,
            PublicKeyAlgorithm,CAConfig,RequestCountryRegion,RequestOrganization,RequestOrganizationUnit,
            RequestCommonName,RequestCity,RequestTitle,RequestEmailAddress,TemplateEnrollmentFlags,TemplateGeneralFlags,
            TemplatePrivateKeyFlags,PublicKeyAlgorithmParameters,RevocationDate,RevocationReason )
            VALUES
              ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}',
              '{17}','{18}','{19}','{21}','{22}','{23}','{24}','{25}','{26}','{27}','{28}','{29}','{30}','{31}','{32}','{33}')
            " -f $Row.RequestID,$Row.BinaryCertificate,$Row.BinaryRequest,$Row.SerialNumber,$Row.RequestDisposition,
            $Row.RequesterName,$Row.CallerName,$Row.CertificateHash,$Row.CertificateTemplate,$Row.CertificateEffectiveDate,
            $Row.CertificateExpirationDate,$Row.IssuedEmailAddress,$Row.IssuedCommonName,$Row.IssuedCountryRegion,
            $Row.IssuedOrganization,$Row.IssuedOrganizationUnit,$Row.RequestType,$Row.PublicKeyLength,$Row.PublicKeyAlgorithm,$Config.CAConfig,$GUID,
            $Row.RequestCountryRegion,$Row.RequestOrganization,$Row.RequestOrganizationUnit,$Row.RequestCommonName,$Row.RequestCity,
            $Row.RequestTitle,$Row.RequestEmailAddress,$Row.TemplateEnrollmentFlags,$Row.TemplateGeneralFlags,$Row.TemplatePrivateKeyFlags,
            $Row.PublicKeyAlgorithmParameters,$Row.RevocationDate,$Row.RevocationReason
        }
        elseif ($Row.CRLRowID)
        {
            $Statement = "
            INSERT INTO CRLs
            ( CRLRowID,CRLNumber,CRLRawCRL,CRLThisUpdate,CRLNextUpdate,CRLNextPublish,CAConfig )
            VALUES
              ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')
            " -f $Row.CRLRowID,$Row.CRLNumber,$Row.CRLRawCRL,$Row.CRLThisUpdate,$Row.CRLNextUpdate,$Row.CRLNextPublish,$Config.CAConfig

            $CRL =  New-Object -ComObject x509enrollment.cx509certificaterevocationlist
            $CRL.InitializeDecode($Row.CRLRawCRL, "6");
            $CRLEntries = $CRL.X509CRLEntries

        }
        $SQLCommand = New-object System.Data.SqlClient.SqlCommand $Statement, $SQLConnection
        try {
            [void]$SQLCommand.ExecuteNonQuery()

            $SANList | % {
                $SAN=$_
                if($SAN)
                {
                    $Statement = "
                    INSERT INTO SANs (RequestID,CAConfig,SubjectAlternativeName) VALUES ('{0}','{1}','{2}')" -f $Row.RequestID,$Config.CAConfig,$SAN

                    $SQLCommand = New-object System.Data.SqlClient.SqlCommand $Statement, $SQLConnection
                    [void]$SQLCommand.ExecuteNonQuery()
                }

            }
            $EnhancedKeyUsages.EnhancedKeyUsages.FriendlyName | ForEach-Object {
                $EKU = $_
                if($EKU)
                {
                    $Statement = "
                    INSERT INTO EKUs (RequestID,CAConfig,Name) VALUES ('{0}','{1}','{2}')" -f $Row.RequestID,$Config.CAConfig,$EKU
                    $SQLCommand = New-object System.Data.SqlClient.SqlCommand $Statement, $SQLConnection
                    [void]$SQLCommand.ExecuteNonQuery()
                }

            }
            $CRLEntries | ForEach-Object {
                $CRLEntry = $_
                if($CRLEntry)
                {
                    $Statement = "INSERT INTO RevokedCerts (CRLRowID,CAConfig,SerialNumber,Reason,RevocationDate) VALUES ('{0}','{1}','{2}','{3}','{4}')" -f $Row.CRLRowID, $Config.CAConfig, $CRLEntry.SerialNumber(), $CRLEntry.RevocationReason, $CRLEntry.RevocationDate
                    $SQLCommand = New-object System.Data.SqlClient.SqlCommand $Statement, $SQLConnection
                    [void]$SQLCommand.ExecuteNonQuery()
                }

            }
        }
        catch {
            "Could not execute statement $statement"
        }

        
    }
}