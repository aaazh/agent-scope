using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

const string OutDir = "../../../AgentScope.App/Assets/Icons/";
const int S = 32;

// Color palette (BGRA bytes)
var BG   = new byte[] { 0x00, 0x00, 0x00, 0x00 }; // transparent
var DARK = new byte[] { 0x44, 0x2D, 0x2D, 0xFF }; // case #2D2D44
var ANT  = new byte[] { 0xFF, 0x63, 0x6C, 0xFF }; // antenna #6C63FF
var SCR  = new byte[] { 0x1A, 0x2E, 0x1A, 0xFF }; // screen bg
var GLOW = new byte[] { 0x41, 0xFF, 0x00, 0xFF }; // #00FF41
var DIM  = new byte[] { 0x2B, 0xAA, 0x00, 0xFF };
var BODY = new byte[] { 0x6A, 0x4A, 0x4A, 0xFF }; // stand #4A4A6A

var pixels = new byte[S * S * 4]; // BGRA flat

void Set(int x, int y, byte[] c) {
    if (x < 0 || x >= S || y < 0 || y >= S) return;
    int i = (y * S + x) * 4;
    Array.Copy(c, 0, pixels, i, 4);
}
void HLine(int y, int x1, int x2, byte[] c) {
    for (int x = x1; x <= x2; x++) Set(x, y, c);
}
void Fill(int x1, int y1, int x2, int y2, byte[] c) {
    for (int y = y1; y <= y2; y++)
    for (int x = x1; x <= x2; x++)
        Set(x, y, c);
}

// Antenna
Set(15, 0, ANT); Set(16, 0, ANT);
HLine(1, 15, 16, ANT); HLine(2, 14, 17, ANT);

// Case outline
HLine(3, 12, 19, DARK);
HLine(4, 10, 21, DARK);
HLine(5, 9, 22, DARK);
for (int y = 6; y <= 24; y++) HLine(y, 9, 22, DARK);
HLine(25, 9, 22, DARK);
HLine(26, 10, 21, DARK);
HLine(27, 11, 20, DARK);

// CRT Screen with scanlines
for (int y = 8; y <= 22; y++)
for (int x = 12; x <= 19; x++)
    Set(x, y, y % 2 == 0 ? SCR : new byte[]{0x14,0x28,0x14,0xFF});

// Waveform pattern on screen (idle state)
int wy = 14;
HLine(wy, 13, 14, GLOW); HLine(wy, 17, 18, GLOW);
HLine(wy+1, 14, 17, DIM);
HLine(wy+2, 15, 16, GLOW);
HLine(wy+3, 13, 14, DIM); HLine(wy+3, 17, 18, DIM);
HLine(wy+4, 14, 17, GLOW);

// Stand
Fill(12, 28, 19, 29, BODY);
HLine(27, 13, 18, BODY);
Fill(11, 30, 20, 31, new byte[]{0x55,0x3A,0x3A,0xFF});

// Build bitmap
using var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
var bd = bmp.LockBits(new Rectangle(0, 0, S, S), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
Marshal.Copy(pixels, 0, bd.Scan0, pixels.Length);
bmp.UnlockBits(bd);

Directory.CreateDirectory(OutDir);
bmp.Save(OutDir + "app.png", ImageFormat.Png);
Console.WriteLine($"OK app.png ({S}x{S})");

// Generate multi-size ICO
SaveIco(bmp, OutDir + "app.ico", new[] { 16, 32, 48, 256 });
Console.WriteLine("OK app.ico (16/32/48/256)");

static void SaveIco(Bitmap source, string path, int[] sizes) {
    var imgs = sizes.Select(s => new Bitmap(source, s, s)).ToArray();
    using var fs = File.Create(path);
    using var bw = new BinaryWriter(fs);
    bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);

    int offset = 6 + sizes.Length * 16;
    var dataArr = new byte[sizes.Length][];

    for (int i = 0; i < sizes.Length; i++) {
        var b = imgs[i];
        int stride = b.Width * 4;
        byte[] raw = new byte[stride * b.Height];
        var bd2 = b.LockBits(new Rectangle(0,0,b.Width,b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(bd2.Scan0, raw, 0, raw.Length);
        b.UnlockBits(bd2);

        int h = b.Height, w = b.Width;
        byte[] buf = new byte[stride * h + ((w+31)/32*4)*h];
        for (int row = 0; row < h; row++) {
            int srcRow = (h-1-row) * stride;
            for (int col = 0; col < stride; col += 4) {
                int dst = row * stride + col;
                buf[dst] = raw[srcRow + col + 2];     // R
                buf[dst+1] = raw[srcRow + col + 1];   // G
                buf[dst+2] = raw[srcRow + col];       // B
                buf[dst+3] = raw[srcRow + col + 3];   // A
            }
        }
        dataArr[i] = buf;

        int ws = w == 256 ? 0 : w, hs = h == 256 ? 0 : h;
        bw.Write((byte)ws); bw.Write((byte)hs); bw.Write((byte)0); bw.Write((byte)0);
        bw.Write((short)1); bw.Write((short)32);
        bw.Write((int)buf.Length); bw.Write((int)offset);
        offset += buf.Length;
    }
    foreach (var d in dataArr) bw.Write(d);
}
