. .\Load-Config.ps1
. .\Load-SMO.ps1

$Server = New-Object Microsoft.SqlServer.Management.Smo.Server $Config.SQLInstance
$DB = New-Object Microsoft.SqlServer.Management.Smo.Database $Server, $Config.Database

$FileGroup = New-Object Microsoft.SqlServer.Management.Smo.FileGroup $DB, 'PRIMARY'
$DB.FileGroups.Add($FileGroup)
$DataFile = New-Object Microsoft.SqlServer.Management.Smo.DataFile $FileGroup, $Config.Database
$FileGroup.Files.Add($DataFile)
$DataFile.FileName = $Config.DatabaseFile
$DataFile.Size = $Config.DatabaseSize
$DataFile.GrowthType = "Percent"
$DataFile.Growth = 10.0
$DataFile.IsPrimaryFile = "TRUE"

$LogFile = New-Object Microsoft.SqlServer.Management.Smo.LogFile $DB, $Config.DatabaseLog
$DB.LogFiles.Add($LogFile)
$LogFile.Filename = $Config.DatabaseLogFile
$LogFile.Size = $Config.DatabaseLogSize
$DataFile.GrowthType = "Percent"
$DataFile.Growth = 10.0

$DB.Create()
