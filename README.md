# Sound Studio (Keyboard Clicker)

![Sound Studio UI](preview.jpg)

Sound Studio là một ứng dụng Desktop mạnh mẽ được phát triển bằng **C# WPF**, mang đến trải nghiệm gõ phím hoàn toàn mới với các hiệu ứng âm thanh phong phú và giao diện Cyberpunk cực kỳ hiện đại.

## Tính năng nổi bật

- 🎨 **Giao diện 3 Cột (Sound Studio Theme)**: Thiết kế Dark Mode chuyên nghiệp, tối ưu hóa trải nghiệm người dùng với các dải màu Neon Gradient (Tím/Xanh).
- ⌨️ **Global Keyboard Hook**: Khả năng phát âm thanh mỗi khi bạn gõ phím, **ngay cả khi ứng dụng đang chạy ngầm** hoặc bị thu nhỏ. Hoàn hảo để giả lập tiếng bàn phím cơ!
- 🎵 **YouTube Music Explorer**: Hỗ trợ bóc tách và tải trực tiếp luồng âm thanh chất lượng cao từ các đường link YouTube (sử dụng `YoutubeExplode`) chỉ trong vài giây.
- 🎚️ **Custom Volume Mixer**: Hệ thống thanh trượt Slider tự thiết kế giao diện, kết nối trực tiếp với MediaPlayer để điều chỉnh âm lượng.
- 📉 **Sóng âm (Visualizer) tương tác**: Dải sóng âm tự động chớp nháy và đổi màu mỗi khi có thao tác gõ phím.
- Tray Icon (Hoạt động ngầm): Thu nhỏ ứng dụng xuống góc đồng hồ hệ thống (System Tray) để không làm phiền màn hình làm việc của bạn.

## Công nghệ sử dụng
- **Ngôn ngữ**: C#
- **Framework**: .NET 10.0 Windows (WPF & Windows Forms for NotifyIcon)
- **Thư viện bên thứ ba**: `YoutubeExplode`

## Hướng dẫn cài đặt và chạy ứng dụng

1. Đảm bảo máy tính của bạn đã cài đặt [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Mở Terminal/PowerShell và di chuyển vào thư mục dự án:
```bash
cd soundapp
```
3. Chạy ứng dụng:
```bash
dotnet run
```

---
*Developed by huu66*
