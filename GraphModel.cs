using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DCSBiosTRC
{
    // One input slot's resolved upstream source, computed once at compile time from the
    // litegraph "links" table so a tick never needs to re-resolve topology.
    public struct InputSource
    {
        public bool IsConnected;
        public int SourceNodeIndex;
        public int SourceSlot;
    }

    public class CompiledNode
    {
        public int Id;
        public string Type;
        public JObject Properties;
        public IGraphNodeExecutor Executor;
        public InputSource[] Inputs;
        public int OutputCount;

        // Current value of each output slot. Persists across passes so a node whose inputs
        // are momentarily "falsy" (see BinaryMathNodeExecutor/ClampNodeExecutor) keeps showing
        // its last real value instead of resetting to zero.
        public double[] Outputs;
    }

    public class CompiledGraph
    {
        // Nodes sorted ascending by litegraph's own "order" field, which is already a valid
        // topological execution order - see GraphModel.Compile.
        public CompiledNode[] NodesInOrder;

        // DCS-BIOS address -> indices (into NodesInOrder) of every "DCS/..." node reading that
        // address, so an incoming UDP event can cheaply tell whether it's relevant at all.
        public Dictionary<int, List<int>> AddressToNodeIndices;
    }

    public static class GraphModel
    {
        private class RawNode
        {
            public int Id;
            public string Type;
            public int Order;
            public JObject Properties;
            public JArray Inputs;
            public JArray Outputs;
        }

        public static CompiledGraph Compile(string graphJson)
        {
            JObject doc = JObject.Parse(graphJson);
            JArray rawNodes = doc["nodes"] as JArray ?? new JArray();
            JArray rawLinks = doc["links"] as JArray ?? new JArray();

            // link id -> (originId, originSlot)
            var linkOrigins = new Dictionary<int, KeyValuePair<int, int>>();
            foreach (JToken linkToken in rawLinks)
            {
                JArray link = linkToken as JArray;
                if (link == null || link.Count < 5) continue;
                int linkId = link[0].Value<int>();
                int originId = link[1].Value<int>();
                int originSlot = link[2].Value<int>();
                linkOrigins[linkId] = new KeyValuePair<int, int>(originId, originSlot);
            }

            var parsed = new List<RawNode>();
            foreach (JToken nodeToken in rawNodes)
            {
                JObject node = nodeToken as JObject;
                if (node == null) continue;

                int mode = node["mode"]?.Value<int>() ?? 0;
                if (mode != 0) continue; // only ALWAYS-mode nodes participate in dataflow execution

                parsed.Add(new RawNode
                {
                    Id = node["id"]?.Value<int>() ?? 0,
                    Type = node["type"]?.Value<string>() ?? "",
                    Order = node["order"]?.Value<int>() ?? 0,
                    Properties = node["properties"] as JObject ?? new JObject(),
                    Inputs = node["inputs"] as JArray ?? new JArray(),
                    Outputs = node["outputs"] as JArray ?? new JArray(),
                });
            }
            parsed.Sort((x, y) => x.Order.CompareTo(y.Order));

            var indexById = new Dictionary<int, int>();
            for (int i = 0; i < parsed.Count; i++) indexById[parsed[i].Id] = i;

            var nodes = new CompiledNode[parsed.Count];
            var addressToNodeIndices = new Dictionary<int, List<int>>();

            for (int i = 0; i < parsed.Count; i++)
            {
                RawNode raw = parsed[i];

                var inputSources = new InputSource[raw.Inputs.Count];
                for (int slot = 0; slot < raw.Inputs.Count; slot++)
                {
                    JObject input = raw.Inputs[slot] as JObject;
                    JToken linkToken = input?["link"];
                    bool hasLink = linkToken != null && linkToken.Type != JTokenType.Null;
                    if (hasLink && linkOrigins.TryGetValue(linkToken.Value<int>(), out var origin) &&
                        indexById.TryGetValue(origin.Key, out int sourceIndex))
                    {
                        inputSources[slot] = new InputSource
                        {
                            IsConnected = true,
                            SourceNodeIndex = sourceIndex,
                            SourceSlot = origin.Value,
                        };
                    }
                    else
                    {
                        inputSources[slot] = new InputSource { IsConnected = false };
                    }
                }

                var compiled = new CompiledNode
                {
                    Id = raw.Id,
                    Type = raw.Type,
                    Properties = raw.Properties,
                    Executor = GraphNodeExecutorRegistry.Resolve(raw.Type),
                    Inputs = inputSources,
                    OutputCount = raw.Outputs.Count,
                    Outputs = new double[Math.Max(raw.Outputs.Count, 1)],
                };
                nodes[i] = compiled;

                if (raw.Type.StartsWith("DCS/", StringComparison.Ordinal))
                {
                    JObject output = raw.Properties["output"] as JObject;
                    int? address = output?["address"]?.Value<int>();
                    if (address.HasValue)
                    {
                        if (!addressToNodeIndices.TryGetValue(address.Value, out List<int> list))
                        {
                            list = new List<int>();
                            addressToNodeIndices[address.Value] = list;
                        }
                        list.Add(i);
                    }
                }
            }

            return new CompiledGraph
            {
                NodesInOrder = nodes,
                AddressToNodeIndices = addressToNodeIndices,
            };
        }
    }
}
