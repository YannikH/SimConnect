using System;
using DcsBiosListener;
using Newtonsoft.Json.Linq;

namespace DCSBiosTRC
{
    public interface IGraphNodeExecutor
    {
        void Execute(NodeExecutionContext ctx);
    }

    // Per-node view into a single execution pass: reads this node's already-resolved input
    // sources (see GraphModel), and reads/writes this node's own output-value cache.
    public class NodeExecutionContext
    {
        private readonly CompiledNode[] _nodes;
        private readonly int _index;

        public NodeExecutionContext(CompiledNode[] nodes, int index, TrcManager manager, IFormulaEvaluator formulaEvaluator)
        {
            _nodes = nodes;
            _index = index;
            Manager = manager;
            FormulaEvaluator = formulaEvaluator;
        }

        public TrcManager Manager { get; }
        public IFormulaEvaluator FormulaEvaluator { get; }
        public JObject Properties => _nodes[_index].Properties;

        public double GetInput(int slot, double defaultValue = 0.0)
        {
            InputSource[] inputs = _nodes[_index].Inputs;
            if (slot < 0 || slot >= inputs.Length) return defaultValue;
            InputSource source = inputs[slot];
            if (!source.IsConnected) return defaultValue;

            double[] upstreamOutputs = _nodes[source.SourceNodeIndex].Outputs;
            if (source.SourceSlot < 0 || source.SourceSlot >= upstreamOutputs.Length) return defaultValue;
            return upstreamOutputs[source.SourceSlot];
        }

        public void SetOutput(int slot, double value)
        {
            double[] outputs = _nodes[_index].Outputs;
            if (slot < 0 || slot >= outputs.Length) return;
            outputs[slot] = value;
        }
    }

    // "DCS/{aircraft}/{category}/{id}" nodes never compute anything themselves - GraphRunner
    // writes their output values directly from incoming UDP events (see GraphRunner.OnUdpDataReceived).
    public class OutputNodeExecutor : IGraphNodeExecutor
    {
        public void Execute(NodeExecutionContext ctx)
        {
        }
    }

    public class NumberNodeExecutor : IGraphNodeExecutor
    {
        public void Execute(NodeExecutionContext ctx)
        {
            double value = ctx.Properties["value"]?.Value<double>() ?? 0.0;
            ctx.SetOutput(0, value);
        }
    }

    // Add/Subtract/Multiply/Divide: if either input is 0/NaN/unconnected, skip entirely and
    // leave the last output value in place - replicates the JS `if (!a || !b) return;` quirk.
    public class BinaryMathNodeExecutor : IGraphNodeExecutor
    {
        private readonly Func<double, double, double> _calculate;

        public BinaryMathNodeExecutor(Func<double, double, double> calculate)
        {
            _calculate = calculate;
        }

        public void Execute(NodeExecutionContext ctx)
        {
            double a = ctx.GetInput(0);
            double b = ctx.GetInput(1);
            if (IsFalsy(a) || IsFalsy(b)) return;
            ctx.SetOutput(0, _calculate(a, b));
        }

        private static bool IsFalsy(double value) => value == 0.0 || double.IsNaN(value);
    }

    public class ClampNodeExecutor : IGraphNodeExecutor
    {
        public void Execute(NodeExecutionContext ctx)
        {
            double inVal = ctx.GetInput(0);
            double min = ctx.GetInput(1);
            double max = ctx.GetInput(2);
            if (IsFalsy(inVal) || IsFalsy(min) || IsFalsy(max)) return;
            ctx.SetOutput(0, Math.Max(Math.Min(inVal, max), min));
        }

        private static bool IsFalsy(double value) => value == 0.0 || double.IsNaN(value);
    }

    // Unlike the other math nodes, unconnected inputs default to 0 rather than skipping -
    // matches the JS Eval node. Only a thrown/non-finite result is dropped.
    public class EvalNodeExecutor : IGraphNodeExecutor
    {
        public void Execute(NodeExecutionContext ctx)
        {
            double a = ctx.GetInput(0, 0.0);
            double b = ctx.GetInput(1, 0.0);
            string operation = ctx.Properties["operation"]?.Value<string>() ?? "a * 2";
            try
            {
                double result = ctx.FormulaEvaluator.Evaluate(operation, a, b);
                if (double.IsNaN(result) || double.IsInfinity(result)) return;
                ctx.SetOutput(0, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eval node formula '{operation}' failed: {ex.Message}");
            }
        }
    }

    public class GeneralGaugeNodeExecutor : IGraphNodeExecutor
    {
        public void Execute(NodeExecutionContext ctx)
        {
            int gaugeId = ctx.Properties["gaugeId"]?.Value<int>() ?? 0;
            var data = new JObject
            {
                ["gaugeType"] = "General",
                ["gaugeId"] = gaugeId,
                ["Servo1"] = Math.Round(ctx.GetInput(0, 1500)),
                ["Servo2"] = Math.Round(ctx.GetInput(1, 1500)),
                ["light"] = Math.Round(ctx.GetInput(2, 0)),
            };
            ctx.Manager.SetGauge("General", gaugeId, data);
        }
    }

    public class AltimeterNodeExecutor : IGraphNodeExecutor
    {
        public void Execute(NodeExecutionContext ctx)
        {
            int gaugeId = ctx.Properties["gaugeId"]?.Value<int>() ?? 0;
            var data = new JObject
            {
                ["gaugeType"] = "Altimeter",
                ["gaugeId"] = gaugeId,
                ["AltFt"] = Math.Round(ctx.GetInput(0, 0)),
                ["light"] = Math.Round(ctx.GetInput(1, 0)),
                ["adjust"] = 0,
            };
            ctx.Manager.SetGauge("Altimeter", gaugeId, data);
        }
    }

    public class HeadingIndicatorNodeExecutor : IGraphNodeExecutor
    {
        public void Execute(NodeExecutionContext ctx)
        {
            int gaugeId = ctx.Properties["gaugeId"]?.Value<int>() ?? 0;
            var data = new JObject
            {
                ["gaugeType"] = "HeadingIndicator",
                ["gaugeId"] = gaugeId,
                ["direction"] = ctx.GetInput(0, 0),
                ["light"] = Math.Round(ctx.GetInput(1, 0)),
            };
            ctx.Manager.SetGauge("HeadingIndicator", gaugeId, data);
        }
    }
}
