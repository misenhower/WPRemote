using Komodex.Common.SampleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.CommonLibrariesTestApp.SampleData
{
    public class SampleDataViewSource : SampleDataBase
    {
        public List<SampleDataGroupedItems<SampleDataPerson>> People { get; set; }
        //public List<SampleDataPerson> People { get; set; }
    }

    public class SampleDataGroupedItems<T> : List<T>
    {
        public string Key { get; set; }
    }

    public class SampleDataPerson : SampleDataBase
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
