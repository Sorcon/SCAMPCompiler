using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SCAMP
{
    public static class Calculator
    {

        private struct Optimize
        {

            public string Expression;
            public string Replacement;

        }

        private static readonly Optimize[] Optimizes = {
            new Optimize() { Expression = @"\bnot\b", Replacement = "~" },
            new Optimize() { Expression = @"(?<!\<)\<\<(?!\<)", Replacement = "<" },
            new Optimize() { Expression = @"\bshl\b", Replacement = "<" },
            new Optimize() { Expression = @"(?<!\>)\>\>(?!\>)", Replacement = ">" },
            new Optimize() { Expression = @"\bshr\b", Replacement = ">" },
            new Optimize() { Expression = @"\band\b", Replacement = "&" },
            new Optimize() { Expression = @"\bxor\b", Replacement = "^" },
            new Optimize() { Expression = @"\bor\b", Replacement = "|" },
            new Optimize() { Expression = @"\s+", Replacement = "" },
        };

        private static decimal Resolve(string value, ValueResolveCallBack call_back)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new Exception("Value is null or empty");
            }
            int sign;
            if (value[0].Equals('\''))
            {
                value = value.Substring(1);
                sign = -1;
            }
            else
            {
                sign = 1;
            }
            if (
                DecimalTryParse(value, out decimal result) ||
                (call_back != null && DecimalTryParse(call_back(value), out result))
            )
            {
                return sign * result;
            }
            throw new Exception("Given value '" + value + "' can't be resolved as number");
        }

        private static string ToStringSign(this decimal value)
        {
            return value < 0 ? "'" + (-value).ToString() : value.ToString();
        }

        private static string ToStringSign(this Int64 value)
        {
            return value < 0 ? "'" + (-value).ToString() : value.ToString();
        }

        // calculate expression without brackets
        private static decimal CalcPlane(string expression, ValueResolveCallBack call_back)
        {
            bool again;

            // Unary plus, minus and bitwise NOT
            Regex regex = new Regex(@"(?<=[+\-~*\/%<>&^|]|^)(?<op>[+\-~])(?<A>[^+\-~*\/%<>&^|]+)");
            do
            {
                again = false;
                expression = regex.Replace(expression, (Match m) =>
                {
                    again = true;

                    var op = m.Groups["op"].Value;
                    var a = Resolve(m.Groups["A"].Value, call_back);

                    switch (op)
                    {
                        case "+":
                            return a.ToString();

                        case "-":
                            return (-a).ToStringSign(); // change notation for negative numbers to 'value from -value
                        case "~":
                            return (~((Int64)a)).ToStringSign();

                        default:
                            throw new Exception("Invalid operator (check regexp for [~+-])");
                    }
                }, 1);
            }
            while (again);

            // Multiplication, division, and remainder
            regex = new Regex(@"(?<A>[^*\/%+\-<>&^|]+)(?<op>[*\/%])(?<B>[^*\/%+\-<>&^|]+)");
            do
            {
                again = false;
                expression = regex.Replace(expression, (Match m) =>
                {
                    again = true;

                    var op = m.Groups["op"].Value;
                    var a = Resolve(m.Groups["A"].Value, call_back);
                    var b = Resolve(m.Groups["B"].Value, call_back);

                    switch (op)
                    {
                        case "*":
                            return (a * b).ToStringSign();

                        case "/":
                            return (a / b).ToStringSign();

                        case "%":
                            return (a % b).ToStringSign();

                        default:
                            throw new Exception("Invalid operator (check regexp for [*/%])");
                    }
                }, 1);
            }
            while (again);

            // Addition and subtraction
            regex = new Regex(@"(?<A>[^*\/%+\-<>&^|]+)(?<op>[+\-])(?<B>[^*\/%+\-<>&^|]+)");
            do
            {
                again = false;
                expression = regex.Replace(expression, (Match m) =>
                {
                    again = true;

                    var op = m.Groups["op"].Value;
                    var a = Resolve(m.Groups["A"].Value, call_back);
                    var b = Resolve(m.Groups["B"].Value, call_back);

                    switch (op)
                    {
                        case "+":
                            return (a + b).ToStringSign();

                        case "-":
                            return (a - b).ToStringSign();

                        default:
                            throw new Exception("Invalid operator (check regexp for [+-])");
                    }
                }, 1);
            }
            while (again);

            // Bitwise left shift and right shift
            regex = new Regex(@"(?<A>[^*\/%+\-<>&^|]+)(?<op>[<>])(?<B>[^*\/%+\-<>&^|]+)");
            do
            {
                again = false;
                expression = regex.Replace(expression, (Match m) =>
                {
                    again = true;

                    var op = m.Groups["op"].Value;
                    var a = Resolve(m.Groups["A"].Value, call_back);
                    var b = Resolve(m.Groups["B"].Value, call_back);

                    switch (op)
                    {
                        case "<":
                            return (((Int64)a) << ((Int32)b)).ToStringSign();

                        case ">":
                            return (((Int64)a) >> ((Int32)b)).ToStringSign();

                        default:
                            throw new Exception("Invalid operator (check regexp for [+-])");
                    }
                }, 1);
            }
            while (again);

            // Bitwise AND
            regex = new Regex(@"(?<A>[^*\/%+\-<>&^|]+)&(?<B>[^*\/%+\-<>&^|]+)");
            do
            {
                again = false;
                expression = regex.Replace(expression, (Match m) =>
                {
                    again = true;

                    var a = Resolve(m.Groups["A"].Value, call_back);
                    var b = Resolve(m.Groups["B"].Value, call_back);

                    return (((Int64)a) & ((Int64)b)).ToStringSign();
                }, 1);
            }
            while (again);

            // Bitwise XOR
            regex = new Regex(@"(?<A>[^*\/%+\-<>&^|]+)\^(?<B>[^*\/%+\-<>&^|]+)");
            do
            {
                again = false;
                expression = regex.Replace(expression, (Match m) =>
                {
                    again = true;

                    var a = Resolve(m.Groups["A"].Value, call_back);
                    var b = Resolve(m.Groups["B"].Value, call_back);

                    return (((Int64)a) ^ ((Int64)b)).ToStringSign();
                }, 1);
            }
            while (again);

            // Bitwise OR
            regex = new Regex(@"(?<A>[^*\/%+\-<>&^|]+)\|(?<B>[^*\/%+\-<>&^|]+)");
            do
            {
                again = false;
                expression = regex.Replace(expression, (Match m) =>
                {
                    again = true;

                    var a = Resolve(m.Groups["A"].Value, call_back);
                    var b = Resolve(m.Groups["B"].Value, call_back);

                    return (((Int64)a) | ((Int64)b)).ToStringSign();
                }, 1);
            }
            while (again);

            return Resolve(expression, call_back);
        }

        public delegate string ValueResolveCallBack(string name);

        public static bool DecimalTryParse(string value, out decimal result)
        {
            if (decimal.TryParse(value, out result))
            {
                return true;
            }
            Match m = Regex.Match(value, "^0x([0-9a-f]+)$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                result = (decimal)Convert.ToInt64(m.Groups[1].Value, 16);
                return true;
            }
            m = Regex.Match(value, "^0b([0-1]+)$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                result = (decimal)Convert.ToInt64(m.Groups[1].Value, 2);
                return true;
            }
            return false;
        }
        public static decimal Calc(string expression, ValueResolveCallBack call_back)
        {
            // normalize expression
            foreach (var optimize in Optimizes)
            {
                expression = Regex.Replace(expression, optimize.Expression, optimize.Replacement, RegexOptions.IgnoreCase);
            }

            Regex regex = new Regex(@"\(\s*(?<expression>[^()]*(?<!\s))\s*\)");
            bool again;
            do
            {
                again = false;
                // find expression in inner brackets if present
                expression = regex.Replace(expression, (Match m) =>
                {
                    again = true;
                    return CalcPlane(m.Groups["expression"].Value, call_back).ToString();
                }, 1);
            }
            while (again);

            return CalcPlane(expression, call_back);
        }

    }

    public class Constant
    {

        public string Name
        {
            get; set;
        }

        public string Value
        {
            get; set;
        }

        public string Comment
        {
            get; set;
        }

        override public string ToString()
        {
            return Name;
        }

        public string ToCode()
        {
            return Name + " equ " + Value + " ; " + Comment;
        }

    }

    public class ConstantList : List<Constant>
    {

        public Constant FindByName(string name, bool deep = true)
        {
            foreach (var item in this)
            {
                if (item.Name.Equals(name))
                {
                    var next = FindByName(item.Value);
                    return next ?? item; // (next == null) ? item : next
                }
            }
            return null;
        }

        public string GetValueByName(string name, bool deep = true)
        {
            var equ = FindByName(name, deep);
            if (equ != null)
            {
                return equ.Value;
            }
            return name;
        }

    }
}