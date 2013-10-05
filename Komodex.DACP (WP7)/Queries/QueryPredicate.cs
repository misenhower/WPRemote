using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Queries
{
    internal class QueryPredicate : QueryElement
    {
        private const string IsOperator = ":";
        private const string IsNotOperator = "!:";

        private readonly string _key;
        private readonly string _value;
        private readonly string _operator;

        private QueryPredicate(string key, string value, string op)
        {
            _key = key;
            _value = value;
            _operator = op;
        }

        public static QueryPredicate Is(string key, string value)
        {
            return new QueryPredicate(key, value, IsOperator);
        }

        public static QueryPredicate Is(string key, int value)
        {
            return Is(key, value.ToString());
        }

        public static QueryPredicate IsNot(string key, string value)
        {
            return new QueryPredicate(key, value, IsNotOperator);
        }

        public static QueryPredicate IsNot(string key, int value)
        {
            return IsNot(key, value.ToString());
        }

        public override string ToString()
        {
            return "'" + _key + _operator + _value + "'";
        }
    }
}
