using Microsoft.AspNetCore.Components;

namespace Tinvo
{
    public static class GlobalConfig
    {
        public static IComponentRenderMode RenderMode { get; set; } = Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer;
    }
}
