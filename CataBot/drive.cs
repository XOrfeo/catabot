using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Drive.v3;
using Google.Apis.Download;
namespace CataBot
{
    class drive
    {
        // Find the current working directory
        private static string location = AppDomain.CurrentDomain.BaseDirectory;
        public static List<Google.Apis.Drive.v3.Data.File> GetFiles(DriveService driveService, string[] extensions)
        {
            // Initialise a list of files.
            List<Google.Apis.Drive.v3.Data.File> Files = new List<Google.Apis.Drive.v3.Data.File>();
            // Define parameters of request.
            FilesResource.ListRequest listRequest = driveService.Files.List();
            listRequest.PageSize = 1000;
            listRequest.Fields = "nextPageToken, files(id, name)";
            // Pull file list.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
            // If there are some files in the GDrive.
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    // And the file is of the correct type.
                    if (extensions.Any(s => file.Name.Contains(s)))
                    {
                        //Add their names to the list.
                        Files.Add(file);
                    }
                }
            }
            return Files;
        }
        public static void syncFiles(DriveService driveService, List<Google.Apis.Drive.v3.Data.File> list, string syncLocation, Discord.Channel txtchnl)
        {
            // Initialise a list to store which files need downloading.
            List<Google.Apis.Drive.v3.Data.File> toSync = new List<Google.Apis.Drive.v3.Data.File>();
            // Store what files are present locally.
            var localFiles = Directory.GetFiles(location + syncLocation);
            // Cycle through all files, If there isn't a local copy, add it to the list.
            foreach (Google.Apis.Drive.v3.Data.File file in list)
            {
                if (!localFiles.Contains(location + syncLocation + file.Name))
                {
                    toSync.Add(file);
                }
            }
            // Tell the channel how many files are to be downloaded.
            txtchnl.SendMessage($"Downloading {toSync.Count} files.");
            // For each file to be downloaded, download it.
            foreach (Google.Apis.Drive.v3.Data.File file in toSync)
            {
                if (!Directory.GetFiles(location + syncLocation).Contains(location + syncLocation + file.Name))
                {
                    DownloadFile(driveService, file, syncLocation + file.Name);
                }
            }
        }
        public static void DownloadFile(DriveService driveService, Google.Apis.Drive.v3.Data.File file, string saveTo)
        {
            // Select the file in the Google Drive
            var request = driveService.Files.Get(file.Id);
            // Initialise the datastream
            var stream = new MemoryStream();
            // Add a handler which will be notified on progress changes.
            request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    // Setup cases to cover each possibility during download (ongoing/complete/failed).
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            // If download completes, save it to the file.
                            SaveStream(stream, saveTo);
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }
            };
            // Download the file.
            request.Download(stream);
        }
        private static void SaveStream(MemoryStream stream, string saveTo)
        {
            using (FileStream file = new FileStream(location + saveTo, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(file);
            }
        }
    }
}