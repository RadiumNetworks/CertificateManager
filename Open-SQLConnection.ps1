. .\Load-Config.ps1

$SQLConnectionString = "Data Source={0};Initial Catalog={1};Integrated Security=SSPI;" -f $Config.SQLInstance,$Config.Database
$SQLConnection = New-object System.Data.SqlClient.SqlConnection $SQLConnectionString
$SQLConnection.Open()