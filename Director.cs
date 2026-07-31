using DcsBiosListener;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        public System.Windows.Forms.Control uiControl;
        public Director()
        {
            listener.DataReceived += (_, e) =>
            {
                if (webView != null && listenAddresses.Contains(e.Address))
                {
                    uiControl?.BeginInvoke((Action)(() => {
                        string script = $"window.dcs.setData({e.Address}, {e.Data})";
                        webView.ExecuteScriptAsync(script);
                    }));
                }
                //if (e.Address == 17428)
                //{
                //    double value = (double)e.Data / 65535.0 * 10.0;
                //    manager.SetLight((int)Math.Round(value));
                //    Console.Out.WriteLine(value);
                //}
            };
            listener.Start();

            manager.OnGaugesChanged += (s, gauges) =>
            {
                var gaugeInfo = gauges.Select(g => new
                {
                    productID = g.ProductID,
                    vendorID = g.VendorID,
                    versionNumber = g.VersionNumber
                }).ToList();
                string json = JsonConvert.SerializeObject(gaugeInfo);
                var script = $"window.trc.setGauges({json})";
                Console.WriteLine(script);
                uiControl?.BeginInvoke((Action)(() =>
                {
                    webView?.ExecuteScriptAsync(script);
                }));
            };
        }
        public void WebviewDataReceived(object sender, WebviewDataEventArgs e)
        {
            if (webView == null) return;
            switch (e.Type) {
                case "PageLoaded":
                    var loader = new DataLoader();
                    loader.loadBiosJsons(webView);
                    manager.RebuildGaugeList();
                    break;
                case "OutputsChanged":
                    List<int> addresses = e.Message["data"].ToObject<List<int>>();
                    listenAddresses = addresses;
                    break;
                case "GaugeChanged":
                    int gaugeIndex = e.Message["data"]["gaugeIndex"].Value<int>();
                    int light = e.Message["data"]["light"].Value<int>();
                    int s1 = e.Message["data"]["Servo1"].Value<int>();
                    int s2 = e.Message["data"]["Servo2"].Value<int>();
                    var gauges = manager.GetGauges();
                    if (gauges.Count > gaugeIndex)
                    {
                        Console.WriteLine($"Setting guage {gaugeIndex} light to {light}");
                        gauges[gaugeIndex].setLight(light);
                        gauges[gaugeIndex].setServo(1, Math.Min(Math.Max(500, s1), 2500));
                        gauges[gaugeIndex].setServo(2, Math.Min(Math.Max(500, s2), 2500));
                    }
                    break;
            }
        }
    }
}
