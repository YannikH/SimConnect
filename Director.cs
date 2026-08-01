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
using TrcSdkLib;

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
                    gaugeType = TrcManager.GetGaugeType(g),
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
                    string gaugeType = e.Message["data"]["gaugeType"].Value<string>();
                    int gaugeIndex = e.Message["data"]["gaugeIndex"].Value<int>();
                    manager.SetGauge(gaugeType, gaugeIndex, (JObject)e.Message["data"]);
                    break;
            }
        }
    }
}
