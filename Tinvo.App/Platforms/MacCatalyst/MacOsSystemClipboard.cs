using System;
using Tinvo.Abstractions;

namespace Tinvo.Platforms.MacCatalyst;

public class MacOsSystemClipboard : ISystemClipboard
{
    public List<SystemClipboardFileInfo> GetFiles()
    {
        var files = new List<SystemClipboardFileInfo>();
        var pasteboard = UIKit.UIPasteboard.General;

        if (pasteboard.HasImages)
        {
            var image = pasteboard.Image;
            if (image != null)
            {
                var imageName = pasteboard.Name ?? $"clipboard-image-{Guid.NewGuid()}.png";
                if(!imageName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    imageName += ".png";

                var imageData = image.AsPNG();
                if (imageData == null)
                    return files;
                files.Add(new SystemClipboardFileInfo
                {
                    Name = imageName,
                    Stream = new MemoryStream(imageData.ToArray())
                });
            }
        }

        if (pasteboard.HasUrls)
        {
            if (pasteboard.Urls != null)
                foreach (var nsUrl in pasteboard.Urls)
                {
                    try
                    {
                        if (!nsUrl.IsFileUrl || string.IsNullOrEmpty(nsUrl.Path))
                            continue;

                        var fileName = nsUrl.LastPathComponent ?? $"clipboard-file-{Guid.NewGuid()}";
                        var fileStream = System.IO.File.OpenRead(nsUrl.Path);

                        files.Add(new SystemClipboardFileInfo
                        {
                            Name = fileName,
                            Stream = fileStream
                        });
                    }
                    catch (Exception)
                    {
                    }
                }
        }

        return files;
    }

    public string GetText()
    {
        var pasteboard = UIKit.UIPasteboard.General;
        return pasteboard.String ?? string.Empty;
    }
}
