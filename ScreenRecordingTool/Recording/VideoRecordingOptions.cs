namespace ScreenRecordingTool
{
	public class VideoRecordingOptions
	{
		public int FrameRate { get; set; }

		public int Height { get; set; }

		public int Width { get; set; }

		public string Path { get; set; }

		public string Name { get; set; }

		public int X { get; set; }

		public int Y { get; set; }

		public string FileContainer { get; set; }

		public string FileName
		{
			get { return $"{Name}.{FileContainer}"; }
		}

		public string FullPath
		{
			get { return $"{Path}\\{FileName}"; }
		}
	}
}