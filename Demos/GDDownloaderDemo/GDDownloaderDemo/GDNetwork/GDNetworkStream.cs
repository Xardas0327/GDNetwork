using System;
using System.Collections.Generic;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;

namespace GDNetwork
{
    abstract public class GDNetworkStream<T> : GDConnection
    {
        protected Queue<T> streamableFiles;
        public long StreamdableBytes { get; protected set; }

        public event EventHandler<StreamByteEventArgs> StreamStatusEvent;

        /// <summary>
        /// If modifying these scopes, delete your previously saved credentials.
        /// </summary>
        /// <param name="clientSecretJson"></param>
        /// <param name="userName"></param>
        /// <param name="appName"></param>
        /// <param name="scopes">If it is null, the service takes DriveReadonly and DriveFile rights.</param>
        /// <param name="authenticationFile">If it is null, the constructor creates one into its own folder.</param>
        public GDNetworkStream(string clientSecretJson, string userName, string appName, string[] scopes = null, FileDataStore authenticationFile = null) :
            base(clientSecretJson, userName, appName, scopes == null ? new string[] { DriveService.Scope.DriveReadonly, DriveService.Scope.DriveFile } : scopes, authenticationFile)
        { }

        public GDNetworkStream(DriveService driveService) : base(driveService) { }

        protected override void Init()
        {
            streamableFiles = new Queue<T>();
            StreamdableBytes = 0;
        }

        abstract public void Add(T file);

        public void Add(IEnumerable<T> files)
        {
            foreach (T file in files)
                Add(file);
        }

        public int CountFiles()
        {
            return streamableFiles.Count;
        }

        abstract public IEnumerable<string> StartStream();

        public void CallStreamStatusEvent(long byteStatus)
        {
            if (StreamStatusEvent != null)
                StreamStatusEvent(this, new StreamByteEventArgs(byteStatus));
        }
    }
}
