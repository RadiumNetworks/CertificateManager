. .\Load-Config.ps1
. .\Load-SMO.ps1

$Server = New-Object Microsoft.SqlServer.Management.Smo.Server $Config.SQLInstance
$DB = New-Object Microsoft.SqlServer.Management.Smo.Database $Server, $Config.Database
$DB.Refresh()

$ENTRYTABLE = New-Object -TypeName Microsoft.SqlServer.Management.SMO.Table -argumentlist $DB, "Entries", "dbo"
$Table = @(
@{Name="RequestID";Type=[Microsoft.SqlServer.Management.Smo.DataType]::Int;Nullable=$False},
@{Name="CAConfig";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="Base64Certificate";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarCharMax;Nullable=$True},
@{Name="Base64Request";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarCharMax;Nullable=$True},
@{Name="SerialNumber";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="RequestDisposition";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="RequesterName";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="RequestType";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="RequestAttributes";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="IssuedEmailAddress";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="IssuedCommonName";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="IssuedCountryRegion";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="IssuedOrganization";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="IssuedOrganizationUnit";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="CallerName";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="CertificateHash";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="CertificateTemplate";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="CertificateEffectiveDate";Type=[Microsoft.SqlServer.Management.Smo.DataType]::DateTime;Nullable=$True},
@{Name="CertificateExpirationDate";Type=[Microsoft.SqlServer.Management.Smo.DataType]::DateTime;Nullable=$True},
@{Name="PublicKeyLength";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="PublicKeyAlgorithm";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="RequestCountryRegion";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="RequestOrganization";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="RequestOrganizationUnit";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="RequestCommonName";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="RequestCity";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="RequestTitle";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="RequestEmailAddress";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="TemplateEnrollmentFlags";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="TemplateGeneralFlags";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="TemplatePrivateKeyFlags";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="PublicKeyAlgorithmParameters";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="RevocationDate";Type=[Microsoft.SqlServer.Management.Smo.DataType]::DateTime;Nullable=$True},
@{Name="RevocationReason";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="Owner";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="Notes";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True})
for($i=0;$i-lt $Table.Length;$i++)
{
    $Column=$Table[$i]
    $SMOColumn =  New-Object -TypeName Microsoft.SqlServer.Management.SMO.Column -argumentlist $ENTRYTABLE,$Column.Name, $Column.Type  
    $SMOColumn.Nullable = $Column.Nullable
    $ENTRYTABLE.Columns.Add($SMOColumn)  
}
$ENTRYTABLE.Create()



$Index = New-Object Microsoft.SqlServer.Management.Smo.Index -ArgumentList $ENTRYTABLE, "EntryTablePK"
$IndexColumn1 = New-Object Microsoft.SqlServer.Management.Smo.IndexedColumn -ArgumentList $Index, "RequestID", $true
$Index.IndexedColumns.Add($IndexColumn1)
$IndexColumn2 = New-Object Microsoft.SqlServer.Management.Smo.IndexedColumn -ArgumentList $Index, "CAConfig", $true
$Index.IndexedColumns.Add($IndexColumn2)
$Index.IndexKeyType = [Microsoft.SqlServer.Management.Smo.IndexKeyType]::DriUniqueKey
$Index.IsClustered = $false  
$Index.FillFactor = 50  
$Index.Create()


$SANTABLE = New-Object -TypeName Microsoft.SqlServer.Management.SMO.Table -argumentlist $DB, "SANs", "dbo"
$Table = @(
@{Name="RequestID";Type=[Microsoft.SqlServer.Management.Smo.DataType]::Int;Nullable=$False},
@{Name="CAConfig";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="SubjectAlternativeName";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True})
for($i=0;$i-lt $Table.Length;$i++)
{
    $Column=$Table[$i]
    $SMOColumn =  New-Object -TypeName Microsoft.SqlServer.Management.SMO.Column -argumentlist $SANTABLE,$Column.Name, $Column.Type  
    $SMOColumn.Nullable = $Column.Nullable
    $SANTABLE.Columns.Add($SMOColumn)  
}
$SANTABLE.Create()

$ForeignKey = New-Object Microsoft.SqlServer.Management.Smo.ForeignKey $SANTABLE, "FK_SAN"
$ForeignKeyColumn1 = New-Object Microsoft.SqlServer.Management.Smo.ForeignKeyColumn $ForeignKey, "RequestID", "RequestID"
$ForeignKey.Columns.Add($ForeignKeyColumn1)
$ForeignKeyColumn2 = New-Object Microsoft.SqlServer.Management.Smo.ForeignKeyColumn $ForeignKey, "CAConfig", "CAConfig"
$ForeignKey.Columns.Add($ForeignKeyColumn2)
$ForeignKey.ReferencedTable = "Entries"
$ForeignKey.ReferencedTableSchema = "dbo"
$ForeignKey.Create()



$EKUTABLE = New-Object -TypeName Microsoft.SqlServer.Management.SMO.Table -argumentlist $DB, "EKUs", "dbo"
$Table = @(
@{Name="RequestID";Type=[Microsoft.SqlServer.Management.Smo.DataType]::Int;Nullable=$False},
@{Name="CAConfig";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="Name";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True})
for($i=0;$i-lt $Table.Length;$i++)
{
    $Column=$Table[$i]
    $SMOColumn =  New-Object -TypeName Microsoft.SqlServer.Management.SMO.Column -argumentlist $EKUTABLE,$Column.Name, $Column.Type  
    $SMOColumn.Nullable = $Column.Nullable
    $EKUTABLE.Columns.Add($SMOColumn)  
}
$EKUTABLE.Create()

$ForeignKey = New-Object Microsoft.SqlServer.Management.Smo.ForeignKey $EKUTABLE, "FK_EKU"
$ForeignKeyColumn1 = New-Object Microsoft.SqlServer.Management.Smo.ForeignKeyColumn $ForeignKey, "RequestID", "RequestID"
$ForeignKey.Columns.Add($ForeignKeyColumn1)
$ForeignKeyColumn2 = New-Object Microsoft.SqlServer.Management.Smo.ForeignKeyColumn $ForeignKey, "CAConfig", "CAConfig"
$ForeignKey.Columns.Add($ForeignKeyColumn2)
$ForeignKey.ReferencedTable = "Entries"
$ForeignKey.ReferencedTableSchema = "dbo"
$ForeignKey.Create()





$CRLTABLE = New-Object -TypeName Microsoft.SqlServer.Management.SMO.Table -argumentlist $DB, "CRLs", "dbo"
$Table = @(
@{Name="CRLRowId";Type=[Microsoft.SqlServer.Management.Smo.DataType]::Int;Nullable=$False},
@{Name="CAConfig";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="CRLNumber";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarCharMax;Nullable=$True},
@{Name="CRLRawCRL";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarCharMax;Nullable=$True},
@{Name="CRLThisUpdate";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="CRLNextUpdate";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True},
@{Name="CRLNextPublish";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$True})
for($i=0;$i-lt $Table.Length;$i++)
{
    $Column=$Table[$i]
    $SMOColumn =  New-Object -TypeName Microsoft.SqlServer.Management.SMO.Column -argumentlist $CRLTABLE, $Column.Name, $Column.Type  
    $SMOColumn.Nullable = $Column.Nullable
    $CRLTABLE.Columns.Add($SMOColumn)  
}
$CRLTABLE.Create()


$Index = New-Object Microsoft.SqlServer.Management.Smo.Index -ArgumentList $CRLTABLE, "CRLTablePK"
$IndexColumn1 = New-Object Microsoft.SqlServer.Management.Smo.IndexedColumn -ArgumentList $Index, "CRLRowId", $true
$Index.IndexedColumns.Add($IndexColumn1)
$IndexColumn2 = New-Object Microsoft.SqlServer.Management.Smo.IndexedColumn -ArgumentList $Index, "CAConfig", $true
$Index.IndexedColumns.Add($IndexColumn2)
$Index.IndexKeyType = [Microsoft.SqlServer.Management.Smo.IndexKeyType]::DriUniqueKey
$Index.IsClustered = $false  
$Index.FillFactor = 50  
$Index.Create()


$RevocationTABLE = New-Object -TypeName Microsoft.SqlServer.Management.SMO.Table -argumentlist $DB, "RevokedCerts", "dbo"
$Table = @(
@{Name="CRLRowId";Type=[Microsoft.SqlServer.Management.Smo.DataType]::Int;Nullable=$False},
@{Name="CAConfig";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="SerialNumber";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True},
@{Name="Reason";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True}
@{Name="RevocationDate";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(128);Nullable=$True})
for($i=0;$i-lt $Table.Length;$i++)
{
    $Column=$Table[$i]
    $SMOColumn =  New-Object -TypeName Microsoft.SqlServer.Management.SMO.Column -argumentlist $RevocationTABLE, $Column.Name, $Column.Type  
    $SMOColumn.Nullable = $Column.Nullable
    $RevocationTABLE.Columns.Add($SMOColumn)  
}
$RevocationTABLE.Create()

$ForeignKey = New-Object Microsoft.SqlServer.Management.Smo.ForeignKey $RevocationTABLE, "FK_CRLRowId_Revocation"
$ForeignKeyColumn1 = New-Object Microsoft.SqlServer.Management.Smo.ForeignKeyColumn $ForeignKey, "CRLRowId", "CRLRowId"
$ForeignKey.Columns.Add($ForeignKeyColumn1)
$ForeignKeyColumn2 = New-Object Microsoft.SqlServer.Management.Smo.ForeignKeyColumn $ForeignKey, "CAConfig", "CAConfig"
$ForeignKey.Columns.Add($ForeignKeyColumn2)
$ForeignKey.ReferencedTable = "CRLs"
$ForeignKey.ReferencedTableSchema = "dbo"
$ForeignKey.Create()


$TemplateTABLE = New-Object -TypeName Microsoft.SqlServer.Management.SMO.Table -argumentlist $DB, "Templates", "dbo"
$Table = @(
@{Name="GUID";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="cn";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="distinguishedName";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="flags";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKICertificateApplicationPolicy";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKICertificateNameFlag";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKICertificatePolicy";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="msPKICertTemplateOID";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="msPKIEnrollmentFlag";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKIMinimalKeySize";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKIPrivateKeyFlag";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKIRAApplicationPolicies";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="msPKIRAPolicies";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKIRASignature";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKISupersedeTemplates";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKITemplateMinorRevision";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKITemplateSchemaVersion";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKICriticalExtensions";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIDefaultCSPs";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="pKIDefaultKeySpec";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIExpirationPeriod";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIExtendedKeyUsage";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIKeyUsage";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIMaxIssuingDepth";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIOverlapPeriod";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="ntSecurityDescriptor";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(3072);Nullable=$False})
for($i=0;$i-lt $Table.Length;$i++)
{
    $Column=$Table[$i]
    $SMOColumn =  New-Object -TypeName Microsoft.SqlServer.Management.SMO.Column -argumentlist $TemplateTABLE, $Column.Name, $Column.Type  
    $SMOColumn.Nullable = $Column.Nullable
    $TemplateTABLE.Columns.Add($SMOColumn)  
}
$TemplateTABLE.Create()


$Index = New-Object Microsoft.SqlServer.Management.Smo.Index -ArgumentList $TemplateTABLE, "TemplateTablePK"
$IndexColumn = New-Object Microsoft.SqlServer.Management.Smo.IndexedColumn -ArgumentList $Index, "GUID", $true
$Index.IndexedColumns.Add($IndexColumn)
$Index.IndexKeyType = [Microsoft.SqlServer.Management.Smo.IndexKeyType]::DriUniqueKey
$Index.IsClustered = $false  
$Index.FillFactor = 50  
$Index.Create()


$TemplateArchiveTABLE = New-Object -TypeName Microsoft.SqlServer.Management.SMO.Table -argumentlist $DB, "TemplatesArchive", "dbo"
$Table = @(
@{Name="Archived";Type=[Microsoft.SqlServer.Management.Smo.DataType]::DateTime;Nullable=$False},
@{Name="GUID";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="cn";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="distinguishedName";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="flags";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKICertificateApplicationPolicy";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKICertificateNameFlag";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKICertificatePolicy";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="msPKICertTemplateOID";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="msPKIEnrollmentFlag";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKIMinimalKeySize";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKIPrivateKeyFlag";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKIRAApplicationPolicies";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="msPKIRAPolicies";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKIRASignature";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKISupersedeTemplates";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKITemplateMinorRevision";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="msPKITemplateSchemaVersion";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKICriticalExtensions";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIDefaultCSPs";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(256);Nullable=$False},
@{Name="pKIDefaultKeySpec";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIExpirationPeriod";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIExtendedKeyUsage";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIKeyUsage";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIMaxIssuingDepth";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="pKIOverlapPeriod";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(64);Nullable=$False},
@{Name="ntSecurityDescriptor";Type=[Microsoft.SqlServer.Management.Smo.DataType]::NVarChar(3072);Nullable=$False})
for($i=0;$i-lt $Table.Length;$i++)
{
    $Column=$Table[$i]
    $SMOColumn =  New-Object -TypeName Microsoft.SqlServer.Management.SMO.Column -argumentlist $TemplateArchiveTABLE, $Column.Name, $Column.Type  
    $SMOColumn.Nullable = $Column.Nullable
    $TemplateArchiveTABLE.Columns.Add($SMOColumn)  
}
$TemplateArchiveTABLE.Create()
