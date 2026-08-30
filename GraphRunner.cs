using System;
using System.Collections.Generic;
using System.IO;
using DcsBiosListener;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DCSBiosTRC
{
    // Owns whichever graph is currently "running on the device". Subscribes to the UDP
    // listener once, for the app's whole lifetime, and reacts to each DataReceived event -
    // it never polls/reads UdpListener state on its own. Deactivate() just makes that handler
    // a fast no-op; there is no tick timer anywhere in this class.
    public class GraphRunner
    {
        private static readonly TimeSpan UiPushInterval = TimeSpan.FromMilliseconds(100);
        private const string ActiveGraphName = "active";

        private readonly TrcManager _manager;
        private readonly IFormulaEvaluator _formulaEvaluator;
        private readonly GraphStorage _storage;
        private readonly Action<string> _setNodeValues;
        private readonly string _activeGraphPath;

        // Read on the UDP thread, written on whatever thread handles Activate/Deactivate
        // (today: the WebView message thread) - volatile gives safe, lock-free visibility
        // without needing anything heavier, since a graph is treated as immutable once compiled.
        private volatile CompiledGraph _compiledGraph;
        private string _activeGraphJson;
        private DateTime _lastUiPush = DateTime.MinValue;

        public GraphRunner(
            UdpListener listener,
            TrcManager manager,
            IFormulaEvaluator formulaEvaluator,
            GraphStorage storage,
            Action<string> setNodeValues)
        {
            _manager = manager;
            _formulaEvaluator = formulaEvaluator;
            _storage = storage;
            _setNodeValues = setNodeValues;

            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DCSBiosTRC");
            Directory.CreateDirectory(appDataDir);
            _activeGraphPath = Path.Combine(appDataDir, "active-graph.json");

            listener.DataReceived += OnUdpDataReceived;

            ResumeFromDisk();
        }

        private void ResumeFromDisk()
        {
            if (!File.Exists(_activeGraphPath)) return;
            try
            {
                Activate(File.ReadAllText(_activeGraphPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to auto-resume the active graph: {ex.Message}");
            }
        }

        public void Activate(string graphJson)
        {
            CompiledGraph compiled;
            try
            {
                compiled = GraphModel.Compile(graphJson);
            }
            catch (Exception ex)
            {
                // Only a genuinely unparseable graph rejects the activation outright - whatever
                // was previously running (if anything) is left untouched.
                Console.WriteLine($"Failed to activate graph: {ex.Message}");
                return;
            }

            _compiledGraph = compiled;
            _activeGraphJson = graphJson;

            _storage.WriteGraphFile(_activeGraphPath, graphJson);

            // Run once immediately so nodes with no live telemetry input (e.g. a bare
            // Math/Number feeding a gauge) still get set at least once.
            ExecutePass(compiled);
            PushNodeValuesToUi(compiled, force: true);

            // Deliberately does NOT push the graph itself back to the UI here (unlike LoadGraph
            // etc.) - the caller that triggered this (an explicit Run, or a live re-run while
            // editing) already has this exact state in its own canvas; re-pushing it would force
            // a full graph.configure() reconfigure/rebuild in the browser for no reason, fighting
            // with whatever the user might still be doing. A freshly (re)connected UI still gets
            // told about an already-active graph separately, via Director's "PageLoaded" handler
            // + TryGetActiveGraph.
        }

        public void Deactivate()
        {
            _compiledGraph = null;
            _activeGraphJson = null;
            if (File.Exists(_activeGraphPath))
            {
                try { File.Delete(_activeGraphPath); }
                catch (IOException ex) { Console.WriteLine($"Failed to delete active graph marker: {ex.Message}"); }
            }
        }

        public bool TryGetActiveGraph(out string name, out string json)
        {
            name = ActiveGraphName;
            json = _activeGraphJson;
            return _compiledGraph != null && json != null;
        }

        private void OnUdpDataReceived(object sender, DcsBiosDataEventArgs e)
        {
            CompiledGraph graph = _compiledGraph;
            if (graph == null) return;
            if (!graph.AddressToNodeIndices.TryGetValue(e.Address, out List<int> nodeIndices)) return;

            foreach (int nodeIndex in nodeIndices)
            {
                ApplyOutputValue(graph.NodesInOrder[nodeIndex], e.Data);
            }

            ExecutePass(graph);
            PushNodeValuesToUi(graph, force: false);
        }

        // Mirrors the JS OutputNode formula exactly (including not applying shift_by):
        // raw = data & mask, scaled = raw / max_value.
        private static void ApplyOutputValue(CompiledNode node, ushort data)
        {
            JObject output = node.Properties["output"] as JObject;
            if (output == null) return;

            int mask = output["mask"]?.Value<int>() ?? 0xFFFF;
            double maxValue = output["max_value"]?.Value<double>() ?? 1.0;
            int raw = data & mask;

            if (node.Outputs.Length > 0) node.Outputs[0] = raw;
            if (node.Outputs.Length > 1) node.Outputs[1] = maxValue;
            if (node.Outputs.Length > 2) node.Outputs[2] = maxValue != 0 ? raw / maxValue : 0.0;
        }

        private void ExecutePass(CompiledGraph graph)
        {
            CompiledNode[] nodes = graph.NodesInOrder;
            for (int i = 0; i < nodes.Length; i++)
            {
                CompiledNode node = nodes[i];
                if (node.Executor == null) continue; // unregistered node type - skip, leave outputs as-is
                try
                {
                    node.Executor.Execute(new NodeExecutionContext(nodes, i, _manager, _formulaEvaluator));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Graph node {node.Id} ({node.Type}) failed to execute: {ex.Message}");
                }
            }
        }

        private void PushNodeValuesToUi(CompiledGraph graph, bool force)
        {
            DateTime now = DateTime.UtcNow;
            if (!force && now - _lastUiPush < UiPushInterval) return;
            _lastUiPush = now;

            var values = new List<object[]>();
            foreach (CompiledNode node in graph.NodesInOrder)
            {
                for (int slot = 0; slot < node.OutputCount; slot++)
                {
                    values.Add(new object[] { node.Id, slot, node.Outputs[slot] });
                }
            }
            if (values.Count == 0) return;

            _setNodeValues?.Invoke(JsonConvert.SerializeObject(values));
        }
    }
}
