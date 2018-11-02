using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;

namespace ScreenRecordingTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private const int DRAWING_PEN_THINKNESS = 15;

        private readonly Recorder _recorder;
		private DispatcherTimer _timer;

		private static bool _inited;
		private bool _isCountdown;
		private bool _isWindowCapturer;

		private RecordingWindow _recordingWindow;

		private bool _isRectangle;
		private Point _rectangleStartPosition;
		private Point _rectangleEndPosition;
		private Stroke _lastRectangle;

        public bool IsRecording => _recorder.IsRecording;

		public bool IsDrawing { get; private set; }

		public bool IsText { get; private set; }

		public bool IsRectangleDrawing { get; private set; }

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
			SetCanvasPen();

			WindowLocator.Start();
			WindowLocator.MouseAction += new EventHandler(OnMouseAction);

			_recordingWindow = new RecordingWindow();

			_inited = true;
		}

		private void SetResolutionLbl()
		{
			if (CaptureWindowPtr != IntPtr.Zero)
			{
				var coordinates = GetRecordingCoordinates();
				ResolutionLbl.Content = $"{coordinates.Width} ✕ {coordinates.Height}";
			}
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
				_recordingWindow.ResetControls();
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
			if (IsRecording)
			{
				return;
			}

			if (Cursor == Cursors.ScrollAll)
				Cursor = Cursors.Arrow;
		}

		public void MouseLeftButton_OnDown(object sender, MouseEventArgs e)
		{
			if (IsRecording)
			{
				return;
			}

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

		public async Task StopRecording()
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
			CaptureWindowBtn.Visibility = Visibility.Visible;
			DrawingCnws.Visibility = Visibility.Hidden;
			PopupMessageBox.Text = "";
			ToggleOverlay(true);
			((App)Application.Current).HandleTrayItems(false);
			ClearDrawing();
			ToggleDrawing(false);
			ToggleText(false);
			ToggleRectangleDrawing(false);
            DrawingCnws.Visibility = Visibility.Hidden;
			ToggleRecordingWindow(false);
			Hide();

			await _recorder.FinishRecording();
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
			CaptureWindowBtn.Visibility = Visibility.Hidden;
			ToggleWindowCapturerMode(false);
			ShowCountdown();
		}

		public void ClearDrawing()
		{
			DrawingCnws.Strokes.Clear();
		}

		public void ToggleText(bool show)
		{
			TextPanel.Visibility = Visibility.Hidden;
			PopupMessageBox.Text = "";
			IsText = show;

			DrawingCnws.EditingMode = InkCanvasEditingMode.None;
			DrawingCnws.UseCustomCursor = show;

			ToggleDrawingIncCanvas(show);
        }

		private void SetCanvasPen()
		{
            DrawingCnws.DefaultDrawingAttributes.Width = DRAWING_PEN_THINKNESS;
			DrawingCnws.DefaultDrawingAttributes.Height = DRAWING_PEN_THINKNESS;
			DrawingCnws.DefaultDrawingAttributes.FitToCurve = false;
			DrawingCnws.DefaultDrawingAttributes.IsHighlighter = true;
			DrawingCnws.Cursor = Cursors.Pen;
		}

		public void ChangeCanvasColor(Color color)
		{
			DrawingCnws.DefaultDrawingAttributes.Color = color;
		}

		public void ToggleDrawing(bool show)
		{
			ToggleDrawingIncCanvas(show);
			DrawingCnws.UseCustomCursor = show;
			DrawingCnws.EditingMode = show ? InkCanvasEditingMode.Ink : InkCanvasEditingMode.None;

			IsDrawing = show;
		}

		private void CaptureWindowBtn_OnClick(object sender, RoutedEventArgs e)
		{
			ToggleWindowCapturerMode(!_isWindowCapturer);
		}

		private void ToggleWindowCapturerMode(bool active)
		{
			CaptureWindowBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(active ? Constans.BTN_COLOR_ACTIVE : Constans.BTN_COLOR));
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

		private System.Drawing.Rectangle GetRecordingCoordinates()
		{
			if (!WindowLocator.GetWindowRect(CaptureWindowPtr, out WindowLocator.RECT rect))
			{
				throw new Exception("Can't find capture window coordinates");
			}

			//not to show recording frame border
			const int ignoredPixels = 3;
			return new System.Drawing.Rectangle(rect.Left + ignoredPixels, rect.Top + ignoredPixels, rect.Right - rect.Left - ignoredPixels * 2, rect.Bottom - rect.Top - ignoredPixels * 2);
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
					_isCountdown = false;
                    _timer.Stop();
                    CounterLbl.Visibility = Visibility.Collapsed;
                    ToggleOverlay(false);
					((App)Application.Current).HandleTrayItems(true);
					
					ToggleRecordingWindow(true);
					DrawingCnws.Visibility = Visibility.Visible;

					var coordinates = GetRecordingCoordinates();
					_recorder.Start(coordinates);
				}
				else
				{
					time = time.Add(TimeSpan.FromSeconds(-1));
				}
			};

			_timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, callback, Application.Current.Dispatcher);
			_timer.Start();
		}


		public void ToggleRectangleDrawing(bool show)
		{
			IsRectangleDrawing = show;
			DrawingCnws.EditingMode = InkCanvasEditingMode.None;
			DrawingCnws.UseCustomCursor = show;
			ToggleDrawingIncCanvas(show);
        }

        private void DrawingCnws_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (IsRectangleDrawing)
			{
				_isRectangle = true;
				_rectangleStartPosition = Mouse.GetPosition(DrawingCnws);
            }
		}

		private void ToggleDrawingIncCanvas(bool active)
		{
			DrawingCnws.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(active ? Constans.CANVAS_COLOR : Constans.CANVAS_TRANSPARENT_COLOR));
        }

		private void DrawingCnws_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (IsRectangleDrawing)
			{
				_isRectangle = false;
            }
			else if(IsText)
			{
				TextPanel.Visibility = Visibility.Visible;
				var p = Mouse.GetPosition(DrawingCnws);
				PopupMessageBox.Text = "";
				PopupMessageBox.Focus();
				TextPanel.Margin = new Thickness(p.X, p.Y, 0, 0);
            }
		}

		private void DrawingCnws_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsRectangleDrawing && _isRectangle)
			{
				_rectangleEndPosition = Mouse.GetPosition(DrawingCnws);

				if (DrawingCnws.Strokes.Count > 0 && DrawingCnws.Strokes[DrawingCnws.Strokes.Count - 1] == _lastRectangle)
				{
					DrawingCnws.Strokes.RemoveAt(DrawingCnws.Strokes.Count - 1);
				}

				DrawRectangle();
			}
		}

		private void DrawRectangle()
		{
			StylusPointCollection pts = new StylusPointCollection();

			pts.Add(new StylusPoint(_rectangleStartPosition.X, _rectangleStartPosition.Y));
			pts.Add(new StylusPoint(_rectangleEndPosition.X, _rectangleStartPosition.Y));
			pts.Add(new StylusPoint(_rectangleEndPosition.X, _rectangleEndPosition.Y));
			pts.Add(new StylusPoint(_rectangleStartPosition.X, _rectangleEndPosition.Y));
			pts.Add(new StylusPoint(_rectangleStartPosition.X, _rectangleStartPosition.Y));

			Stroke s = new Stroke(pts);
			_lastRectangle = s;
			s.DrawingAttributes.Color = Colors.Red;
			DrawingCnws.Strokes.Add(s);
		}

		public void RemoveRectangle()
		{
			if (_lastRectangle != null)
			{
				DrawingCnws.Strokes.Remove(_lastRectangle);
            }
		}

		private IntPtr CaptureWindowPtr
		{
			get
			{
				return new WindowInteropHelper(this).Handle;
			}
		}
    }
}

