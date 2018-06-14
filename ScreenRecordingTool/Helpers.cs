using System.Diagnostics;

namespace ScreenRecordingTool
{
    public static class Helpers
    {
        public static void OpenFolder(string path)
        {
            var startInformation = new ProcessStartInfo { FileName = path };
            Process.Start(startInformation);
        }
    }
}
