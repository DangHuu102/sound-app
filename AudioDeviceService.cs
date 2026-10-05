using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace soundapp
{
    public sealed class AudioDeviceService : IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator;
        private MMDevice? _currentDevice;
        private readonly object _lock = new object();

        public event Action<string>? DeviceChanged;
        public event Action<float>? VolumeChanged;

        public AudioDeviceService()
        {
            _enumerator = new MMDeviceEnumerator();
            RefreshCurrentDevice();
        }

        public void RefreshCurrentDevice()
        {
            lock (_lock)
            {
                try
                {
                    _currentDevice?.Dispose();
                    _currentDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    
                    string friendlyName = _currentDevice.FriendlyName;
                    float currentVol = _currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                    bool isMuted = _currentDevice.AudioEndpointVolume.Mute;

                    // Gọi event ra ngoài (UI sẽ hứng)
                    DeviceChanged?.Invoke(friendlyName.Length > 22 ? friendlyName.Substring(0, 22) + "…" : friendlyName);
                    VolumeChanged?.Invoke(currentVol);
                }
                catch
                {
                    DeviceChanged?.Invoke("Audio Device Not Found");
                }
            }
        }

        public void SetMasterVolumeAsync(float volume)
        {
            lock (_lock)
            {
                try
                {
                    if (_currentDevice != null)
                        _currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                }
                catch (COMException)
                {
                    RefreshCurrentDevice();
                }
                catch { }
            }
        }

        public void ToggleMuteAsync()
        {
            lock (_lock)
            {
                try
                {
                    if (_currentDevice != null)
                        _currentDevice.AudioEndpointVolume.Mute = !_currentDevice.AudioEndpointVolume.Mute;
                }
                catch (COMException)
                {
                    RefreshCurrentDevice();
                }
                catch { }
            }
        }

        public void Dispose()
        {
            _currentDevice?.Dispose();
            _enumerator.Dispose();
        }
    }
}
