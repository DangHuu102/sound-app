# 🎵 Sound Studio

> Ứng dụng nghe nhạc Desktop Cyberpunk — Stream YouTube, quản lý lịch sử, Mini Player nổi.

![Sound Studio UI](preview.jpg)

---

## ✨ Tính năng

| Tính năng | Mô tả |
|---|---|
| ☁️ **Cloud Streaming** | Stream nhạc trực tiếp từ YouTube — không tải file, không tốn dung lượng ổ cứng |
| 🎵 **Mini Player** | Cửa sổ nổi nhỏ gọn, luôn hiển thị trên cùng (Always on Top), kéo thả tự do, tên bài hát lướt Marquee |
| 📜 **Recent Tracks** | Lưu lịch sử nghe vào database SQLite, nhấn để phát lại ngay lập tức |
| 🧹 **Clear History** | Xóa toàn bộ lịch sử nghe chỉ với 1 click |
| 🔊 **Volume Control** | Thanh trượt điều chỉnh âm lượng real-time |
| 🗂️ **Local File Support** | Hỗ trợ mở file nhạc `.mp3` / `.wav` trực tiếp từ máy tính |
| 🖥️ **System Tray** | Thu nhỏ xuống khay hệ thống, click đúp để mở lại |

---

## 🎨 Giao diện

- **Theme**: Cyberpunk / Neon Dark (`#0B0914`, `#161324`)
- **Màu nổi bật**: Tím `#5D26C1` → Xanh lá `#59C173`
- **Layout**: 3 cột — Sidebar lịch sử | Trung tâm stream nhạc | Bên phải Now Playing + Volume

---

## 🛠️ Công nghệ

- **Ngôn ngữ**: C# (.NET 10.0 Windows)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Thư viện**:
  - [`YoutubeExplode`](https://github.com/Tyrrrz/YoutubeExplode) — Lấy stream URL từ YouTube
  - [`Microsoft.Data.Sqlite`](https://www.nuget.org/packages/Microsoft.Data.Sqlite/) — Database lưu lịch sử
  - [`Dapper`](https://github.com/DapperLib/Dapper) — ORM nhẹ cho SQLite

---

## 🚀 Cài đặt & Chạy

**Yêu cầu**: Cài [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
# Clone dự án
git clone https://github.com/DangHuu102/sound-app.git
cd sound-app

# Chạy ứng dụng
dotnet run
```

---

## 📐 Kiến trúc hiện tại

```
Sound Studio (WPF Desktop)
├── MainWindow.xaml / .cs     — Giao diện chính + Logic stream YouTube
├── MiniPlayerWindow.xaml/.cs — Cửa sổ Mini Player nổi
└── DatabaseManager.cs        — Quản lý SQLite (lịch sử nghe)
```

> 🔭 **Kế hoạch tương lai**: Chuyển đổi sang kiến trúc Client-Server (Frontend WPF + Backend ASP.NET Core API + Music Storage)

---

*Developed by [huu66](https://github.com/DangHuu102)*
