using Certificate_Manager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Certificate_Manager.Pages.Admin
{
    public sealed partial class Setup : Page
    {
        public Setup()
        {
            InitializeComponent();
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
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            MigrationLog.Text += $"[{timestamp}] {message}\n";
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
    }
}
