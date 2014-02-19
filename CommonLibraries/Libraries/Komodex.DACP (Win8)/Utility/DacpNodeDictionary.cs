using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public class DacpNodeDictionary : Dictionary<string, byte[]>
    {
        protected DacpNodeDictionary()
        { }

        public static DacpNodeDictionary Parse(byte[] data)
        {
            return Parse(DacpUtility.GetResponseNodes(data));
        }

        public static DacpNodeDictionary Parse(IEnumerable<DacpNode> nodes)
        {
            DacpNodeDictionary dictionary = new DacpNodeDictionary();

            foreach (var node in nodes)
                dictionary[node.Key] = node.Value;

            return dictionary;
        }

        public string GetString(string key, string defaultValue = default(string))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DacpUtility.GetStringValue(this[key]);
        }

        public byte GetByte(string key, byte defaultValue = default(byte))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return this[key][0];
        }

        public short GetShort(string key, short defaultValue = default(short))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DacpUtility.GetInt16Value(this[key]);
        }

        public short? GetNullableShort(string key, short? defaultValue = default(short?))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DacpUtility.GetInt16Value(this[key]);
        }

        public int GetInt(string key, int defaultValue = default(int))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DacpUtility.GetInt32Value(this[key]);
        }

        public int? GetNullableInt(string key, int? defaultValue = default(int?))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DacpUtility.GetInt32Value(this[key]);
        }

        public long GetLong(string key, long defaultValue = default(long))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DacpUtility.GetInt64Value(this[key]);
        }

        public bool GetBool(string key, bool defaultValue = default(bool))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DacpUtility.GetBoolValue(this[key]);
        }

        public DateTime GetDateTime(string key, DateTime defaultValue = default(DateTime))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DacpUtility.GetDateTimeValue(this[key]);
        }
    }
}
