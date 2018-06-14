using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ScreenRecordingTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static Rect _workArea = SystemParameters.WorkArea;

		private readonly Recorder _recorder;
		private DispatcherTimer _timer;

		private static bool _inited;
		private bool _isCountdown;

		private RecordingWindow _recordingWindow;

		public bool IsRecording => _recorder.IsRecording;

		/// <inheritdoc />
		public MainWindow()
		{
			// to avoid double initialization
			if (_inited)
			{
				return;
			}

			_recorder = new Recorder();
			InitializeComponent();
			ToggleOverlay(true);
			BindHotKeys();

			WindowLocator.Start();
			WindowLocator.MouseAction += new EventHandler(OnMouseAction);

			_recordingWindow = new RecordingWindow();

			_inited = true;
		}

		private void BindHotKeys()
		{
			
		}

		//private void StartStopCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		//{
		//	if (IsRecording)
		//	{
		//		StopRecording();
		//	}
		//	else
		//	{
		//		StartRecording();
		//	}
		//}

		private void ToggleRecordingWindow(bool show)
		{
			if (show)
			{
				_recordingWindow.Left = Left;
				_recordingWindow.Top = Top - 40;

				_recordingWindow.StartTimer();

				_recordingWindow.Show();
			}
			else
			{
				_recordingWindow.Hide();
				_recordingWindow.StopTimer();
			}
		}

		private IntPtr CaptureWindowPtr
		{
			get
			{
				return new WindowInteropHelper(this).Handle;
			}
		}

		private void OnMouseAction(object sender, EventArgs e)
		{
			if (!IsVisible || IsRecording || _isCountdown)
			{
				return;
			}

			if (!WindowLocator.GetCursorPos(out WindowLocator.POINT p))
			{
				return;
			}

			IntPtr windowsPtr = WindowLocator.WindowFromPoint(p);

			//Debug.WriteLine(string.Format("last: {0} new: {1}", last, windowsPtr.ToInt32()));

			if (CaptureWindowPtr != windowsPtr)
			{
				const int maxChars = 256;
				StringBuilder className = new StringBuilder(maxChars);

				var handle = WindowLocator.GetForegroundWindow();

				if (WindowLocator.GetClassName(handle, className, maxChars) > 0)
				{
					//Debug.WriteLine(className.ToString() + "+" + WindowLocator.IsDesktop(className.ToString()));
					if (WindowLocator.IsDesktop(className.ToString()))
					{
						WindowLocator.MoveWindow(CaptureWindowPtr, 0, 0, (int)_workArea.Right, (int)_workArea.Bottom, true);
					}
					else if (className.ToString() == "NotifyIconOverflowWindow" || className.ToString() == "Shell_TrayWnd")
					{
						//ignore them
						return;
					}
					else
					{
						WindowLocator.SetForegroundWindow(windowsPtr);
						WindowLocator.GetWindowRect(windowsPtr, out WindowLocator.RECT rect);
						WindowLocator.MoveWindow(CaptureWindowPtr, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
					}
				}
			}
		}

		//protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		//{
		//    base.OnPropertyChanged(e);

		//    if (e.Property == Window.WindowStateProperty)
		//    {
		//        if ((WindowState)e.NewValue == WindowState.Maximized)
		//        {
		//            WindowState = WindowState.Normal;
		//            WindowLocator.MoveWindow(CaptureWindowPtr, 0, 0, (int)_workArea.Right, (int)_workArea.Bottom, true);
		//        }
		//    }
		//}

		public void MouseLeftButton_OnUp(object sender, MouseEventArgs e)
		{
			if (Cursor == Cursors.ScrollAll)
				Cursor = Cursors.Arrow;
		}

		public void MouseLeftButton_OnDown(object sender, MouseEventArgs e)
		{
			if (_isCountdown)
			{
				return;
			}

			if (Cursor != Cursors.ScrollAll)
			{
				Cursor = Cursors.ScrollAll;
			}

			DragMove();
		}

		public void ToggleOverlay(bool show)
		{
			Main.Background.Opacity = show ? 0.2 : 0;
		}

		public void StopRecording()
		{
			if (!_recorder.IsRecording)
			{
				return;
			}

			_recorder.Stop();

			Main.ResizeMode = ResizeMode.CanResizeWithGrip;
			StartBtn.Visibility = Visibility.Visible;
			CloseBtn.Visibility = Visibility.Visible;
			ToggleOverlay(true);
			((App)Application.Current).HandleTrayItems(false);

			Hide();
			ToggleRecordingWindow(false);
		}

		public void StartRecording()
		{
			if (_recorder.IsRecording)
			{
				return;
			}

			Main.ResizeMode = ResizeMode.NoResize;
			StartBtn.Visibility = Visibility.Hidden;
			CloseBtn.Visibility = Visibility.Hidden;
			ShowCountdown();
		}

		private void StartBtn_OnClick(object sender, RoutedEventArgs e)
		{
			StartRecording();
		}

		private void CloseImg_OnMouseDown(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		private Rectangle GetRecordingCoordinates()
		{
			if (!WindowLocator.GetWindowRect(CaptureWindowPtr, out WindowLocator.RECT rect))
			{
				throw new Exception("Can't find capture window coordinates");
			}

			//not to show recording frame border
			const int ignoredPixels = 3;
			return new Rectangle(rect.Left + ignoredPixels, rect.Top + ignoredPixels, rect.Right - rect.Left - ignoredPixels * 2, rect.Bottom - rect.Top - ignoredPixels * 2);
			//return new Position(rect.Top + ignoredPixels, rect.Bottom - ignoredPixels, rect.Left + ignoredPixels, rect.Right - ignoredPixels);
		}

		private void ShowCountdown()
		{
			_isCountdown = true;

			CounterLbl.Visibility = Visibility.Visible;

			var countdownSeconds = Properties.Settings.Default.Countdown;
			var time = TimeSpan.FromSeconds(countdownSeconds - 1);
			CounterLbl.Content = countdownSeconds;

			EventHandler callback = (s, e) =>
			{
				CounterLbl.Content = time.TotalSeconds;
				if (time == TimeSpan.Zero)
				{
					_timer.Stop();
					CounterLbl.Visibility = Visibility.Hidden;
					ToggleOverlay(false);
					_isCountdown = false;
					((App)Application.Current).HandleTrayItems(true);

					ToggleRecordingWindow(true);

					RunVideoProcesses();
				}
				time = time.Add(TimeSpan.FromSeconds(-1));
			};

			_timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, callback, Application.Current.Dispatcher);
			_timer.Start();
		}

		//todo: remove convertor
		private void RunVideoProcesses()
		{
			_recorder.Start(GetRecordingCoordinates());
		}
	}
}
