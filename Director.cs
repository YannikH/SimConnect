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
using System.Windows.Forms;
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
        private readonly GraphStorage storage = new GraphStorage();
        public GraphRunner graphRunner;

        // The file path a plain "Save" writes back to - set whenever a graph is loaded (by name
        // or via the Open dialog) or saved via "Save As". Null until any of those has happened.
        private string currentGraphPath;
        public Director()
        {
            graphRunner = new GraphRunner(listener, manager, new NCalcFormulaEvaluator(), storage, SendNodeValues);

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

            // Debug visibility into what actually reaches hardware, after rate limiting -
            // shown in-app since a release build has no attached console and DevTools aren't
            // reliably available.
            manager.OnGaugeUpdateApplied += (s, update) =>
            {
                var entry = new
                {
                    time = update.Timestamp.ToString("HH:mm:ss.fff"),
                    gaugeType = update.GaugeType,
                    gaugeId = update.GaugeId,
                    data = update.Data,
                };
                string json = JsonConvert.SerializeObject(entry);
                string script = $"window.trc.onGaugeUpdate({json})";
                uiControl?.BeginInvoke((Action)(() =>
                {
                    webView?.ExecuteScriptAsync(script);
                }));
            };
        }
        public async void WebviewDataReceived(object sender, WebviewDataEventArgs e)
        {
            if (webView == null) return;
            switch (e.Type) {
                case "PageLoaded":
                    var loader = new DataLoader();
                    loader.loadBiosJsons(webView);
                    manager.RebuildGaugeList();
                    SendGraphList();
                    if (graphRunner.TryGetActiveGraph(out string activeName, out string activeJson))
                    {
                        SendGraphToUi(activeName, activeJson);
                    }
                    break;
                case "OutputsChanged":
                    List<int> addresses = e.Message["data"].ToObject<List<int>>();
                    listenAddresses = addresses;
                    break;
                case "RequestGraphList":
                    SendGraphList();
                    break;
                case "LoadGraph":
                    {
                        string name = e.Message["data"]["name"].Value<string>();
                        try
                        {
                            string graphJson = storage.LoadGraph(name);
                            currentGraphPath = storage.GetGraphPath(name);
                            SendGraphToUi(name, graphJson);
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"Failed to load graph '{name}': {ex.Message}");
                        }
                        break;
                    }
                case "SaveGraph":
                    {
                        string graphJson = e.Message["data"]["graph"].ToString(Formatting.None);
                        if (!string.IsNullOrEmpty(currentGraphPath))
                        {
                            storage.WriteGraphFile(currentGraphPath, graphJson);
                            SendGraphList();
                        }
                        else
                        {
                            // Nothing to overwrite yet (a brand-new, never saved/loaded graph) -
                            // fall back to the same "Save As" flow.
                            ShowSaveAsDialog(graphJson);
                        }
                        break;
                    }
                case "SaveGraphDialog":
                    {
                        string graphJson = e.Message["data"]["graph"].ToString(Formatting.None);
                        ShowSaveAsDialog(graphJson);
                        break;
                    }
                case "LoadGraphDialog":
                    {
                        uiControl?.BeginInvoke((Action)(() =>
                        {
                            using (var dialog = new OpenFileDialog
                            {
                                InitialDirectory = storage.GetGraphsPath(),
                                Filter = "Graph files (*.json)|*.json|All files (*.*)|*.*",
                            })
                            {
                                if (dialog.ShowDialog(uiControl.FindForm()) == DialogResult.OK)
                                {
                                    string name = Path.GetFileNameWithoutExtension(dialog.FileName);
                                    string graphJson = File.ReadAllText(dialog.FileName);
                                    currentGraphPath = dialog.FileName;
                                    SendGraphToUi(name, graphJson);
                                }
                            }
                        }));
                        break;
                    }
                case "ActivateGraph":
                    {
                        string graphJson = e.Message["data"]["graph"].ToString(Formatting.None);
                        graphRunner.Activate(graphJson);
                        break;
                    }
                case "DeactivateGraph":
                    graphRunner.Deactivate();
                    uiControl?.BeginInvoke((Action)(() => { webView?.ExecuteScriptAsync("window.dcs.onGraphDeactivated()"); }));
                    break;
                case "GaugeChanged":
                    string gaugeType = e.Message["data"]["gaugeType"].Value<string>();
                    int gaugeId = e.Message["data"]["gaugeId"].Value<int>();
                    try
                    {
                        if (gaugeType == "AltimeterAdjust")
                        {
                            await manager.ApplyGaugeUpdate(gaugeType, gaugeId, (JObject)e.Message["data"]);
                        } else
                        {
                            await manager.SetGauge(gaugeType, gaugeId, (JObject)e.Message["data"]);
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        Console.WriteLine("Gauge disconnected");
                    }
                    break;
            }
        }

        // Shared by "SaveGraph" (when there's nowhere known to overwrite yet) and "SaveGraphDialog".
        private void ShowSaveAsDialog(string graphJson)
        {
            uiControl?.BeginInvoke((Action)(() =>
            {
                using (var dialog = new SaveFileDialog
                {
                    InitialDirectory = storage.GetGraphsPath(),
                    Filter = "Graph files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    AddExtension = true,
                })
                {
                    if (dialog.ShowDialog(uiControl.FindForm()) == DialogResult.OK)
                    {
                        storage.WriteGraphFile(dialog.FileName, graphJson);
                        currentGraphPath = dialog.FileName;
                        SendGraphList();
                    }
                }
            }));
        }

        // The one function that pushes a graph into the browser's canvas - used for LoadGraph,
        // LoadGraphDialog, and GraphRunner handing over whatever it's currently running (either
        // just-activated, or auto-resumed at startup once the UI connects).
        private void SendGraphToUi(string name, string graphJson)
        {
            string script = $"window.dcs.onGraphLoaded({JsonConvert.SerializeObject(name)}, {graphJson})";
            uiControl?.BeginInvoke((Action)(() => { webView?.ExecuteScriptAsync(script); }));
        }

        private void SendNodeValues(string valuesJson)
        {
            string script = $"window.dcs.setNodeValues({valuesJson})";
            uiControl?.BeginInvoke((Action)(() => { webView?.ExecuteScriptAsync(script); }));
        }

        private void SendGraphList()
        {
            if (webView == null) return;
            string json = JsonConvert.SerializeObject(storage.GetGraphNames());
            string script = $"window.dcs.setGraphList({json})";
            uiControl?.BeginInvoke((Action)(() => { webView?.ExecuteScriptAsync(script); }));
        }
    }
}
