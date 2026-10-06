using System;
using System.IO;
using System.Text.RegularExpressions;

string file = @"d:\Solution1\soundapp\MainWindow.xaml.cs";
string text = File.ReadAllText(file);

// 1. Remove _mediaPlayer declaration and add NAudio variables
text = Regex.Replace(text, @"private readonly MediaPlayer _mediaPlayer = new MediaPlayer\(\);\s*", @"private NAudio.Wave.WaveOutEvent? _waveOut;
        private NAudio.Wave.MediaFoundationReader? _mfReader;
        ");

// 2. SetupMediaPlayer -> SetupAudioPlayer
text = text.Replace("SetupMediaPlayer();", "SetupAudioPlayer();");
string setupAudioPattern = @"private void SetupMediaPlayer\(\)[\s\S]*?(?=\s*public void UpdateUserUI)";
string newSetupAudio = @"private void SetupAudioPlayer()
        {
            // Initialization is handled per-track for NAudio
        }";
text = Regex.Replace(text, setupAudioPattern, newSetupAudio);

// 3. PlayTrackAsync: stop old audio, open new audio
text = Regex.Replace(text, @"Dispatcher\.Invoke\(\(\) =>\s*\{\s*_mediaPlayer\.Stop\(\);\s*_mediaPlayer\.Close\(\);\s*_soundFilePath = streamUrl;", @"Dispatcher.Invoke(() =>
                    {
                        StopAudio();
                        _soundFilePath = streamUrl;");

text = Regex.Replace(text, @"CurrentFileText\.Text = title;\s*_mediaPlayer\.Open\(new Uri\(_soundFilePath\)\);", @"CurrentFileText.Text = title;
                        PlayAudioStream(_soundFilePath);");

// 4. PlayButton_Click
text = Regex.Replace(text, @"_mediaPlayer\.Pause\(\);", @"_waveOut?.Pause();");
text = Regex.Replace(text, @"if \(_mediaPlayer\.NaturalDuration\.HasTimeSpan && _mediaPlayer\.Position >= _mediaPlayer\.NaturalDuration\.TimeSpan\)\s*\{\s*_mediaPlayer\.Position = TimeSpan\.Zero;\s*\}", @"if (_mfReader != null && _mfReader.Position >= _mfReader.Length)
                {
                    _mfReader.Position = 0;
                }");
text = Regex.Replace(text, @"_mediaPlayer\.Play\(\);", @"_waveOut?.Play();");

// 5. StopButton_Click and Window_Closing
text = Regex.Replace(text, @"_mediaPlayer\.Stop\(\);", @"StopAudio();");
text = Regex.Replace(text, @"_mediaPlayer\.Close\(\);", @"// closed via StopAudio");

// 6. Open local file
text = Regex.Replace(text, @"_mediaPlayer\.Open\(new Uri\(_soundFilePath\)\);", @"PlayAudioStream(_soundFilePath);
                _waveOut?.Pause(); // Pause immediately for local file load");

// Add helper methods at the end of the class
string helpers = @"
        private void PlayAudioStream(string url)
        {
            try
            {
                StopAudio();
                _mfReader = new NAudio.Wave.MediaFoundationReader(url);
                _waveOut = new NAudio.Wave.WaveOutEvent();
                _waveOut.Init(_mfReader);
                _waveOut.Volume = 1.0f;
                
                _waveOut.PlaybackStopped += (s, e) =>
                {
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
                            YoutubeStatusText.Text = "?? Ðã phát xong";
                        }
                    });
                };
                
                _waveOut.Play();
                _isPlaying = true;
                PlayPauseBtn.Tag = "playing";
                YoutubeStatusText.Text = $"? {CurrentFileText.Text}";
                PlayPauseBtn.Opacity = 1.0;
            }
            catch (Exception ex)
            {
                ShowNotification($"L?i phát nh?c: {ex.Message}", "error");
                _isPlaying = false;
                PlayPauseBtn.Tag = "paused";
                YoutubeStatusText.Text = "L?i phát âm thanh";
            }
        }

        private void StopAudio()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            if (_mfReader != null)
            {
                _mfReader.Dispose();
                _mfReader = null;
            }
        }
    }
}";

text = Regex.Replace(text, @"    \}\s*\}\s*$", helpers);
File.WriteAllText(file, text);
