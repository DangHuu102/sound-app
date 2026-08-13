using System;
using System.Media;
using System.IO;

class Program
{
    static void Main()
    {
        // 1. Lấy đường dẫn file click.wav nằm ngay cạnh file .exe (trong thư mục net10.0)
        string fileNhac = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "click.wav");

        Console.WriteLine("--- UNG DUNG CLICK LA KEU (CONSOLE) ---");

        // 2. Kiểm tra xem file có thực sự tồn tại không
        if (!File.Exists(fileNhac))
        {
            Console.WriteLine("LOI: Khong tim thay file click.wav!");
            Console.WriteLine("Hay kiem tra lai thu muc: " + AppDomain.CurrentDomain.BaseDirectory);
            Console.ReadLine();
            return;
        }

        try
        {
            // 3. Khởi tạo SoundPlayer với đúng đường dẫn file
            SoundPlayer player = new SoundPlayer(fileNhac);

            Console.WriteLine("Bam phim bat ky de phat am thanh (Bam ESC de thoat)...");

            while (true)
            {
                // Đọc phím bấm từ bàn phím
                ConsoleKeyInfo key = Console.ReadKey(true);

                // Thoát nếu người dùng bấm phím ESC
                if (key.Key == ConsoleKey.Escape) break;

                // PHÁT AM THANH
                player.Play();
                Console.WriteLine("-> CLICK!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Co loi xay ra: " + ex.Message);
            Console.ReadLine();
        }
    }
}