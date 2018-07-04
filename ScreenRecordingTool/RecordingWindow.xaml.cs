using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;


namespace ScreenRecordingTool
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
	        StopBtn.Focusable = false;
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

		private void Stop_Click(object sender, RoutedEventArgs e)
		{
			((App)Application.Current).Stop();
		}

		private void DrawingBtn_Click(object sender, RoutedEventArgs e)
		{
			var mw = ((MainWindow) Application.Current.MainWindow);
			mw.ToggleDrawing(!mw.IsDrawing);
			ClearDrawingBtn.Visibility = mw.IsDrawing ? Visibility.Visible : Visibility.Hidden;
			DrawingBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mw.IsDrawing ? "#6EB1E1" : "#FFFFFC"));
		}

	    private void ClearDrawingBtn_Click(object sender, RoutedEventArgs e)
	    {
		    var mw = ((MainWindow)Application.Current.MainWindow);
		    mw.ClearDrawing();
		}


	    private void TextBtn_Click(object sender, RoutedEventArgs e)
	    {
		    var mw = ((MainWindow)Application.Current.MainWindow);
		    mw.ToggleText(!mw.IsText);
		    TextBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mw.IsText ? "#6EB1E1" : "#FFFFFC"));
	    }
	}
}
