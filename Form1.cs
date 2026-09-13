using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing.Text;
namespace Bluetooth
{
    public partial class Form1 : Form
    {
        // Đường dẫn đến tool CLI của chị (đổi lại cho đúng thực tế)
        private string cliPath = @"bluetooth_battery.exe";
        private bool isLowBatteryWarned = false;

        public Form1()
        {
            KillDuplicateInstances();
            InitializeComponent();
            // Ẩn form khi vừa khởi động
            this.Load += (s, e) => this.Hide();

            // Chạy cập nhật ngay khi mở
            UpdateBatteryIcon();
        }

        private void KillDuplicateInstances()
        {
            Process current = Process.GetCurrentProcess();
            // Lấy danh sách các tiến trình có cùng tên đang chạy
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process process in processes)
            {
                // Nếu không phải là tiến trình hiện tại thì kill nó
                if (process.Id != current.Id)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(1000); // Đợi 1 giây để tiến trình cũ đóng hoàn toàn
                    }
                    catch { /* Bỏ qua nếu không đủ quyền hạn hoặc tiến trình đã tự thoát */ }
                }
            }
        }


        private void NotifyIcon1_Click1(object sender, EventArgs e)
        {
            ToggleBluetooth();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateBatteryIcon();
        }

        private void ToggleBluetooth()
        {
            try
            {
                // Tự động lấy đường dẫn thư mục hiện tại của app và nối với tên file exe C++
                string toggleExePath = System.IO.Path.Combine(Application.StartupPath, "toggle_bluetooth.exe");

                // Kiểm tra xem file exe C++ có thực sự nằm ở đó không để tránh crash app
                if (!System.IO.File.Exists(toggleExePath))
                {
                    MessageBox.Show("Không tìm thấy file toggle_bluetooth.exe trong thư mục ứng dụng!", "Lỗi");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = toggleExePath,
                    UseShellExecute = false,
                    CreateNoWindow = true // Chạy ngầm hoàn toàn, không nháy bảng đen CMD lên màn hình
                };

                using (Process p = Process.Start(startInfo))
                {
                    p.WaitForExit(); // Chờ file C++ thực hiện đảo trạng thái Bluetooth xong xuôi
                }

                // Cập nhật lại biểu tượng pin và trạng thái ngay lập tức sau khi click
                UpdateBatteryIcon();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thực hiện bật/tắt Bluetooth: " + ex.Message, "Thông báo");
            }
        }

        private void UpdateBatteryIcon()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = cliPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                string output = "";
                using (Process process = Process.Start(startInfo))
                {
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }

                // Kiểm tra nếu không tìm thấy thiết bị nào kết nối (Dựa theo thông báo thực tế của tool)
                if (string.IsNullOrWhiteSpace(output) ||
                    output.Contains("No connected Bluetooth devices") ||
                    output.Contains("were found"))
                {
                    // Hiện ô màu xám và chữ X báo ngắt kết nối
                    notifyIcon1.Icon = CreateDynamicTextIcon("X", 0);
                    notifyIcon1.Text = "AirPods: Đã ngắt kết nối";
                    isLowBatteryWarned = false;
                    return;
                }

                // Nếu có thiết bị, lọc lấy số phần trăm pin
                Match match = Regex.Match(output, @"(\d+)%");
                if (match.Success)
                {
                    string batteryLevelStr = match.Groups[1].Value;
                    int batteryLevel = int.Parse(batteryLevelStr);

                    notifyIcon1.Icon = CreateDynamicTextIcon(batteryLevelStr, batteryLevel);
                    notifyIcon1.Text = $"Pin Bluetooth: {batteryLevel}%";

                    // Kiểm tra nếu pin xuống <= 70% thì kêu beep beep
                    if (batteryLevel <= 70)
                    {
                        if (!isLowBatteryWarned)
                        {
                            Console.Beep(800, 200); // Kêu beep lần 1
                            Console.Beep(800, 200); // Kêu beep lần 2
                            isLowBatteryWarned = true; // Đánh dấu đã cảnh báo xong
                        }
                    }
                    else
                    {
                        isLowBatteryWarned = false; // Reset khi pin sạc lên trên 70%
                    }
                }
                else
                {
                    // Trường hợp xuất hiện lỗi lạ không lấy được số %
                    notifyIcon1.Icon = CreateDynamicTextIcon("--", 0);
                    notifyIcon1.Text = "Không xác định được pin";
                }
            }
            catch (Exception ex)
            {
                notifyIcon1.Icon = SystemIcons.Error;
                notifyIcon1.Text = "Lỗi: " + ex.Message;
            }
        }

        // Hàm vẽ số phần trăm lên một Icon
        // Hàm vẽ icon hình viên pin dựa trên số phần trăm
        private Icon CreateBatteryIcon(int percentage)
        {
            // Giới hạn phần trăm từ 0 đến 100
            percentage = Math.Max(0, Math.Min(100, percentage));

            using (Bitmap bitmap = new Bitmap(16, 16))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // 1. Xác định màu sắc theo lượng pin
                Color batteryColor = Color.LimeGreen; // Pin xanh
                if (percentage <= 20) batteryColor = Color.Red;       // Pin yếu màu đỏ
                else if (percentage <= 50) batteryColor = Color.Orange; // Pin trung bình màu cam

                using (Pen pen = new Pen(Color.White, 1)) // Viền màu trắng cho nổi bật
                using (Brush brush = new SolidBrush(batteryColor))
                {
                    // 2. Vẽ thân viên pin (Tọa độ: X=1, Y=4, Rộng=12, Cao=8)
                    g.DrawRectangle(pen, 1, 4, 12, 8);

                    // 3. Vẽ đầu viên pin (cục nhô ra ở bên phải)
                    g.DrawLine(pen, 14, 6, 14, 9);

                    // 4. Tính toán độ rộng thanh năng lượng bên trong (tối đa 9 pixel)
                    int fillWidth = (int)Math.Round((percentage / 100.0) * 9);

                    // Vẽ phần pin được nạp đầy bên trong
                    if (fillWidth > 0)
                    {
                        g.FillRectangle(brush, 3, 6, fillWidth, 5);
                    }
                }

                // Chuyển đổi bitmap thành Icon để gán cho NotifyIcon
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }

        // Giải phóng icon khi đóng ứng dụng để tránh tràn bộ nhớ


        // Đặt dòng này ở TRÊN CÙNG của hàm, hoặc ngay dưới chỗ khai báo class Form1 để gọi thư viện Windows hủy icon cũ
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);
        private Icon CreateDynamicTextIcon(string text, int percentage)
        {
            // Tạo bitmap chuẩn 16x16 pixel
            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);

                // Bật chế độ nét cao, chống nhòe chữ
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                // 1. Tự động đổi màu nền tùy theo % pin cho đẹp và trực quan
                Color bgColor = Color.DarkGreen; // Trên 50% nền xanh lá đậm
                if (percentage <= 20) bgColor = Color.Crimson;      // Dưới 20% nền đỏ
                else if (percentage <= 50) bgColor = Color.DarkOrange; // Từ 21%-50% nền cam

                // 2. Vẽ một nền hình vuông bo tròn nhẹ (hoặc hình tròn) màu sắc
                using (Brush bgBrush = new SolidBrush(bgColor))
                {
                    // Vẽ phủ kín ô 16x16 để icon to nhất có thể
                    g.FillRectangle(bgBrush, 0, 0, 16, 16);
                }

                // 3. Vẽ chữ số 80 MÀU TRẮNG đè lên nền màu để nhìn rõ mồn một
                using (Font font = new Font("Arial", 10, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    // Vẽ chữ số ở chính giữa (Dịch X sang -0.5 để chữ 2 chữ số không bị lệch phải)
                    if(percentage == 100) text = "F"; // Nếu 100% thì hiển thị chữ F (Full) cho gọn
                    g.DrawString(text, font, textBrush, new RectangleF(-0.5f, 0, 17, 16), sf);
                }
            }

            // 4. Giải phóng icon cũ để tránh lỗi bóp méo hình của Windows
            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);

            // Đoạn này cực kỳ quan trọng để sửa lỗi chấm đen méo mó:
            if (notifyIcon1.Icon != null)
            {
                DestroyIcon(notifyIcon1.Icon.Handle);
            }

            bitmap.Dispose();
            return icon;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            notifyIcon1.Dispose();
            base.OnFormClosing(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            timer1.Enabled = true;
        }

    }
}