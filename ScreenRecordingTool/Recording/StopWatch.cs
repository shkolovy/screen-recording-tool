using System;
using System.Windows.Threading;

namespace ScreenRecordingTool
{
	public sealed class StopWatch
	{
		private DispatcherTimer _timer;
		private TimeSpan _time;

		public bool IsActive { get; set; }

		public StopWatch(Action<TimeSpan> action)
		{
			_time = TimeSpan.FromSeconds(0);
			_timer = new DispatcherTimer();
			_timer.Interval = new TimeSpan(0, 0, 1);
			_timer.Tick += (s, e) => {
				_time = _time.Add(TimeSpan.FromSeconds(1));
				action(_time);
			};
		}

		public void Start()
		{
			_timer.Start();
			IsActive = true;
		}

		public void Stop()
		{
			_timer.Stop();
			IsActive = false;
		}

		public void Reset()
		{
			_time = TimeSpan.FromSeconds(0);
		}
	}
}