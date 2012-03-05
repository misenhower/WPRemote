using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Komodex.Bonjour.DNS
{
    // DNS Record Type Reference:
    // http://en.wikipedia.org/wiki/List_of_DNS_record_types

    /// <summary>
    /// Resource record types
    /// </summary>
    internal enum RRType
    {
        Unknown = 0,
        All = 255,

        A = 1,
        PTR = 12,
        SRV = 33,
        TXT = 16,
    }
}
