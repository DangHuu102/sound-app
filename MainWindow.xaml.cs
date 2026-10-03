using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using NAudio.CoreAudioApi;

namespace soundapp
{
    public partial class MainWindow : Window
    {
        // Dùng chung 1 instance, không tạo mới mỗi lần
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer();
        private readonly YoutubeClient _youtube = new YoutubeClient();
        private Forms.NotifyIcon _notifyIcon = null!;
        private string? _soundFilePath;
        private string? _currentThumbnailUrl;
        private MMDevice? _audioDevice;
        private bool _isUpdatingSlider = false;

        public MainWindow()
        {
            InitializeComponent();
            DatabaseManager.InitializeDatabase();
            DatabaseManager.InitializePlaylists();
            LoadHistory();
            LoadPlaylists();
            CurrentFileText.Text = "Ready to play";
            SetupTrayIcon();
            InitAudioDevice();
        }

        private void InitAudioDevice()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                _audioDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                // Đồng bộ slider với volume hiện tại của Windows
                _isUpdatingSlider = true;
                VolumeSlider.Value = _audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                _isUpdatingSlider = false;
            }
            catch { }
        }

        private void SetupTrayIcon()
        {
            var contextMenu = new Forms.ContextMenuStrip();
            var exitItem = new Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();
            contextMenu.Items.Add(exitItem);

            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "Sound Studio",
                ContextMenuStrip = contextMenu
            };
            _notifyIcon.DoubleClick += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
            };
        }

        private void LoadHistory()
        {
            try
            {
                HistoryList.ItemsSource = DatabaseManager.GetHistory();
            }
            catch { }
        }

        private void HistoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (HistoryList.SelectedItem is PlayHistory history)
            {
                YoutubeUrlTextBox.Text = history.Url;
                LoadYoutubeButton_Click(null!, null!);
            }
        }

        private void ClearHistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            DatabaseManager.ClearHistory();
            LoadHistory();
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            var signUp = new SignUpWindow();
            signUp.Show();
            this.Hide();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Hide();
        }

        private void LoadPlaylists()
        {
            try { PlaylistList.ItemsSource = DatabaseManager.GetPlaylists(); } catch { }
        }

        private void CreatePlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreatePlaylistDialog();
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.PlaylistName))
            {
                DatabaseManager.CreatePlaylist(dialog.PlaylistName);
                LoadPlaylists();
            }
        }

        private void MiniPlayerBtn_Click(object sender, RoutedEventArgs e)
        {
            var miniPlayer = new MiniPlayerWindow(this, CurrentFileText.Text, _currentThumbnailUrl ?? "");
            this.Hide();
            miniPlayer.Show();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_soundFilePath))
                _mediaPlayer.Play();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_soundFilePath))
                _mediaPlayer.Stop();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                _soundFilePath = dialog.FileName;
                _mediaPlayer.Open(new Uri(_soundFilePath));
                CurrentFileText.Text = System.IO.Path.GetFileName(_soundFilePath);
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingSlider) return;

            // Điều chỉnh âm lượng Windows thực sự (0.0 - 1.0)
            try
            {
                if (_audioDevice != null)
                    _audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = (float)e.NewValue;
            }
            catch { }

            // Cũng set cho MediaPlayer nội bộ
            _mediaPlayer.Volume = e.NewValue;

            // Cập nhật nhãn % hiển thị bên cạnh slider
            var pct = (int)(e.NewValue * 100);
            if (VolumePercentText != null)
                VolumePercentText.Text = pct + "%";
        }

        private void MuteBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_audioDevice == null) return;
                bool isMuted = _audioDevice.AudioEndpointVolume.Mute;
                _audioDevice.AudioEndpointVolume.Mute = !isMuted;

                // Cập nhật tooltip/tag để biết trạng thái
                MuteBtn.ToolTip = (!isMuted) ? "Unmute" : "Mute";

                // Thay đổi nội dung nút bằng cách rebuild template content
                var tb = (System.Windows.Controls.TextBlock)((System.Windows.Controls.Border)MuteBtn.Template.FindName("PART_Root", MuteBtn) ?? new System.Windows.Controls.Border()).Child;
                // Đơn giản hơn: dùng Tag để track state
                MuteBtn.Tag = !isMuted;
                VolumePercentText.Text = (!isMuted) ? "🔇 Muted" : ((int)(VolumeSlider.Value * 100)) + "%";
            }
            catch { }
        }

        private async void LoadYoutubeButton_Click(object sender, RoutedEventArgs e)
        {
            string url = YoutubeUrlTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(url)) return;

            YoutubeUrlTextBox.IsEnabled = false;
            YoutubeStatusText.Text = "Loading...";

            try
            {
                var video = await _youtube.Videos.GetAsync(url);
                var manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id);
                var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (streamInfo == null)
                {
                    YoutubeStatusText.Text = "No audio stream found.";
                    return;
                }

                // Dispose stream cũ trước khi mở cái mới
                _mediaPlayer.Stop();
                _mediaPlayer.Close();

                _soundFilePath = streamInfo.Url;
                _currentThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url;

                // Load thumbnail nhẹ hơn: giới hạn kích thước decode
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(_currentThumbnailUrl);
                    bmp.DecodePixelWidth = 320; // giới hạn decode để tiết kiệm RAM
                    bmp.CacheOption = BitmapCacheOption.None;
                    bmp.EndInit();
                    NowPlayingImage.Source = bmp;
                }
                catch { }

                CurrentFileText.Text = video.Title;
                YoutubeStatusText.Text = $"▶ {video.Title}";

                DatabaseManager.AddHistory(video.Title, url, _currentThumbnailUrl);
                LoadHistory();

                _mediaPlayer.Open(new Uri(_soundFilePath));
                _mediaPlayer.Volume = VolumeSlider?.Value ?? 1.0;
                _mediaPlayer.Play();
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
                _notifyIcon.ShowBalloonTip(1500, "Sound Studio", "Running in background.", Forms.ToolTipIcon.Info);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
