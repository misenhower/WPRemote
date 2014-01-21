using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Queries
{
    internal class DacpQueryPredicate : DacpQueryElement
    {
        private const string IsOperator = ":";
        private const string IsNotOperator = "!:";

        private readonly string _key;
        private readonly string _value;
        private readonly string _operator;

        private DacpQueryPredicate(string key, string value, string op)
        {
            _key = key;
            _value = value;
            _operator = op;
        }

        public static DacpQueryPredicate Is(string key, object value)
        {
            return new DacpQueryPredicate(key, value.ToString(), IsOperator);
        }

        public static DacpQueryPredicate IsNot(string key, object value)
        {
            return new DacpQueryPredicate(key, value.ToString(), IsNotOperator);
        }

        public static DacpQueryPredicate IsNotEmpty(string key)
        {
            return IsNot(key, string.Empty);
        }

        public override string ToString()
        {
            return "'" + _key + _operator + Uri.EscapeDataString(DacpUtility.EscapeSingleQuotes(_value)) + "'";
        }
    }
}
