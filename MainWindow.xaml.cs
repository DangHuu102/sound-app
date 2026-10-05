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
        private AudioDeviceService? _audioService;
        private bool _isUpdatingSlider = false;
        private CancellationTokenSource? _playCts;

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
            SetupMediaPlayer();
        }

        private void SetupMediaPlayer()
        {
            _mediaPlayer.MediaOpened += (s, e) =>
            {
                // Giữ volume MediaPlayer nội bộ luôn ở mức 100% để tránh lỗi Double-Scaling
                // Volume thực sự sẽ do Windows Master Volume quyết định qua _audioService.
                _mediaPlayer.Volume = 1.0; 
                _mediaPlayer.Play();
                Dispatcher.Invoke(() =>
                {
                    _isPlaying = true;
                    PlayPauseBtn.Tag = "playing";
                    YoutubeStatusText.Text = $"▶ {CurrentFileText.Text}";
                    PlayPauseBtn.Opacity = 1.0;
                });
            };

            _mediaPlayer.MediaFailed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ShowNotification($"Lỗi phát nhạc: {e.ErrorException?.Message}", "error");
                    _isPlaying = false;
                    PlayPauseBtn.Tag = "paused";
                    YoutubeStatusText.Text = "";
                });
            };

            _mediaPlayer.MediaEnded += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _isPlaying = false;
                    PlayPauseBtn.Tag = "paused";
                    YoutubeStatusText.Text = "⏹ Đã phát xong";
                });
            };
        }

        private void InitAudioDevice()
        {
            _audioService = new AudioDeviceService();

            _audioService.DeviceChanged += (deviceName) =>
            {
                Dispatcher.Invoke(() =>
                {
                    DeviceNameText.Text = deviceName;
                    DeviceTypeText.Text = deviceName.Contains("Not Found") ? "⚠️ Cắm tai nghe/loa" : "🔊 Audio Output";
                    if (deviceName.Contains("Not Found")) ShowNotification("Không tìm thấy thiết bị âm thanh!", "error");
                });
            };

            _audioService.VolumeChanged += (volume) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _isUpdatingSlider = true;
                    VolumeSlider.Value = volume;
                    VolumePercentText.Text = (int)(volume * 100) + "%";
                    _isUpdatingSlider = false;
                });
            };
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

        private void HistoryList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (HistoryList.SelectedItem is PlayHistory history)
            {
                YoutubeUrlTextBox.Text = history.Url;
                _ = PlayTrackAsync(history.Url);
                // Xóa chọn để người dùng có thể click lại bài này nếu muốn
                HistoryList.SelectedItem = null;
            }
        }

        private async System.Threading.Tasks.Task PlayTrackAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            // Hủy request tải nhạc trước đó nếu user bấm liên tục
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            Dispatcher.Invoke(() =>
            {
                HistoryList.IsEnabled = false; // Tạm khóa để tránh spam click làm giật máy
                YoutubeUrlTextBox.IsEnabled = false;
                YoutubeStatusText.Text = "Đang tải audio...";
            });

            try
            {
                var video = await _youtube.Videos.GetAsync(url, token);
                var manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id, token);
                var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (streamInfo == null || token.IsCancellationRequested) return;

                Dispatcher.Invoke(() =>
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Close();

                    _soundFilePath = streamInfo.Url;
                    _currentThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url;

                    try
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(_currentThumbnailUrl);
                        bmp.DecodePixelWidth = 320; 
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.None;
                        bmp.EndInit();
                        NowPlayingImage.Source = bmp;
                    }
                    catch { }

                    CurrentFileText.Text = video.Title;
                    _mediaPlayer.Open(new Uri(_soundFilePath));
                    
                    // Thêm history ngầm không block UI
                    System.Threading.Tasks.Task.Run(() => 
                    {
                        DatabaseManager.AddHistory(video.Title, url, _currentThumbnailUrl);
                        Dispatcher.Invoke(() => LoadHistory());
                    });
                });
            }
            catch (OperationCanceledException) { /* Bỏ qua nếu bị cancel do bấm bài mới */ }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowNotification($"Lỗi tải YouTube: {ex.Message}", "error"));
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    HistoryList.IsEnabled = true;
                    YoutubeUrlTextBox.IsEnabled = true;
                    if (YoutubeStatusText.Text == "Đang tải audio...") YoutubeStatusText.Text = "";
                });
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

        private void EmailSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SmtpSettingsWindow { Owner = this };
            settings.ShowDialog();
        }

        private void ShowNotification(string msg, string type = "info")
        {
            ToastMessage.Text = msg;
            if (type == "error")
            {
                ToastIcon.Text = "❌";
                ToastNotification.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4D6D"));
            }
            else if (type == "warning")
            {
                ToastIcon.Text = "⚠️";
                ToastNotification.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFAA00"));
            }
            else
            {
                ToastIcon.Text = "✅";
                ToastNotification.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9B4DFF"));
            }

            ToastNotification.Visibility = Visibility.Visible;

            // Tự ẩn sau 3.5 giây
            _ = System.Threading.Tasks.Task.Delay(3500).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() => ToastNotification.Visibility = Visibility.Collapsed);
            });
        }

        private bool _isPlaying = false;

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_soundFilePath))
            {
                ShowNotification("Chưa có bài hát! Hãy chọn bài hoặc dán link YouTube.", "warning");
                return;
            }

            if (_isPlaying)
            {
                _mediaPlayer.Pause();
                _isPlaying = false;
                PlayPauseBtn.Tag = "paused";
                YoutubeStatusText.Text = "⏸ Paused";
            }
            else
            {
                // Nếu đang ở cuối bài thì phát lại từ đầu
                if (_mediaPlayer.NaturalDuration.HasTimeSpan && _mediaPlayer.Position >= _mediaPlayer.NaturalDuration.TimeSpan)
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                }

                _mediaPlayer.Play();
                _isPlaying = true;
                PlayPauseBtn.Tag = "playing";
                YoutubeStatusText.Text = $"▶ {CurrentFileText.Text}";
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_soundFilePath))
            {
                _mediaPlayer.Stop();
                _isPlaying = false;
                PlayPauseBtn.Tag = "paused";
                YoutubeStatusText.Text = "⏹ Stopped";
            }
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
                CurrentFileText.Text = System.IO.Path.GetFileName(_soundFilePath);
                _mediaPlayer.Open(new Uri(_soundFilePath));
                // KHÔNG Play ngay, user tự ấn Play. 
                _isPlaying = false;
                PlayPauseBtn.Tag = "paused";
                YoutubeStatusText.Text = "Đã tải file. Bấm Play để phát.";
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingSlider) return;
            
            _audioService?.SetMasterVolumeAsync((float)e.NewValue);
            VolumePercentText.Text = (int)(e.NewValue * 100) + "%";
        }

        private void MuteBtn_Click(object sender, RoutedEventArgs e)
        {
            _audioService?.ToggleMuteAsync();
            // Giao diện sẽ được cập nhật thông qua DeviceChanged event nếu cần
            MuteBtn.Tag = MuteBtn.Tag == null ? "muted" : null;
        }

        private void LoadYoutubeButton_Click(object sender, RoutedEventArgs e)
        {
            string url = YoutubeUrlTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowNotification("Vui lòng dán đường link YouTube vào ô trống!", "warning");
                return;
            }
            _ = PlayTrackAsync(url);
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
