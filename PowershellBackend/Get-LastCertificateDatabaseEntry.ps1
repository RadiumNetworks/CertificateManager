. .\Load-Config.ps1
. .\Define-Variables.ps1
. .\Define-CASchema.ps1

function Get-LastCertificateDatabaseEntry
{
    param(
        $Table,
        $IndexColumn,
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

    $CaView.SetRestriction($Indices.RequestID,$CVR_SEEK_GE,$CVR_SORT_DESCEND,0)

    #Open the defined view on the CA database
    $RowObj= $CAView.OpenView()

    #Get the pointer to the first row
    [void]$RowObj.Next()

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
    $Row
}
