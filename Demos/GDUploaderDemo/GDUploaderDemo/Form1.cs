using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using GDNetwork;

namespace GDUploaderDemo
{
    public partial class GDUploaderDemo : Form
    {
        GDUpSynchronizer synchronizer;
        string FullSize;

        string[] fileSizeType = new string[] {"B", "KB", "MB", "GB", "TB" };

        public GDUploaderDemo()
        {
            InitializeComponent();
            // Google Drive Folder example: \GDLauncher 
            string clientSecretJson = null; //Load your own client_secret.json

            synchronizer = new GDUpSynchronizer(clientSecretJson, "user", "GDUploaderDemo");
            synchronizer.FullSizeEvent += SaveFullSize;
            synchronizer.CurrentSizeEvent += ShowStatus;
        }

        private void fullUploaderButton_Click(object sender, EventArgs e)
        {
            DialogResult result = uploadDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                statusLabel.Text = "Check files";
                Task.Run(() =>
                {
                    synchronizer.Sync(gDPath.Text, uploadDialog.SelectedPath, "password");
                    statusLabel.Text = "Ready";
                });
            }
        }

        private void SaveFullSize(object source, StreamByteEventArgs e)
        {
            FullSize = ConvertFileSizeType(e.Bytes);
            statusLabel.Text = "0B/" + FullSize;
        }

        private void ShowStatus(object source, StreamByteEventArgs e)
        {
            statusLabel.Text = ConvertFileSizeType(e.Bytes) + "/" + FullSize;
        }

        private string ConvertFileSizeType(long Bytes)
        {
            float number = Bytes;
            int i = 0;
            while(number>1024)
            {
                number = number / 1024;
                ++i;
            }

            i=i>=fileSizeType.Length?i - 1:i;

            return number.ToString("0.00")+fileSizeType[i];
        }
    }
}
