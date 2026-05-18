using CertificateManager.Admin.Data;
using CertificateManager.Admin.Data.Models.Views;
using CertificateManager.Admin.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace CertificateManager.Admin.Pages.Request
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class New : Page
    {
        private int _lastRequestId;

        public New()
        {
            InitializeComponent();
            this.Loaded += New_Loaded;
        }

        private async void New_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                var caConfigs = await db.Entry
                    .Select(entry => entry.CAConfig)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                CAConfigCombo.Items.Clear();
                foreach (var ca in caConfigs)
                    CAConfigCombo.Items.Add(ca);

                if (CAConfigCombo.Items.Count > 0)
                    CAConfigCombo.SelectedIndex = 0;
            }
            catch
            {
                CAConfigCombo.PlaceholderText = "Failed to load CAs";
            }
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            var request = new CARequest();
            var svc = new CertificateAuthoritySvc();
            try
            {
                request.RawRequest = Convert.FromBase64String(Base64RequestInput.Text);
                request.RequestType = 263168;
                svc.ParseRequest(request);
                ParsedDataOutput.Text = string.Empty;
                ParsedDataOutput.Text += $"Subject: " + svc.Subject + Environment.NewLine;
                ParsedDataOutput.Text += $"Template:  " + svc.TemplateInfo + Environment.NewLine;
                ParsedDataOutput.Text += $"Subject Alternative Names: " + Environment.NewLine + string.Join(Environment.NewLine, svc.SubjectAlternativeNames) + Environment.NewLine;
                ParsedDataOutput.Text += $"Enhanced Key Usages: " + Environment.NewLine + string.Join(Environment.NewLine, svc.EKUs) + Environment.NewLine;
                SubmitButton.IsEnabled = true;
            }
            catch( Exception ex)
            {
                ParsedDataOutput.Text = ex.Message;
            }
            
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var caConfig = CAConfigCombo.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(caConfig))
            {
                OutputBox.Text = "Please select a Certificate Authority.";
                return;
            }

            try
            {
                SubmitButton.IsEnabled = false;
                LoadingRing.IsActive = true;

                const int CR_IN_ANY = 0;
                const int CR_IN_BASE64 = 0x1;
                const int CR_IN_PKCS10 = 0x100;

                const int CR_DISP_ISSUED = 3;
                const int CR_DISP_UNDER_SUBMISSION = 5;

                const int CR_OUT_BASE64 = 0x1;

                var base64Request = Base64RequestInput.Text;

                (int disposition, int requestId, string? certificate) = await Task.Run(() =>
                {
                    var certRequest = new CERTCLILib.CCertRequestClass();
                    int disp = certRequest.Submit(
                        CR_IN_BASE64 | CR_IN_ANY,
                        base64Request,
                        null,
                        caConfig);
                    int reqId = certRequest.GetRequestId();

                    string? cert = null;
                    if (disp == CR_DISP_ISSUED)
                    {
                        cert = certRequest.GetCertificate(CR_OUT_BASE64);
                    }

                    return (disp, reqId, cert);
                });

                _lastRequestId = requestId;

                if (disposition == CR_DISP_ISSUED)
                {
                    OutputBox.Text = $"Certificate issued successfully. Request ID: {requestId}\n\n"
                        + "-----BEGIN CERTIFICATE-----\n"
                        + certificate
                        + "-----END CERTIFICATE-----";
                }
                else if (disposition == CR_DISP_UNDER_SUBMISSION)
                {
                    OutputBox.Text = $"Request submitted and pending approval. Request ID: {requestId}\n"
                        + "Use the Retrieve button after the request has been approved.";
                    RetrieveButton.IsEnabled = true;
                }
                else
                {
                    OutputBox.Text = $"Request submitted. Disposition: {disposition}, Request ID: {requestId}";
                }
            }
            catch (Exception ex)
            {
                OutputBox.Text = $"Submit failed: {ex.Message}";
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                LoadingRing.IsActive = false;
            }
        }

        private async void RetrieveButton_Click(object sender, RoutedEventArgs e)
        {
            var caConfig = CAConfigCombo.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(caConfig) || _lastRequestId == 0)
            {
                OutputBox.Text = "No pending request to retrieve.";
                return;
            }

            try
            {
                RetrieveButton.IsEnabled = false;
                LoadingRing.IsActive = true;

                const int CR_DISP_ISSUED = 3;
                const int CR_OUT_BASE64 = 0x1;

                (int disposition, string? certificate) = await Task.Run(() =>
                {
                    var certRequest = new CERTCLILib.CCertRequestClass();
                    int disp = certRequest.RetrievePending(_lastRequestId, caConfig);

                    string? cert = null;
                    if (disp == CR_DISP_ISSUED)
                    {
                        cert = certRequest.GetCertificate(CR_OUT_BASE64);
                    }

                    return (disp, cert);
                });

                if (disposition == CR_DISP_ISSUED)
                {
                    OutputBox.Text = $"Certificate retrieved successfully. Request ID: {_lastRequestId}\n\n"
                        + "-----BEGIN CERTIFICATE-----\n"
                        + certificate
                        + "-----END CERTIFICATE-----";
                    RetrieveButton.IsEnabled = false;
                }
                else
                {
                    OutputBox.Text = $"Certificate not yet issued. Disposition: {disposition}, Request ID: {_lastRequestId}\n"
                        + "The request may still be pending approval. Try again later.";
                    RetrieveButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                OutputBox.Text = $"Retrieve failed: {ex.Message}";
                RetrieveButton.IsEnabled = true;
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }
    }
}
