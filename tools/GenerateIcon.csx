// C# Script: Generate the pixel CRT TV app icon for AgentScope
// Run: dotnet-script tools/GenerateIcon.csx
// Or manually: dotnet run --project tools/IconGen

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

const string OutputPath = @"AgentScope.App\Assets\Icons\app.ico";
const int Size = 32;

// Brand colors
var caseDark   = Color.FromArgb(0xFF, 0x2D, 0x2D, 0x44);
var caseMid    = Color.FromArgb(0xFF, 0x3D, 0x3D, 0x5A);
var caseLight  = Color.FromArgb(0xFF, 0x5D, 0x5D, 0x7A);
var antenna    = Color.FromArgb(0xFF, 0x6C, 0x63, 0xFF);
var screenBG   = Color.FromArgb(0xFF, 0x1A, 0x2E, 0x1A);
var screenGlow = Color.FromArgb(0xFF, 0x00, 0xFF, 0x41);
var screenDim  = Color.FromArgb(0xFF, 0x00, 0xAA, 0x2B);
var baseColor  = Color.FromArgb(0xFF, 0x4A, 0x4A, 0x6A);
var transparent = Color.FromArgb(0x00, 0x00, 0x00, 0x00);

// Pixel TV design (32×32):
// Row 0-1:  Antenna spike (centered)
// Row 2-7:  Case top + curved corners
// Row 8-21: Screen area (CRT green) + case borders
// Row 22-25: Case bottom
// Row 26-29: Base/stand
// Row 30-31: Base feet

var pixels = new Color[Size, Size];
for (int y = 0; y < Size; y++)
for (int x = 0; x < Size; x++)
    pixels[x, y] = transparent;

// Helper: draw horizontal line
void HLine(int y, int x1, int x2, Color c) {
    for (int x = x1; x <= x2; x++) pixels[x, y] = c;
}
void VLine(int x, int y1, int y2, Color c) {
    for (int y = y1; y <= y2; y++) pixels[x, y] = c;
}
void Rect(int x1, int y1, int x2, int y2, Color c) {
    for (int y = y1; y <= y2; y++)
    for (int x = x1; x <= x2; x++)
        pixels[x, y] = c;
}

// Antenna - centered spike
Rect(15, 1, 16, 1, antenna);   // tip
Rect(14, 2, 17, 2, antenna);   // base of antenna
Rect(14, 0, 15, 0, antenna);   // left antenna
Rect(16, 0, 17, 0, antenna);   // right antenna

// Case outline (slightly rounded corners)
// Top edge
HLine(3, 12, 19, caseDark);
HLine(4, 10, 21, caseDark);
HLine(5, 9, 22, caseDark);
// Sides and top of case
for (int y = 6; y <= 24; y++) {
    HLine(y, 9, 22, caseDark);
    pixels[10, y] = caseMid;
    pixels[21, y] = caseMid;
}
// Inner case border
for (int y = 7; y <= 23; y++) {
    pixels[11, y] = caseDark;
    pixels[20, y] = caseDark;
}

// Screen area (CRT green, rows 8-22)
Rect(12, 8, 19, 22, screenBG);

// Screen scan lines
for (int y = 8; y <= 22; y++) {
    for (int x = 12; x <= 19; x++) {
        if (y % 2 == 0)
            pixels[x, y] = screenBG;
        else
            pixels[x, y] = Color.FromArgb(0xFF, 0x14, 0x28, 0x14);
    }
}

// Screen glow - waveform pattern (idle state)
// Horizontal glow lines simulating CRT waveform
int y0 = 14;
HLine(y0, 13, 14, screenGlow);
HLine(y0, 17, 18, screenGlow);
HLine(y0 + 1, 14, 15, screenDim);
HLine(y0 + 1, 16, 17, screenDim);
HLine(y0 + 2, 15, 16, screenGlow);
HLine(y0 + 3, 13, 14, screenDim);
HLine(y0 + 3, 17, 18, screenDim);
HLine(y0 + 4, 14, 15, screenGlow);
HLine(y0 + 4, 16, 17, screenGlow);

// Screen corner glow (CRT vignette)
for (int y = 8; y <= 22; y++) {
    for (int x = 12; x <= 19; x++) {
        var dx = Math.Min(x - 12, 19 - x);
        var dy = Math.Min(y - 8, 22 - y);
        var edgeDist = Math.Min(dx, dy);
        if (edgeDist <= 1) {
            var factor = (edgeDist + 1) / 3.0;
            int r = (int)(pixels[x, y].R * factor);
            int g = (int)(pixels[x, y].G * factor);
            int b = (int)(pixels[x, y].B * factor);
            pixels[x, y] = Color.FromArgb(0xFF, Math.Max(r, 0), Math.Max(g, 0), Math.Max(b, 0));
        }
    }
}

// Bottom of case
HLine(25, 9, 22, caseDark);
HLine(26, 10, 21, caseDark);
HLine(27, 11, 20, caseDark);

// Stand
Rect(12, 28, 19, 29, baseColor);
Rect(13, 27, 18, 27, baseColor);
Rect(11, 30, 20, 31, baseColor);
// Stand feet
HLine(30, 13, 14, Color.FromArgb(0xFF, 0x3A, 0x3A, 0x55));
HLine(30, 17, 18, Color.FromArgb(0xFF, 0x3A, 0x3A, 0x55));
HLine(31, 12, 13, Color.FromArgb(0xFF, 0x3A, 0x3A, 0x55));
HLine(31, 18, 19, Color.FromArgb(0xFF, 0x3A, 0x3A, 0x55));

// Build bitmap
using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
for (int y = 0; y < Size; y++)
for (int x = 0; x < Size; x++)
    bmp.SetPixel(x, y, pixels[x, y]);

// Save as PNG first
var pngPath = OutputPath.Replace(".ico", ".png");
Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
bmp.Save(pngPath, ImageFormat.Png);
Console.WriteLine($"Saved PNG: {pngPath} ({Size}×{Size})");

// Generate multi-size ICO
GenerateIco(bmp, OutputPath);
Console.WriteLine($"Saved ICO: {OutputPath}");

static void GenerateIco(Bitmap source, string icoPath) {
    // Create ICO with sizes: 16, 32, 48, 256
    int[] sizes = { 16, 32, 48, 256 };
    var images = new List<Bitmap>();
    foreach (var s in sizes) {
        var resized = new Bitmap(source, new Size(s, s));
        images.Add(resized);
    }

    using var fs = new FileStream(icoPath, FileMode.Create);
    using var writer = new BinaryWriter(fs);

    // ICO header
    writer.Write((short)0);      // reserved
    writer.Write((short)1);      // ICO type
    writer.Write((short)sizes.Length); // image count

    // Calculate image data offsets
    int imageOffset = 6 + sizes.Length * 16; // header + directory entries
    var imageDataList = new List<byte[]>();

    for (int i = 0; i < sizes.Length; i++) {
        using var ms = new MemoryStream();
        // Save as BMP (ICO uses BMP data without BITMAPFILEHEADER)
        // We need to handle alpha: use 32-bit BMP
        var bmp = images[i];
        var bmpData = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        // Write BITMAPINFOHEADER
        ms.Write(BitConverter.GetBytes(40), 0, 4);       // biSize
        ms.Write(BitConverter.GetBytes(bmp.Width), 0, 4);  // biWidth
        ms.Write(BitConverter.GetBytes(bmp.Height * 2), 0, 4); // biHeight (×2 for ICO: image + mask)
        ms.Write(new byte[] { 1, 0 }, 0, 2);             // biPlanes
        ms.Write(new byte[] { 32, 0 }, 0, 2);            // biBitCount
        ms.Write(new byte[8], 0, 8);                     // biCompression..biClrImportant

        // Write pixel data (BGRA, bottom-up)
        int stride = bmp.Width * 4;
        byte[] pixels = new byte[stride * bmp.Height];
        Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);
        // BGRA → RGBA swap
        for (int j = 0; j < pixels.Length; j += 4) {
            (pixels[j], pixels[j + 2]) = (pixels[j + 2], pixels[j]);
        }
        // Flip vertically (BMP is bottom-up)
        for (int row = 0; row < bmp.Height; row++) {
            ms.Write(pixels, (bmp.Height - 1 - row) * stride, stride);
        }

        // Null mask for 32-bit
        int maskSize = ((bmp.Width + 31) / 32 * 4) * bmp.Height;
        ms.Write(new byte[maskSize], 0, maskSize);

        bmp.UnlockBits(bmpData);
        imageDataList.Add(ms.ToArray());
    }

    // Write directory entries
    for (int i = 0; i < sizes.Length; i++) {
        int s = sizes[i] == 256 ? 0 : sizes[i]; // 256 → 0 in ICO format
        writer.Write((byte)s);     // width
        writer.Write((byte)s);     // height
        writer.Write((byte)0);     // color palette
        writer.Write((byte)0);     // reserved
        writer.Write((short)1);    // color planes
        writer.Write((short)32);   // bits per pixel
        writer.Write((int)imageDataList[i].Length); // data size
        writer.Write((int)imageOffset);             // data offset
        imageOffset += imageDataList[i].Length;
    }

    // Write image data
    foreach (var data in imageDataList)
        writer.Write(data);
}
