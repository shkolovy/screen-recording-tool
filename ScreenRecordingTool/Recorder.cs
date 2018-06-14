using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Accord.Video;
using Accord.Video.FFMPEG;

namespace ScreenRecordingTool
{
	public sealed class Recorder
	{
		private readonly Object _syncObj = new Object();

		private VideoFileWriter _videoWriter;
		private IVideoSource _videoSource;
		private bool _isRecording;
		private Image _watermarkImage;
		private Rectangle _position;

		private const int FRAME_RATE = 10;
		private const int BIT_RATE = 1200000;
		private const string FILE_CONTAINER = "mp4";

		//yellow, half transparent
		private Color cursorHighlightColor = Color.FromArgb(60, 252, 249, 90);
		private const int CURSOR_HIGHLIGHT_DIAMETER = 60;

		private const int WATERMARK_PADDING = 20;

		private DateTime? _firstFrameTime { get; set; }

		public bool IsRecording => _isRecording || _videoWriter != null && _videoWriter.IsOpen;

		public void Start(Rectangle position)
		{
			_position = position;
			//resolution can't be odd
			_position.Width = _position.Width % 2 == 0 ? _position.Width : _position.Width - 1;
			_position.Height = _position.Height % 2 == 0 ? _position.Height : _position.Height - 1;

			CreateInputDirectoryIfNotExists();

			_watermarkImage = Image.FromFile(Properties.Settings.Default.WatermarkPath);

			_videoSource = new ScreenCaptureStream(_position);
			_videoSource.NewFrame += CaptureFrame;
			_videoSource.Start();

			_videoWriter = new VideoFileWriter();

			var inputFilePath = $"{Properties.Settings.Default.SaveToPath}\\{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.{FILE_CONTAINER}";
			_videoWriter.Open(inputFilePath, _position.Width, _position.Height, FRAME_RATE, VideoCodec.H264, BIT_RATE);

			_isRecording = true;
			_firstFrameTime = null;
		}

		public void Stop()
		{
			if (!_isRecording)
				return;

			lock (_syncObj)
			{
				_isRecording = false;

				_videoWriter.Close();
				_videoWriter.Dispose();

				if (_videoSource != null && _videoSource.IsRunning)
				{
					_videoSource.Stop();
					_videoSource.NewFrame -= CaptureFrame;
				}

				if (_watermarkImage != null)
				{
					_watermarkImage.Dispose();
				}
			}
		}

		private void CreateInputDirectoryIfNotExists()
		{
			bool directoryExists = Directory.Exists(Properties.Settings.Default.SaveToPath);

			if (!directoryExists)
			{
				Directory.CreateDirectory(Properties.Settings.Default.SaveToPath);
			}
		}

		private void CaptureFrame(object sender, NewFrameEventArgs eventArgs)
		{
			lock (_syncObj)
			{
				if (!_isRecording)
				{
					return;
				}

				using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
				{
					using (Graphics graphics = Graphics.FromImage(bitmap))
					{
						WindowLocator.CURSORINFO pci;
						pci.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WindowLocator.CURSORINFO));

						if (WindowLocator.GetCursorInfo(out pci))
						{
							if (pci.flags == WindowLocator.CURSOR_SHOWING)
							{
								var cursorX = pci.ptScreenPos.X - _position.Left;
								var cursorY = pci.ptScreenPos.Y - _position.Top;

								WindowLocator.DrawIcon(graphics.GetHdc(), cursorX, cursorY, pci.hCursor);
								graphics.ReleaseHdc();

								DrawCursorHighlight(graphics, cursorX, cursorY);
							}
						}

						DrawWatermark(graphics, bitmap.Width, bitmap.Height);
					}

					_videoWriter.WriteVideoFrame(bitmap);
				}
			}
		}

		private void DrawWatermark(Graphics graphics, int width, int height)
		{
			//move it to the right bottom corner
			var x = width - _watermarkImage.Width - WATERMARK_PADDING;
			var y = height - _watermarkImage.Height - WATERMARK_PADDING;
			graphics.DrawImage(_watermarkImage, x, y);
		}

		private void DrawCursorHighlight(Graphics graphics, int cursorX, int cursorY)
		{
			using (Brush highlightBruch = new SolidBrush(cursorHighlightColor))
			{
				//set highlight around cursor
				//depends on cursor type width/height is approx 16 pixels
				graphics.FillEllipse(highlightBruch, cursorX - CURSOR_HIGHLIGHT_DIAMETER / 2 + 8, cursorY - CURSOR_HIGHLIGHT_DIAMETER / 2 + 8, CURSOR_HIGHLIGHT_DIAMETER, CURSOR_HIGHLIGHT_DIAMETER);
			}
		}
	}
}
