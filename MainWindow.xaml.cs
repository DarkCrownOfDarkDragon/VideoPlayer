using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace KP_IT
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] Videoformats; // Содержит данные о поддерживающихся форматах
        List<string> VideoQueue;
        int CurrentVideoPositionInQueue;

        DispatcherTimer timerVideoTime;

        ImageBrush PlayImageBrush;
        ImageBrush PauseImageBrush;

        bool didPausedVideo; // До действия 
        bool IsPausedVideo; // На данный момент

        DispatcherTimer timerForDisappiar;

        bool FullScreenMode; 
        WindowState state; // Для выхода из полного экрана
        WindowStyle style;

        bool mouseOnObjects;

        public MainWindow()
        {
            InitializeComponent();

            Videoformats = new string[] { ".mp4", ".wmv"};
            VideoQueue = new List<string>();

            timerVideoTime = new DispatcherTimer(); // Время для воспроизведения видео
            timerVideoTime.Interval = TimeSpan.FromSeconds(0.1);
            timerVideoTime.Tick += new EventHandler(timerForVideo_Tick);

            timerForDisappiar = new DispatcherTimer();
            timerForDisappiar.Tick += new EventHandler(timerTick);

            PlayImageBrush = new ImageBrush(new BitmapImage(new Uri("Play.png", UriKind.Relative))) { Stretch = Stretch.Uniform };
            PauseImageBrush = new ImageBrush(new BitmapImage(new Uri("Pause.png",UriKind.Relative))) { Stretch = Stretch.Uniform };
        }
        private void GetVideo_Click(object sender, RoutedEventArgs e)
        {   
            OpenFileDialog open = new OpenFileDialog(); // Диалоговое окно
            open.Filter = "MP4(.mp4) |*.mp4|Windows Media Video(.wmv) |*.wmv|" +
                "Windows Media Video Advanced Profile(.wmva) |*wmva|Windows Media Video Advanced Profile(.wmvc1)|*wmvc1";

            if (!open.ShowDialog().Value) { return; } // Если закрыли

            VideoQueue.Clear();

            for (int i = 0; i < Videoformats.Length; i++)
                VideoQueue.AddRange(Directory.GetFiles(System.IO.Path.GetDirectoryName(open.FileName), "*" + Videoformats[i], SearchOption.TopDirectoryOnly));

            CurrentVideoPositionInQueue = VideoQueue.FindIndex(x => x == open.FileName);

            GetVideoFile(open.FileName);
        }
        private void GetVideoFile(string uri)
        {
            TimeSlider.IsEnabled = false;
            PauseVideo();
            IsPausedVideo = true;
            SpeedLabel.Content = "x1";

            MediaEl.Source = new Uri(uri);

            BeginTimeLabel.Content = "00:00:00";
            MediaEl.SpeedRatio = 1;
            TimeSlider.Value = 0;
            TimeSlider.IsEnabled = true;

            MediaEl.Play(); // Для инициализации
            MediaEl.Pause();
        }
        private void MixVideo_Click(object sender, RoutedEventArgs e)
        {
            if (MediaEl.Source == null) return;

            string currentVideo = MediaEl.Source.OriginalString;

            Random rnd = new Random();
            for (int i = 0; i < VideoQueue.Count - 1; i++)
            {
                int IndexOffirstVideo = rnd.Next(VideoQueue.Count - 1);
                int IndexOfsecondVideo = rnd.Next(VideoQueue.Count - 1);
                string temp = VideoQueue[IndexOffirstVideo];
                VideoQueue[IndexOffirstVideo] = VideoQueue[IndexOfsecondVideo];
                VideoQueue[IndexOfsecondVideo] = temp;
            }

            CurrentVideoPositionInQueue = VideoQueue.FindIndex(x => x == currentVideo);
        } 
        private void NextVideo_Click(object sender, RoutedEventArgs e)
        {
            if (MediaEl.Source == null) return;

            if (CurrentVideoPositionInQueue == VideoQueue.Count - 1) CurrentVideoPositionInQueue = 0;
            else CurrentVideoPositionInQueue++;

            GetVideoFile(VideoQueue[CurrentVideoPositionInQueue]);
        }
        private void PreviousVideo_Click(object sender, RoutedEventArgs e) 
        {
            if (MediaEl.Source == null) return;

            if (CurrentVideoPositionInQueue == 0) CurrentVideoPositionInQueue = VideoQueue.Count - 1;
            else CurrentVideoPositionInQueue--;

            GetVideoFile(VideoQueue[CurrentVideoPositionInQueue]);
        }
        private void timerForVideo_Tick(object sender, EventArgs e) 
        {
            TimeSlider.Value = MediaEl.Position.TotalSeconds;
            TimeSlider.SelectionEnd = MediaEl.Position.TotalSeconds;
        }
        private void TimerSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) 
        {
            if (MediaEl.Source != null && IsPausedVideo) didPausedVideo = true;
            else PauseVideo();
        }
        private void TimerSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) 
        {
            if (MediaEl.Source == null) return;
            if (!didPausedVideo) ResumeVideo();
        }
        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) 
        {
            if (MediaEl.Source == null) { TimeSlider.Value = 0; return; }

            MediaEl.Position = TimeSpan.FromSeconds(TimeSlider.Value);
            TimeSlider.SelectionEnd = TimeSlider.Value; // Визуальное оформление прошедшего времени
            BeginTimeLabel.Content = MediaEl.Position.ToString("hh':'mm':'ss");
        }
        private void MediaEl_MediaOpened(object sender, RoutedEventArgs e)
        {
            TimeSlider.Maximum = MediaEl.NaturalDuration.TimeSpan.TotalSeconds;
            TimeSlider.Minimum = 0;
            EndTimeLabel.Content = MediaEl.NaturalDuration.TimeSpan;
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e) 
        {
            PlayVideo();
        }
        private void MediaEl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PlayVideo();
        } 
        private void PlayVideo()
        {
            if (MediaEl.Source == null) return;
            try
            {
                if (IsPausedVideo) ResumeVideo();
                else PauseVideo();
            }
            catch { }
        }
        private void PauseVideo()
        {
            MediaEl.Pause();
            IsPausedVideo = true;
            timerVideoTime.Stop();
            PlayButton.Background = null;
            PlayButton.Background = PlayImageBrush;
            didPausedVideo = true;
        }
        private void ResumeVideo()
        {
            MediaEl.Play();
            IsPausedVideo = false;
            timerVideoTime.Start();
            PlayButton.Background = null;
            PlayButton.Background = PauseImageBrush;
            didPausedVideo = false;
        }
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) 
        {
            MediaEl.Volume = VolumeSlider.Value;
            VolumeSlider.SelectionEnd = VolumeSlider.Value;
        }
        private void Left_Click(object sender, RoutedEventArgs e)
        {
            MinusTenSec();
        } 
        private void Right_Click(object sender, RoutedEventArgs e)
        {
            PlusTenSec();
        } 
        private void PlusTenSec()
        {
            if (MediaEl.Source == null) return;

            TimeSlider.Value += 10;
        } 
        private void MinusTenSec()
        {
            if (MediaEl.Source == null) return;
            TimeSlider.Value -= 10;
        } 
        private void TimeSlider_KeyUp(object sender, KeyEventArgs e)
        {
            RestartTimeForDissapiar();
            if (e.Key == Key.Left) MinusTenSec();
            if (e.Key == Key.Right) PlusTenSec();
        } 
        private void FullScreen_Click(object sender, RoutedEventArgs e) 
        {
            if (!FullScreenMode)
            {
                FullScreenMode = true;
                state = MediaPlayerWindow.WindowState;
                style = MediaPlayerWindow.WindowStyle;
                Visibility = Visibility.Collapsed;
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
                Topmost = true;
                Visibility = Visibility.Visible;
            }
            else
            {
                FullScreenMode = false;
                Topmost = false;
                WindowState = state;
                WindowStyle = style;
            }
        }
        private void SpeedLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) 
        {
            if (MediaEl.Source == null) return;

            if (MediaEl.SpeedRatio != 2)
            {
                MediaEl.SpeedRatio += 0.25;
                SpeedLabel.Content = $"x{MediaEl.SpeedRatio}";
            }
            else
            {
                MediaEl.SpeedRatio = 0.25;
                SpeedLabel.Content = "x0.25";
            }
        }
        private void MediaPlayerWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if(!mouseOnObjects) RestartTimeForDissapiar();
        }
        private void timerTick(object sender, EventArgs e)
        {
            this.Cursor = Cursors.None;
            DoubleAnimation HiddenAnimation = new DoubleAnimation();
            HiddenAnimation.From = 1.0;
            HiddenAnimation.To = 0.0;
            HiddenAnimation.Duration = TimeSpan.FromSeconds(1);
            SliderGrid.BeginAnimation(OpacityProperty, HiddenAnimation);
            timerForDisappiar.Stop();
        }
        private void RestartTimeForDissapiar()
        {
            StopTimeForDissapiar();
            StartTimeForDissapiar();
        }
        private void StopTimeForDissapiar()
        {
            SliderGrid.BeginAnimation(OpacityProperty, null);
            this.Cursor = Cursors.Arrow;

            timerForDisappiar.Stop();
            timerForDisappiar.Interval = new TimeSpan(0, 0, 3);
        }
        private void StartTimeForDissapiar()
        {
            timerForDisappiar.Start();
        }
        private void VolumeSlider_KeyUp(object sender, KeyEventArgs e)
        {
            RestartTimeForDissapiar();
        }
        private void Objects_MouseEnter(object sender, MouseEventArgs e)
        {
            StopTimeForDissapiar();
            mouseOnObjects = true;
        }
        private void Objects_MouseLeave(object sender, MouseEventArgs e)
        {
            StartTimeForDissapiar();
            mouseOnObjects = false;
        }
    }
}
