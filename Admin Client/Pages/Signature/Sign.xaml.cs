using CertificateManager.Admin.Data;
using CertificateManager.Admin.Data.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace CertificateManager.Admin.Pages.Signature
{
    public sealed partial class Sign : Page
    {
        private string? _uploadedFileName;
        private string? _signedScriptContent;
        private List<CertificateItem> _certificates = new();

        public Sign()
        {
            InitializeComponent();
            LoadCodeSigningCertificates();
            StoreLocationCombo.Items.Add(new ComboBoxItem { Content = "Current User", Tag = "CurrentUser" });
            StoreLocationCombo.Items.Add(new ComboBoxItem { Content = "Local Machine", Tag = "LocalMachine" });
            SignerInput.Text = WindowsIdentity.GetCurrent()?.Name ?? "Unknown User";
        }

        private void StoreLocationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadCodeSigningCertificates();
        }

        private void LoadCodeSigningCertificates()
        {
            _certificates.Clear();
            CSCertificateCombo.Items.Clear();

            var storeTag = (StoreLocationCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "CurrentUser";
            var location = storeTag == "LocalMachine" ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;

            var certs = SignatureHelper.GetCodeSigningCertificates(location);
            foreach (var info in certs)
            {
                var item = new CertificateItem
                {
                    Thumbprint = info.Thumbprint,
                    Subject = info.Subject,
                    Issuer = info.Issuer,
                    NotAfter = info.NotAfter,
                    StoreLocation = info.StoreLocation
                };
                _certificates.Add(item);
                CSCertificateCombo.Items.Add(item);
            }

            if (_certificates.Count > 0)
                CSCertificateCombo.SelectedIndex = 0;
        }

        private X509Certificate2? GetSelectedCertificate()
        {
            if (CSCertificateCombo.SelectedItem is not CertificateItem selected)
                return null;

            using var store = new X509Store(StoreName.My, selected.StoreLocation);
            store.Open(OpenFlags.ReadOnly);
            var found = store.Certificates.Find(X509FindType.FindByThumbprint, selected.Thumbprint, false);
            return found.Count > 0 ? found[0] : null;
        }

        private void DisplaySignatureInfoAsync()
        {
            SigType.Text = "";
            SigSigner.Text = "";
            SigIssuer.Text = "";
            SigSerialNumber.Text = "";
            SigThumbprint.Text = "";
            SigHashAlgorithm.Text = "";
            SigStatus.Text = "";
            SigStatusMessage.Text = "";
            SigTimestamp.Text = "";
        }

        private async System.Threading.Tasks.Task DisplaySignatureInfoAsync(string signedContent, string signatureType, X509Certificate2 cert)
        {
            SigType.Text = signatureType;
            SigSigner.Text = cert.GetNameInfo(X509NameType.SimpleName, false);
            SigIssuer.Text = cert.GetNameInfo(X509NameType.SimpleName, true);
            SigSerialNumber.Text = cert.SerialNumber;
            SigThumbprint.Text = cert.Thumbprint;
            SigHashAlgorithm.Text = signatureType == "Authenticode" ? "SHA1" : cert.SignatureAlgorithm.FriendlyName ?? "Unknown";

            if (signatureType == "Authenticode")
            {
                // Verify to get full status info
                string tempFile = Path.Combine(Path.GetTempPath(), $"info_{Guid.NewGuid():N}.ps1");
                await File.WriteAllTextAsync(tempFile, signedContent, new UTF8Encoding(false));
                try
                {
                    var result = VerifyScript(tempFile);
                    dynamic sig = result;
                    SigStatus.Text = sig.Status.ToString();
                    SigStatusMessage.Text = sig.StatusMessage ?? "";
                    SigTimestamp.Text = sig.TimeStamperCertificate != null
                        ? sig.TimeStamperCertificate.Subject
                        : "(none)";
                }
                finally
                {
                    File.Delete(tempFile);
                }
            }
            else
            {
                SigStatus.Text = "CMS Signed (use Verify to check)";
                SigStatusMessage.Text = "CMS signatures are not verifiable by Authenticode.";
                SigTimestamp.Text = "(not applicable)";
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".ps1");
            picker.FileTypeFilter.Add(".psm1");
            picker.FileTypeFilter.Add(".psd1");
            picker.FileTypeFilter.Add(".xlsx");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _uploadedFileName = file.Name;
                ScriptInput.Text = await FileIO.ReadTextAsync(file);
                ShowStatus("File loaded: " + file.Name, InfoBarSeverity.Informational);
            }

            DisplaySignatureInfoAsync();
        }

        private async void SignButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptInput.Text))
            {
                ShowStatus("Please paste or upload a script first.", InfoBarSeverity.Warning);
                return;
            }

            try
            {
                var selectedType = (SignatureTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Authenticode";
                string? timestampServer = string.IsNullOrWhiteSpace(TimestampServerInput.Text) ? null : TimestampServerInput.Text.Trim();

                X509Certificate2? cert = GetSelectedCertificate();
                if (cert == null)
                {
                    ShowStatus("Please select a code signing certificate.", InfoBarSeverity.Warning);
                    return;
                }

                string scriptText = ScriptInput.Text;

                if (selectedType == "Authenticode")
                {
                    _signedScriptContent = await SignAuthenticodeAsync(scriptText, cert, timestampServer);
                }
                else
                {
                    _signedScriptContent = SignCms(scriptText, cert);
                }

                DownloadButton.IsEnabled = true;

                await DisplaySignatureInfoAsync(_signedScriptContent, selectedType, cert);

                await LogSignedScriptAsync(cert, scriptText, selectedType);

                ShowStatus($"Script signed successfully with {selectedType} signature.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatus($"Signing failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async System.Threading.Tasks.Task<string> SignAuthenticodeAsync(string scriptText, X509Certificate2 cert, string? timestampServer)
        {
            return await SignatureHelper.SignAuthenticodeAsync(scriptText, cert, timestampServer);
        }

        public string BuildSignatureBlock(byte[] signature)
        {
            return SignatureHelper.BuildSignatureBlock(signature);
        }

        public string RemoveSignatureBlock(string scriptText)
        {
            return SignatureHelper.RemoveSignatureBlock(scriptText);
        }

        public object SignScript(string filePath, X509Certificate2 cert, string? timestampServer = null)
        {
            return SignatureHelper.SignScript(filePath, cert, timestampServer);
        }

        public object VerifyScript(string filePath)
        {
            return SignatureHelper.VerifyScript(filePath);
        }

        private string SignCms(string scriptText, X509Certificate2 cert)
        {
            return SignatureHelper.SignCms(scriptText, cert);
        }

        // --- Database logging ---

        private async System.Threading.Tasks.Task LogSignedScriptAsync(X509Certificate2 cert, string scriptText, string signatureType)
        {
            string hash = SignatureHelper.ComputeScriptHash(scriptText);
            string partialscript = SignatureHelper.TruncateScript(scriptText);

            var record = new SignedScript
            {
                Base64Certificate = Convert.ToBase64String(cert.RawData),
                FileName = _uploadedFileName ?? "pasted_script.ps1",
                ScriptContent = partialscript,
                FileHash = hash,
                SerialNumber = cert.SerialNumber,
                Signer = SignerInput.Text,
                SignDate = DateTime.UtcNow,
                Notes = $"[{signatureType}] {NotesInput.Text}"
            };

            using var db = new AppDbContext();
            db.SignedScript.Add(record);
            await db.SaveChangesAsync();
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            StatusBar.Message = message;
            StatusBar.Severity = severity;
            StatusBar.IsOpen = true;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_signedScriptContent))
                return;

            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("PowerShell Script", new[] { ".ps1" });
            picker.SuggestedFileName = _uploadedFileName ?? "signed_script.ps1";

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, _signedScriptContent);
                ShowStatus("Signed script saved: " + file.Name, InfoBarSeverity.Success);
            }
        }

        private void VerifyButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void InputScript_Changed(object sender, TextChangedEventArgs e)
        {
            DisplaySignatureInfoAsync();
        }
    }

    internal class CertificateItem
    {
        public string Thumbprint { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Issuer { get; set; } = "";
        public DateTime NotAfter { get; set; }
        public StoreLocation StoreLocation { get; set; }

        public override string ToString() => $"{Subject} (Issuer: {Issuer}, Expires: {NotAfter:yyyy-MM-dd})";
    }
}
