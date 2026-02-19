#Define variables / constants
$CVRC_COLUMN_SCHEMA = 0
$CVRC_COLUMN_RESULT = 1
$CVRC_COLUMN_VALUE = 2
$CVRC_COLUMN_MASK = 0xfff

$CVR_SORT_NONE = 0
$CVR_SORT_ASCEND = 1
$CVR_SORT_DESCEND = 2

$CVR_SEEK_NONE = 0
$CVR_SEEK_EQ = 1
$CVR_SEEK_LT = 2
$CVR_SEEK_LE = 4
$CVR_SEEK_GE = 8
$CVR_SEEK_GT = 10
$CVR_SEEK_MASK = 0xff

$CV_OUT_BASE64HEADER = 0
$CV_OUT_BASE64 = 1
$CV_OUT_BINARY = 2
$CV_OUT_BASE64REQUESTHEADER = 3
$CV_OUT_HEX = 4
$CV_OUT_HEXASCII = 5
$CV_OUT_BASE64X509CRLHEADER = 9
$CV_OUT_HEXADDR = 0xa
$CV_OUT_HEXASCIIADDR = 0xb
$CV_OUT_ENCODEMASK = 0xff

$RequestBeingProcessed = 8
$RequestTakenUnderSubmission = 9
$ArchivedForeignCertificate = 12
$CACertificate = 15
$ParentCACertificate = 16
$KeyRecoveryAgentCertificate = 17
$IssuedCertificate = 20
$RevokedCertificate = 21
$FailedRequest = 30
$DeniedRequest = 31

#Supported Extensions

$AlternativeNames = "2.5.29.17"
$AuthorityInformationAccess = "1.3.6.1.5.5.7.1.1"
$AuthorityKeyIdentifier = "2.5.29.35"
$BasicConstraints = "2.5.29.19"
$CertificatePolicies = "2.5.29.32"
$CRLDistributionPoints = "2.5.29.31"
$EnhancedKeyUsage = "2.5.29.37"
$FreshestCRL = "2.5.29.46"
$KeyUsage = "2.5.29.15"
$MSApplicationPolicies = "1.3.6.1.4.1.311.21.10"
$NameConstraints = "2.5.29.30"
$PolicyConstraints = "2.5.29.36"
$PolicyMappings = "2.5.29.33"
$PrivateKeyUsagePeriod = "2.5.29.16"
$SMimeCapabilities = "1.2.840.113549.1.9.15"
$SubjectDirectoryAttributes = "2.5.29.9"
$SubjectKeyIdentifier = "2.5.29.14"
$Template = "1.3.6.1.4.1.311.21.7"
$TemplateName = "1.3.6.1.4.1.311.20.2"

$CVRC_TABLE_REQCERT = 0
$CVRC_TABLE_CRL = 0x5000