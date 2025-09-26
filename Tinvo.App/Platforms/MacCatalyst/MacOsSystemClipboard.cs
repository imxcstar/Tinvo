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
                var imageData = image.AsPNG();
                if(imageData == null)
                    return files;
                files.Add(new SystemClipboardFileInfo
                {
                    Name = $"{Guid.NewGuid()}.png",
                    Stream = new MemoryStream(imageData.ToArray())
                });
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
