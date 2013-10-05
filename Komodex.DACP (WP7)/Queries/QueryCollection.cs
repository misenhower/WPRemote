using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Queries
{
    internal class QueryCollection : QueryElement
    {
        private const string AndOperator = "+";
        private const string OrOperator = ",";

        private readonly string _operator;
        private readonly List<QueryElement> _elements;

        private QueryCollection(string op, params QueryElement[] args)
        {
            _operator = op;
            _elements = new List<QueryElement>(args);
        }

        public static QueryCollection And(params QueryElement[] args)
        {
            return new QueryCollection(AndOperator, args);
        }

        public static QueryCollection Or(params QueryElement[] args)
        {
            return new QueryCollection(OrOperator, args);
        }

        public void Add(QueryElement element)
        {
            _elements.Add(element);
        }

        public override string ToString()
        {
            return "(" + string.Join(_operator, _elements) + ")";
        }
    }
}
