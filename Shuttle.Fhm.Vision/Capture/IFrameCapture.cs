using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Shuttle.Fhm.Vision.Capture;

/// <summary>Captures the pixels of a window into an ImageSharp image.</summary>
public interface IFrameCapture {
    /// <summary>Captures the current contents of the window identified by <paramref name="handle"/>.</summary>
    Image<Rgba32> Capture(IntPtr handle);
}
