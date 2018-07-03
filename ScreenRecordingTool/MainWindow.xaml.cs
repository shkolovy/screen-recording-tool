﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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
		private bool _isWindowCapturer;

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
			SetResolutionLbl();

			WindowLocator.Start();
			WindowLocator.MouseAction += new EventHandler(OnMouseAction);

			_recordingWindow = new RecordingWindow();

			_inited = true;
		}

		private void SetResolutionLbl()
		{
			ResolutionLbl.Content = $"{Math.Ceiling(Width)} ✕ {Math.Ceiling(Height)}";
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SetResolutionLbl();
		}

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
			if (!_isWindowCapturer || !IsVisible || IsRecording || _isCountdown)
			{
				return;
			}

			if (!WindowLocator.GetCursorPos(out WindowLocator.POINT p))
			{
				return;
			}

			IEnumerable<IntPtr> wisibleWindows = WindowLocator.FindWindows();
			IntPtr foundWindow = WindowLocator.WindowFromPoint(p);
			IntPtr window = wisibleWindows.FirstOrDefault(w => w == foundWindow);

			if (window != null && window != CaptureWindowPtr)
			{
				WindowLocator.SetForegroundWindow(foundWindow);
				WindowLocator.GetWindowRect(foundWindow, out WindowLocator.RECT rect);
				WindowLocator.MoveWindow(CaptureWindowPtr, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
			}
		}

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
			ResolutionLbl.Visibility = Visibility.Visible;
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
			ResolutionLbl.Visibility = Visibility.Hidden;
			ToggleWindowCapturerMode(false);
			ShowCountdown();
		}

		private void CaptureWindowBtn_OnClick(object sender, RoutedEventArgs e)
		{
			ToggleWindowCapturerMode(!_isWindowCapturer);
		}

		private void ToggleWindowCapturerMode(bool active)
		{
			CaptureWindowBtn.Content = active ? "Capture Window (on)" : "Capture Window (off)";
			InfoLbl.Visibility = active ? Visibility.Visible : Visibility.Hidden;

			_isWindowCapturer = active;
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

					_recorder.Start(GetRecordingCoordinates());
				}
				else
				{
					time = time.Add(TimeSpan.FromSeconds(-1));
				}
			};

			_timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, callback, Application.Current.Dispatcher);
			_timer.Start();
		}
	}
}
