
using DownloadManagerApp.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using static Google.Apis.Requests.BatchRequest;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace DownloadManagerApp.Services
{
    class DownloadService
    {
        //observe for when there is no network connection and raise an event message
        public delegate void NetworkAvailabilityChangedEventHandler(object? sender, NetworkAvailabilityEventArgs e);
        public event NetworkAvailabilityChangedEventHandler? NetworkChanged;

        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;

        public bool isAbleToDownload;
        
        public DownloadService(bool isAbleToDownload)
        {
            this.isAbleToDownload = isAbleToDownload;
        }

        public void OnDownloadCompleted(DownloadCompletedEventArgs e)
        {
            DownloadCompleted?.Invoke(this, e);
        }

        public class DownloadCompletedEventArgs : EventArgs
        {
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public string ConnectionStatus { get; set; }
            public DownloadItem DownloadItem { get; set; } = default!;
        }
        //Gets the files to be downloaded and returns them as a file stream
        public async Task<Stream> GetFileStream(HttpClient httpClient, string address,CancellationToken token)
        {
            try
            {
                //sends httpResponse GET then checks content of the stream header before reading body
                var response = await httpClient.GetAsync(
                    address, 
                    HttpCompletionOption.ResponseHeadersRead,
                    token
                    );
                //checks if the response code is a success and throws an exception if not
                response.EnsureSuccessStatusCode();
                //called after sending the request to get body
                return await response.Content.ReadAsStreamAsync();
                

            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Could not connect to HttpClient");
                return Stream.Null;
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Download Cancelled due to timeout");
                return Stream.Null;
            }
            catch (Exception)
            {
                return Stream.Null;
            }
            

            
 
        }

        // copy from fileStream to a new file on the directory
        public async Task DownloadStream(HttpClient _httpClient,
            Stream fileStream, 
            long? totalBytes, 
            IProgress<double> progressCallBack, 
            IProgress<string> downloadSpeedCallBack, 
            DownloadItem currentDownload, 
            CancellationToken cancellationToken)
        {

            string fileName = currentDownload.FileName;
            var downloadPath = currentDownload.DestinationPath;
            var fullPath = Path.Combine(downloadPath, fileName);

            long bytesReadSoFar = currentDownload.TotalBytesRead;


            Directory.CreateDirectory(downloadPath);


            //if a file exists inside that path and the length is equal current download
            if (File.Exists(fullPath))
            {

                var existingFile = new FileInfo(fullPath);

                // If the file size is equal to totalBytes, consider it completed
                if (existingFile.Length == totalBytes && existingFile.Exists)
                {

                    var result = MessageBox.Show(
                 "A file with the same name already exists in this location. Would you like to replace the existing file?",
                 "File Found",
                 MessageBoxButton.YesNo,
                 MessageBoxImage.Question);

                    //overwrite existing file
                    if (result == MessageBoxResult.Yes)
                    {
                        File.Delete(fullPath);

                    }
                    //create a new instance with appended filename
                    else if (result == MessageBoxResult.No)
                    {

                        // Generate a unique filename
                        string name = Path.GetFileNameWithoutExtension(fileName);
                        string extension = Path.GetExtension(fileName);
                        int count = 1;


                        do
                        {
                            fileName = $"{name}({count++}){extension}";
                            fullPath = Path.Combine(downloadPath, fileName);
                        } while (File.Exists(fullPath));
                    }

                }
                else if (existingFile.Exists)
                {
                     var resumeResult = MessageBox.Show(
                      "A partial file exists. Resume download?",
                      "Resume Download",
                      MessageBoxButton.YesNo,
                      MessageBoxImage.Question);

                    if (resumeResult == MessageBoxResult.Yes)
                    {
                        
                        await ResumeDownload(_httpClient, currentDownload, cancellationToken);
                        return; 
                    }
                    else
                    {
                        File.Delete(fullPath); // delete partial file and start fresh
                        bytesReadSoFar = 0;
                        currentDownload.TotalBytesRead = 0;
                    }

                }
            }


                // Try to seek if supported
                try { fileStream.Seek(bytesReadSoFar, SeekOrigin.Begin); }
                catch (NotSupportedException) { }

            
                //define chunks we want to load in at a time to read
                byte[] buffer = new byte[81920];
                int bytesRead;
                double downloadSpeed;
                DateTime startTime = DateTime.Now;
                long lastBytesRead = currentDownload.TotalBytesRead;

                

                
                using (FileStream outputFileStream = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
                {
                   
                    try
                    {
                        //read the first 80 kb of file, with 0 offset, advancing by the read length
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                         

                            cancellationToken.ThrowIfCancellationRequested();

                            await outputFileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                            currentDownload.TotalBytesRead += bytesRead;

                            if (totalBytes.HasValue && totalBytes > 0)
                            {
                                double progressPercentage = (double)(currentDownload.TotalBytesRead * 100.0 / totalBytes.Value);
                                progressCallBack.Report(progressPercentage);

                            }

                            if ((DateTime.Now - startTime).TotalMilliseconds >= 1000)
                            {
                                downloadSpeed = (currentDownload.TotalBytesRead * 8) / (DateTime.Now - startTime).TotalSeconds / 1000000;
                                downloadSpeedCallBack.Report($"{downloadSpeed:F2} Mbps");
                                lastBytesRead = currentDownload.TotalBytesRead;
                            }

                        }
                        progressCallBack.Report(100.0);
                        //when download is finished, raise an event to begin upload to selected drive if it is enabled
                        OnDownloadCompleted(new DownloadCompletedEventArgs
                        {
                            FileName = fileName,
                            FilePath = fullPath,
                            DownloadItem = currentDownload,
                            ConnectionStatus = "Completed"

                        });

                       

                    }
                    catch (HttpRequestException)
                    {
                        currentDownload.ConnectionStatus = "Connection Lost";
                        await SaveProgress(currentDownload,downloadPath);

                    }
                    catch (OperationCanceledException)
                    {

                        currentDownload.ConnectionStatus = "Download Canceled";
                        await SaveProgress(currentDownload, downloadPath);

                    }
                }
            }
        
        

        

        public async Task SaveProgress(DownloadItem currentItem,string destinationFolder)
        {
            try {
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                string savePath = Path.Combine(destinationFolder, "saveProgress.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                FileStream createStream = File.Create(savePath);
                await JsonSerializer.SerializeAsync(createStream, currentItem, options);
                await createStream.FlushAsync();


            }
            catch (Exception)
            {
                Console.WriteLine("Failed to save Progress");
            }
      
        }

        //read saved metadata from save progress
        public async Task<DownloadItem> LoadProgress(string filePath)
        {
            FileStream readStream = File.OpenRead(filePath);
            var loadedJson = await JsonSerializer.DeserializeAsync<DownloadItem>(readStream);
            return loadedJson;


        }


        //restart download
        public async Task ResumeDownload(HttpClient _httpClient, DownloadItem currentItem, CancellationToken cancellationToken)
        {
            long existingLength = 0;
          
            
            string filePath = Path.Combine(currentItem.DestinationPath, Path.GetFileName(currentItem.FileName));

            if (File.Exists(filePath))
            {
                existingLength = new FileInfo(filePath).Length;
                currentItem.TotalBytesRead = existingLength;

                var request = new HttpRequestMessage(HttpMethod.Get, currentItem.Address);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                //check if the http server allows partial downloads
                if (response.StatusCode != HttpStatusCode.PartialContent && existingLength > 0)
                {
                    MessageBox.Show("Server does not support partial downloads, restating download");
                    
                }

                currentItem.ConnectionStatus = "Downloading";

                var stream = await response.Content.ReadAsStreamAsync();
                long? totalLength = existingLength + response.Content.Headers.ContentLength;

                // Create callback handlers (optional if you already have them)
                var progress = new Progress<double>(value => currentItem.Progress = value);
                var speed = new Progress<string>(value => currentItem.DownloadSpeed = value);

                // Resume download using existing stream
                await DownloadStream(
                    _httpClient,
                    stream,
                    totalLength,
                    progress,
                    speed,
                    currentItem,
                    cancellationToken
                );
            }
            

        }



        public static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] units = { "Bytes", "KB", "MB", "GB", "TB" };

            double size = bytes;
            int unitIndex = 0;

            while (size >= scale && unitIndex < units.Length - 1)
            {
                size /= scale;
                unitIndex++;
            }

            return $"{size:F2}{units[unitIndex]}";
        }

    }

    public static class HttpResponseMessageExtensions
    {
        public static async Task<long?> GetFileSizeAsync(this HttpClient client, Uri downloadUri)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, downloadUri);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return response.Content.Headers.ContentLength;
            }
            catch (HttpRequestException)
            {

                MessageBox.Show("Could not connect to httpClient");
                return null;
            }
        }
    }
}
