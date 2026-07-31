using DcsBiosListener;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace DCSBiosTRC
{
    public partial class Form1 : Form
    {
        private Director _director;
        public event EventHandler<WebviewDataEventArgs> WebDataReceived;
        public event EventHandler<CoreWebView2> WebViewLoaded;
        public Form1(Director director)
        {
            _director = director;
            InitializeComponent();
        }

        private void webView21_Click(object sender, EventArgs e)
        {

        }

        private async void webView_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dist");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "dcs.trc",
                folder, // @"C:\Work\SimConnect\ui\dist", 
                CoreWebView2HostResourceAccessKind.Allow
            );

            string url = "https://dcs.trc/index.html";
#if DEBUG
            url = "http://localhost:5173";
#endif
            if (webView != null && webView.CoreWebView2 != null)
            {
                WebViewLoaded.Invoke(this, webView.CoreWebView2);
                webView.CoreWebView2.Navigate(url);
            }
        }

        private void webView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                WebViewLoaded.Invoke(this, webView.CoreWebView2);
                var loader = new DataLoader();
                var obj = JObject.Parse(e.WebMessageAsJson);
                string type = obj["type"].Value<string>();
                WebDataReceived.Invoke(this, new WebviewDataEventArgs(type, obj));
            }
        }
    }
    public class WebviewDataEventArgs : EventArgs
    {
        public string Type { get; }
        public string Data { get; }

        public JObject Message { get; }

        public WebviewDataEventArgs(string type, JObject message)
        {
            Type = type;
            Message = message;
        }
    }
}
