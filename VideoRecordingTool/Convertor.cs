using MediaToolkit;
using MediaToolkit.Model;

namespace VideoRecordingTool
{
    public static class Convertor
    {
        public static void ConvertToMp4(string inputPath, string outputPath)
        {
            var inputFile = new MediaFile { Filename = inputPath };
            var outputFile = new MediaFile { Filename = outputPath };

            using (var engine = new Engine())
            {
                engine.Convert(inputFile, outputFile);
            }
        }
    }
}
