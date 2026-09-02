using CertificateManager.Admin.Data;
using CertificateManager.Admin.Data.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CertificateManager.Admin.Pages.Admin
{
    public sealed partial class Setup : Page
    {
        public Setup()
        {
            InitializeComponent();
            LoadConnectionString();
        }

        private void LoadConnectionString()
        {
            var settings = UserSettings.Load();
            if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                ConnectionStringBox.Text = settings.ConnectionString;
            }
        }

        private void SaveConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            var connStr = ConnectionStringBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(connStr))
            {
                ConnectionInfoBar.Message = "Connection string cannot be empty.";
                ConnectionInfoBar.Severity = InfoBarSeverity.Warning;
                ConnectionInfoBar.IsOpen = true;
                return;
            }

            var settings = UserSettings.Load();
            settings.ConnectionString = connStr;
            settings.Save();

            ConnectionInfoBar.Message = "Connection string saved. It will be used for all future database operations.";
            ConnectionInfoBar.Severity = InfoBarSeverity.Success;
            ConnectionInfoBar.IsOpen = true;
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            var connStr = ConnectionStringBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(connStr))
            {
                ConnectionInfoBar.Message = "Please enter a connection string first.";
                ConnectionInfoBar.Severity = InfoBarSeverity.Warning;
                ConnectionInfoBar.IsOpen = true;
                return;
            }

            try
            {
                TestConnectionButton.IsEnabled = false;
                using var connection = new SqlConnection(connStr);
                await connection.OpenAsync();
                ConnectionInfoBar.Message = $"Connection successful. Server: {connection.DataSource}, Database: {connection.Database}";
                ConnectionInfoBar.Severity = InfoBarSeverity.Success;
            }
            catch (Exception ex)
            {
                ConnectionInfoBar.Message = $"Connection failed: {ex.Message}";
                ConnectionInfoBar.Severity = InfoBarSeverity.Error;
            }
            finally
            {
                ConnectionInfoBar.IsOpen = true;
                TestConnectionButton.IsEnabled = true;
            }
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetBusy(true);
                AppendLog("Checking for pending migrations...");

                using var db = new AppDbContext();
                var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
                var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();

                AppendLog($"Applied migrations: {applied.Count}");
                foreach (var m in applied)
                    AppendLog($"  [Applied] {m}");

                AppendLog($"Pending migrations: {pending.Count}");
                foreach (var m in pending)
                    AppendLog($"  [Pending] {m}");

                if (pending.Count == 0)
                {
                    ShowStatus("Database is up to date. No pending migrations.", InfoBarSeverity.Success);
                }
                else
                {
                    ShowStatus($"{pending.Count} pending migration(s) found. Click 'Apply Database Migrations' to update.", InfoBarSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
                ShowStatus($"Failed to check migrations: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void MigrateButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = new ContentDialog
            {
                Title = "Confirm Database Migration",
                Content = "This will apply all pending migrations to the database. Are you sure?",
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirm.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            try
            {
                SetBusy(true);
                MigrateButton.IsEnabled = false;

                AppendLog("Starting database migration...");

                using var db = new AppDbContext();

                var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
                if (pending.Count == 0)
                {
                    AppendLog("No pending migrations to apply.");
                    ShowStatus("Database is already up to date.", InfoBarSeverity.Success);
                    return;
                }

                AppendLog($"Applying {pending.Count} migration(s)...");
                foreach (var m in pending)
                    AppendLog($"  Will apply: {m}");

                await db.Database.MigrateAsync();

                AppendLog("Migration completed successfully.");
                ShowStatus($"Successfully applied {pending.Count} migration(s).", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                AppendLog($"Migration failed: {ex.Message}");
                if (ex.InnerException != null)
                    AppendLog($"  Inner: {ex.InnerException.Message}");
                ShowStatus($"Migration failed: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                SetBusy(false);
                MigrateButton.IsEnabled = true;
            }
        }

        private void AppendLog(string message)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                var done = new ManualResetEventSlim(false);
                DispatcherQueue.TryEnqueue(() =>
                {
                    AppendLog(message);
                    done.Set();
                });
                done.Wait();
                return;
            }
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            MigrationLog.Text += $"[{timestamp}] {message}\n";
            MigrationLog.Select(MigrationLog.Text.Length, 0);
        }

        private void AppendImportLog(string message)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                var done = new ManualResetEventSlim(false);
                DispatcherQueue.TryEnqueue(() =>
                {
                    AppendImportLog(message);
                    done.Set();
                });
                done.Wait();
                return;
            }
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            ImportLog.Text += $"[{timestamp}] {message}\n";
            ImportLog.Select(ImportLog.Text.Length, 0);
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
        }

        private void SetBusy(bool busy)
        {
            ProgressIndicator.IsActive = busy;
            CheckButton.IsEnabled = !busy;
        }

        private async void InitButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = new ContentDialog
            {
                Title = "Confirm Full import of certificates and requests",
                Content = "This will import all entries to the database. Are you sure?",
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirm.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            try
            {
                SetBusy(true);
                InitButton.IsEnabled = false;
                InitProgressIndicator.IsActive = true;

                string caConfig = CAConfigString.Text;
                if (string.IsNullOrWhiteSpace(caConfig))
                {
                    ShowStatus("Please enter a CA configuration string (e.g., server\\CAName).", InfoBarSeverity.Warning);
                    return;
                }

                AppendImportLog("Starting CA database import...");
                AppendImportLog($"Connecting to: {caConfig}");

                var svc = new CertificateAuthoritySvc();

                int startRequestId = 0;
                if (!double.IsNaN(StartRequestIdInput.Value) && StartRequestIdInput.Value > 0)
                {
                    startRequestId = (int)StartRequestIdInput.Value;
                    AppendImportLog($"Starting import from RequestID >= {startRequestId}");
                }

                Action<int> progressCallback = (pct) =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ImportProgressBar.Value = pct;
                        ImportProgressText.Text = $"{pct}% Progress on Import";
                    });
                };

                const int batchSize = 1000;
                int totalRequests = 0;
                int totalCertificates = 0;
                int nextRequestId = startRequestId;

                await Task.Run(() =>
                {
                    while (true)
                    {
                        AppendImportLog($"Reading up to {batchSize} entries from RequestID >= {nextRequestId}...");

                        var (requests, certificates) = svc.ReadCADbEntries(
                            caConfig,
                            nextRequestId,
                            batchSize,
                            AppendImportLog,
                            progressCallback,
                            writeSqlLog: false);

                        totalRequests += requests.Count;
                        totalCertificates += certificates.Count;

                        if (requests.Count == 0)
                            break;

                        int lastProcessedRequestId = requests.Max(request => request.RequestId);
                        AppendImportLog($"Batch completed at RequestID {lastProcessedRequestId} ({requests.Count} entries).");

                        if (requests.Count < batchSize || lastProcessedRequestId == int.MaxValue)
                            break;

                        if (lastProcessedRequestId < nextRequestId)
                            throw new InvalidOperationException("The CA database did not return an increasing RequestID.");

                        nextRequestId = lastProcessedRequestId + 1;
                    }
                });

                AppendImportLog($"Read {totalRequests} requests and {totalCertificates} certificates.");
                ShowStatus($"Successfully read {totalRequests} requests and {totalCertificates} certificates from CA database.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                AppendImportLog($"Error: {ex.Message}");
                if (ex.InnerException != null)
                    AppendImportLog($"  Inner: {ex.InnerException.Message}");
                ShowStatus($"Failed to read CA database: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                SetBusy(false);
            }
            InitButton.IsEnabled = true;
            InitProgressIndicator.IsActive = false;
        }
    }
}
