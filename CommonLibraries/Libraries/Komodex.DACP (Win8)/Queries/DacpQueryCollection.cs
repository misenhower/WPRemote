using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Queries
{
    internal class DacpQueryCollection : DacpQueryElement
    {
        private const string AndOperator = "+";
        private const string OrOperator = ",";

        private readonly string _operator;
        private readonly List<DacpQueryElement> _elements;

        private DacpQueryCollection(string op, params DacpQueryElement[] args)
        {
            _operator = op;
            _elements = new List<DacpQueryElement>(args);
        }

        public static DacpQueryCollection And(params DacpQueryElement[] args)
        {
            return new DacpQueryCollection(AndOperator, args);
        }

        public static DacpQueryCollection Or(params DacpQueryElement[] args)
        {
            return new DacpQueryCollection(OrOperator, args);
        }

        public void Add(DacpQueryElement element)
        {
            _elements.Add(element);
        }

        public override string ToString()
        {
            return "(" + string.Join(_operator, _elements) + ")";
        }
    }
}
