using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;

namespace GDNetwork
{
    public class GDDownloader : GDNetworkStream<DownloadableFile>
    {
        /// <summary>
        /// If modifying these scopes, delete your previously saved credentials.
        /// </summary>
        /// <param name="clientSecretJson"></param>
        /// <param name="userName"></param>
        /// <param name="appName"></param>
        /// <param name="scopes">If it is null, the service takes DriveReadonly rights.</param>
        /// <param name="authenticationFile">If it is null, the constructor creates one into its own folder.</param>
        public GDDownloader(string clientSecretJson, string userName, string appName, string[] scopes = null, FileDataStore authenticationFile = null)  :
            base(clientSecretJson, userName, appName, scopes == null ? new string[] { DriveService.Scope.DriveReadonly } : scopes, authenticationFile){}

        public GDDownloader(DriveService driveService) : base(driveService){}

        public override void Add(DownloadableFile file)
        {
            streamableFiles.Enqueue(file);

            if (file.GDFile.Size != null)
                StreamdableBytes += file.GDFile.Size.Value;
        }

        /// <summary>
        /// It starts downloading the files. It uses yield for return.
        /// </summary>
        public override IEnumerable<string> StartStream()
        {
            long byteStatus = 0;

            while (streamableFiles.Count != 0)
            {
                DownloadableFile file = streamableFiles.Dequeue();
                FilesResource.GetRequest request = Service.Files.Get(file.GDFile.Id);

                string filePath = "";

                using (MemoryStream stream = new MemoryStream())
                {
                    request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                {
                                    CallStreamStatusEvent(byteStatus + progress.BytesDownloaded);
                                    break;
                                }
                            case DownloadStatus.Completed:
                                {
                                    StreamdableBytes -= progress.BytesDownloaded;
                                    byteStatus += progress.BytesDownloaded;

                                    CallStreamStatusEvent(byteStatus);
                                    filePath =SaveFile(stream, file);
                                    break;
                                }
                            case DownloadStatus.Failed:
                                {
                                    throw new Exception("Failed to download file");
                                }
                        }
                    };
                    request.DownloadWithStatus(stream);
                }

                yield return filePath;
            }
        }

        private string SaveFile(MemoryStream stream, DownloadableFile file)
        {
            if (!Directory.Exists(file.Folder))
                Directory.CreateDirectory(file.Folder);

            string path = file.Folder + "\\" + file.GDFile.Name;

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(fs);
            }

            return path;
        }
    }
}
