. .\Load-Config.ps1
. .\Define-Variables.ps1
. .\Define-CASchema.ps1
. .\Export-CertificateDatabaseEntry.ps1
. .\Open-SQLConnection.ps1
. .\Get-LastCertificateDatabaseEntry.ps1

$LastRow = Get-LastCertificateDatabaseEntry `
    -Table $CVRC_TABLE_REQCERT `
    -SQLConnection $SQLConnection `
    -ColumnInformationType $CVRC_COLUMN_SCHEMA `
    -Columns $CertColumns `

$stepsize = 10
for ($i=0;$i -lt $LastRow.RequestID; $i+=$stepsize)
{
    $Filter = @(
        @{IndexColumn="RequestID";QueryValue=$i;SeekQualifier=$CVR_SEEK_GE;SortQualifier=$CVR_SORT_NONE},
        @{IndexColumn="RequestID";QueryValue=($i+$stepsize);SeekQualifier=$CVR_SEEK_LT;SortQualifier=$CVR_SORT_NONE}
    )

    Export-CertificateDatabaseEntry `
    -Table $CVRC_TABLE_REQCERT `
    -Filter $Filter `
    -SQLConnection $SQLConnection `
    -ColumnInformationType $CVRC_COLUMN_SCHEMA `
    -Columns $CertColumns `

}