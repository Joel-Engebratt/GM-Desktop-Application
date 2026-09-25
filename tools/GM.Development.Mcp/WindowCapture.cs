using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GM.Development.Mcp
{
    internal static class WindowCapture
    {
        public static byte[] Capture(IntPtr window)
        {
            if (IsIconic(window)) throw new InvalidOperationException("Restore the window before capturing it.");
            if (!GetWindowRect(window, out var rectangle)) throw new InvalidOperationException("Could not read window bounds.");
            var width = rectangle.Right - rectangle.Left;
            var height = rectangle.Bottom - rectangle.Top;
            if (width <= 0 || height <= 0 || (long)width * height > 16_000_000)
                throw new InvalidOperationException("Window dimensions are unavailable or too large to capture.");
            using var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var dc = graphics.GetHdc();
                try
                {
                    // PW_RENDERFULLCONTENT supports WPF's composed window content.
                    if (!PrintWindow(window, dc, 2)) throw new InvalidOperationException("Windows could not capture the window.");
                }
                finally { graphics.ReleaseHdc(dc); }
            }
            var colors = new HashSet<int>();
            // A private/non-rendering desktop may return only a frame and blank client area.
            for (var y = height / 5; y < height * 4 / 5; y += Math.Max(1, height / 40))
                for (var x = width / 5; x < width * 4 / 5; x += Math.Max(1, width / 40))
                    colors.Add(bitmap.GetPixel(x, y).ToArgb());
            if (colors.Count < 5)
                throw new InvalidOperationException("The captured client area appears blank. Run the MCP server on a normal rendering Windows desktop, restore its window, and retry.");
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rectangle { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr window, out Rectangle rectangle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr window);
    }
}
