using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Common.Converters
{
    public class InverseBooleanConverter : BooleanConverterBase<bool>
    {
        public InverseBooleanConverter()
        {
            TrueValue = false;
            FalseValue = true;
        }
    }
}
