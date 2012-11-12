
namespace Komodex.Bonjour.DNS
{
    // DNS Record Type Reference:
    // http://en.wikipedia.org/wiki/List_of_DNS_record_types

    /// <summary>
    /// Resource record types
    /// </summary>
    internal enum ResourceRecordType
    {
        Unknown = 0,
        All = 255,

        A = 1,
        PTR = 12,
        SRV = 33,
        TXT = 16,
    }
}
