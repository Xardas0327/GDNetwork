using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using Ionic.Zip;

namespace GDNetwork
{
    public class GDUpSynchronizer : GDSynchronizer
    {
        private string zipPassword;

        /// <summary>
        /// If modifying these scopes, delete your previously saved credentials.
        /// </summary>
        /// <param name="clientSecretJson"></param>
        /// <param name="userName"></param>
        /// <param name="appName"></param>
        /// <param name="scopes">If it is null, the service takes Drive right.</param>
        /// <param name="authenticationFile">If it is null, the constructor creates one into its own folder.</param>
        public GDUpSynchronizer(string clientSecretJson, string userName, string appName, string[] scopes = null, FileDataStore authenticationFile = null) :
            base(clientSecretJson, userName, appName, scopes == null ? new string[] { DriveService.Scope.Drive } : scopes, authenticationFile)
        { }

        public GDUpSynchronizer(DriveService driveService) : base(driveService) { }

        /// <summary>
        /// It deletes the unnecessary files on Google Drive.
        /// </summary>
        protected override void DeleteFiles()
        {
            foreach (string fileId in deletableFiles)
                Service.Files.Delete(fileId).Execute();
        }

        /// <summary>
        /// It uploaded the new files and deletes the unnecessary files on Google Drive.
        /// Those files do not uploaded, which were added to ExceptionFiles. 
        /// </summary>
        /// <param name="driveFolder"></param>
        /// <param name="localFolder"></param>
        /// <param name="password">Password of zip files.</param>
        public override void Sync(string driveFolder, string localFolder, string password)
        {
            if (driveFolder==null)
                throw new ArgumentNullException("driveFolder");
            if (string.IsNullOrEmpty(localFolder))
                throw new ArgumentNullException("localFolder");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            driveFolder += "\\" + GetFileName(localFolder);
            zipPassword = password;

            GDUploader uploader = new GDUploader(Service);
            uploader.StreamStatusEvent += CatchStatus;

            try
            {
                CollectUpSynchronizerData(uploader, localFolder, driveFolder);
                CallFullSizeEvent(uploader.StreamdableBytes);

                DeleteFiles();

                foreach (string file in uploader.StartStream())
                    System.IO.File.Delete(file);
            }
            catch (Exception e)
            {
                DeleteZipFiles(localFolder);
                throw e;
            }
        }

        /// <summary>
        /// It start synchronizing the selected files or/and folders to the Google Drive.
        /// The selected folders is synchronized as the normal folder sync.
        /// When it upload the selected files, it check the ExceptionFiles only and
        /// it do not check the selected files are new or not.
        /// </summary>
        /// <param name="driveFolder"></param>
        /// <param name="localFolder"></param>
        /// <param name="password">Password of zip files.</param>
        public void Sync(string driveFolder, string localFolder, IEnumerable<string> localFiles, string password)
        {
            if (string.IsNullOrEmpty(driveFolder))
                throw new ArgumentNullException("driveFolder");
            if (string.IsNullOrEmpty(localFolder))
                throw new ArgumentNullException("localFolder");
            if (localFiles==null)
                throw new ArgumentNullException("localFiles");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            driveFolder += "\\" + GetFileName(localFolder);
            zipPassword = password;

            GDUploader uploader = new GDUploader(Service);
            uploader.StreamStatusEvent += CatchStatus;

            try
            {
                CollectUpSynchronizerData(uploader, localFolder, localFiles, driveFolder);

                CallFullSizeEvent(uploader.StreamdableBytes);

                DeleteFiles();

                foreach (string file in uploader.StartStream())
                    System.IO.File.Delete(file);
            }
            catch (Exception e)
            {
                DeleteZipFiles(localFolder);
                throw e;
            }
        }

        /// <summary>
        /// It collect the uploadable files into param  GDUploader variable and
        /// it collect the unnecessary files into global deletableFiles variable.
        /// </summary>
        /// <param name="uploader"></param>
        /// <param name="localFolder"></param>
        /// <param name="driveFolder"></param>
        private void CollectUpSynchronizerData(GDUploader uploader, string localFolder, string driveFolder)
        {
            string parentId = ConvertGDFolderToGDParentId(driveFolder);
            List<Google.Apis.Drive.v3.Data.File> unnecessaryFiles = getGDFiles(parentId).ToList();
            List<string> files = getLocalFiles(localFolder);

            foreach (string path in files)
            {
                if(!exceptionLocalFiles.Contains(path))
                {
                    string fileName = GetFileName(path);
                    Google.Apis.Drive.v3.Data.File gDFile;

                    if (Directory.Exists(path))
                    {
                        gDFile = searchGDFile(unnecessaryFiles, fileName);
                        if (gDFile != null && gDFile.MimeType == "application/vnd.google-apps.folder")
                            unnecessaryFiles.Remove(gDFile);

                        CollectUpSynchronizerData(uploader, path, driveFolder + "\\" + fileName);
                    }
                    else
                    {
                        gDFile = searchGDFile(unnecessaryFiles, fileName + ".zip");

                        if (gDFile != null && System.IO.File.GetLastWriteTime(path) <= gDFile.ModifiedTime.Value)
                            unnecessaryFiles.Remove(gDFile);
                        else
                        {
                            string zipFile = path + ".zip";
                            using (ZipFile zip = new ZipFile(zipFile))
                            {
                                zip.Password = zipPassword;
                                zip.AddFile(path, "");
                                zip.Save();

                                uploader.Add(new UploadableFile(zipFile, driveFolder));
                            }
                        }
                    }
                }
            }

            foreach (Google.Apis.Drive.v3.Data.File unnecessaryFile in unnecessaryFiles)
                AddToDeletableFile(unnecessaryFile.Id);
        }

        /// <summary>
        /// It collect the uploadable files into param  GDUploader variable and
        /// it collect the unnecessary files into global deletableFiles variable.
        /// </summary>
        /// <param name="uploader"></param>
        /// <param name="localFolder"></param>
        /// <param name="driveFolder"></param>
        private void CollectUpSynchronizerData(GDUploader uploader, string localFolder, IEnumerable<string> localFiles, string driveFolder)
        {
            foreach (string path in localFiles)
            {
                if (!exceptionLocalFiles.Contains(path) && path.StartsWith(localFolder))
                {
                    string currentDrivePath = driveFolder + path.Substring(localFolder.Length);

                    if (Directory.Exists(path))
                        CollectUpSynchronizerData(uploader, path, currentDrivePath);
                    else
                    {
                        string currentDriveFolder = currentDrivePath.Substring(0, currentDrivePath.LastIndexOf('\\'));
                        string parentId = ConvertGDFolderToGDParentId(currentDriveFolder);
                        string fileName = GetFileName(path);

                        if (parentId!=null)
                        {
                            string fileId = GetGDFileId(fileName + ".zip", parentId);

                            if (fileId != null)
                                AddToDeletableFile(fileId);
                        }

                        string zipFile = path + ".zip";
                        using (ZipFile zip = new ZipFile(zipFile))
                        {
                            zip.Password = zipPassword;
                            zip.AddFile(path, "");
                            zip.Save();

                            uploader.Add(new UploadableFile(zipFile, currentDriveFolder));
                        }
                    }
                }
            }
        }

        private Google.Apis.Drive.v3.Data.File searchGDFile(List<Google.Apis.Drive.v3.Data.File> gDFiles, string fileName)
        {
            foreach (Google.Apis.Drive.v3.Data.File gDFile in gDFiles)
            {
                if (gDFile.Name == fileName)
                    return gDFile;
            }
            return null;
        }

    }
}
