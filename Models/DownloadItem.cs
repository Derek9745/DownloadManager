using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadManagerApp.Models
{
    public partial class DownloadItem : ObservableObject
    {
        [ObservableProperty]
        private Uri address;

        [ObservableProperty]
        private string fileName;

        [ObservableProperty]
        private string destinationPath;

        [ObservableProperty]
        private double progress;

        [ObservableProperty]
        private string size;

        [ObservableProperty]
        private long? byteSize;

        [ObservableProperty]
        private string downloadSpeed;

        [ObservableProperty]
        private string status;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string connectionStatus;

        [ObservableProperty]
        private long totalBytesRead = 0;

        [ObservableProperty]
        private bool isCompleted = true;

        [ObservableProperty]
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();


        public DownloadItem(Uri address, string fileName, double progress, string size, long? byteSize,string downloadSpeed, bool isSelected, string status, string connectionStatus, long totalBytesRead, string destinationPath, bool isCompleted, CancellationTokenSource cancellationTokenSource)
        {
            this.address = address;
            this.fileName = fileName;
            this.progress = progress;
            this.size = size;
            this.byteSize = byteSize;
            this.downloadSpeed = downloadSpeed;
            this.isSelected = isSelected;
            this.status = status;
            this.connectionStatus = connectionStatus;
            this.totalBytesRead = totalBytesRead;
            this.destinationPath = destinationPath;
            this.isCompleted = isCompleted;
            this.cancellationTokenSource = cancellationTokenSource;
        }

    }





}
