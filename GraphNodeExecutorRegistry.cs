using System;
using System.Collections.Generic;

namespace DCSBiosTRC
{
    public static class GraphNodeExecutorRegistry
    {
        private static readonly IGraphNodeExecutor OutputExecutor = new OutputNodeExecutor();

        private static readonly Dictionary<string, IGraphNodeExecutor> ByType = new Dictionary<string, IGraphNodeExecutor>
        {
            ["Math/Number"] = new NumberNodeExecutor(),
            ["Math/Add"] = new BinaryMathNodeExecutor((a, b) => a + b),
            ["Math/Subtract"] = new BinaryMathNodeExecutor((a, b) => a - b),
            ["Math/Multiply"] = new BinaryMathNodeExecutor((a, b) => a * b),
            ["Math/Divide"] = new BinaryMathNodeExecutor((a, b) => a / b),
            ["Math/Clamp"] = new ClampNodeExecutor(),
            ["Math/Eval"] = new EvalNodeExecutor(),
            ["TRC/GeneralGauge"] = new GeneralGaugeNodeExecutor(),
            ["TRC/Altimeter"] = new AltimeterNodeExecutor(),
            ["TRC/HeadingIndicator"] = new HeadingIndicatorNodeExecutor(),
        };

        // Returns null for an unregistered node type - callers treat that as "skip this node".
        public static IGraphNodeExecutor Resolve(string type)
        {
            if (type != null && type.StartsWith("DCS/", StringComparison.Ordinal))
            {
                return OutputExecutor;
            }
            return ByType.TryGetValue(type ?? "", out IGraphNodeExecutor executor) ? executor : null;
        }
    }
}
