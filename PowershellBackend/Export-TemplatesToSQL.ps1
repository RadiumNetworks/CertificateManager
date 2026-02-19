. .\Load-Config.ps1
. .\New-ADConnection.ps1
. .\Get-ADObject.ps1
. .\Open-SQLConnection.ps1

function Export-TemplatesToSQL
{
    param(
        $SQLConnection,
        $ADConnectionInfo
    )
    $Templates = Get-ADObject `
        -LDAPConnection $ADConnectionInfo.LDAPConnection `
        -NamingContext $ADConnectionInfo.configurationNC `
        -WorkingFolder "C:\Temp" `
        -PageSize $ADConnectionInfo.PageSize `
        -LDAPFilter "(&(objectclass=pKICertificateTemplate))" `
        -AttributeList "cn","distinguishedName","flags","msPKI-Certificate-Application-Policy","msPKI-Certificate-Name-Flag", `
        "msPKI-Certificate-Policy","msPKI-Cert-Template-OID","msPKI-Enrollment-Flag","msPKI-Minimal-Key-Size", `
        "msPKI-Private-Key-Flag","msPKI-RA-Application-Policies","msPKI-RA-Policies","msPKI-RA-Signature","msPKI-Supersede-Templates", `
        "msPKI-Template-Minor-Revision","msPKI-Template-Schema-Version","objectGUID","pKICriticalExtensions", `
        "pKIDefaultCSPs","pKIDefaultKeySpec","pKIExpirationPeriod","pKIExtendedKeyUsage","pKIKeyUsage", `
        "pKIMaxIssuingDepth","pKIOverlapPeriod","nTSecurityDescriptor"
    $Templates
    for($i=0;$i -lt $Templates.Count;$i++)
    {
        $Template = New-Object PSObject
        $Template | Add-Member -MemberType NoteProperty -Name cn -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name distinguishedName -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name flags -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKICertificateApplicationPolicy -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKICertificateNameFlag -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKICertificatePolicy -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKICertTemplateOID -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKIEnrollmentFlag -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKIMinimalKeySize -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKIPrivateKeyFlag -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKIRAApplicationPolicies -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKIRAPolicies -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKIRASignature -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKISupersedeTemplates -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKITemplateMinorRevision -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name msPKITemplateSchemaVersion -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name objectGUID -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name pKICriticalExtensions -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name pKIDefaultCSPs -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name pKIDefaultKeySpec -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name pKIExpirationPeriod -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name pKIExtendedKeyUsage -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name pKIKeyUsage -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name pKIMaxIssuingDepth -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name pKIOverlapPeriod -Value ""
        $Template | Add-Member -MemberType NoteProperty -Name ntSecurityDescriptor -Value ""


        if($Templates[$i].Attributes.cn)
        {
            $Template.cn = $Templates[$i].Attributes.cn[0]
        }
        if($Templates[$i].Attributes.distinguishedname)
        {
            $Template.distinguishedName = $Templates[$i].Attributes.distinguishedname[0]
        }
        if($Templates[$i].Attributes.flags)
        {
            $Template.flags = $Templates[$i].Attributes.flags[0]
        }
        if($Templates[$i].Attributes."mspki-certificate-application-policy")
        {
            $Template.msPKICertificateApplicationPolicy = $Templates[$i].Attributes."mspki-certificate-application-policy"[0]
        }
        if($Templates[$i].Attributes."mspki-certificate-name-flag")
        {
            $Template.msPKICertificateNameFlag = $Templates[$i].Attributes."mspki-certificate-name-flag"[0]
        }
        if($Templates[$i].Attributes."mspki-certificate-policy")
        {
            $Template.msPKICertificatePolicy = $Templates[$i].Attributes."mspki-certificate-policy"[0]
        }
        if($Templates[$i].Attributes."mspki-cert-template-oid")
        {
            $Template.msPKICertTemplateOID = $Templates[$i].Attributes."mspki-cert-template-oid"[0]
        }
        if($Templates[$i].Attributes."mspki-enrollment-flag")
        {
            $Template.msPKIEnrollmentFlag = $Templates[$i].Attributes."mspki-enrollment-flag"[0]
        }
        if($Templates[$i].Attributes."mspki-minimal-key-size")
        {
            $Template.msPKIMinimalKeySize = $Templates[$i].Attributes."mspki-minimal-key-size"[0]
        }
        if($Templates[$i].Attributes."mspki-private-key-flag")
        {
            $Template.msPKIPrivateKeyFlag = $Templates[$i].Attributes."mspki-private-key-flag"[0]
        }
        if($Templates[$i].Attributes."mspki-ra-application-policies")
        {
            $Template.msPKIRAApplicationPolicies = $Templates[$i].Attributes."mspki-ra-application-policies"[0]
        }
        if($Templates[$i].Attributes."mspki-ra-policies")
        {
            $Template.msPKIRAPolicies = $Templates[$i].Attributes."mspki-ra-policies"[0]
        }        
        if($Templates[$i].Attributes."mspki-ra-signature")
        {
            $Template.msPKIRASignature = $Templates[$i].Attributes."mspki-ra-signature"[0]
        }        
        if($Templates[$i].Attributes."mspki-supersede-templates")
        {
            $Template.msPKISupersedeTemplates = $Templates[$i].Attributes."mspki-supersede-templates"[0]
        }        
        if($Templates[$i].Attributes."mspki-template-minor-revision")
        {
            $Template.msPKITemplateMinorRevision = $Templates[$i].Attributes."mspki-template-minor-revision"[0]
        }        
        if($Templates[$i].Attributes."mspki-template-schema-version")
        {
            $Template.msPKITemplateSchemaVersion = $Templates[$i].Attributes."mspki-template-schema-version"[0]
        }
        if($Templates[$i].Attributes.objectguid)
        {
            $Template.objectGUID = (New-Object System.Guid $Templates[$i].Attributes.objectguid).Guid
        }
        if($Templates[$i].Attributes."pkicriticalextensions")
        {
            $Template.pKICriticalExtensions = $Templates[$i].Attributes."pkicriticalextensions"[0]
        }        
        if($Templates[$i].Attributes.pkidefaultcsps)
        {
            $pkidefaultcsps = @()
            for ($j = 0;$j -lt 2; $j++) { $pkidefaultcsps += $Templates[$i].Attributes.pkidefaultcsps[$j]}
            $Template.pKIDefaultCSPs = [string]::Join(";",$pkidefaultcsps)
        }        
        if($Templates[$i].Attributes.pkidefaultkeyspec)
        {
            $Template.pKIDefaultKeySpec= $Templates[$i].Attributes.pkidefaultkeyspec[0]
        }        
        if($Templates[$i].Attributes.pkiexpirationperiod)
        {
            $PKIExpirationPeriod = $Templates[$i].Attributes.pkiexpirationperiod[0]
            [array]::Reverse($PKIExpirationPeriod)
            $LittleEndianByte = -join ($PKIExpirationPeriod | ForEach-Object {"{0:x2}" -f $_  })
            $Template.pKIExpirationPeriod = [math]::Round([convert]::ToInt64($LittleEndianByte,16) * -0.0000001 / 3600 / 24,2)
        }        
        if($Templates[$i].Attributes.pkiextendedkeyusage)
        {
            $Template.pKIExtendedKeyUsage = $Templates[$i].Attributes.pkiextendedkeyusage[0]
        }
        if($Templates[$i].Attributes.pkikeyusage)
        {
            $Template.pKIKeyUsage = -join ([string]$Templates[$i].Attributes.pkikeyusage[0])
        }
        if($Templates[$i].Attributes.pkimaxissuingdepth)
        {
            $Template.pKIMaxIssuingDepth = $Templates[$i].Attributes.pkimaxissuingdepth[0]
        }
        if($Templates[$i].Attributes.pkioverlapperiod)
        {
            $PKIOverlapPeriod = $Templates[$i].Attributes.pkioverlapperiod[0]
            [array]::Reverse($PKIOverlapPeriod)
            $LittleEndianByte = -join ($PKIOverlapPeriod | ForEach-Object {"{0:x2}" -f $_  })
            $Template.pKIOverlapPeriod = [math]::Round([convert]::ToInt64($LittleEndianByte,16) * -0.0000001 / 3600 / 24,2)
        }
        
        if($Templates[$i].Attributes.ntsecuritydescriptor)
        {
            $ntsecuritydescriptor = $Templates[$i].Attributes.ntsecuritydescriptor[0]
            $ACL = New-Object System.Security.AccessControl.CommonSecurityDescriptor "true","true",$ntSecurityDescriptor,0
            $SDDL = $ACL.GetSddlForm([System.Security.AccessControl.AccessControlSections]::ALL)
            $Template.ntSecurityDescriptor = $SDDL
        }
        else
        {
            $Template.ntSecurityDescriptor = "Could not read ntsecuritydescriptor"
        }

        if($Templates[$i].Attributes.objectsid)
        {
            $SID = (New-Object System.Security.Principal.SecurityIdentifier([byte[]]$Templates[$i].Attributes.objectsid[0],0)).Value
        }
        $Statement = "update Templates set cn='{1}',distinguishedName='{2}',flags='{3}',msPKICertificateApplicationPolicy='{4}',msPKICertificateNameFlag='{5}',
        msPKICertificatePolicy='{6}',msPKICertTemplateOID='{7}',msPKIEnrollmentFlag='{8}',msPKIMinimalKeySize='{9}',msPKIPrivateKeyFlag='{10}',
        msPKIRAApplicationPolicies='{11}',msPKIRAPolicies='{12}',msPKIRASignature='{13}',msPKISupersedeTemplates='{14}',msPKITemplateMinorRevision='{15}',
        msPKITemplateSchemaVersion='{16}',pKICriticalExtensions='{17}',pKIDefaultCSPs='{18}',pKIDefaultKeySpec='{19}',pKIExpirationPeriod='{20}',pKIExtendedKeyUsage='{21}',
        pKIKeyUsage='{22}',pKIMaxIssuingDepth='{23}',pKIOverlapPeriod='{24}',ntSecurityDescriptor='{25}' where GUID='{0}' 
        IF @@ROWCOUNT=0
        INSERT INTO Templates 
        (GUID,cn,distinguishedName,flags,msPKICertificateApplicationPolicy,msPKICertificateNameFlag,msPKICertificatePolicy,msPKICertTemplateOID,
        msPKIEnrollmentFlag,msPKIMinimalKeySize,msPKIPrivateKeyFlag,msPKIRAApplicationPolicies,msPKIRAPolicies,msPKIRASignature,msPKISupersedeTemplates,
        msPKITemplateMinorRevision,msPKITemplateSchemaVersion,pKICriticalExtensions,pKIDefaultCSPs,pKIDefaultKeySpec,pKIExpirationPeriod,pKIExtendedKeyUsage,
        pKIKeyUsage,pKIMaxIssuingDepth,pKIOverlapPeriod,ntSecurityDescriptor) 
        VALUES 
        ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}','{23}','{24}','{25}')" `
        -f $Template.objectGUID,$Template.cn,$Template.distinguishedName,$Template.flags,$Template.msPKICertificateApplicationPolicy,$Template.msPKICertificateNameFlag,$Template.msPKICertificatePolicy, `
        $Template.msPKICertTemplateOID,$Template.msPKIEnrollmentFlag,$Template.msPKIMinimalKeySize,$Template.msPKIPrivateKeyFlag,$Template.msPKIRAApplicationPolicies,$Template.msPKIRAPolicies, `
        $Template.msPKIRASignature,$Template.msPKISupersedeTemplates,$Template.msPKITemplateMinorRevision,$Template.msPKITemplateSchemaVersion,$Template.pKICriticalExtensions,$Template.pKIDefaultCSPs, `
        $Template.pKIDefaultKeySpec,$Template.pKIExpirationPeriod,$Template.pKIExtendedKeyUsage,$Template.pKIKeyUsage,$Template.pKIMaxIssuingDepth,$Template.pKIOverlapPeriod,$Template.ntSecurityDescriptor
        
        $SQLCommand = New-object System.Data.SqlClient.SqlCommand $Statement, $SQLConnection
        try
        {
            [void]$SQLCommand.ExecuteNonQuery()
        }
        catch
        {
            $Statement
        }
    }
}

Export-TemplatesToSQL -ADConnectionInfo $ADConnectionInfo -SQLConnection $SQLConnection 
