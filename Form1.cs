using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DcsBiosListener;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private void webView_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            string url = "";
#if DEBUG
            url = "http://localhost:5173/";
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
                var loader = new DataLoader();
                var obj = JObject.Parse(e.WebMessageAsJson);
                string type = obj["type"].Value<string>();
                string data = "";
                if (obj.ContainsKey("data") && type != "OutputsChanged") {
                    data = obj["data"].Value<string>();
                }
                WebDataReceived.Invoke(this, new WebviewDataEventArgs(type, data, obj));
            }
        }
    }
    public class WebviewDataEventArgs : EventArgs
    {
        public string Type { get; }
        public string Data { get; }

        public JObject Message { get; }

        public WebviewDataEventArgs(string type, string data, JObject message)
        {
            Type = type;
            Data = data;
            Message = message;
        }
    }
}
