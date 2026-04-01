using CertificateManager.Admin.Data.Models.Views;
using CertificateManager.Admin.Data.Services;
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
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace CertificateManager.Admin.Pages.Request
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class New : Page
    {
        public New()
        {
            InitializeComponent();
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

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RetrieveButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
