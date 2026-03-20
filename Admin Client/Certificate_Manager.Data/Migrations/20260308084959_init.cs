using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Certificate_Manager.Data.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CRL",
                columns: table => new
                {
                    CRLRowId = table.Column<int>(type: "int", nullable: false),
                    CAConfig = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CRLNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CRLRawCRL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CRLThisUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CRLNextUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CRLNextPublish = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CRL", x => new { x.CRLRowId, x.CAConfig });
                });

            migrationBuilder.CreateTable(
                name: "Entry",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    CAConfig = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Base64Certificate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Base64Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestDisposition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequesterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedEmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedCommonName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedCountryRegion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedOrganizationUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CallerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateEffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CertificateExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublicKeyLength = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicKeyAlgorithm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestCountryRegion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestOrganizationUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestCommonName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestEmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateEnrollmentFlags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateGeneralFlags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplatePrivateKeyFlags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicKeyAlgorithmParameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevocationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Owner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Successor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Monitoring = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entry", x => new { x.RequestId, x.CAConfig });
                });

            migrationBuilder.CreateTable(
                name: "Template",
                columns: table => new
                {
                    GUID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistinguishedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    flags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKICertificateApplicationPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKICertificateNameFlag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKICertificatePolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKICertTemplateOID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIEnrollmentFlag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIMinimalKeySize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIPrivateKeyFlag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIRAApplicationPolicies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIRAPolicies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIRASignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKISupersedeTemplates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKITemplateMinorRevision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKITemplateSchemaVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKICriticalExtensions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIDefaultCSPs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIDefaultKeySpec = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIExpirationPeriod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIExtendedKeyUsage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIKeyUsage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIMaxIssuingDepth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIOverlapPeriod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ntSecurityDescriptor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Template", x => x.GUID);
                });

            migrationBuilder.CreateTable(
                name: "TemplatesArchiv",
                columns: table => new
                {
                    GUID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistinguishedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    flags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKICertificateApplicationPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKICertificateNameFlag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKICertificatePolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKICertTemplateOID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIEnrollmentFlag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIMinimalKeySize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIPrivateKeyFlag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIRAApplicationPolicies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIRAPolicies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKIRASignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKISupersedeTemplates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKITemplateMinorRevision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    msPKITemplateSchemaVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKICriticalExtensions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIDefaultCSPs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIDefaultKeySpec = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIExpirationPeriod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIExtendedKeyUsage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIKeyUsage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIMaxIssuingDepth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pKIOverlapPeriod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ntSecurityDescriptor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplatesArchiv", x => x.GUID);
                });

            migrationBuilder.CreateTable(
                name: "RevokedCert",
                columns: table => new
                {
                    RevokedCertId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CRLRowId = table.Column<int>(type: "int", nullable: false),
                    CAConfig = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevocationDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevokedCert", x => x.RevokedCertId);
                    table.ForeignKey(
                        name: "FK_RevokedCert_CRL_CRLRowId_CAConfig",
                        columns: x => new { x.CRLRowId, x.CAConfig },
                        principalTable: "CRL",
                        principalColumns: new[] { "CRLRowId", "CAConfig" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EKU",
                columns: table => new
                {
                    SANId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    CAConfig = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EKU", x => x.SANId);
                    table.ForeignKey(
                        name: "FK_EKU_Entry_RequestId_CAConfig",
                        columns: x => new { x.RequestId, x.CAConfig },
                        principalTable: "Entry",
                        principalColumns: new[] { "RequestId", "CAConfig" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SAN",
                columns: table => new
                {
                    SANId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    CAConfig = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubjectAlternativeName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAN", x => x.SANId);
                    table.ForeignKey(
                        name: "FK_SAN_Entry_RequestId_CAConfig",
                        columns: x => new { x.RequestId, x.CAConfig },
                        principalTable: "Entry",
                        principalColumns: new[] { "RequestId", "CAConfig" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EKU_RequestId_CAConfig",
                table: "EKU",
                columns: new[] { "RequestId", "CAConfig" });

            migrationBuilder.CreateIndex(
                name: "IX_RevokedCert_CRLRowId_CAConfig",
                table: "RevokedCert",
                columns: new[] { "CRLRowId", "CAConfig" });

            migrationBuilder.CreateIndex(
                name: "IX_SAN_RequestId_CAConfig",
                table: "SAN",
                columns: new[] { "RequestId", "CAConfig" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EKU");

            migrationBuilder.DropTable(
                name: "RevokedCert");

            migrationBuilder.DropTable(
                name: "SAN");

            migrationBuilder.DropTable(
                name: "Template");

            migrationBuilder.DropTable(
                name: "TemplatesArchiv");

            migrationBuilder.DropTable(
                name: "CRL");

            migrationBuilder.DropTable(
                name: "Entry");
        }
    }
}
