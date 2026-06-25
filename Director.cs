using DcsBiosListener;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DCSBiosTRC
{
    public class Director
    {
        private DcsBiosListener.UdpListener listener = new DcsBiosListener.UdpListener();
        public TrcManager manager = new TrcManager();
        public List<int> listenAddresses = new List<int>();
        public CoreWebView2 webView;
        public Director()
        {

            listener.DataReceived += (_, e) =>
            {
                if (webView != null && listenAddresses.Contains(e.Address))
                {
                    webView.ExecuteScriptAsync("console.log('asdf2')");
                    //webView.ExecuteScriptAsync("console.log('blablabla')");
                    //string script = $"window.dcs.setData({e.Address}, {e.Data})";
                    //webView.ExecuteScriptAsync(script);
                }
                //if (e.Address == 17428)
                //{
                //    double value = (double)e.Data / 65535.0 * 10.0;
                //    manager.SetLight((int)Math.Round(value));
                //    Console.Out.WriteLine(value);
                //}
            };
            listener.Start();
        }
        public void WebviewDataReceived(object sender, WebviewDataEventArgs e)
        {
            if (webView == null) return;
            webView.ExecuteScriptAsync("console.log('asdf')");
            switch (e.Type) {
                case "PageLoaded":
                    var loader = new DataLoader();
                    loader.loadBiosJsons(webView);
                    break;
                case "OutputsChanged":
                    List<int> addresses = e.Message["data"].ToObject<List<int>>();
                    listenAddresses = addresses;
                    break;
                case "GaugeChanged":
                    int gaugeIndex = e.Message["data"]["gaugeIndex"].Value<int>();
                    int light = e.Message["data"]["light"].Value<int>();
                    var gauges = manager.GetGauges();
                    if (gauges.Count > gaugeIndex)
                    {
                        Console.WriteLine($"Setting guage {gaugeIndex} light to {light}");
                        gauges[gaugeIndex].setLight(light);
                    }
                    break;
            }
        }
    }
}
