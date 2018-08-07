using System;
using System.Diagnostics;
using System.IO;

namespace ScreenRecordingTool.Recording
{
    public sealed class FfmpegProcessor
    {
	    private Process _ffmpegProcess;

        public FfmpegProcessor(string arguments)
	    {
		    var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");

		    if (!File.Exists(ffmpegPath))
		    {
			    throw new FileNotFoundException($"{ffmpegPath} not found");
		    }

            _ffmpegProcess = new Process
		    {
			    StartInfo =
			    {
				    FileName = ffmpegPath,
				    Arguments = arguments,
				    UseShellExecute = false,
				    CreateNoWindow = true,
				    RedirectStandardError = true,
				    RedirectStandardInput = true
			    },
			    EnableRaisingEvents = true
		    };

		    _ffmpegProcess.ErrorDataReceived += (s, e) => ProcessTheErrorData(s, e);
		    _ffmpegProcess.Start();
		    _ffmpegProcess.BeginErrorReadLine();
        }


	    public void WaitForExit()
	    {
			// ffmpeg.exe is selfclosing so just waiting for exit
		    _ffmpegProcess.WaitForExit();
        }

	    private void ProcessTheErrorData(object s, DataReceivedEventArgs e)
	    {
		    Trace.WriteLine($"FFMPEG - {e.Data}");
	    }
    }
}
