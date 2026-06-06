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
        public Form1(Director director)
        {
            _director = director;
            InitializeComponent();
            //var listener = new DcsBiosListener.UdpListener();
            //listener.DataReceived += (_, e) =>
            //{
            //    if (webView != null && webView.CoreWebView2 != null)
            //    {
            //        webView.CoreWebView2.ExecuteScriptAsync($"window.dcs.setData({e.Address}, {e.Data})");
            //    }
            //};
            //listener.Start();
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
                webView.CoreWebView2.Navigate(url);
            }
        }

        private void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                string jsonStr = "{\"test\": \"abcd\"}";
                webView.CoreWebView2.ExecuteScriptAsync($"console.log({jsonStr})");
                new DataLoader().loadJsons(webView);
            }
        }

        private void webView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                _director.webView = webView;
                var loader = new DataLoader();
                var obj = JObject.Parse(e.WebMessageAsJson);
                string type = obj["type"].Value<string>();
                if (type == "PageLoaded")
                {
                    loader.loadJsons(webView);
                } else if (type == "OutputsChanged")
                {
                    List<int> addresses = obj["data"].ToObject<List<int>>();
                    _director.listenAddresses = addresses;
                }
                else if (type == "GaugeChanged")
                {
                    int gaugeIndex = obj["data"]["gaugeIndex"].Value<int>();
                    int light = obj["data"]["light"].Value<int>();
                    var gauges = _director.manager.GetGauges();
                    if (gauges.Count > gaugeIndex)
                    {
                        Console.WriteLine($"Setting guage {gaugeIndex} light to {light}");
                        gauges[gaugeIndex].setLight(light);
                    }
                }
            }
        }
    }
}
