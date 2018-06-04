using System;
using System.Windows;
using System.Windows.Threading;

namespace VideoRecordingTool
{
    /// <summary>
    /// Interaction logic for RecordingWindow.xaml
    /// </summary>
    public partial class RecordingWindow : Window
    {
        DispatcherTimer _timer;

        public RecordingWindow()
        {
            InitializeComponent();
        }

        public void StartTimer()
        {
            var time = TimeSpan.FromSeconds(0);
            TimerLbl.Content = time;
            _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Background, (s, e) => {
                time = time.Add(TimeSpan.FromSeconds(1));
                TimerLbl.Content = time.ToString();
            }, Application.Current.Dispatcher);
        }

        public void StopTimer()
        {
            _timer.Stop();
        }
    }
}
