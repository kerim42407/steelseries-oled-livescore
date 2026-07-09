using System.Drawing;
using System.Drawing.Drawing2D;

namespace OledLiveScore
{
    // Draws the tray icon in code so nothing extra ships next to the exe.
    internal static class AppIcon
    {
        public static Icon Create()
        {
            using (var bmp = new Bitmap(32, 32))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (var fill = new SolidBrush(Color.FromArgb(30, 180, 90)))
                        g.FillEllipse(fill, 1, 1, 30, 30);
                    using (var pen = new Pen(Color.White, 2f))
                        g.DrawEllipse(pen, 1, 1, 30, 30);
                    using (var font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var text = new SolidBrush(Color.White))
                    using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString("LS", font, text, new RectangleF(0, 0, 32, 32), fmt);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
    }
}
