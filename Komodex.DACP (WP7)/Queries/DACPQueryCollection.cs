using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Queries
{
    internal class DACPQueryCollection : DACPQueryElement
    {
        private const string AndOperator = "+";
        private const string OrOperator = ",";

        private readonly string _operator;
        private readonly List<DACPQueryElement> _elements;

        private DACPQueryCollection(string op, params DACPQueryElement[] args)
        {
            _operator = op;
            _elements = new List<DACPQueryElement>(args);
        }

        public static DACPQueryCollection And(params DACPQueryElement[] args)
        {
            return new DACPQueryCollection(AndOperator, args);
        }

        public static DACPQueryCollection Or(params DACPQueryElement[] args)
        {
            return new DACPQueryCollection(OrOperator, args);
        }

        public void Add(DACPQueryElement element)
        {
            _elements.Add(element);
        }

        public override string ToString()
        {
            return "(" + string.Join(_operator, _elements) + ")";
        }
    }
}
