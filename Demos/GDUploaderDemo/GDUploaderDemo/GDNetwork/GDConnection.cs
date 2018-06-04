using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace GDNetwork
{
    abstract public class GDConnection
    {
        public DriveService Service { get; protected set; }
        protected Dictionary<string, string> folderIds;

        /// <summary>
        /// If modifying these scopes, delete your previously saved credentials.
        /// </summary>
        /// <param name="clientSecretJson"></param>
        /// <param name="userName"></param>
        /// <param name="appName"></param>
        /// <param name="scopes"></param>
        /// <param name="authenticationFile">If it is null, the constructor create one into its own folder.</param>
        public GDConnection(string clientSecretJson, string userName, string appName, string[] scopes, FileDataStore authenticationFile = null)
        {
            try
            {
                if (string.IsNullOrEmpty(clientSecretJson))
                    throw new ArgumentNullException("clientSecretJson");
                if (string.IsNullOrEmpty(userName))
                    throw new ArgumentNullException("userName");
                if (string.IsNullOrEmpty(appName))
                    throw new ArgumentNullException("appName");
                if(scopes==null || scopes.Length==0)
                    throw new ArgumentNullException("scopes");

                UserCredential credential;

                using (MemoryStream stream = new MemoryStream())
                {
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(clientSecretJson);
                    writer.Flush();
                    stream.Position = 0;

                    if (authenticationFile == null)
                    {
                        string credPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "//Credentials";
                        authenticationFile = new FileDataStore(credPath, true);
                    }

                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
                                                                                 scopes,
                                                                                 userName,
                                                                                 CancellationToken.None,
                                                                                 authenticationFile).Result;
                }

                Service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = appName
                });

                folderIds = new Dictionary<string, string>();
                folderIds.Add("root", "root");
                Init();
            }
            catch (Exception ex)
            {
                throw new Exception("CreateDriveServiceFailed", ex);
            }
        }

        public GDConnection(DriveService driveService)
        {
            if (driveService == null)
                throw new ArgumentNullException("MissingDriveService");

            Service = driveService;
            folderIds = new Dictionary<string, string>();
            folderIds.Add("root", "root");

            Init();
        }

        /// <summary>
        /// It runs after contructor.
        /// </summary>
        protected virtual void Init()
        {

        }

        /// <summary>
        /// It is useable on Google Drive.
        /// </summary>
        /// <param name="gDPath"></param>
        /// <returns> If the folder does not exist, it returns with null.</returns>
        protected string ConvertGDFolderToGDParentId(string gDPath)
        {
            if (string.IsNullOrEmpty(gDPath))
                return folderIds["root"];

            if (folderIds.ContainsKey(gDPath))
                return folderIds[gDPath];

            Queue<string> folders = new Queue<string>(gDPath.Split('\\'));
            folders.Dequeue();
            string path = "";
            string parentId = folderIds["root"];

            while (folders.Count != 0)
            {
                string folder = folders.Dequeue();
                path += "\\" + folder;

                if (folderIds.ContainsKey(path))
                    parentId = folderIds[path];
                else
                {
                    parentId = GetGDFolderId(folder, parentId);
                    if (parentId == null)
                        return null;

                    folderIds.Add(path, parentId);
                }
            }

            return parentId;
        }

        /// <summary>
        /// It is useable on Google Drive.
        /// </summary>
        /// <param name="parentId">If it is empty, it looks for the folder in root directory</param>
        protected string GetGDFolderId(string folderName, string parentId = "")
        {
            if (string.IsNullOrEmpty(parentId))
                parentId = "root";

            FilesResource.ListRequest listRequest = Service.Files.List();
            listRequest.PageSize = 1;
            listRequest.Q = "mimeType='application/vnd.google-apps.folder' and trashed=false ";
            listRequest.Q += "and '" + parentId + "' in parents and name='" + folderName + "'";
            listRequest.Fields = "files(id)";

            IList<Google.Apis.Drive.v3.Data.File> folders = listRequest.Execute().Files;
            if (folders == null || folders.Count == 0)
                return null;

            return folders[0].Id;
        }

        /// <summary>
        /// It is useable on Google Drive.
        /// </summary>
        /// <param name="parentId">If it is empty, it looks for he folder in root directory</param>
        protected string GetGDFileId(string fileName, string parentId = "")
        {
            if (string.IsNullOrEmpty(parentId))
                parentId = "root";

            FilesResource.ListRequest listRequest = Service.Files.List();
            listRequest.PageSize = 1;
            listRequest.Q = "mimeType!='application/vnd.google-apps.folder' and trashed=false ";
            listRequest.Q += "and '" + parentId + "' in parents and name='" + fileName + "'";
            listRequest.Fields = "files(id)";

            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
            if (files == null || files.Count == 0)
                return null;

            return files[0].Id;
        }

        /// <summary>
        ///  It returns the file name or the last folder of the path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        protected string GetFileName(string filePath)
        {
            int fileNameStart = filePath.LastIndexOf('\\') + 1;

            return filePath.Substring(fileNameStart);
        }
    }
}
