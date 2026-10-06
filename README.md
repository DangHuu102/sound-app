# 🎵 Sound Studio

> Ứng dụng nghe nhạc Desktop Cyberpunk — Stream YouTube, quản lý lịch sử, AI Recommend, NAudio Engine và Mini Player nổi.

![Sound Studio UI](preview.jpg)

---

## 🌟 Tính năng Nổi bật

| Nhóm | Tính năng | Mô tả |
|---|---|---|
| **🎧 Phát Nhạc** | **NAudio Core Engine** | Hệ thống phát nhạc được viết lại với NAudio và MediaFoundationReader, xử lý mượt mà luồng chunked HTTPS từ YouTube mà không bị rớt mạng hay mất tiếng. |
| | **Smart Search & URL** | Chỉ cần dán Link YouTube hoặc gõ tên bài hát (VD: "Making My Way"), ứng dụng tự động tìm và phát chuẩn xác. |
| | **Auto-Recommend (Up Next)** | Tự động lấy 5 bài hát gợi ý có phong cách tương tự nạp vào danh sách chờ (Queue). Có Toggle để Bật/Tắt dễ dàng. |
| | **Local Audio** | Hỗ trợ phát nhạc có sẵn trên ổ cứng máy tính. |
| **👤 Người dùng** | **Hệ thống Tài khoản** | Đăng ký & Đăng nhập mượt mà. Giao diện thay đổi theo trạng thái đăng nhập. |
| | **Bảo mật giao diện** | Tích hợp tính năng "Con mắt" (👁) bật/tắt hiển thị mật khẩu bằng thủ thuật WPF TextBox / PasswordBox overlay. |
| | **Lịch sử & Queue** | Lưu lịch sử vào SQLite. 1 click phát lại ngay. Có nút Clear nhanh gọn. |
| **💻 Hệ thống** | **Hardware Audio Sync** | Đồng bộ trực tiếp thanh trượt âm lượng với **Master Volume** của Windows OS (thông qua CoreAudioApi). |
| | **Mini Player (PiP)** | Cửa sổ nhỏ luôn hiển thị trên cùng (Always on Top) để tiện đổi bài. |
| | **Download to MP3** | Tải bài hát đang nghe về máy tính dưới định dạng âm thanh. |
| | **Khay hệ thống (Tray)** | Thu nhỏ xuống góc màn hình gọn gàng. |

---

## 📦 Packages & Dependencies (Thư viện)

Dự án sử dụng các thư viện mã nguồn mở tối ưu nhất cho .NET 10.0 WPF:

1. **NAudio** (v3.1.0) 
   - *Core Audio Engine*: Thay thế WPF MediaPlayer. Dùng WaveOutEvent và MediaFoundationReader để xử lý Audio Stream độ trễ thấp. Xử lý phần cứng CoreAudioApi (Volume/Device).
2. **YoutubeExplode** (v6.6.2)
   - *Youtube Scraper*: Lấy luồng dữ liệu (Stream Manifest) .mp4/.m4a hoặc Muxed trực tiếp từ YouTube. Hỗ trợ tìm kiếm video thông minh.
3. **Dapper** (v2.1.79)
   - *Micro ORM*: Tương tác cực nhanh với CSDL bằng câu lệnh SQL thô, ánh xạ tự động vào Object C#.
4. **Microsoft.Data.Sqlite** (v10.0.12)
   - *Database*: Cơ sở dữ liệu siêu nhẹ (Local Database) dùng để lưu thông tin User, Lịch sử nghe nhạc, Stream Cache.
5. **SpotifyAPI.Web** (v7.4.2)
   - *Tương lai*: Chuẩn bị sẵn để tích hợp lấy Playlist và Recommendation từ Spotify.
6. **AdysTech.CredentialManager** (v3.1.0)
   - *Security*: Tương tác với Windows Credential Manager.

---

## 🎨 Giao diện (WPF)

- **Theme**: Cyberpunk / Neon Dark (#0B0914, #161324)
- **Glassmorphism**: Áp dụng hiệu ứng nền trong suốt, bo góc (CornerRadius) cho các ô Input và Panel.
- **Layout**: Kiến trúc chia cột thông minh.
  - Trái: Lịch sử nghe (Recent Tracks) & Playlist.
  - Giữa: Màn hình chính, Trạng thái bài hát, Tìm kiếm.
  - Phải: Hàng đợi (Queue), Đề xuất tương tự & Quản lý User.

---

## 🛠 Cài đặt & Chạy

**Yêu cầu hệ thống**: Máy tính chạy Windows cài sẵn [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

`ash
# 1. Clone dự án về máy
git clone https://github.com/DangHuu102/sound-app.git
cd sound-app

# 2. Chạy ứng dụng
dotnet run
`
> *Lưu ý: Ngay lần chạy đầu tiên, ứng dụng sẽ tự động khởi tạo CSDL SQLite (database.db) và gieo sẵn một tài khoản admin: dmin@soundstudio.com (pass: dmin).*

---

## 📁 Kiến trúc Mã nguồn

`	ext
Sound Studio/
├── App.xaml / .cs                # Quản lý Global State (App.CurrentUser)
├── MainWindow.xaml / .cs         # UI Chính + Logic nòng cốt (NAudio)
├── LoginWindow.xaml / .cs        # Form Đăng nhập & Toggle Password
├── SignUpWindow.xaml / .cs       # Form Đăng ký
├── MiniPlayerWindow.xaml / .cs   # Chế độ thu nhỏ Picture-in-Picture
├── DatabaseManager.cs            # Lớp tĩnh tương tác SQLite (Dapper)
├── AudioDeviceService.cs         # Lớp quản lý Master Volume (NAudio CoreAudioApi)
└── RecommendationService.cs      # Lấy gợi ý bài hát mới từ YouTube (Up Next)
`

---
*Developed by [huu66](https://github.com/DangHuu102)*