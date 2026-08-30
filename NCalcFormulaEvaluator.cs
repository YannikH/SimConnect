using System;
using System.Globalization;
using System.Text.RegularExpressions;
using NCalc;

namespace DCSBiosTRC
{
    // Evaluates the user-typed "operation" formula on a Math/Eval node (e.g. "a * 2 - 3").
    //
    // NCalc's expression language differs from the JS `new Function("a","b",...)` semantics
    // the UI used to use, in two ways that matter for parity with formulas users already typed:
    //  - Parameters must be written as [a]/[b], not bare identifiers - see
    //    https://ncalc.github.io/ncalc/articles/language/parameters.html
    //  - There is no native `cond ? then : else` ternary operator; the equivalent is the
    //    `if(cond, then, else)` function - see
    //    https://ncalc.github.io/ncalc/articles/language/operators.html
    // Transform() rewrites a formula written the "JS way" into NCalc's syntax before parsing,
    // so existing/expected formulas keep working without the user needing to learn NCalc's dialect.
    public class NCalcFormulaEvaluator : IFormulaEvaluator
    {
        private static readonly Regex VariableA = new Regex(@"\ba\b", RegexOptions.Compiled);
        private static readonly Regex VariableB = new Regex(@"\bb\b", RegexOptions.Compiled);

        // Single, non-nested ternary: "cond ? then : else" -> "if(cond, then, else)".
        // A lone "?" not immediately followed by another "?" (so "??" is left alone).
        private static readonly Regex Ternary = new Regex(@"^(.*?)\?(?!\?)(.*?):(.*)$", RegexOptions.Compiled);

        public double Evaluate(string expression, double a, double b)
        {
            string ncalcExpression = ToNCalcSyntax(expression);
            var expr = new Expression(ncalcExpression, CultureInfo.InvariantCulture);
            expr.Parameters["a"] = a;
            expr.Parameters["b"] = b;
            object result = expr.Evaluate();
            return Convert.ToDouble(result, CultureInfo.InvariantCulture);
        }

        private static string ToNCalcSyntax(string operation)
        {
            string text = operation;

            text = Regex.Replace(text, @"Math\.max", "Max", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"Math\.min", "Min", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"Math\.pow", "Pow", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"Math\.abs", "Abs", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"Math\.sqrt", "Sqrt", RegexOptions.IgnoreCase);

            Match ternaryMatch = Ternary.Match(text);
            if (ternaryMatch.Success)
            {
                text = $"if({ternaryMatch.Groups[1].Value.Trim()},{ternaryMatch.Groups[2].Value.Trim()},{ternaryMatch.Groups[3].Value.Trim()})";
            }

            text = VariableA.Replace(text, "[a]");
            text = VariableB.Replace(text, "[b]");
            return text;
        }
    }
}
