using ScreenRecordingTool.Recording;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ScreenRecordingTool
{
	public sealed class VideoWriter: IDisposable
	{
		private NamedPipeServerStream _ffmpegStream;
		private FfmpegProcessor _ffmpegProcess;
		private byte[] _videoBuffer;

		private string pipePrefix = @"\\.\pipe\";
		private string pipeName = $"ffmpeg-{Guid.NewGuid()}";

		public VideoWriter(VideoRecordingOptions videoRecordingOptions)
		{
			_videoBuffer = new byte[videoRecordingOptions.Width * videoRecordingOptions.Height * 4];
			_ffmpegStream = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, _videoBuffer.Length);
			StartFfmpegProcess(videoRecordingOptions);
		}

		private void StartFfmpegProcess(VideoRecordingOptions videoRecordingOptions)
		{
			var inputArgs = $"-thread_queue_size 512 -use_wallclock_as_timestamps 1 -f rawvideo -pix_fmt rgb32 -video_size {videoRecordingOptions.Width}x{videoRecordingOptions.Height} -i {pipePrefix}{pipeName}";
			var outputArgs = $"-vcodec libx264 -crf 15 -pix_fmt yuv420p -preset medium -r {videoRecordingOptions.FrameRate} -y {Path.Combine(videoRecordingOptions.Path, videoRecordingOptions.FileName)}";

			_ffmpegProcess = new FfmpegProcessor($"{inputArgs} {outputArgs}");
		}

		public async Task WriteFrame(Bitmap frame)
		{
			if (!_ffmpegStream.IsConnected)
			{
				_ffmpegStream.WaitForConnection();
			}

			var bits = frame.LockBits(new Rectangle(Point.Empty, frame.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
			Marshal.Copy(bits.Scan0, _videoBuffer, 0, _videoBuffer.Length);
			frame.UnlockBits(bits);
			frame.Dispose();

			await _ffmpegStream.WriteAsync(_videoBuffer, 0, _videoBuffer.Length);
		}

		public void Dispose()
		{
			_ffmpegStream.Flush();
			_ffmpegStream.Dispose();
			_ffmpegProcess.WaitForExit();
		}
	}
}