using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Komodex.Common.Analytics
{
    [XmlType("IssueReport")]
    public class IssueReport
    {
        internal IssueReport() { }

        public static IssueReport Create(Exception e =null)
        {
            IssueReport report = new IssueReport();
            report.ID = Guid.NewGuid();
            report.Date = DateTimeOffset.Now;
            report.AppName = AppInfo.Name;
            report.AppDisplayName = AppInfo.DisplayName;
            report.AppVersion = AppInfo.Version;
#if DEBUG
            report.IsDebug = true;
#endif
            report.DeviceType = DeviceInfo.Type.ToString();
            report.CultureName = CultureInfo.CurrentCulture.EnglishName;

            if (e!=null)
            {
                report.ExceptionType = e.GetType().Name;
                
            }

            return report;
        }

        public Guid ID { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Type { get; set; }
        public string AppName { get; set; }
        public string AppDisplayName { get; set; }
        public string AppVersion { get; set; }
        public bool IsDebug { get; set; }
        public string DeviceType { get; set; }
        public string CultureName { get; set; }

        public string MessageBody { get; set; }

        public string ExceptionType { get; set; }
    }
}
