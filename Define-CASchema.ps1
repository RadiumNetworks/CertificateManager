#Define the required columns and their outputformat 
#certutil -schema
$CertColumns = @{
    "RequestID"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request ID"};
    "RequestDisposition"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Disposition"};
    "RequesterName"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Requester Name"};
    "RequestType"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Type"};
    "RequestAttributes"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Attributes"};
    "CallerName"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Caller Name"};
    "CertificateHash"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Certificate Hash"};
    "CertificateTemplate"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Certificate Template"};
    "BinaryCertificate"=@{"Format"=$CV_OUT_BASE64HEADER;"ColumnName"="Binary Certificate"};
    "SerialNumber"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Serial Number"};
    "CertificateEffectiveDate"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Certificate Effective Date"};
    "CertificateExpirationDate"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Certificate Expiration Date"};
    "BinaryRequest"=@{"Format"=$CV_OUT_BASE64REQUESTHEADER;"ColumnName"="Binary Request"};
    "IssuedEmailAddress"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Issued Email Address"};
    "IssuedCommonName"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Issued Common Name"};
    "IssuedCountryRegion"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Issued Country/Region"};
    "IssuedOrganization"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Issued Organization"};
    "IssuedOrganizationUnit"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Issued Organization Unit"};
    "PublicKeyLength"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Public Key Length"};
    "PublicKeyAlgorithm"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Public Key Algorithm"};
    "RequestCountryRegion"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Country/Region"};
    "RequestOrganization"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Organization"};
    "RequestOrganizationUnit"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Organization Unit"};
    "RequestCommonName"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Common Name"};
    "RequestCity"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request City"};
    "RequestTitle"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Title"};
    "RequestEmailAddress"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Request Email Address"};
    "TemplateEnrollmentFlags"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Template Enrollment Flags"};
    "TemplateGeneralFlags"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Template General Flags"};
    "TemplatePrivateKeyFlags"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Template Private Key Flags"};
    "PublicKeyAlgorithmParameters"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Public Key Algorithm Parameters"};
    "RevocationDate"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Revocation Date"};
    "RevocationReason"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="Revocation Reason"};
}

#Define the required columns and their outputformat 
#certutil -schema CRL
$CRLColumns = @{
    "CRLRowID"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="CRL Row ID"};
    "CRLNumber"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="CRL Number"};
    "CRLRawCRL"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="CRL Raw CRL"};
    "CRLThisUpdate"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="CRL This Update"};
    "CRLNextUpdate"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="CRL Next Update"};
    "CRLNextPublish"=@{"Format"=$CV_OUT_BASE64;"ColumnName"="CRL Next Publish"};
}