using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using Google.Apis.Util.Store;

namespace GDNetwork
{
    public class GDUploader : GDNetworkStream<UploadableFile>
    {
        /// <summary>
        /// If modifying these scopes, delete your previously saved credentials.
        /// </summary>
        /// <param name="clientSecretJson"></param>
        /// <param name="userName"></param>
        /// <param name="appName"></param>
        /// <param name="scopes">If it is null, the service takes DriveReadonly and DriveFile right.</param>
        /// <param name="authenticationFile">If it is null, the constructor creates one into its own folder.</param>
        public GDUploader(string clientSecretJson, string userName, string appName, string[] scopes=null , FileDataStore authenticationFile = null) :
            base(clientSecretJson, userName, appName, scopes==null? new string[] { DriveService.Scope.DriveReadonly, DriveService.Scope.DriveFile}:scopes, authenticationFile) { }

        public GDUploader(DriveService driveService) : base(driveService) { }

        public override void Add(UploadableFile file)
        {
            streamableFiles.Enqueue(file);
            StreamdableBytes += new FileInfo(file.File).Length;
        }

        /// <summary>
        /// It starts uploading the files. It uses yield for return.
        /// </summary>
        public override IEnumerable<string> StartStream()
        {
            long byteStatus = 0;

            while (streamableFiles.Count != 0)
            {
                UploadableFile file = streamableFiles.Dequeue();
                string parentId = CreateGDPath(file.GDPath);

                Google.Apis.Drive.v3.Data.File gDFile = new Google.Apis.Drive.v3.Data.File();
                gDFile.Name = GetFileName(file.File);
                gDFile.Parents = new List<string>();
                gDFile.Parents.Add(parentId);

                FilesResource.CreateMediaUpload request;
                using (var stream = new FileStream(file.File, FileMode.Open))
                {
                    request = Service.Files.Create(gDFile, stream, GetMimeTypeByWindowsRegistry(file.File));
                    request.ProgressChanged += (IUploadProgress progress) =>
                    {
                        switch (progress.Status)
                        {
                            case UploadStatus.Uploading:
                                {
                                    CallStreamStatusEvent(byteStatus + progress.BytesSent);
                                    break;
                                }
                            case UploadStatus.Completed:
                                {
                                    StreamdableBytes -= progress.BytesSent;
                                    byteStatus += progress.BytesSent;

                                    CallStreamStatusEvent(byteStatus);
                                    break;
                                }
                            case UploadStatus.Failed:
                                {
                                    throw new Exception("Failed to upload file");
                                }
                        }
                    };
                    request.Upload();
                }

                yield return file.File;
            }
        }

        /// <summary>
        /// The folders are created on Google Drive, which do not exist.
        /// </summary>
        /// <param name="gDPath"></param>
        /// <returns>The last folder id</returns>
        private string CreateGDPath(string gDPath)
        {
            string parentId = ConvertGDFolderToGDParentId(gDPath);

            if (parentId != null)
                return parentId;

            Queue<string> folders = new Queue<string>(gDPath.Split('\\'));
            folders.Dequeue();
            parentId = folderIds["root"];
            string path = "";

            while (folders.Count != 0)
            {
                string folder = folders.Dequeue();
                path += "\\" + folder;

                string newParentId = ConvertGDFolderToGDParentId(path);

                if (newParentId == null)
                {
                    Google.Apis.Drive.v3.Data.File gDFolder = new Google.Apis.Drive.v3.Data.File();
                    gDFolder.Name = folder;
                    gDFolder.MimeType = "application/vnd.google-apps.folder";
                    gDFolder.Parents = new List<string>();
                    gDFolder.Parents.Add(parentId);

                    FilesResource.CreateRequest request = Service.Files.Create(gDFolder);
                    request.Fields = "id";

                    var result = request.Execute();

                    newParentId = result.Id;
                    folderIds.Add(path, newParentId);
                }

                parentId = newParentId;
            }

            return parentId;
        }

        private string GetMimeTypeByWindowsRegistry(string fileNameOrExtension)
        {
            string mimeType = "application/unknown";
            string ext = (fileNameOrExtension.Contains(".")) ? System.IO.Path.GetExtension(fileNameOrExtension).ToLower() : "." + fileNameOrExtension;
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);

            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();

            return mimeType;
        }
    }
}
