using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenRecordingTool
{
	public sealed class VideoRecording : IDisposable
	{
		public bool IsRecording { get; private set; }
		public bool IsPause { get; private set; }


		public VideoRecordingOptions VideoRecordingOptions { get; set; }

		private BlockingCollection<Bitmap> _frames;
		private VideoWriter _writer;

		private Task _writeFrameTask;
		private Task _captureFrameTask;

		private Image _watermarkImage;
		private Color CURSOR_HIGHLIGHT_COLOR = Color.FromArgb(60, 252, 249, 90);
		private const int WATERMARK_PADDING = 20;
		private const int CURSOR_HIGHLIGHT_DIAMETER = 60;


		public VideoRecording(VideoRecordingOptions videoRecordingOptions)
		{
			_frames = new BlockingCollection<Bitmap>();
			_writer = new VideoWriter(videoRecordingOptions);
			VideoRecordingOptions = videoRecordingOptions;

			try
			{
				_watermarkImage = Image.FromFile(Properties.Settings.Default.WatermarkPath);
			}
			catch (FileNotFoundException)
			{
				// ignore
			}
		}

		/// <summary>
		/// Start capture frames and write them into file
		/// </summary>
		public void Start()
		{
			IsRecording = true;
			_captureFrameTask = Task.Run(() => CaptureFrames());
			_writeFrameTask = Task.Run(() => WriteFrames());
		}

		/// <summary>
		/// Stop adding new frames
		/// </summary>
		public void StopCapturing()
		{
			_frames.CompleteAdding();
		}

		public void PauseResume()
		{
			if (!IsRecording)
			{
				return;
			}

			IsPause = !IsPause;
		}

		/// <summary>
		/// Wait until all frames have been processed
		/// </summary>
		/// <returns></returns>
		public async Task FinishRecording()
		{
			await _captureFrameTask;
			await _writeFrameTask;

			IsRecording = false;
		}

		public void Dispose()
		{
			_writer.Dispose();
		}

		private Bitmap CaptureFrame()
		{
			var bitmap = new Bitmap(VideoRecordingOptions.Width, VideoRecordingOptions.Height);

			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.CopyFromScreen(VideoRecordingOptions.X, VideoRecordingOptions.Y, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);

				WindowLocator.CURSORINFO pci;
				pci.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WindowLocator.CURSORINFO));

				if (WindowLocator.GetCursorInfo(out pci))
				{
					if (pci.flags == WindowLocator.CURSOR_SHOWING)
					{
						var cursorX = pci.ptScreenPos.X - VideoRecordingOptions.X;
						var cursorY = pci.ptScreenPos.Y - VideoRecordingOptions.Y;

						WindowLocator.DrawIcon(graphics.GetHdc(), cursorX, cursorY, pci.hCursor);
						graphics.ReleaseHdc();

						DrawCursorHighlight(graphics, cursorX, cursorY);
					}
				}

				DrawWatermark(graphics, bitmap.Width, bitmap.Height);
			}

			return bitmap;
		}

		private void DrawWatermark(Graphics graphics, int width, int height)
		{
			if (_watermarkImage == null)
			{
				return;
			}

			//move it to the right bottom corner
			var x = width - _watermarkImage.Width - WATERMARK_PADDING;
			var y = height - _watermarkImage.Height - WATERMARK_PADDING;
			graphics.DrawImage(_watermarkImage, x, y);
		}

		private void DrawCursorHighlight(Graphics graphics, int cursorX, int cursorY)
		{
			using (Brush highlightBruch = new SolidBrush(CURSOR_HIGHLIGHT_COLOR))
			{
				//set highlight around cursor
				//depends on cursor type width/height is approx 16 pixels
				graphics.FillEllipse(highlightBruch, cursorX - CURSOR_HIGHLIGHT_DIAMETER / 2 + 8, cursorY - CURSOR_HIGHLIGHT_DIAMETER / 2 + 8, CURSOR_HIGHLIGHT_DIAMETER, CURSOR_HIGHLIGHT_DIAMETER);
			}
		}

		private async Task CaptureFrames()
		{
			Task<Bitmap> task = null;
			var frameInterval = TimeSpan.FromSeconds(1.0 / VideoRecordingOptions.FrameRate);

			while (!_frames.IsAddingCompleted)
			{
				if (IsPause)
				{
					continue;
				}

				var timestamp = DateTime.Now;

				task = Task.Run(() => CaptureFrame());

				if (task != null)
				{
					var frame = await task;

					try
					{
						_frames.Add(frame);
					}
					catch (InvalidOperationException)
					{
						//ignore this ex. Last frame can't be added if IsAddingCopleated is set in another thread
					}
				}

				var timeTillNextFrame = timestamp + frameInterval - DateTime.Now;

				if (timeTillNextFrame > TimeSpan.Zero)
				{
					Thread.Sleep(timeTillNextFrame);
				}
			}
		}

		private async Task WriteFrames()
		{
			while (!_frames.IsCompleted)
			{
				if (IsPause)
				{
					continue;
				}

				_frames.TryTake(out var frame, -1);

				if (frame != null)
				{
					await _writer.WriteFrame(frame);
				}
			}
		}
	}
}