using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadManagerApp.Models;
using DownloadManagerApp.Services;
using DownloadManagerApp.ViewModels;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Http;
using MaterialDesignColors.Recommended;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static DownloadManagerApp.Services.DownloadService;
using static DownloadManagerApp.Config.AppConfiguration;
using DownloadManagerApp.Config;
namespace DownloadManagerApp
{
    public partial class MainViewModel : ObservableObject
    {
        private HttpClient _httpClient = new();
        private DownloadService _downloadService = new(true);
        private GoogleDriveService _googleDriveService = new();
        private PlotViewModel _plotViewModel = new();
        private DatabaseServices _databaseService;
       
        //exposes_plotViewModel to the xaml as a public read only property
        public PlotViewModel PlotVM => _plotViewModel;

        private UserCredential _credential;
        string googleClientId = AppConfiguration.Configuration["Authentication:Google:ClientId"];
        string googleClientSecret = AppConfiguration.Configuration["Authentication:Google:ClientSecret"];

        [ObservableProperty]
        public ObservableCollection<DownloadItem> downloadQueue = new();

        [ObservableProperty]
        public ObservableCollection<FileNode> driveFiles = new();

        [ObservableProperty]
        private string filePath;

        [ObservableProperty]
        private string destinationPath;

        [ObservableProperty]
        private bool isSyncEnabled;

        [ObservableProperty]
        public string connectionStatus;

        [ObservableProperty]
        private string fileName;


        public MainViewModel(){
            Initialize();
           
            //DriveFiles.Add(new FileNode("Test File 1","1",false));
            //DriveFiles.Add(new FileNode("Test File 2","2",false));

            _downloadService.NetworkChanged += OnNetworkConnectivityLost;
            _downloadService.DownloadCompleted += DownloadService_DownloadCompleted;
        }

        public async void Initialize()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "downloadHistory.db");
            _databaseService = new(dbPath);
            await _databaseService.InitializeDatabaseAsync();

            var points = await _databaseService.GetDataPoints();
            _plotViewModel.InitializePoints(points);

            if (IsSyncEnabled)
            {

                _credential = await _googleDriveService.Login(googleClientId, googleClientSecret);
                await LoadDriveFiles();
            }
        }
    

        private void OnNetworkConnectivityLost(object? sender, NetworkAvailabilityEventArgs e)
        {
            _downloadService.isAbleToDownload = false;
            MessageBox.Show("No internet Available");
        }


        private async void DownloadService_DownloadCompleted(object? sender, DownloadCompletedEventArgs e)
        {
            MessageBox.Show("Download Completed");
            e.DownloadItem.ConnectionStatus = "Completed";
            e.DownloadItem.IsCompleted = false;
            //refresh treeview when a file is uploaded
            if (IsSyncEnabled == true)
            {
                await LoadDriveFiles();

                MessageBox.Show($"Download completed!\nFile: {e.FileName}\nPath: {e.FilePath}, Beggining upload to Google drive");
                try
                {
                    var fileId = await _googleDriveService.UploadToDrive(_credential, e.FilePath, e.DownloadItem);
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.ToString());
                }
            }

            //sends the latest download as a point to the plotModel, and returns that point to be added into the database
            var latestPoint = _plotViewModel.UpdatePoints(((Convert.ToDouble(e.DownloadItem.ByteSize)) / (1024.0 * 1024.0))).Last();
            await _databaseService.InsertDataPointAsync(latestPoint);
            await _databaseService.CleanTable();
        }

        private async Task LoadDriveFiles()
        {
            FileList files = await _googleDriveService.ListFromDrive(_credential);
            AddFilesToTree(files);
        }

        private void AddFilesToTree(FileList files)
        {

            bool isFolder;

            foreach (var file in files.Files)
            {
                if (file.MimeType == "application/vnd.google-apps.folder")
                {
                     isFolder = true;
                }

                else
                {
                     isFolder = false;
                }

                DriveFiles.Add(new FileNode(file.Name, file.Id, isFolder));
               
            }
        }
        [RelayCommand]
        private async Task LoadQueueAsync()
        {

            if (string.IsNullOrWhiteSpace(FilePath))
            {
                MessageBox.Show("Please enter a valid download URL.");
                return;
            }


            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                Uri downloadUri = new Uri(FilePath);
                string fileName = Path.GetFileName(downloadUri.LocalPath);
                FileName = fileName;
               // await _downloadService.GetFileStream(_httpClient, downloadUri.AbsoluteUri, cancellationTokenSource.Token);
                long? byteSize = await _httpClient.GetFileSizeAsync(downloadUri);
                string size = FormatBytes(byteSize ?? 0);
                DownloadItem download = new DownloadItem(
                    downloadUri,
                    fileName,
                    0,
                    size,
                    byteSize,
                    "0",
                    false,
                    "Pause",
                    "Ready",
                    0,
                    " ",
                    true,
                    cancellationTokenSource
                );

                DownloadQueue.Add(download);
            }
            catch (UriFormatException)
            {
                MessageBox.Show("Only Http or Https URI are allowed");
                return;
            }
            
        }



        //open computer directory to select destination path
        [RelayCommand]
        private void Browse()
        {
            OpenFolderDialog openFolderDialog = new();
            openFolderDialog.ShowDialog();
            var folderName = openFolderDialog.FolderName;
            DestinationPath = folderName;
        }

        [RelayCommand]
        private void Delete()
        {
            for (int i = DownloadQueue.Count - 1; i >= 0; i--)
            {
                if (DownloadQueue[i].IsSelected)
                {
                    DownloadQueue[i].CancellationTokenSource.Cancel();
                    DownloadQueue.RemoveAt(i);
                }
            }
            MessageBox.Show("Selected items cleared from queue");
        }


        //Download selected file in the queue to a destination directory
        [RelayCommand]
        private async Task Download(DownloadItem currentDownload)
        {
            //create a string from the current xaml binding
            string path = DestinationPath;

            //Check if the destination path text box is empty
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("The file path is empty, downloading to default download directory");
                path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }

            currentDownload.DestinationPath = path;
            //get httpresponse content body
            var fileStream = await _downloadService.GetFileStream(_httpClient, currentDownload.Address.AbsoluteUri, currentDownload.CancellationTokenSource.Token);
            //get httpresonse content headers
            var response = await _httpClient.GetAsync(currentDownload.Address, HttpCompletionOption.ResponseHeadersRead,currentDownload.CancellationTokenSource.Token);
            long? totalBytes = response.Content.Headers.ContentLength;
           
            var progressHandler = new Progress<double>(value => currentDownload.Progress = value);
            var downloadHandler = new Progress<string>(value => currentDownload.DownloadSpeed = value);

            await _downloadService.DownloadStream(
                _httpClient,
                fileStream,
                totalBytes,
                progressHandler,
                downloadHandler,
                currentDownload,
                currentDownload.CancellationTokenSource.Token
                );
           
        }

        [RelayCommand]
        private async Task PauseResume(DownloadItem currentDownload)
        {
                 
            if (currentDownload.Progress > 0)
            {

                currentDownload.CancellationTokenSource.Cancel();
                currentDownload.Status = "Resume";
            }
                 
            //resume the download
            else
            {
               
                var token = currentDownload.CancellationTokenSource.Token;
                await _downloadService.ResumeDownload(_httpClient, currentDownload, token);
                currentDownload.Status = "Pause";


            }
        }
    }



}

