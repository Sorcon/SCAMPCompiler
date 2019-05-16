using System;

namespace SCAMP
{
    public class Parameter : ICloneable
    {

        private string _Raw;

        private decimal? _Value;

        public Parameter(decimal value)
        {
            _Value = value;
        }

        public Parameter(string raw)
        {
            Raw = raw;
        }

        public string Raw
        {
            get
            {
                return _Raw;
            }
            set
            {
                _Raw = value;
                // immediately parse simple values
                _Value = Calculator.DecimalTryParse(_Raw, out decimal decimal_value) ? decimal_value : (decimal?)null;
            }
        }
        public decimal? Value => _Value;

        public bool Resolved => _Value != null;

        public void Resolve(Calculator.ValueResolveCallBack call_back)
        {
            if (_Value == null || Raw != null)
            {
                _Value = null;
                _Value = Calculator.Calc(Raw, call_back);
            }
        }

        public override string ToString()
        {
            return _Value == null ? Raw : _Value.ToString();
        }

        public object Clone()
        {
            return new Parameter(ToString());
        }

    }
}