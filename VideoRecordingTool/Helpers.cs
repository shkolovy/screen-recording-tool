using System.Diagnostics;

namespace VideoRecordingTool
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
