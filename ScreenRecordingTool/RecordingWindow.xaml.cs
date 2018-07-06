using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
	        BindColorPicker();
        }


	    private void BindColorPicker()
	    {
		    var colors = new Dictionary<SolidColorBrush, Color> {
			    {
				    new SolidColorBrush(Color.FromRgb(255, 255, 60)) , Color.FromArgb(128, 255, 255, 60)
			    },
			    {
				    new SolidColorBrush(Color.FromRgb(30, 180, 255)) , Color.FromArgb(128, 30, 180, 255)
			    },
			    {
				    new SolidColorBrush(Color.FromRgb(255, 30, 30)) , Color.FromArgb(128, 255, 30, 30)
			    }};

		    DrawingColorsCombo.ItemsSource = colors;
		    DrawingColorsCombo.SelectedValue = colors.FirstOrDefault();
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
			DrawingBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mw.IsDrawing ? Constans.BTN_COLOR_ACTIVE : Constans.BTN_COLOR));
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
		    TextBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mw.IsText ? Constans.BTN_COLOR_ACTIVE : Constans.BTN_COLOR));
	    }

		private void DrawingColorsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var mw = ((MainWindow)Application.Current.MainWindow);
			var selectedItem = (KeyValuePair<SolidColorBrush, Color>?)(sender as ComboBox).SelectedItem;

			if (selectedItem == null)
				return;

			mw.ChangeCanvasColor(selectedItem.Value.Value);
		}
	}
}
