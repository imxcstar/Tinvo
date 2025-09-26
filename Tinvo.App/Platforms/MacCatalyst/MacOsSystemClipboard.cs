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
                var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                var imageData = image.AsPNG();
                System.IO.File.WriteAllBytes(tempPath, imageData.ToArray());
                files.Add(new SystemClipboardFileInfo
                {
                    Name = System.IO.Path.GetFileName(tempPath),
                    Stream = new MemoryStream(System.IO.File.ReadAllBytes(tempPath))
                });
            }
        }

        if (pasteboard.HasStrings)
        {
            var text = pasteboard.String;
            if (!string.IsNullOrEmpty(text))
            {
                var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
                System.IO.File.WriteAllText(tempPath, text);
                files.Add(new SystemClipboardFileInfo
                {
                    Name = System.IO.Path.GetFileName(tempPath),
                    Stream = new MemoryStream(System.IO.File.ReadAllBytes(tempPath))
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
