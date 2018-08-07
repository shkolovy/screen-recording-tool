using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace ScreenRecordingTool
{
    /// <summary>
    /// Interaction logic for RecordingWindow.xaml
    /// </summary>
    public partial class RecordingWindow : Window
    {
	    private StopWatch _stopWatch;

        public RecordingWindow()
        {
            InitializeComponent();
	        StopBtn.Focusable = false;
	        BindColorPicker();
	        _stopWatch = new StopWatch((TimeSpan time) => {
		        TimerLbl.Content = time.ToString();
	        });
        }

        public void StartTimer()
        {
	        _stopWatch.Start();
        }

        public void StopTimer()
        {
	        _stopWatch.Stop();
			_stopWatch.Reset();
        }

		private void Stop_Click(object sender, RoutedEventArgs e)
		{
			App.Stop();
		}

        #region Drawing

	    public void ResetControls()
	    {
		    ToggleBtn(false, RectangleBtn);
		    ToggleBtn(false, DrawingBtn);
		    ToggleBtn(false, TextBtn);
		    DrawingColorsCombo.Visibility = Visibility.Hidden;
        }

	    private void RectangleBtn_Click(object sender, RoutedEventArgs e)
	    {
		    var mw = MainWindow;
		    mw.ToggleText(false);
            mw.ToggleDrawing(false);
		    DrawingColorsCombo.Visibility = Visibility.Hidden;
            mw.ToggleRectangleDrawing(!mw.IsRectangleDrawing);

		    ToggleBtn(mw.IsRectangleDrawing, RectangleBtn);
		    ToggleBtn(false, DrawingBtn);
		    ToggleBtn(false, TextBtn);

            MainWindow.ClearDrawing();
        }

	    private void DrawingBtn_Click(object sender, RoutedEventArgs e)
	    {
		    var mw = MainWindow;
		    mw.ToggleText(false);
            mw.ToggleRectangleDrawing(false);
		    mw.ToggleDrawing(!mw.IsDrawing);
		    DrawingColorsCombo.Visibility = mw.IsDrawing ? Visibility.Visible : Visibility.Hidden;
            ToggleBtn(mw.IsDrawing, DrawingBtn);
		    ToggleBtn(false, RectangleBtn);
		    ToggleBtn(false, TextBtn);

            MainWindow.ClearDrawing();
        }

	    private void TextBtn_Click(object sender, RoutedEventArgs e)
	    {
		    var mw = MainWindow;
		    mw.ToggleRectangleDrawing(false);
		    mw.ToggleDrawing(false);
            mw.ToggleText(!mw.IsText);
		    DrawingColorsCombo.Visibility = Visibility.Hidden;
            ToggleBtn(mw.IsText, TextBtn);
		    ToggleBtn(false, RectangleBtn);
		    ToggleBtn(false, DrawingBtn);

		    MainWindow.ClearDrawing();
        }

        private void ClearDrawingBtn_Click(object sender, RoutedEventArgs e)
	    {
		    MainWindow.ClearDrawing();
	    }

	    private void DrawingColorsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	    {
		    var selectedItem = (KeyValuePair<SolidColorBrush, Color>?)(sender as ComboBox).SelectedItem;

		    if (selectedItem == null)
			    return;

		    MainWindow.ChangeCanvasColor(selectedItem.Value.Value);
	    }

	    private void BindColorPicker()
	    {
		    var colors = new Dictionary<SolidColorBrush, Color> {
			    {
					// yellow
				    new SolidColorBrush(Color.FromRgb(255, 255, 60)) , Color.FromArgb(128, 255, 255, 60)
			    },
			    {
					// blue
				    new SolidColorBrush(Color.FromRgb(30, 180, 255)) , Color.FromArgb(128, 30, 180, 255)
			    },
			    {
					// red
				    new SolidColorBrush(Color.FromRgb(255, 30, 30)) , Color.FromArgb(128, 255, 30, 30)
			    }};

		    DrawingColorsCombo.ItemsSource = colors;
		    DrawingColorsCombo.SelectedValue = colors.FirstOrDefault();
	    }

	    private void ToggleBtn(bool ative, Button btn)
	    {
		    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ative ? Constans.BTN_COLOR_ACTIVE : Constans.BTN_COLOR));
	    }

        #endregion


	    private App App
	    {
		    get
		    {
			    return (App) Application.Current;
		    }
	    }

	    private MainWindow MainWindow
	    {
		    get
		    {
			    return ((MainWindow) Application.Current.MainWindow);
		    }
	    }
    }
}
