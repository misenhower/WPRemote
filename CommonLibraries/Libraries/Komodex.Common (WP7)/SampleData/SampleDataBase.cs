using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Common.SampleData
{
    public abstract class SampleDataBase
    {
        public SampleDataBase()
        {
            SampleDataHelper.FillSampleData(this);
        }
    }
}
