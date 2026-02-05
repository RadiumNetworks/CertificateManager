using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static Azure.Core.HttpHeader;

namespace Certificate_Management.Pages.Certificates
{
    public class IndexModel : PageModel
    {
        public List<CertificateData> listCertificates = new List<CertificateData>();
        public CertificateData filterinfo = new CertificateData();
        public String SQLFilter = "";
        public void OnGet()
        {
            try
            {
                String connectionString = "Data Source=DATA01\\SQLPKI;Initial Catalog=CADB;Integrated Security=True;Encrypt=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    String sql = @"(SELECT  Entries.RequestID, Entries.CAConfig, RequestCommonName, RequestCity, RequesterName,RequestCountryRegion, RequestOrganization, RequestOrganizationUnit, RequestEmailAddress, CertificateEffectiveDate, CertificateExpirationDate, Owner, Notes, STRING_AGG(SANs.SubjectAlternativeName, ', ') as SANList FROM Entries 
                        LEFT JOIN SANs on Entries.RequestID = SANs.RequestID and Entries.CAConfig = SANs.CAConfig
                        Group by Entries.RequestID, Entries.CAConfig, RequestCommonName, RequestCity, RequesterName, RequestCountryRegion, RequestOrganization, RequestOrganizationUnit, RequestEmailAddress, CertificateEffectiveDate, CertificateExpirationDate, Owner, Notes)";
                    using (SqlCommand command = new SqlCommand(sql, connection)) 
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CertificateData certificateData = new CertificateData();

                                if (!reader.IsDBNull(0))
                                    certificateData.RequestID = "" + reader.GetInt32(0);
                                else
                                    certificateData.RequestID = "";
                                if (!reader.IsDBNull(1))
                                    certificateData.CAConfig = "" + reader.GetString(1);
                                else
                                    certificateData.CAConfig = "";
                                if (!reader.IsDBNull(2))
                                    if (reader.GetString(2).Length > 0)
                                    {
                                        certificateData.RequestCommonName = "CN=" + reader.GetString(2);
                                    }
                                    else
                                    {
                                        certificateData.RequestCommonName = "";
                                    }
                                else
                                    certificateData.RequestCommonName = "";
                                if (!reader.IsDBNull(3))
                                    if (reader.GetString(3).Length > 0)
                                    {
                                        certificateData.RequestCity = "L=" + reader.GetString(3);
                                    }
                                    else
                                    {
                                        certificateData.RequestCity = "";
                                    }
                                else
                                    certificateData.RequestCity = "";
                                if (!reader.IsDBNull(4))
                                    certificateData.RequesterName = "" + reader.GetString(4);
                                else
                                    certificateData.RequesterName = "";
                                if (!reader.IsDBNull(5))
                                    if (reader.GetString(5).Length > 0)
                                    {
                                        certificateData.RequestCountryRegion = "C=" + reader.GetString(5);
                                    }
                                    else
                                    {
                                        certificateData.RequestCountryRegion = "";
                                    }
                                else
                                    certificateData.RequestCountryRegion = "";
                                if (!reader.IsDBNull(6))
                                    if (reader.GetString(6).Length > 0)
                                    {
                                        certificateData.RequestOrganization = "O=" + reader.GetString(6);
                                    }
                                    else
                                    {
                                        certificateData.RequestOrganization = "";
                                    }
                                else
                                    certificateData.RequestOrganization = "";
                                if (!reader.IsDBNull(7))
                                    if (reader.GetString(7).Length > 0)
                                    {
                                        certificateData.RequestOrganizationUnit = "OU=" + reader.GetString(7);
                                    }
                                    else
                                    {
                                        certificateData.RequestOrganizationUnit = "";
                                    }
                                else
                                    certificateData.RequestOrganizationUnit = "";
                                if (!reader.IsDBNull(8))
                                    if (reader.GetString(8).Length > 0)
                                    {
                                        certificateData.RequestEmailAddress = "E=" + reader.GetString(8);
                                    }
                                    else
                                    {
                                        certificateData.RequestEmailAddress = "";
                                    }
                                else
                                    certificateData.RequestEmailAddress = "";
                                if (!reader.IsDBNull(9))
                                    certificateData.StartDate = "" + reader.GetString(9);
                                else
                                    certificateData.StartDate = "";
                                if (!reader.IsDBNull(10))
                                    certificateData.EndDate = "" + reader.GetString(10);
                                else
                                    certificateData.EndDate = "";
                                if (!reader.IsDBNull(11))
                                    certificateData.Owner = "" + reader.GetString(11);
                                else
                                    certificateData.Owner = "";
                                if (!reader.IsDBNull(12))
                                    certificateData.Notes = "" + reader.GetString(12);
                                else
                                    certificateData.Notes = "";
                                if (!reader.IsDBNull(13))
                                    certificateData.SANList = "" + reader.GetString(13);
                                else
                                    certificateData.SANList = "";

                                listCertificates.Add(certificateData);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
        }

        public void OnPost()
        {
            filterinfo.CAConfig = Request.Form["CAConfig"];
            filterinfo.RequestID = Request.Form["RequestID"];
            filterinfo.RequestCommonName = Request.Form["Subject"];
            if (filterinfo.CAConfig.Length != 0)
            {
                SQLFilter = "Entries.CAConfig = '" + filterinfo.CAConfig + "'";
            }
            if (filterinfo.RequestID.Length != 0)
            {
                if (SQLFilter.Length == 0)
                {
                    SQLFilter = "Entries.RequestID = '" + filterinfo.RequestID + "'";
                }
                else 
                {
                    SQLFilter = SQLFilter + " AND Entries.RequestID = '" + filterinfo.RequestID + "'";
                }
            }
            if (filterinfo.RequestCommonName.Length != 0)
            {
                if (SQLFilter.Length == 0)
                {
                    SQLFilter = "IssuedCommonName = '" + filterinfo.RequestCommonName + "'";
                }
                else
                {
                    SQLFilter = SQLFilter + " AND IssuedCommonName = '" + filterinfo.RequestCommonName + "'";
                }
                
            }
            try
            {
                String connectionString = "Data Source=DATA01\\SQLPKI;Initial Catalog=CADB;Integrated Security=True;Encrypt=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    StringBuilder sql = new StringBuilder();
                    if (SQLFilter.Length == 0)
                    {
                        sql.AppendLine("SELECT Entries.RequestID, Entries.CAConfig, RequestCommonName, RequestCity, RequesterName,RequestCountryRegion, RequestOrganization, RequestOrganizationUnit, RequestEmailAddress, CertificateEffectiveDate, CertificateExpirationDate, Owner, Notes, STRING_AGG(SANs.SubjectAlternativeName, ', ') as SANList FROM Entries");
                        sql.AppendLine("LEFT JOIN SANs on Entries.RequestID = SANs.RequestID and Entries.CAConfig = SANs.CAConfig");
                        sql.AppendLine("Group by Entries.RequestID, Entries.CAConfig, RequestCommonName, RequestCity, RequesterName, RequestCountryRegion, RequestOrganization, RequestOrganizationUnit, RequestEmailAddress, CertificateEffectiveDate, CertificateExpirationDate, Owner, Notes");

                    }
                    else
                    {
                        sql = new StringBuilder();
                        sql.AppendLine("SELECT Entries.RequestID, Entries.CAConfig, RequestCommonName, RequestCity, RequesterName,RequestCountryRegion, RequestOrganization, RequestOrganizationUnit, RequestEmailAddress, CertificateEffectiveDate, CertificateExpirationDate, Owner, Notes, STRING_AGG(SANs.SubjectAlternativeName, ', ') as SANList FROM Entries");
                        sql.AppendLine("LEFT JOIN SANs on Entries.RequestID = SANs.RequestID and Entries.CAConfig = SANs.CAConfig where " + SQLFilter);
                        sql.AppendLine("Group by Entries.RequestID, Entries.CAConfig, RequestCommonName, RequestCity, RequesterName, RequestCountryRegion, RequestOrganization, RequestOrganizationUnit, RequestEmailAddress, CertificateEffectiveDate, CertificateExpirationDate, Owner, Notes");
                    }

                    using (SqlCommand command = new SqlCommand(sql.ToString(), connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CertificateData certificateData = new CertificateData();

                                if (!reader.IsDBNull(0))
                                    certificateData.RequestID = "" + reader.GetInt32(0);
                                else
                                    certificateData.RequestID = "";
                                if (!reader.IsDBNull(1))
                                    certificateData.CAConfig = "" + reader.GetString(1);
                                else
                                    certificateData.CAConfig = "";
                                if (!reader.IsDBNull(2))
                                    if (reader.GetString(2).Length > 0)
                                    {
                                        certificateData.RequestCommonName = "CN=" + reader.GetString(2);
                                    }
                                    else
                                    {
                                        certificateData.RequestCommonName = "";
                                    }
                                else
                                    certificateData.RequestCommonName = "";
                                if (!reader.IsDBNull(3))
                                    if (reader.GetString(3).Length > 0)
                                    {
                                        certificateData.RequestCity = "L=" + reader.GetString(3);
                                    }
                                    else
                                    {
                                        certificateData.RequestCity = "";
                                    }
                                else
                                    certificateData.RequestCity = "";
                                if (!reader.IsDBNull(4))
                                    certificateData.RequesterName = "" + reader.GetString(4);
                                else
                                    certificateData.RequesterName = "";
                                if (!reader.IsDBNull(5))
                                    if (reader.GetString(5).Length > 0)
                                    {
                                        certificateData.RequestCountryRegion = "C=" + reader.GetString(5);
                                    }
                                    else
                                    {
                                        certificateData.RequestCountryRegion = "";
                                    }
                                else
                                    certificateData.RequestCountryRegion = "";
                                if (!reader.IsDBNull(6))
                                    if (reader.GetString(6).Length > 0)
                                    {
                                        certificateData.RequestOrganization = "O=" + reader.GetString(6);
                                    }
                                    else
                                    {
                                        certificateData.RequestOrganization = "";
                                    }
                                else
                                    certificateData.RequestOrganization = "";
                                if (!reader.IsDBNull(7))
                                    if (reader.GetString(7).Length > 0)
                                    {
                                        certificateData.RequestOrganizationUnit = "OU=" + reader.GetString(7);
                                    }
                                    else
                                    {
                                        certificateData.RequestOrganizationUnit = "";
                                    }
                                else
                                    certificateData.RequestOrganizationUnit = "";
                                if (!reader.IsDBNull(8))
                                    if (reader.GetString(8).Length > 0)
                                    {
                                        certificateData.RequestEmailAddress = "E=" + reader.GetString(8);
                                    }
                                    else
                                    {
                                        certificateData.RequestEmailAddress = "";
                                    }
                                else
                                    certificateData.RequestEmailAddress = "";
                                if (!reader.IsDBNull(9))
                                    certificateData.StartDate = "" + reader.GetString(9);
                                else
                                    certificateData.StartDate = "";
                                if (!reader.IsDBNull(10))
                                    certificateData.EndDate = "" + reader.GetString(10);
                                else
                                    certificateData.EndDate = "";
                                if (!reader.IsDBNull(11))
                                    certificateData.Owner = "" + reader.GetString(11);
                                else
                                    certificateData.Owner = "";
                                if (!reader.IsDBNull(12))
                                    certificateData.Notes = "" + reader.GetString(12);
                                else
                                    certificateData.Notes = "";
                                if (!reader.IsDBNull(13))
                                    certificateData.SANList = "" + reader.GetString(13);
                                else
                                    certificateData.SANList = "";


                                listCertificates.Add(certificateData);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
        }
    }

    public class CertificateData
    {
        public string? RequestID;
        public string? CAConfig;
        public string? RequestCommonName;
        public string? RequestCity;
        public string? RequesterName;
        public string? RequestCountryRegion;
        public string? RequestOrganization;
        public string? RequestOrganizationUnit;
        public string? RequestEmailAddress;
        public string? StartDate;
        public string? EndDate;
        public string? Owner;
        public string? Notes;
        public string? SANList;
    }
}
