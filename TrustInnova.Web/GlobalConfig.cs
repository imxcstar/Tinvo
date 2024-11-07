using Microsoft.AspNetCore.Components;

namespace TrustInnova
{
    public static class GlobalConfig
    {
        public static IComponentRenderMode RenderMode { get; set; } = Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer;
    }
}
