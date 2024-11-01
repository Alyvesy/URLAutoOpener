using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace URLAutoOpener
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;
        private Timer timer;
        private HttpClient httpClient;
        private readonly Icon icon_connected = new Icon("icon_connected.ico");
        private readonly Icon icon_disconnected = new Icon("icon_disconnected.ico");
        private DateTime lastLoginAttempt = DateTime.MinValue;
        private bool wasDisconnected = false;

        // Property Settings
        private string appName = "URLAutoOpener";  // App name in notify
        private string url = "https://github.com/Alyvesy";  // URL you want to open
        private int checkInterval = 2000;  // Detect delay (Recommand higher than 500)
        // You can change which websites for detection
        private List<string> detecterSite = new List<string> { "https://208.67.222.222", "https://1.1.1.1", "https://8.8.8.8" };
        private List<string> detecterSiteCN = new List<string> { "http://www.baidu.com", "https://ping.chinaz.com", "https://8.8.8.8" };

        public Form1()
        {
            InitializeComponent();
            LoadConfig();
            SetupTrayIcon();

            httpClient = new HttpClient();

            timer = new Timer();
            timer.Interval = checkInterval;
            timer.Tick += CheckInternetConnection;
            timer.Start();
        }
         
        private void LoadConfig()
        {
            try
            {
                var configLines = File.ReadAllLines("config.txt");
                foreach (var line in configLines)
                {
                    if (line.StartsWith("App Name="))
                    {
                        var value = line.Split('=')[1];
                        if(!string.IsNullOrEmpty(value)) appName = Convert.ToString(value);
                    }
                    if (line.StartsWith("URL="))
                    {
                        var value = line.Split('=')[1];
                        if (!string.IsNullOrEmpty(value)) url = Convert.ToString(value);
                    }
                    if (line.StartsWith("Detection Interval="))
                    {
                        var value = line.Split('=')[1];
                        if (!string.IsNullOrEmpty(value)) checkInterval = int.Parse(value);
                    }
                    if (line.StartsWith("Detection Region="))
                    {
                        var value = line.Split('=')[1];
                        if (int.Parse(value) == 1 && !string.IsNullOrEmpty(value)) 
                            detecterSite = detecterSiteCN;  // 0: Standard; 1: China
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading config: {ex.Message}");
            }
        }

        private void SetupTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = icon_connected;
            notifyIcon.Visible = true;
            notifyIcon.Text = appName;

            var contextMenu = new ContextMenuStrip();

            var exitItem = new ToolStripMenuItem("!Exit!");
            exitItem.Click += (s, e) => { ExitApp(); };
            contextMenu.Items.Add(exitItem);

            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => { OpenAbout(); };
            contextMenu.Items.Add(aboutItem);

            var intervalDisplay = new ToolStripMenuItem(checkInterval.ToString() + " ms/detect");
            contextMenu.Items.Add(intervalDisplay);


            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ExitApp()
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void OpenAbout()
        {
            ProcessStartInfo psiAbout = new ProcessStartInfo
            {
                FileName = "https://github.com/Alyvesy/URLAutoOpener.git",
                UseShellExecute = true,
            };
            Process.Start(psiAbout);
        }

        private async void CheckInternetConnection(object sender, EventArgs e)
        {
            bool allDisconnected = true;

            foreach (var website in detecterSite)
            {
                try
                {
                    var response = await httpClient.GetAsync(website);
                    if (response.IsSuccessStatusCode)
                    {
                        allDisconnected = false;
                        break;
                    }
                }
                catch { }
                await Task.Delay(200);  // To avoid Network fluctuations, delay 200ms per detection
            }

            // Update notify icon
            notifyIcon.Icon = allDisconnected ? icon_disconnected : icon_connected;

            // Reset timer after connected
            if (!allDisconnected && wasDisconnected)
            {
                lastLoginAttempt = DateTime.MinValue;
            }
            // If disconnected again and 30min after last connect atempt
            if (allDisconnected && (DateTime.Now - lastLoginAttempt).TotalMinutes >= 30)
            {
                AutoLogin();
                lastLoginAttempt = DateTime.Now;
            }

            // Update connect staue
            wasDisconnected = allDisconnected;
        }

        private void AutoLogin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during login: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }
    }
}
