. .\Load-Config.ps1
. .\Define-Variables.ps1
. .\Define-CASchema.ps1
. .\Export-CertificateDatabaseEntry.ps1
. .\Open-SQLConnection.ps1

$Filter = @(
    @{IndexColumn="CRLRowId";QueryValue=0;SeekQualifier=$CVR_SEEK_GE;SortQualifier=$CVR_SORT_NONE}
)

Export-CertificateDatabaseEntry `
    -Table $CVRC_TABLE_CRL `
    -Filter $Filter `
    -SQLConnection $SQLConnection `
    -ColumnInformationType $CVRC_COLUMN_SCHEMA `
    -Columns $CRLColumns
