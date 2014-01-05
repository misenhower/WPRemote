using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Queries
{
    internal class DACPQueryPredicate : DACPQueryElement
    {
        private const string IsOperator = ":";
        private const string IsNotOperator = "!:";

        private readonly string _key;
        private readonly string _value;
        private readonly string _operator;

        private DACPQueryPredicate(string key, string value, string op)
        {
            _key = key;
            _value = value;
            _operator = op;
        }

        public static DACPQueryPredicate Is(string key, object value)
        {
            return new DACPQueryPredicate(key, value.ToString(), IsOperator);
        }

        public static DACPQueryPredicate IsNot(string key, object value)
        {
            return new DACPQueryPredicate(key, value.ToString(), IsNotOperator);
        }

        public static DACPQueryPredicate IsNotEmpty(string key)
        {
            return IsNot(key, string.Empty);
        }

        public override string ToString()
        {
            return "'" + _key + _operator + Uri.EscapeDataString(DACPUtility.EscapeSingleQuotes(_value)) + "'";
        }
    }
}
