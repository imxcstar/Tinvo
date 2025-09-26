using System;

namespace Tinvo.Abstractions;

public interface ISystemClipboard
{
    public string GetText();

    public List<SystemClipboardFileInfo> GetFiles();
}

public class SystemClipboardFileInfo
{
    public string? Name { get; set; }
    public required Stream Stream { get; set; }
}