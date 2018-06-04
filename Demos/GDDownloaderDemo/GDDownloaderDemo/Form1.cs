using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GDNetwork;

namespace GDDownloaderDemo
{
    public partial class GDDownloaderDemo : Form
    {
        GDDownSynchronizer synchronizer;
        string FullSize;
        string gDPath;

        string[] fileSizeType = new string[] { "B", "KB", "MB", "GB", "TB" };

        public GDDownloaderDemo()
        {
            InitializeComponent();

            gDPath = ""; // add the Google Drive path, where you uploaded the program with the GDUploaderDemo. ForExample: \\GDLauncher\\Test
            string clientSecretJson = null; //Load your own client_secret.json
            
            synchronizer = new GDDownSynchronizer(clientSecretJson, "user", "GDDownloaderDemo");
            synchronizer.FullSizeEvent += SaveFullSize;
            synchronizer.CurrentSizeEvent += ShowStatus;
        }

        private void checkButton_Click(object sender, EventArgs e)
        {
            DialogResult result = downloadDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                statusLabel.Text = "Check files";
                Task.Run(() =>
                {
                    if(synchronizer.CheckChanges(downloadDialog.SelectedPath, gDPath))
                    {
                        statusLabel.Text = "New Version Exists";
                    }
                    else
                    {
                        statusLabel.Text = "Uptodate";
                    }
                });
            }
        }

        private void downloadButton_Click(object sender, EventArgs e)
        {
            DialogResult result = downloadDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                statusLabel.Text = "Check files";
                Task.Run(() =>
                {
                    synchronizer.Sync(downloadDialog.SelectedPath, gDPath, "password");
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
            while (number > 1024)
            {
                number = number / 1024;
                ++i;
            }

            i = i >= fileSizeType.Length ? i - 1 : i;

            return number.ToString("0.00") + fileSizeType[i];
        }
    }
}
