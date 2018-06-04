
namespace GDNetwork
{
    public class DownloadableFile
    {
        public string Folder { get; private set; }
        public Google.Apis.Drive.v3.Data.File GDFile { get; private set; }

        public DownloadableFile(Google.Apis.Drive.v3.Data.File file, string folder)
        {
            GDFile = file;
            Folder = folder;
        }
    }
}
