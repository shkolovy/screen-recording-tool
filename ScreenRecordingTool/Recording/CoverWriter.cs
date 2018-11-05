using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace ScreenRecordingTool.Recording
{
	public sealed class CoverWriter : IDisposable
	{
		private VideoRecordingOptions _options;

		private const string TEMP_COVER_IMG = "cover.png";
		private const string TEMP_COVER_VID = "cover.mp4";

		private string FileWithCover
		{
			get { return $"{_options.Path}\\{_options.Name}_c.{_options.FileContainer}"; }
		}

		public CoverWriter(VideoRecordingOptions options)
		{
			_options = options;
		}

		public void Write()
		{
			CreateCoverImg(_options.Width, _options.Height);

			var coverProcessorArgs = $"-framerate 10 -loop 1 -t 2 -video_size {_options.Width}x{_options.Height} -i {_options.Path}\\{TEMP_COVER_IMG} -y {_options.Path}\\{TEMP_COVER_VID}";
			var coverProcessor = new FfmpegProcessor(coverProcessorArgs);
			coverProcessor.WaitForExit();

			var mergeCoverProcessorArgs = $"-i {_options.Path}\\{TEMP_COVER_VID} -i {_options.FullPath} -filter_complex \"[0:v]fade = type =out:duration = 0.5:start_time = 1.5,setpts = PTS - STARTPTS[v0]; [1:v]fade = type =in:duration = 0.5:start_time = 0, setpts=PTS-STARTPTS[v1]; [v0] [v1] concat=n=2:v=1[v]\" -map \"[v]\" -pix_fmt yuv420p -preset medium -y {FileWithCover}";
			var mergeCoverProcessor = new FfmpegProcessor(mergeCoverProcessorArgs);
			mergeCoverProcessor.WaitForExit();
		}

		private void CreateCoverImg(int w, int h)
		{
			var bmp = new Bitmap(w, h);
			using (Graphics graph = Graphics.FromImage(bmp))
			{
				graph.FillRectangle(new SolidBrush(Color.White), 0, 0, w, h);

				try
				{
					var logo = Image.FromFile("cover-img.png");

					double scale = 0.7;

					var scaleWidth = w * scale;
					var scaleHeight = scaleWidth / logo.Width * logo.Height;

					var pX = w / 2 - scaleWidth / 2;
					var pY = h / 2 - scaleHeight / 2;

					graph.DrawImage(logo, (int)pX, (int)pY, (int)scaleWidth, (int)scaleHeight);

					graph.SmoothingMode = SmoothingMode.AntiAlias;
					graph.InterpolationMode = InterpolationMode.HighQualityBicubic;
					graph.PixelOffsetMode = PixelOffsetMode.HighQuality;
				}
				catch (FileNotFoundException e)
				{
					// ignore
				}
			}

			bmp.Save($"{_options.Path}\\{TEMP_COVER_IMG}");
		}

		/// <summary>
		/// Delete temp files
		/// </summary>
		public void Dispose()
		{
			try
			{
				if (!File.Exists(FileWithCover))
				{
					return;
				}

				File.Delete(_options.FullPath);
				File.Delete($"{_options.Path}\\{TEMP_COVER_IMG}");
				File.Delete($"{_options.Path}\\{TEMP_COVER_VID}");
			}
			catch { }
		}
	}
}
