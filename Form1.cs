using System;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BSPlaylistDownloader
{
    public partial class Form1 : Form
    {
        private string[] playlists;
        private bool mouseDown;
        private Point lastLocation;

        private string BSPath;
        private string CustomLevelsPath;
        private string PlaylistsPath;

        private bool playlistsloaded = false;
        private bool autostart = true;

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

            checkBox1.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("AutoStart"));

            string appVersion = String.Format("{0}.{1}.{2}",Assembly.GetExecutingAssembly().GetName().Version.Major, Assembly.GetExecutingAssembly().GetName().Version.Minor, Assembly.GetExecutingAssembly().GetName().Version.Build);
            label3.Text = String.Format("{0}", appVersion);
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
            if (Directory.Exists(location) && File.Exists(location + "\\Beat Saber.exe"))
            {
                label4.Text = "";
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
                config.AppSettings.Settings.Remove("BeatSaberLocation");
                config.AppSettings.Settings.Add("BeatSaberLocation", location);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                BSPath = location;
                PlaylistsPath = location + "\\Playlists\\";
                CustomLevelsPath = location + "\\Beat Saber_Data\\CustomLevels\\";

                if (!Directory.Exists(PlaylistsPath))
                {
                    richTextBox1.SelectionColor = Color.DarkRed;
                    richTextBox1.AppendText("Directory \"Playlists\" does not exists.\n");
                    Directory.CreateDirectory(PlaylistsPath);
                    if (Directory.Exists(PlaylistsPath))
                    {
                        richTextBox1.SelectionColor = Color.Green;
                        richTextBox1.AppendText("Created \"Playlists\" directory.\n");
                    }
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;
                }

                if (!Directory.Exists(CustomLevelsPath))
                {
                    richTextBox1.SelectionColor = Color.DarkRed;
                    richTextBox1.AppendText("Directory \"/Beat Saber_Data/CustomLevels\" does not exists.\n");
                    Directory.CreateDirectory(CustomLevelsPath);
                    if (Directory.Exists(CustomLevelsPath))
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
                playlistsloaded = true;
                playlists = openFileDialog1.FileNames;
                richTextBox1.SelectionColor = Color.Green;
                richTextBox1.AppendText(String.Format("[I] Loaded {0} playlists \n", openFileDialog1.FileNames.Length));
                richTextBox1.SelectionColor = richTextBox1.ForeColor;

                progressBar1.Visible = false;
                progressBar1.Value = 0;
                label5.Text = "";

                if (autostart)
                {
                    await RunParsePlaylistsAsync();
                } else
                {
                    button3.Enabled = true;
                }
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private async Task RunParsePlaylistsAsync()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            button2.Enabled = false;
            button3.Enabled = false;
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
                label5.Text = String.Format("{0}/{1}", value, playlistscount);
            });

            progressBar2.Visible = true;
            var progress2 = new Progress<int>(value =>
            {
                progressBar2.Value = value;
            });
            var progress2max = new Progress<int>(value =>
            {
                progressBar2.Maximum = value;
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
            var richbox1color = new Progress<Color>(value =>
            {
                richTextBox1.SelectionColor = value;
            });

            await Task.Run(() => ParsePlaylists(progress1, label5text, progress2, progress2max, label6text, richbox1text, richbox1color));

            progressBar1.Value = progressBar1.Maximum;
            label5.Text = String.Format("Completed");
            label6.Text = "";
            label6.Visible = false;
            progressBar2.Visible = false;
            button2.Enabled = true;
            playlistsloaded = false;
            playlists = null;
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            richTextBox1.AppendText(String.Format("\n\n[I] Total execution time: {0}ms \n", elapsedMs));
        }

        private async Task ParsePlaylists(IProgress<int> progress1, IProgress<string> label5text, IProgress<int> progress2, IProgress<int> progress2max, IProgress<string> label6text, IProgress<string> richbox1text, IProgress<Color> richbox1color)
        {
            int i = 0;
            var regex = new Regex(@"\r\n?|\n|\t", RegexOptions.Compiled);
            foreach (string file in playlists)
            {
                string filename = Path.GetFileName(file);
                string destfilepath = PlaylistsPath + filename;

                ++i;
                string jsonData = File.ReadAllText(file);
                Playlist playlist = JsonConvert.DeserializeObject<Playlist>(jsonData);

                string title = Trunc(regex.Replace(playlist.playlistTitle, " - "), 40);
                string author = playlist.playlistAuthor;
                int songcount = playlist.songs.Count;

                if (File.Exists(destfilepath))
                {
                    richbox1color.Report(Color.Orange);
                    richbox1text.Report(String.Format("[W]Playlist {0} already exists, skipping.\n", title));
                    richbox1color.Report(richTextBox1.ForeColor);
                    continue;
                }

                richbox1color.Report(Color.Green);
                richbox1text.Report(String.Format("[I] Loaded {0} songs from playlist {1} by {2}.\n", songcount, title, author));
                richbox1color.Report(richTextBox1.ForeColor);

                label5text.Report(String.Format("{0} {1}", title, i));

                progress2max.Report(songcount);
                int ii = 0;
                foreach (PlaylistItem playlistitem in playlist.songs)
                {
                    ++ii;
                    var songtitle = Trunc(regex.Replace(playlistitem.songName, " - "), 40);
                    label6text.Report(String.Format("{0} ({1}/{2})", songtitle, ii, songcount));
                    progress2.Report(ii);
                    string songhash = playlistitem.hash;
                    await Task.Run(() => DownloadSong(richbox1text, richbox1color, songhash));
                }

                File.Copy(file, destfilepath);

                Thread.Sleep(100);

                progress1.Report(i);
            }
        }

        private void DownloadSong(IProgress<string> richbox1text, IProgress<Color> richbox1color, string hash)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    var jsonData = wc.DownloadString("https://api.beatsaver.com/maps/hash/" + hash);

                    //var song = JsonConvert.DeserializeObject<JToken>(jsonData);

                    dynamic convertObj = JObject.Parse(jsonData);

                    string songID = convertObj.id;
                    string songName = convertObj.metadata.songName;
                    string levelAuthorName = convertObj.metadata.levelAuthorName;
                    levelAuthorName = levelAuthorName.Replace(":", "");

                    string foldername = String.Format("{0} ({1} - {2})", songID, songName, levelAuthorName);
                    string folderpath = CustomLevelsPath + foldername;

                    if (!Directory.Exists(folderpath))
                    {
                        string downloadUrl = convertObj.versions[0].downloadURL;
                        string filepath = String.Format(folderpath + ".zip");
                        try
                        {
                            if (File.Exists(filepath))
                                File.Delete(filepath);
                            wc.DownloadFile(downloadUrl, filepath);
                            ZipFile.ExtractToDirectory(filepath, folderpath);
                            File.Delete(filepath);
                            richbox1color.Report(Color.Green);
                            richbox1text.Report(String.Format("[I]\tDownloaded {0}\n", foldername));
                            richbox1color.Report(richTextBox1.ForeColor);
                        }
                        catch (Exception e)
                        {
                            richbox1color.Report(Color.DarkRed);
                            richbox1text.Report(String.Format("[E]\tError while downloading {0} from {1}\n", foldername, downloadUrl));
                            richbox1color.Report(richTextBox1.ForeColor);
                        }
                    }
                    else
                    {
                        richbox1color.Report(Color.Orange);
                        richbox1text.Report(String.Format("[W]\t{0} already exists, skipping.\n", foldername));
                        richbox1color.Report(richTextBox1.ForeColor);
                    }
                }
            }
            catch (Exception e)
            {
                richbox1text.Report(String.Format("[E]\tError while fetching data\n"));
            }
        }

        private string Trunc(string s, int len) => s?.Length > len ? String.Format("{0}...", s.Substring(0, len)) : s;

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            autostart = !autostart;

            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            config.AppSettings.Settings.Remove("AutoStart");
            config.AppSettings.Settings.Add("AutoStart", Convert.ToString(autostart));
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if(playlistsloaded)
                await RunParsePlaylistsAsync();
        }
    }

    public class Playlist
    {
        public string playlistTitle { get; set; }
        public string playlistAuthor { get; set; }
        public string playlistDescription { get; set; }
        public string syncURL { get; set; }
        public IList<PlaylistItem> songs { get; set; }
    }

    public class PlaylistItem
    {
        public string key { get; set; }
        public string hash { get; set; }
        public string songName { get; set; }
        public string uploader { get; set; }
    }
}
