using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Common.Phone.Controls
{
    // This file contains several standard controls that are inherited to provide XAML compatibility
    // between WP7 and WP8 apps. These controls come from different assemblies depending on the OS
    // so inheriting them here allows the same namespace to be used for apps built on each platform.

    public class Pivot : Microsoft.Phone.Controls.Pivot { }
    public class PivotItem : Microsoft.Phone.Controls.PivotItem { }
    public class PivotHeadersControl : Microsoft.Phone.Controls.Primitives.PivotHeadersControl { }
    public class Panorama : Microsoft.Phone.Controls.Panorama { }
    public class PanoramaItem : Microsoft.Phone.Controls.PanoramaItem { }
    public class LongListSelector : Microsoft.Phone.Controls.LongListSelector { }
}
