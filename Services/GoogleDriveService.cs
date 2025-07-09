using DownloadManagerApp.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace DownloadManagerApp
{
    public class GoogleDriveService
    {
        

        public async Task<UserCredential> Login(string googleClientId, string googleClientSecret)
        {
            ClientSecrets secrets = new ClientSecrets()
            {
                ClientId = googleClientId,
                ClientSecret = googleClientSecret
            };

            return GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, new[] { "https://www.googleapis.com/auth/drive.file" }, "user", CancellationToken.None).Result;
        }


        public  Task<FileList> ListFromDrive(UserCredential _credential)
        {
            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "DownloadManagerApp"
            });

            var listFileRequest = service.Files.List().ExecuteAsync();

            return listFileRequest;

        }

        public async Task<string> UploadToDrive(UserCredential _credential, string filePath, DownloadItem downloadItem)
        {

            try
            {

                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = "DownloadManagerApp"
                });



                //check if file that was just downloaded is either a file or a folder of files
                if (Directory.Exists(filePath))
                {

                    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = downloadItem.FileName,
                        MimeType = "application/vnd.google-apps.folder",

                    };

                    var folderRequest = service.Files.Create(fileMetadata);
                    folderRequest.Fields = "id";
                    var folder = await folderRequest.ExecuteAsync();

                    FilesResource.CreateMediaUpload request;

                    // Create a new file on drive.
                    using (var stream = new FileStream(filePath,
                               FileMode.Open))
                    {
                        // Create a new file, with metadata and stream.
                        request = service.Files.Create(
                            fileMetadata, stream, "application/octet-stream");
                        request.Fields = "id";
                        await request.UploadAsync();
                    }

                    var file = request.ResponseBody;
                    // Prints the uploaded file id.
                    MessageBox.Show($"Upload successful!\nGoogle Drive File ID: {file.Id}");
                    return file.Id;

                }
            }

            catch (Exception e)
            {
                // TODO(developer) - handle error appropriately
                if (e is AggregateException)
                {
                    MessageBox.Show("Credential not found");

                }
                else if (e is FileNotFoundException)
                {
                    MessageBox.Show("File not found");

                }
                else
                {
                    throw;
                }
            }
            return null;
        }
    }
}
           




        
    

