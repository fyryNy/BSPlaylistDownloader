using System;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace BSPlaylistDownloader
{
    public partial class Form1 : Form
    {
        private string[] playlists;
        private bool mouseDown;
        private Point lastLocation;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string location = ConfigurationManager.AppSettings.Get("BeatSaberLocation");
            if (String.IsNullOrWhiteSpace(location) || !Directory.Exists(location))
            {
                textBox1.Text = "Double-click to select game location... (or paste it)";
            }
            else
                textBox1.Text = location;

            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            label3.Text = String.Format("{0} (pre-alpha)", appVersion);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //this.Close();
            Application.Exit();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void textBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string location = textBox1.Text;
            if (Directory.Exists(location) && File.Exists(location + "/Beat Saber.exe"))
            {
                label4.Text = "";
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
                config.AppSettings.Settings.Remove("BeatSaberLocation");
                config.AppSettings.Settings.Add("BeatSaberLocation", location);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                if (!Directory.Exists(location + "/Playlists"))
                {
                    richTextBox1.SelectionColor = Color.DarkRed;
                    richTextBox1.AppendText("Directory \"Playlists\" does not exists.\n");
                    Directory.CreateDirectory(location + "/Playlists");
                    if (Directory.Exists(location + "/Playlists"))
                    {
                        richTextBox1.SelectionColor = Color.Green;
                        richTextBox1.AppendText("Created \"Playlists\" directory.\n");
                    }
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;
                }

                if (!Directory.Exists(location + "/Beat Saber_Data/CustomLevels"))
                {
                    richTextBox1.SelectionColor = Color.DarkRed;
                    richTextBox1.AppendText("Directory \"/Beat Saber_Data/CustomLevels\" does not exists.\n");
                    Directory.CreateDirectory(location + "/Beat Saber_Data/CustomLevels");
                    if (Directory.Exists(location + "/Beat Saber_Data/CustomLevels"))
                    {
                        richTextBox1.SelectionColor = Color.Green;
                        richTextBox1.AppendText("Created \"/Beat Saber_Data/CustomLevels\" directory.\n");
                    }
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;
                }

                button2.Enabled = true;
            }
            else
            {
                label4.Text = "Please provide proper game path";
                button2.Enabled = false;
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                richTextBox1.SelectionColor = Color.Green;
                richTextBox1.AppendText(String.Format("Loaded {0} playlists \n", openFileDialog1.FileNames.Length));
                richTextBox1.SelectionColor = richTextBox1.ForeColor;
                playlists = openFileDialog1.FileNames;

                await RunParsePlaylistsAsync();

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                richTextBox1.AppendText(String.Format("\n\nTotal execution time: {0}ms \n", elapsedMs));
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private async Task RunParsePlaylistsAsync()
        {
            int playlistscount = playlists.Length;
            progressBar1.Visible = true;
            progressBar1.Maximum = playlistscount;
            var progress1 = new Progress<int>(value =>
            {
                progressBar1.Value = value;
            });
            label5.Visible = true;
            var label5text = new Progress<string>(value =>
            {
                label5.Text = value;
            });

            progressBar2.Visible = true;
            var progress2 = new Progress<int>(value =>
            {
                progressBar2.Value = value;
            });
            label6.Visible = true;
            var label6text = new Progress<string>(value =>
            {
                label6.Text = value;
            });

            var richbox1text = new Progress<string>(value =>
            {
                richTextBox1.AppendText(value);
            });

            await Task.Run(() => ParsePlaylists(progress1, label5text, progress2, label6text, richbox1text));

            progressBar1.Value = progressBar1.Maximum;
            label5.Text = String.Format("Completed");
            label6.Visible = false;
            progressBar2.Visible = false;
        }

        private void ParsePlaylists(IProgress<int> progress1, IProgress<string> label5text, IProgress<int> progress2, IProgress<string> label6text, IProgress<string> richbox1text)
        {
            int i = 0;
            var regex = new Regex(@"\r\n?|\n|\t", RegexOptions.Compiled);
            foreach (string file in playlists)
            {
                string jsonData = File.ReadAllText(file);
                Playlist playlist = JsonConvert.DeserializeObject<Playlist>(jsonData);

                string title = regex.Replace(playlist.playlistTitle, " - ");
                string author = playlist.playlistAuthor;
                int songcount = playlist.songs.Count;

                progressBar2.Value = 0;
                progressBar2.Maximum = songcount;
                //label6.Text = String.Format("Downloading {0} songs", songcount);

                //richTextBox1.AppendText(String.Format("Playlist \"{0}\" by {1} loaded, {2} songs. \n", title, author, songcount));
                if (progress1 != null)
                    progress1.Report(i++);
                Thread.Sleep(500);
            }
        }
    }

    public class Playlist
    {
        public string playlistTitle { get; set; }
        public string playlistAuthor { get; set; }
        public string playlistDescription { get; set; }
        public string syncURL { get; set; }
        public IList<Song> songs { get; set; }
    }

    public class Song
    {
        public string key { get; set; }
        public string hash { get; set; }
        public string songName { get; set; }
        public string uploader { get; set; }
    }
}
