using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo.Application
{
    public enum PlatformType
    {
        Unknown,
        Maui,
        Linux,
        WebServer,
        WebAssembly,
        Winformedge
    }

    public interface IPlatform
    {
        public PlatformType Type { get; init; }
    }

    public class Platform : IPlatform
    {
        public required PlatformType Type { get; init; }
    }
}
