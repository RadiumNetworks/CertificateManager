using CertificateManager.Admin.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CertificateManager.Admin.Pages.Statistics
{
    public class TemplateStatistic
    {
        public int UseCount { get; set; }
        public string CN { get; set; } = string.Empty;
        public string MsPKICertTemplateOID { get; set; } = string.Empty;
    }

    public sealed partial class Overview : Page
    {
        private readonly ObservableCollection<TemplateStatistic> _statistics = new();

        public Overview()
        {
            InitializeComponent();
            ResultsListView.ItemsSource = _statistics;
        }

        private async void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            int maxExpiration = (int)MaxExpirationMonths.Value;
            if (maxExpiration < 1) maxExpiration = 2;

            QueryButton.IsEnabled = false;
            ProgressIndicator.IsActive = true;
            _statistics.Clear();

            try
            {
                var results = await Task.Run(() => LoadStatistics(maxExpiration));

                foreach (var item in results)
                {
                    _statistics.Add(item);
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            finally
            {
                QueryButton.IsEnabled = true;
                ProgressIndicator.IsActive = false;
            }
        }

        private static ObservableCollection<TemplateStatistic> LoadStatistics(int maxExpirationMonths)
        {
            var results = new ObservableCollection<TemplateStatistic>();

            const string sql = @"
                SELECT COUNT(*) as UseCount, sub.CN, sub.msPKICertTemplateOID FROM
                (SELECT e.RequestId, e.CAConfig, t.msPKICertTemplateOID, t.CN
                  FROM [dbo].[Entry] e
                  JOIN [dbo].[Template] t
                  ON e.CertificateTemplate = t.msPKICertTemplateOID
                  WHERE e.CertificateExpirationDate < DATEADD(MONTH, @MaxExpiration, GETDATE())
                    AND e.CertificateExpirationDate > GETDATE()
                UNION
                SELECT e.RequestId, e.CAConfig, t.msPKICertTemplateOID, t.CN
                  FROM [dbo].[Entry] e
                  JOIN [dbo].[Template] t
                  ON e.CertificateTemplate = t.CN
                  WHERE e.CertificateExpirationDate < DATEADD(MONTH, @MaxExpiration, GETDATE())
                    AND e.CertificateExpirationDate > GETDATE()) AS sub
                GROUP BY sub.CN, sub.msPKICertTemplateOID
                ORDER BY UseCount DESC";

            using var db = new AppDbContext();
            var connection = db.Database.GetDbConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new SqlParameter("@MaxExpiration", maxExpirationMonths));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new TemplateStatistic
                {
                    UseCount = reader.GetInt32(0),
                    CN = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    MsPKICertTemplateOID = reader.IsDBNull(2) ? "" : reader.GetString(2)
                });
            }

            return results;
        }
    }
}
