using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Util.Store;

namespace GDNetwork
{
    abstract public class GDSynchronizer : GDConnection
    {
        protected Queue<string> deletableFiles;
        protected HashSet<string> exceptionLocalFiles;
        protected int maxRequestPageSize = 0;
        /// <summary>
        /// It is between 1 and 1000 or 0 (this is infinite).
        /// This is a maximum limit to how many files and folders it can find on a particular folder.
        /// If it is not infinite, it is faster.
        /// Default value is 0.
        /// </summary>
        public int MaxRequestPageSize
        {
            get { return maxRequestPageSize; }
            set
            {
                if (value < 0 || value > 1000)
                    throw new Exception("InvalidMaxRequestPageSize: the value is between 1 and 1000 or 0 (this is infinite).");

                maxRequestPageSize = value;
            }
        }

        /// <summary>
        /// It sends the size of downloaded/uploaded bytes.
        /// </summary>
        public event EventHandler<StreamByteEventArgs> CurrentSizeEvent;

        /// <summary>
        /// It sends the full size of downloadable/uploadable bytes.
        /// </summary>
        public event EventHandler<StreamByteEventArgs> FullSizeEvent;

        /// <summary>
        /// If modifying these scopes, delete your previously saved credentials.
        /// </summary>
        /// <param name="clientSecretJson"></param>
        /// <param name="userName"></param>
        /// <param name="appName"></param>
        /// <param name="scopes">If it is null, the service takes Drive right.</param>
        /// <param name="authenticationFile">If it is null, the constructor creates one into its own folder.</param>
        public GDSynchronizer(string clientSecretJson, string userName, string appName, string[] scopes = null, FileDataStore authenticationFile = null) :
            base(clientSecretJson, userName, appName, scopes == null ? new string[] { DriveService.Scope.Drive } : scopes, authenticationFile)
        { }

        public GDSynchronizer(DriveService driveService) : base(driveService) { }

        protected override void Init()
        {
            deletableFiles = new Queue<string>();
            exceptionLocalFiles = new HashSet<string>();
        }

        public void AddExceptionLocalFile(string path)
        {
            exceptionLocalFiles.Add(path);
        }

        public void RemoveExceptionLocalFile(string path)
        {
            exceptionLocalFiles.Remove(path);
        }

        public void RemoveAllExceptionLocalFiles()
        {
            exceptionLocalFiles.Clear();
        }

        protected void AddToDeletableFile(string file)
        {
            deletableFiles.Enqueue(file);
        }

        protected void AddToDeletableFile(List<string> files)
        {
            foreach (string file in files)
                AddToDeletableFile(file);
        }

        abstract protected void DeleteFiles();

        abstract public void Sync(string driveFolder, string localFolder, string password);

        /// <summary>
        /// It is useable on local diractory.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onlyZipFiles">If it is true, it returns the zip files and folders.</param>
        /// <returns></returns>
        protected List<string> getLocalFiles(string path, bool onlyZipFiles = false)
        {
            List<string> files = new List<string>();

            if (Directory.Exists(path))
            {
                if (onlyZipFiles)
                    files.AddRange(Directory.GetFiles(path, "*.zip"));
                else
                    files.AddRange(Directory.GetFiles(path));

                files.AddRange(Directory.GetDirectories(path));
            }

            return files;
        }

        /// <summary>
        /// It is useable on Google Drive.
        /// </summary>
        protected IEnumerable<Google.Apis.Drive.v3.Data.File> getGDFiles(string parentId)
        {
            if (string.IsNullOrEmpty(parentId))
                return new List<Google.Apis.Drive.v3.Data.File>();

            IEnumerable<Google.Apis.Drive.v3.Data.File> files;

            FilesResource.ListRequest listRequest = Service.Files.List();
            listRequest.Q = "trashed=false and '" + parentId + "' in parents";
            listRequest.Fields = "*";

            if (MaxRequestPageSize == 0)
            {
                Google.Apis.Requests.PageStreamer<Google.Apis.Drive.v3.Data.File, FilesResource.ListRequest, FileList, string> pageStreamer =
                new Google.Apis.Requests.PageStreamer<Google.Apis.Drive.v3.Data.File, FilesResource.ListRequest, FileList, string>(
                                                   (req, token) => listRequest.PageToken = token,
                                                   response => response.NextPageToken,
                                                   response => response.Files);
                files = pageStreamer.Fetch(listRequest);
            }
            else
            {
                listRequest.PageSize = MaxRequestPageSize;
                files = listRequest.Execute().Files;
            }

            return files;
        }

        protected void DeleteZipFiles(string localFolder)
        {
            List<string> files = getLocalFiles(localFolder, true);
            foreach (string path in files)
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                else
                    DeleteZipFiles(path);
            }
        }

        /// <summary>
        /// You can catch a StreamByteEvent and call it with CurrentSizeEvent.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected void CatchStatus(object source, StreamByteEventArgs e)
        {
            if (CurrentSizeEvent != null)
                CurrentSizeEvent(this, e);
        }

        /// <summary>
        /// It calls the FullSizeEvent.
        /// </summary>
        /// <param name="byteSize"></param>
        protected void CallFullSizeEvent(long byteSize)
        {
            if (FullSizeEvent != null)
                FullSizeEvent(this, new StreamByteEventArgs(byteSize));
        }

    }
}
