using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace soundapp
{
    public partial class MainWindow : Window
    {
        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private Forms.NotifyIcon _notifyIcon;
        private string _soundFilePath;
        private string _currentThumbnailUrl;

        public MainWindow()
        {
            InitializeComponent();
            DatabaseManager.InitializeDatabase();
            LoadHistory();

            // Setup default sound
            CurrentFileText.Text = "Ready to play";

            // Setup System Tray Icon
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = false,
                Text = "Sound Studio App"
            };
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private void LoadHistory()
        {
            try
            {
                var history = DatabaseManager.GetHistory();
                HistoryList.ItemsSource = history;
            }
            catch { }
        }

        private void HistoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (HistoryList.SelectedItem is PlayHistory history)
            {
                YoutubeUrlTextBox.Text = history.Url;
                LoadYoutubeButton_Click(null, null);
            }
        }

        private void MiniPlayerBtn_Click(object sender, RoutedEventArgs e)
        {
            var miniPlayer = new MiniPlayerWindow(this, CurrentFileText.Text, _currentThumbnailUrl);
            this.Hide();
            miniPlayer.Show();
        }

        private void UpdateSoundFile()
        {
            if (!string.IsNullOrEmpty(_soundFilePath))
            {
                _mediaPlayer.Open(new Uri(_soundFilePath));
            }
            else
            {
                CurrentFileText.Text = "NO SOUND";
            }
        }



        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_soundFilePath))
            {
                _mediaPlayer.Play();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_soundFilePath))
            {
                _mediaPlayer.Stop();
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _soundFilePath = openFileDialog.FileName;
                UpdateSoundFile();
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = e.NewValue;
            }
        }

        private async void LoadYoutubeButton_Click(object sender, RoutedEventArgs e)
        {
            string url = YoutubeUrlTextBox.Text;
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                YoutubeStatusText.Text = "Loading video info...";
                YoutubeUrlTextBox.IsEnabled = false;

                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(url);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                
                if (streamInfo != null)
                {
                    YoutubeStatusText.Text = $"Streaming audio... ({video.Title})";
                    
                    _currentThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url;
                    try { NowPlayingImage.Source = new BitmapImage(new Uri(_currentThumbnailUrl)); } catch { }
                    
                    DatabaseManager.AddHistory(video.Title, url, _currentThumbnailUrl);
                    LoadHistory();

                    _soundFilePath = streamInfo.Url;
                    CurrentFileText.Text = video.Title;
                    
                    if (VolumeSlider != null)
                    {
                        _mediaPlayer.Open(new Uri(_soundFilePath));
                        _mediaPlayer.Volume = VolumeSlider.Value;
                        _mediaPlayer.Play(); // Auto play
                    }
                }
                else
                {
                    YoutubeStatusText.Text = "No audio stream found.";
                }
            }
            catch (Exception ex)
            {
                YoutubeStatusText.Text = "Error: " + ex.Message;
            }
            finally
            {
                YoutubeUrlTextBox.IsEnabled = true;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(2000, "Sound Studio", "Running in background.", Forms.ToolTipIcon.Info);
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            _notifyIcon.Visible = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _notifyIcon.Dispose();
        }
    }
}
