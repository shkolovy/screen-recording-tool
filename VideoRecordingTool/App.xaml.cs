using System.Diagnostics;
using System.IO;
using System.Windows;

//todo
//add logger
//add button on maximaze

namespace VideoRecordingTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private System.Windows.Forms.NotifyIcon _trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            //only one instance can be running
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("Instance already running");
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
            MainWindow = new MainWindow();
            MainWindow.Hide();

            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.DoubleClick += (s, args) => Tray_OnDoubleClick();
            _trayIcon.Visible = true;
            _trayIcon.Text = "Use double click (or F5) to Start or Stop recording";

            CreateContextMenu();
            HandleTrayItems(false);

            ShowBalloonMessage("Select area and press \"Start\" for recoring.");
        }

        private void ShowBalloonMessage(string message)
        {
            _trayIcon.ShowBalloonTip(5000, "Screen Recorder", message, System.Windows.Forms.ToolTipIcon.Info);
        }

        private void CreateContextMenu()
        {
            //todo: add labels to resources
            _trayIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            _trayIcon.ContextMenuStrip.Items.Add("Capture").Click += (s, e) => Start();
            _trayIcon.ContextMenuStrip.Items.Add("Stop").Click += (s, e) => Stop();

            //_trayIcon.ContextMenuStrip.Items.Add("Settings").Click += (s, e) => Settings();
            _trayIcon.ContextMenuStrip.Items.Add("Go to folder").Click += (s, e) => GoToFolder();
            _trayIcon.ContextMenuStrip.Items.Add("About").Click += (s, e) => About();
            _trayIcon.ContextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            _trayIcon.ContextMenuStrip.Items.Add("Exit").Click += (s, e) => ExitApp();

            _trayIcon.ContextMenuStrip.Items[0].Image = VideoRecordingTool.Properties.Resources.dot;
            _trayIcon.ContextMenuStrip.Items[1].Image = VideoRecordingTool.Properties.Resources.stop;
        }

        public void HandleTrayItems(bool isRecording)
        {
            _trayIcon.ContextMenuStrip.Items[0].Enabled = !isRecording;
            _trayIcon.ContextMenuStrip.Items[1].Enabled = isRecording;
            _trayIcon.Icon = isRecording ? VideoRecordingTool.Properties.Resources.stop_icon : VideoRecordingTool.Properties.Resources.main_icon;
        }

        public void Tray_OnDoubleClick()
        {
            if (MainWindow.IsVisible)
            {
                if (((MainWindow)Current.MainWindow).IsRecording)
                {
                    Stop();
                }
                {
                    MainWindow.Hide();
                }
            }
            else
            {
                MainWindow.Show();
            }
        }

        private void Start()
        {
            MainWindow.Show();
        }

        public void About()
        {
            //todo: implement
            MessageBox.Show("About....");
        }

        public void Settings()
        {
            //todo: implement
            MessageBox.Show("Settings....");
        }

        private void GoToFolder()
        {
            string path = VideoRecordingTool.Properties.Settings.Default.SaveToPath;
            bool directoryExists = Directory.Exists(path);

            if (!directoryExists)
            {
                MessageBox.Show($"{path} does not exist");
            }
            else
            {
                Helpers.OpenFolder(path);
            }
        }

        private void Stop()
        {
            ((MainWindow)Current.MainWindow).StopRecording();
            ShowBalloonMessage("Converting... the video will appear shortly");
        }

        private void ExitApp()
        {
            Application.Current.Shutdown();
        }
    }
}
