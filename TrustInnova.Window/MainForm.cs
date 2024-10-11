using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.AspNetCore.Components.Web;

namespace TrustInnova
{
    public partial class MainForm : Form
    {
        private BlazorWebView webView;

        public MainForm(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            webView = new BlazorWebView
            {
                Dock = DockStyle.Fill
            };

            webView.HostPage = "wwwroot\\index.html";
            webView.Services = serviceProvider;
            webView.RootComponents.Add<Main>("#app");
            webView.RootComponents.Add<HeadOutlet>("head::after");
            this.Controls.Add(webView);
        }
    }
}
