using ScreenRecordingTool.Recording;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ScreenRecordingTool
{
	public sealed class Recorder
	{
		private const string FILE_CONTAINER = "mp4";
		private const int FRAME_RATE = 10;

        private VideoRecording _videoRecording;

		public bool IsRecording => _videoRecording != null && _videoRecording.IsRecording;

        public Recorder()
		{
		}

		public void Start(System.Drawing.Rectangle coordinates)
		{
			if (_videoRecording != null && _videoRecording.IsRecording)
			{
				throw new Exception("Is recording");
			}

			//resolution can't be odd
			coordinates.Width = coordinates.Width % 2 == 0 ? coordinates.Width : coordinates.Width - 1;
			coordinates.Height = coordinates.Height % 2 == 0 ? coordinates.Height : coordinates.Height - 1;

			var options = new VideoRecordingOptions
			{
				X = coordinates.X,
				Y = coordinates.Y,
				Width = coordinates.Width,
				Height = coordinates.Height,
				FrameRate = FRAME_RATE,
				Path = Properties.Settings.Default.SaveToPath,
				Name = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}",
				FileContainer = FILE_CONTAINER
            };

			CreateInputDirectoryIfNotExists(options.Path);

            _videoRecording = new VideoRecording(options);

            _videoRecording.Start();
        }

		public void Stop()
		{
			if (!_videoRecording.IsRecording)
			{
				throw new Exception("Is not recording");
			}

			_videoRecording.StopCapturing();
        }

		public async Task FinishRecording()
		{
			if (!_videoRecording.IsRecording)
			{
				throw new Exception("Is not recording");
			}

			await _videoRecording.FinishRecording();
			_videoRecording.Dispose();

			var coverWriter = new CoverWriter(_videoRecording.VideoRecordingOptions);
			coverWriter.Write();
			coverWriter.Dispose();
        }

        private void CreateInputDirectoryIfNotExists(string path)
        {
            bool directoryExists = Directory.Exists(path);

            if (!directoryExists)
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
