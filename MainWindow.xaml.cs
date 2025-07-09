using DownloadManagerApp.Services;
using Google.Apis.Auth.OAuth2;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using static DownloadManagerApp.Services.DownloadService;

namespace DownloadManagerApp
{

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            
            DataContext = new MainViewModel();

            
        }

       
    }
}