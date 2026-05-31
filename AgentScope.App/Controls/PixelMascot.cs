using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AgentScope.App.Controls;

/// <summary>
/// A WPF custom control that renders pixel-art mascot animations using WriteableBitmap.
/// Each AI tool has a unique mascot with status-driven animation frames.
/// </summary>
public class PixelMascot : FrameworkElement
{
    private const int SourceSize = 32;
    private const int DisplaySize = 64;

    private WriteableBitmap? _bitmap;
    private int _currentFrame;
    private double _frameAccumulator;
    private bool _isAnimating;

    // Frame data for each animation
    private List<byte[]>? _currentAnimation;

    // --- Dependency Properties ---

    public static readonly DependencyProperty ToolIdProperty =
        DependencyProperty.Register(nameof(ToolId), typeof(string), typeof(PixelMascot),
            new FrameworkPropertyMetadata("claude", OnVisualChanged));

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(string), typeof(PixelMascot),
            new FrameworkPropertyMetadata("idle", OnVisualChanged));

    public static readonly DependencyProperty FpsProperty =
        DependencyProperty.Register(nameof(Fps), typeof(int), typeof(PixelMascot),
            new FrameworkPropertyMetadata(10));

    public string ToolId
    {
        get => (string)GetValue(ToolIdProperty);
        set => SetValue(ToolIdProperty, value);
    }

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public int Fps
    {
        get => (int)GetValue(FpsProperty);
        set => SetValue(FpsProperty, value);
    }

    public PixelMascot()
    {
        Width = DisplaySize;
        Height = DisplaySize;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _bitmap = new WriteableBitmap(DisplaySize, DisplaySize, 96, 96, PixelFormats.Bgra32, null);
        LoadAnimation();
        _isAnimating = true;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isAnimating = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var mascot = (PixelMascot)d;
        if (mascot.IsLoaded)
        {
            mascot.LoadAnimation();
        }
    }

    /// <summary>
    /// Load the animation frames for the current tool + status combination.
    /// Frame data is generated from palette JSON files embedded as resources.
    /// </summary>
    private void LoadAnimation()
    {
        // In production: load from Assets/Mascots/{ToolId}_{Status}.json
        // For now: generate simple procedural pixel art per tool+status
        _currentAnimation = GenerateAnimationFrames(ToolId, Status);
        _currentFrame = 0;
        _frameAccumulator = 0;

        // Render first frame
        if (_currentAnimation is { Count: > 0 })
            RenderFrame(_currentAnimation[0]);
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isAnimating || _currentAnimation == null || _currentAnimation.Count == 0)
            return;

        var frameDuration = 1000.0 / Fps;
        _frameAccumulator += (e as RenderingEventArgs)?.RenderingTime.TotalMilliseconds ?? 16.67;

        while (_frameAccumulator >= frameDuration)
        {
            _frameAccumulator -= frameDuration;
            _currentFrame = (_currentFrame + 1) % _currentAnimation.Count;
        }

        RenderFrame(_currentAnimation[_currentFrame]);
    }

    /// <summary>
    /// Render a single frame of pixel data to the WriteableBitmap using nearest-neighbor upscaling.
    /// </summary>
    private unsafe void RenderFrame(byte[] pixelData)
    {
        if (_bitmap == null) return;

        _bitmap.Lock();
        var backBuffer = (uint*)_bitmap.BackBuffer;
        var stride = _bitmap.BackBufferStride / 4; // pixels per row (4 bytes per pixel BGRA)

        for (int y = 0; y < DisplaySize; y++)
        {
            var srcY = y * SourceSize / DisplaySize;
            for (int x = 0; x < DisplaySize; x++)
            {
                var srcX = x * SourceSize / DisplaySize;
                var srcIdx = (srcY * SourceSize + srcX) * 4;

                byte b = srcIdx < pixelData.Length ? pixelData[srcIdx] : (byte)0;
                byte g = srcIdx + 1 < pixelData.Length ? pixelData[srcIdx + 1] : (byte)0;
                byte r = srcIdx + 2 < pixelData.Length ? pixelData[srcIdx + 2] : (byte)0;
                byte a = srcIdx + 3 < pixelData.Length ? pixelData[srcIdx + 3] : (byte)255;

                backBuffer[y * stride + x] = (uint)((a << 24) | (r << 16) | (g << 8) | b);
            }
        }

        _bitmap.AddDirtyRect(new Int32Rect(0, 0, DisplaySize, DisplaySize));
        _bitmap.Unlock();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_bitmap != null)
        {
            drawingContext.DrawImage(_bitmap, new Rect(0, 0, DisplaySize, DisplaySize));
        }
    }

    /// <summary>
    /// Generate simple procedural pixel art frames as placeholder.
    /// In production, these are loaded from JSON asset files.
    /// </summary>
    private static List<byte[]> GenerateAnimationFrames(string toolId, string status)
    {
        var frames = new List<byte[]>();
        var frameCount = status switch
        {
            "idle" => 3,
            "running" => 4,
            "waiting_permission" => 3,
            "error" => 3,
            "done" => 2,
            _ => 2
        };

        for (int f = 0; f < frameCount; f++)
        {
            var frame = new byte[SourceSize * SourceSize * 4]; // BGRA

            // Generate a simple colored square with a pixel "face" that varies per frame
            var (r, g, b) = toolId switch
            {
                "claude" => (0xDA, 0x77, 0x5A), // Warm orange
                "codex" => (0x6C, 0x63, 0xFF),  // Purple
                _ => (0x6C, 0x63, 0xFF)
            };

            // Draw a filled rounded square body
            for (int y = 4; y < SourceSize - 4; y++)
            {
                for (int x = 4; x < SourceSize - 4; x++)
                {
                    // Simple body shape: slight squish based on status
                    var offset = status == "running" ? f % 2 * 2 : 0;
                    if (x >= 4 && x < SourceSize - 4 && y >= 4 + offset && y < SourceSize - 4 + offset)
                    {
                        var idx = ((y - offset) * SourceSize + x) * 4;
                        if (idx >= 0 && idx + 3 < frame.Length)
                        {
                            frame[idx] = b;     // B
                            frame[idx + 1] = g; // G
                            frame[idx + 2] = r; // R
                            frame[idx + 3] = 255; // A
                        }
                    }
                }
            }

            // Draw "eyes" — two white pixels that move per frame
            var eyeY = status == "waiting_permission" ? 10 : 9;
            var leftEyeX = 10;
            var rightEyeX = 18;

            if (status == "error") { leftEyeX -= 1; rightEyeX += 1; }
            if (status == "done" && f == 1) { eyeY = 14; } // blink

            DrawPixel(frame, leftEyeX, eyeY, 255, 255, 255);
            DrawPixel(frame, rightEyeX, eyeY, 255, 255, 255);
            DrawPixel(frame, leftEyeX + 1, eyeY, 50, 50, 50); // pupil
            DrawPixel(frame, rightEyeX + 1, eyeY, 50, 50, 50);

            frames.Add(frame);
        }

        return frames;
    }

    private static void DrawPixel(byte[] frame, int x, int y, byte r, byte g, byte b)
    {
        if (x < 0 || x >= SourceSize || y < 0 || y >= SourceSize) return;
        var idx = (y * SourceSize + x) * 4;
        frame[idx] = b;
        frame[idx + 1] = g;
        frame[idx + 2] = r;
        frame[idx + 3] = 255;
    }
}
