using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using Ionic.Zip;

namespace GDNetwork
{
    public class GDDownSynchronizer : GDSynchronizer
    {
        /// <summary>
        /// If modifying these scopes, delete your previously saved credentials.
        /// </summary>
        /// <param name="clientSecretJson"></param>
        /// <param name="userName"></param>
        /// <param name="appName"></param>
        /// <param name="scopes">If it is null, the service takes DriveReadonly right.</param>
        /// <param name="authenticationFile">If it is null, the constructor creates one into its own folder.</param>
        public GDDownSynchronizer(string clientSecretJson, string userName, string appName, string[] scopes = null, FileDataStore authenticationFile = null) :
            base(clientSecretJson, userName, appName, scopes == null ? new string[] { DriveService.Scope.DriveReadonly } : scopes, authenticationFile) { }

        public GDDownSynchronizer(DriveService driveService) : base(driveService) { }

        /// <summary>
        /// It deletes from local folder, which is not contained to ExceptionFiles.
        /// </summary>
        protected override void DeleteFiles()
        {
            while (deletableFiles.Count != 0)
            {
                string path = deletableFiles.Dequeue();

                if (!exceptionLocalFiles.Contains(path))
                {
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                    else
                    {
                        bool isExceptionParent = false;
                        foreach (string exceptionFile in exceptionLocalFiles)
                        {
                            DirectoryInfo dinfo = Directory.GetParent(exceptionFile);

                            while (dinfo != null && !isExceptionParent)
                            {
                                isExceptionParent = path == dinfo.FullName;
                                dinfo = dinfo.Parent;
                            }
                        }

                        if (isExceptionParent)
                        {
                            List<string> files = getLocalFiles(path);
                            AddToDeletableFile(files);
                        }
                        else
                            Directory.Delete(path, true);
                    }
                }
            }
        }

        /// <summary>
        /// It downloads the files, which is needed and deletes the unnecessary files.
        /// It doesn't delete those, which were added to ExceptionFiles.
        /// </summary>
        /// <param name="localFolder"></param>
        /// <param name="driveFolder"></param>
        /// <param name="password">Password of zip files.</param>
        public override void Sync(string localFolder, string driveFolder, string password)
        {
            if (string.IsNullOrEmpty(localFolder))
                throw new ArgumentNullException("localFolder");
            if (string.IsNullOrEmpty(driveFolder))
                throw new ArgumentNullException("driveFolder");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            string parentId = ConvertGDFolderToGDParentId(driveFolder);
            if (parentId == null)
                throw new Exception("NotExistPath");

            localFolder += "\\" + GetFileName(driveFolder);

            GDDownloader downloader = new GDDownloader(Service);
            downloader.StreamStatusEvent += CatchStatus;

            CollectDownSynchronizerData(downloader, localFolder, parentId);

            CallFullSizeEvent(downloader.StreamdableBytes);

            DeleteFiles();

            try
            {
                foreach (string file in downloader.StartStream())
                    UnZipFile(file, password);
            }
            catch(Exception e)
            {
                DeleteZipFiles(localFolder);
                throw e;
            }
        }

        /// <summary>
        /// It collect the downloadable files into param  GDDownloader variable and
        /// it collect the unnecessary files into global deletableFiles variable.
        /// </summary>
        /// <param name="downloader"></param>
        /// <param name="localFolder"></param>
        /// <param name="parentId"></param>
        private void CollectDownSynchronizerData(GDDownloader downloader, string localFolder, string parentId)
        {
            List<string> unnecessaryFiles = getLocalFiles(localFolder);
            IEnumerable<Google.Apis.Drive.v3.Data.File> files = getGDFiles(parentId);

            foreach (Google.Apis.Drive.v3.Data.File file in files)
            {
                if (file.MimeType == "application/vnd.google-apps.folder")
                {
                    string folderPath = localFolder + "\\" + file.Name;
                    unnecessaryFiles.Remove(folderPath);
                    CollectDownSynchronizerData(downloader, folderPath, file.Id);
                }
                else
                {
                    if (file.FileExtension == "zip")
                    {
                        string fileName = localFolder + "\\" + file.Name.Substring(0, file.Name.Length - 4);

                        if (unnecessaryFiles.Contains(fileName) && System.IO.File.GetLastWriteTime(fileName) >= file.ModifiedTime.Value)
                            unnecessaryFiles.Remove(fileName);
                        else
                            downloader.Add(new DownloadableFile(file, localFolder));
                    }
                }
            }

            AddToDeletableFile(unnecessaryFiles);
        }

        /// <summary>
        /// It checks for there are new files on Google Drive.
        /// </summary>
        /// <param name="localFolder"></param>
        /// <param name="driveFolder"></param>
        /// <returns></returns>
        public bool CheckChanges(string localFolder, string driveFolder)
        {
            if (string.IsNullOrEmpty(localFolder))
                throw new ArgumentNullException("localFolder");
            if (string.IsNullOrEmpty(driveFolder))
                throw new ArgumentNullException("driveFolder");

            string parentId = ConvertGDFolderToGDParentId(driveFolder);
            if (parentId == null)
                throw new Exception("NotExistPath");

            localFolder += "\\" + GetFileName(driveFolder);

            return CheckFiles(localFolder, parentId);
        }

        private bool CheckFiles(string localFolder, string parentId)
        {
            List<string> unnecessaryFiles = getLocalFiles(localFolder);
            IEnumerable<Google.Apis.Drive.v3.Data.File> files = getGDFiles(parentId);

            foreach (Google.Apis.Drive.v3.Data.File file in files)
            {
                if (file.MimeType == "application/vnd.google-apps.folder")
                {
                    string folderPath = localFolder + "\\" + file.Name;
                    unnecessaryFiles.Remove(folderPath);
                    bool haveChange = CheckFiles(folderPath, file.Id);

                    if (haveChange)
                        return true;
                }
                else
                {
                    if (file.FileExtension == "zip")
                    {
                        string fileName = localFolder + "\\" + file.Name.Substring(0, file.Name.Length - 4);

                        if (unnecessaryFiles.Contains(fileName) && System.IO.File.GetLastWriteTime(fileName) >= file.ModifiedTime.Value)
                            unnecessaryFiles.Remove(fileName);
                        else
                            return true;
                    }
                }
            }

            return unnecessaryFiles.Count != 0;
        }

        private void UnZipFile(string zipFile, string password)
        {
            int fileNameStart = zipFile.LastIndexOf('\\') + 1;
            string path = zipFile.Substring(0, fileNameStart);
            string unZipfile = zipFile.Substring(0, zipFile.Length - 4);


            ZipFile zip = ZipFile.Read(zipFile);
            try
            {
                zip[0].Extract(path);

                System.IO.File.Delete(unZipfile);
                throw new Exception("Zip File has not password.");
            }
            catch (BadPasswordException)
            {
                zip.Password = password;
                zip[0].Extract(path);

                System.IO.File.SetLastWriteTime(unZipfile, System.IO.File.GetLastWriteTime(path));
            }
            finally
            {
                zip.Dispose();
                System.IO.File.Delete(zipFile);
            }
        }
    }
}
