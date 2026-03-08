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
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Certificate_Manager.Pages;
using Windows.Security.Cryptography.Certificates;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Certificate_Manager
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                var tag = selectedItem.Tag?.ToString();
                var pageType = tag switch
                {
                    "List_Certificates" => typeof(Certificate_Manager.Pages.Certificate.List),
                    "List_Requests" => typeof(Certificate_Manager.Pages.Request.List),
                    "New_Request" => typeof(Certificate_Manager.Pages.Request.New),
                    "Setup" => typeof(Certificate_Manager.Pages.Admin.Setup),
                    _ => typeof(Certificate_Manager.Pages.Certificate.List)
                };

                ContentFrame.Navigate(pageType);

            }
        }
    }
}
