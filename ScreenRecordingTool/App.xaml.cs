using System.Diagnostics;
using System.IO;
using System.Windows;

//todo
//add logger
//add button on maximaze

namespace ScreenRecordingTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private System.Windows.Forms.NotifyIcon _trayIcon;
	    private KeyboardHook keyboardhook;
	    private System.Windows.Forms.Keys _hotKey = System.Windows.Forms.Keys.F3;

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
            HideCaptureWindow();

            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.DoubleClick += (s, args) => Tray_OnDoubleClick();
            _trayIcon.Visible = true;
            _trayIcon.Text = "Use double click to Start or Stop recording";

            CreateContextMenu();
            HandleTrayItems(false);

            ShowBalloonMessage($"Double click on the Icon, select area and press \"Start\" for recoring. (Or press {_hotKey.ToString()})");

	        AddHotkeys();
        }

	    private void AddHotkeys()
	    {
		    keyboardhook = new KeyboardHook(true);

		    keyboardhook.AddHookedKey(_hotKey);
		    keyboardhook.KeyUp += new System.Windows.Forms.KeyEventHandler(KeyboardHook_KeyUp);
		    keyboardhook.Hook();
		}

	    private void KeyboardHook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
	    {
		    if (MainWindow.IsVisible)
		    {
			    if (((MainWindow)Current.MainWindow).IsRecording)
			    {
				    Stop();
			    }
			    else
			    {
					((MainWindow)Current.MainWindow).StartRecording();
				}
			}
		    else
		    {
				ShowCaptureWindow();
			}

			e.Handled = true;
	    }

		private void ShowBalloonMessage(string message)
        {
            _trayIcon.ShowBalloonTip(2000, "Screen Recorder", message, System.Windows.Forms.ToolTipIcon.Info);
        }

        private void CreateContextMenu()
        {
            //todo: add labels to resources
            _trayIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            _trayIcon.ContextMenuStrip.Items.Add($"Capture ({_hotKey.ToString()})").Click += (s, e) => ShowCaptureWindow();
            _trayIcon.ContextMenuStrip.Items.Add($"Stop ({_hotKey.ToString()})").Click += (s, e) => Stop();

	        _trayIcon.ContextMenuStrip.Items.Add("Go to folder").Click += (s, e) => GoToFolder();
			_trayIcon.ContextMenuStrip.Items.Add("Settings").Click += (s, e) => Settings();
            _trayIcon.ContextMenuStrip.Items.Add("About").Click += (s, e) => About();
            _trayIcon.ContextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            _trayIcon.ContextMenuStrip.Items.Add("Exit").Click += (s, e) => ExitApp();

            _trayIcon.ContextMenuStrip.Items[0].Image = ScreenRecordingTool.Properties.Resources.dot;
            _trayIcon.ContextMenuStrip.Items[1].Image = ScreenRecordingTool.Properties.Resources.stop;
        }

        public void HandleTrayItems(bool isRecording)
        {
            _trayIcon.ContextMenuStrip.Items[0].Enabled = !isRecording;
            _trayIcon.ContextMenuStrip.Items[1].Enabled = isRecording;
            _trayIcon.Icon = isRecording ? ScreenRecordingTool.Properties.Resources.stop_icon : ScreenRecordingTool.Properties.Resources.main_icon;
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
	                HideCaptureWindow();
                }
            }
            else
            {
	            ShowCaptureWindow();
            }
        }

        private void ShowCaptureWindow()
        {
            MainWindow.Show();
	        ((MainWindow) Current.MainWindow).StartBtn.Focus();
        }

	    private void HideCaptureWindow()
	    {
		    MainWindow.Hide();
	    }

		public void About()
        {
            MessageBox.Show("arshkolo :)");
        }

        public void Settings()
        {
            MessageBox.Show("All setting are in ScreenRecordingTool.exe.config");
        }

        private void GoToFolder()
        {
            string path = ScreenRecordingTool.Properties.Settings.Default.SaveToPath;
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
            ShowBalloonMessage("Your video is ready.");
	        Helpers.OpenFolder(ScreenRecordingTool.Properties.Settings.Default.SaveToPath);
		}

        private void ExitApp()
        {
	        _trayIcon.Icon = null;
			Application.Current.Shutdown();

	        this.keyboardhook.KeyUp -= KeyboardHook_KeyUp;
	        this.keyboardhook.Dispose();
		}
    }
}
