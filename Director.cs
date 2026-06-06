using DcsBiosListener;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCSBiosTRC
{
    public class Director
    {
        private DcsBiosListener.UdpListener listener = new DcsBiosListener.UdpListener();
        public TrcManager manager = new TrcManager();
        public List<int> listenAddresses = new List<int>();
        public Microsoft.Web.WebView2.WinForms.WebView2 webView;
        public Director()
        {

            listener.DataReceived += (_, e) =>
            {
                if (webView != null && listenAddresses.Contains(e.Address))
                {
                    webView.CoreWebView2.ExecuteScriptAsync($"window.dcs.setData({e.Address}, {e.Data})");
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
    }
}
