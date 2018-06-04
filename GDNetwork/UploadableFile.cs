
namespace GDNetwork
{
    public class UploadableFile
    {
        public string File { get; private set; }
        public string GDPath { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="gDPath">It starts with \, the empty is the root directory.</param>
        public UploadableFile(string file, string gDPath)
        {
            File = file;
            GDPath = gDPath;
        }
    }
}
