using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace soundapp
{
    public partial class MainWindow : Window
    {
        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private Forms.NotifyIcon _notifyIcon;
        private string _soundFilePath;
        private DispatcherTimer _visualizerTimer;

        // Global Keyboard Hook variables
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static MainWindow? _instance;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;

            // Setup default sound
            _soundFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "click.wav");
            UpdateSoundFile();

            // Setup System Tray Icon
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = false,
                Text = "Sound Studio App"
            };
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            
            // Setup Visualizer Timer for the Waveform
            _visualizerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _visualizerTimer.Tick += (s, e) =>
            {
                WaveformPanel.Opacity = 0.5; // Dim down
                KeyDisplay.Text = "READY";
                _visualizerTimer.Stop();
            };

            // Setup Keyboard Hook
            _hookID = SetHook(_proc);
        }

        private void UpdateSoundFile()
        {
            if (File.Exists(_soundFilePath))
            {
                _mediaPlayer.Open(new Uri(_soundFilePath));
                CurrentFileText.Text = Path.GetFileName(_soundFilePath);
            }
            else
            {
                CurrentFileText.Text = "NO SOUND";
            }
        }

        public void PlaySoundAndFlash(string keyName)
        {
            Dispatcher.Invoke(() =>
            {
                if (File.Exists(_soundFilePath))
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                    _mediaPlayer.Play();
                }

                // Flash visualizer
                WaveformPanel.Opacity = 1.0;
                KeyDisplay.Text = keyName.ToUpper();

                _visualizerTimer.Stop();
                _visualizerTimer.Start();
            });
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_soundFilePath))
            {
                _mediaPlayer.Play();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_soundFilePath))
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
                    YoutubeStatusText.Text = $"Downloading audio... ({video.Title})";
                    
                    string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{streamInfo.Container}");
                    
                    await youtube.Videos.Streams.DownloadAsync(streamInfo, tempFile);

                    YoutubeStatusText.Text = "Ready!";
                    
                    _soundFilePath = tempFile;
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
            UnhookWindowsHookEx(_hookID);
            _notifyIcon.Dispose();
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(vkCode);
                _instance?.PlaySoundAndFlash(key.ToString());
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}
