function Get-ADObject
{
    param(
        $LDAPConnection,
        $NamingContext,
        $WorkingFolder = "C:\Temp",
        $PageSize = 1000,
        $LDAPFilter = "(objectclass=*)",
        $AttributeList,
        $SearchScope = [System.DirectoryServices.Protocols.SearchScope]::Subtree
    )


    $PageSizeControl = New-Object System.DirectoryServices.Protocols.PageResultRequestControl $PageSize

    $SearchRequest = New-Object System.DirectoryServices.Protocols.SearchRequest $NamingContext,$LDAPFilter,$SearchScope,$null
    $Null = $SearchRequest.Controls.Add($PageSizeControl)
    Foreach ($Attribute in $AttributeList) 
    {
        $Null = $SearchRequest.Attributes.Add($Attribute)
    }

    $SearchResponse = @()

    while ($true)
    {
	    $PagedSearchResponse = $LDAPConnection.SendRequest($SearchRequest)
	    $PageResponse = $PagedSearchResponse.Controls | Where-Object { $_.Type -eq $PageSizeControl.Type }
	    $SearchResponse += $PagedSearchResponse.Entries
	    if ($PageResponse.Cookie.Length -eq 0) {break}
	    $PageSizeControl.Cookie = $PageResponse.Cookie
    }

    $SearchResponse 
}