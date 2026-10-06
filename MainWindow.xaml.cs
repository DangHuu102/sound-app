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
        private string? _currentYoutubeUrl;
        private AudioDeviceService? _audioService;
        private bool _isUpdatingSlider = false;
        private CancellationTokenSource? _playCts;
        private System.Collections.ObjectModel.ObservableCollection<TrackItem> _queueItems = new();

        public MainWindow()
        {
            InitializeComponent();
            DatabaseManager.InitializeDatabase();
            DatabaseManager.InitializePlaylists();
            LoadHistory();
            LoadPlaylists();
            QueueList.ItemsSource = _queueItems;
            CurrentFileText.Text = "Ready to play";
            SetupTrayIcon();
            InitAudioDevice();
            SetupMediaPlayer();
            UpdateUserUI();
        }

        public void UpdateUserUI()
        {
            if (App.CurrentUser != null)
            {
                AuthPanel.Visibility = Visibility.Collapsed;
                UserProfilePanel.Visibility = Visibility.Visible;
                UserNameText.Text = App.CurrentUser.DisplayName;
            }
            else
            {
                AuthPanel.Visibility = Visibility.Visible;
                UserProfilePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUser = null;
            UpdateUserUI();
        }

        private void SetupMediaPlayer()
        {
            _mediaPlayer.MediaOpened += (s, e) =>
            {
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
                    YoutubeStatusText.Text = "Lỗi phát âm thanh";
                });
            };

            _mediaPlayer.MediaEnded += (s, e) =>
            {
                // Đảm bảo bài hát thực sự đã phát được một lúc (tránh lỗi WMP bỏ qua bài liền lập tức)
                if (!_mediaPlayer.NaturalDuration.HasTimeSpan || _mediaPlayer.Position.TotalSeconds < 1)
                {
                    return; 
                }

                Dispatcher.Invoke(() =>
                {
                    if (_queueItems.Count > 0)
                    {
                        var nextTrack = _queueItems[0];
                        _queueItems.RemoveAt(0);
                        _ = PlayTrackAsync(nextTrack.YoutubeUrl);
                    }
                    else
                    {
                        _isPlaying = false;
                        PlayPauseBtn.Tag = "paused";
                        YoutubeStatusText.Text = "🎵 Đã phát xong";
                    }
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

        private void HistoryItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            e.Handled = true;
            System.IO.File.AppendAllText("debug_log.txt", $"[{DateTime.Now}] HistoryItem_Click Fired!\n");
            if ((sender as System.Windows.FrameworkElement)?.DataContext is PlayHistory history)
            {
                System.IO.File.AppendAllText("debug_log.txt", $"[{DateTime.Now}] Playing History: {history.Url}\n");
                YoutubeUrlTextBox.Text = history.Url;
                Dispatcher.InvokeAsync(() => { _ = PlayTrackAsync(history.Url); });
            }
        }

        private void QueueItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            e.Handled = true;
            System.IO.File.AppendAllText("debug_log.txt", $"[{DateTime.Now}] QueueItem_Click Fired!\n");
            if ((sender as System.Windows.FrameworkElement)?.DataContext is TrackItem track)
            {
                System.IO.File.AppendAllText("debug_log.txt", $"[{DateTime.Now}] Playing Queue: {track.YoutubeUrl}\n");
                Dispatcher.InvokeAsync(() => 
                {
                    _queueItems.Remove(track);
                    YoutubeUrlTextBox.Text = track.YoutubeUrl;
                    _ = PlayTrackAsync(track.YoutubeUrl);
                });
            }
        }

        private async System.Threading.Tasks.Task PlayTrackAsync(string input)
        {
            System.IO.File.AppendAllText("debug_log.txt", $"[{DateTime.Now}] PlayTrackAsync called with input: {input}\n");
            if (string.IsNullOrWhiteSpace(input)) return;

            // Hủy request tải nhạc trước đó nếu user bấm liên tục (Khởi tạo CancellationToken trước)
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            Dispatcher.Invoke(() =>
            {
                HistoryList.IsEnabled = false; // Tạm khóa để tránh spam click làm giật máy
                YoutubeUrlTextBox.IsEnabled = false;
                YoutubeStatusText.Text = "Đang xử lý...";
            });

            try
            {
                // Kiểm tra xem input có phải là URL hợp lệ không
                bool isUrl = Uri.TryCreate(input, UriKind.Absolute, out var uriResult) 
                             && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                
                string targetUrl = input;

                // Nếu không phải URL -> Coi như là câu truy vấn tìm kiếm
                if (!isUrl)
                {
                    Dispatcher.Invoke(() => YoutubeStatusText.Text = $"Đang tìm kiếm '{input}'...");
                    
                    // 1. Tìm trong lịch sử Local (Smart Search)
                    string? localMatch = DatabaseManager.SearchLocalTrack(input);
                    if (!string.IsNullOrEmpty(localMatch))
                    {
                        targetUrl = localMatch;
                    }
                    else
                    {
                        // 2. Tìm trên YouTube (Cấp token để có thể hủy nếu user spam tìm kiếm)
                        var searchResult = await _youtube.Search.GetVideosAsync(input).FirstOrDefaultAsync(token);
                        if (searchResult == null)
                        {
                            Dispatcher.Invoke(() => ShowNotification("Không tìm thấy kết quả nào!", "warning"));
                            return;
                        }
                        targetUrl = searchResult.Url;
                    }

                    // Cập nhật lại TextBox cho đúng URL
                    Dispatcher.Invoke(() => YoutubeUrlTextBox.Text = targetUrl);
                }

                Dispatcher.Invoke(() => YoutubeStatusText.Text = "Đang tải audio...");
                string streamUrl = DatabaseManager.GetValidStreamUrl(targetUrl) ?? "";
                string title = "";
                string thumbUrl = "";

                // Get minimal video info if we don't have stream or need metadata
                var video = await _youtube.Videos.GetAsync(targetUrl, token);
                title = video.Title;
                thumbUrl = video.Thumbnails.GetWithHighestResolution().Url;

                    if (string.IsNullOrEmpty(streamUrl))
                    {
                        var manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id, token);
                        // Chỉ lấy định dạng mp4/m4a vì WPF MediaPlayer không giải mã được WebM/Opus mặc định
                        var streamInfo = manifest.GetAudioOnlyStreams()
                            .Where(s => s.Container.Name == "mp4" || s.Container.Name == "m4a")
                            .GetWithHighestBitrate();

                        // Fallback 1: Nếu không có m4a, thử lấy luồng video+audio định dạng mp4 (WPF vẫn phát được tiếng)
                        if (streamInfo == null)
                        {
                            streamInfo = manifest.GetMuxedStreams()
                                .Where(s => s.Container.Name == "mp4")
                                .GetWithHighestBitrate();
                        }

                        if (streamInfo == null)
                        {
                            Dispatcher.Invoke(() => ShowNotification("Bài hát này không hỗ trợ định dạng MP4. Vui lòng thử bài khác!", "error"));
                            return;
                        }

                        if (token.IsCancellationRequested) return;
                        streamUrl = streamInfo.Url;
                        
                        // Lưu vào cache để dùng lại
                        DatabaseManager.SaveStreamUrl(targetUrl, streamUrl);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        _mediaPlayer.Stop();
                        _mediaPlayer.Close();

                        _soundFilePath = streamUrl;
                        _currentThumbnailUrl = thumbUrl;
                        _currentYoutubeUrl = targetUrl;

                        try
                        {
                            var bmp = new System.Windows.Media.Imaging.BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(_currentThumbnailUrl);
                            bmp.DecodePixelWidth = 320; 
                            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // Sửa lỗi rò rỉ bộ nhớ
                            bmp.EndInit();
                            bmp.Freeze(); // Cho phép dùng cross-thread và giải phóng nhanh
                            NowPlayingImage.Source = bmp;
                        }
                        catch { }

                        CurrentFileText.Text = title;
                        _mediaPlayer.Open(new Uri(_soundFilePath));
                        
                        // Thêm history ngầm không block UI
                        System.Threading.Tasks.Task.Run(async () => 
                        {
                            try
                            {
                                var meta = DatabaseManager.ParseMetadata(title);
                                DatabaseManager.AddHistory(title, targetUrl, _currentThumbnailUrl);
                                Dispatcher.InvokeAsync(() => LoadHistory()); // InvokeAsync chống deadlock
                                
                                // Lấy gợi ý bài hát mới và cho vào Queue (Up Next)
                                var recommendations = await RecommendationService.GetRecommendationsAsync(meta.SongTitle, meta.Artist);
                                
                                // Hủy add nếu user đã next bài khác
                                if (token.IsCancellationRequested) return;

                                Dispatcher.InvokeAsync(() => 
                                {
                                    // Bổ sung nút Toggle tính năng tự play sau này (tạm thời cứ check count)
                                    if (_queueItems.Count < 5)
                                    {
                                        foreach (var track in recommendations)
                                        {
                                            if (!_queueItems.Any(q => q.YoutubeUrl == track.YoutubeUrl))
                                            {
                                                _queueItems.Add(track);
                                            }
                                        }
                                    }
                                });
                            }
                            catch { } // Tránh UnobservedTaskException gây crash toàn app
                        });
                    });
            }
            catch (OperationCanceledException) { 
                System.IO.File.AppendAllText("debug_log.txt", $"[{DateTime.Now}] PlayTrackAsync canceled.\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("debug_log.txt", $"[{DateTime.Now}] PlayTrackAsync Exception: {ex.Message}\n{ex.StackTrace}\n");
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
            _queueItems.Clear(); // Tự động xóa luôn danh sách chờ
        }

        private void ClearQueueBtn_Click(object sender, RoutedEventArgs e)
        {
            _queueItems.Clear();
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

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentYoutubeUrl))
            {
                ShowNotification("Vui lòng tải một bài hát từ YouTube trước khi tải xuống!", "warning");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Lưu Audio (MP3)",
                Filter = "Audio Files (*.mp3)|*.mp3",
                FileName = CurrentFileText.Text + ".mp3"
            };

            if (dialog.ShowDialog() == true)
            {
                string savePath = dialog.FileName;
                string urlToDownload = _currentYoutubeUrl;

                DownloadProgressGrid.Visibility = Visibility.Visible;
                DownloadProgressBar.Value = 0;
                DownloadProgressText.Text = "0%";

                try
                {
                    var video = await _youtube.Videos.GetAsync(urlToDownload);
                    var manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id);
                    var streamInfo = manifest.GetAudioOnlyStreams()
                        .Where(s => s.Container.Name == "mp4" || s.Container.Name == "m4a")
                        .GetWithHighestBitrate();

                    if (streamInfo == null)
                    {
                        streamInfo = manifest.GetMuxedStreams()
                            .Where(s => s.Container.Name == "mp4")
                            .GetWithHighestBitrate();
                    }

                    if (streamInfo == null)
                    {
                        ShowNotification("Lỗi: Không tìm thấy link tải MP4 tương thích!", "error");
                        DownloadProgressGrid.Visibility = Visibility.Collapsed;
                        return;
                    }

                    var progress = new Progress<double>(p =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            DownloadProgressBar.Value = p * 100;
                            DownloadProgressText.Text = $"{(int)(p * 100)}%";
                        });
                    });

                    await _youtube.Videos.Streams.DownloadAsync(streamInfo, savePath, progress);
                    ShowNotification("✅ Tải xuống thành công!", "info");
                }
                catch (Exception ex)
                {
                    ShowNotification($"Lỗi tải xuống: {ex.Message}", "error");
                }
                finally
                {
                    DownloadProgressGrid.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingSlider) return;
            
            _audioService?.SetMasterVolumeAsync((float)e.NewValue);
            if (VolumePercentText != null)
            {
                VolumePercentText.Text = (int)(e.NewValue * 100) + "%";
            }
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
