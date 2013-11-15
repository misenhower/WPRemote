using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP
{
    public class DACPNodeDictionary : Dictionary<string, byte[]>
    {
        protected DACPNodeDictionary()
        { }

        public static DACPNodeDictionary Parse(byte[] data)
        {
            return Parse(DACPUtility.GetResponseNodes(data));
        }

        public static DACPNodeDictionary Parse(IEnumerable<DACPNode> nodes)
        {
            DACPNodeDictionary dictionary = new DACPNodeDictionary();

            foreach (var node in nodes)
                dictionary[node.Key] = node.Value;

            return dictionary;
        }

        public string GetString(string key, string defaultValue = default(string))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DACPUtility.GetStringValue(this[key]);
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
            return DACPUtility.GetInt16Value(this[key]);
        }

        public int GetInt(string key, int defaultValue = default(int))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DACPUtility.GetInt32Value(this[key]);
        }

        public int? GetNullableInt(string key, int? defaultValue = default(int?))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DACPUtility.GetInt32Value(this[key]);
        }

        public long GetLong(string key, long defaultValue = default(long))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DACPUtility.GetInt64Value(this[key]);
        }

        public bool GetBool(string key, bool defaultValue = default(bool))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DACPUtility.GetBoolValue(this[key]);
        }

        public DateTime GetDateTime(string key, DateTime defaultValue = default(DateTime))
        {
            if (!this.ContainsKey(key))
                return defaultValue;
            return DACPUtility.GetDateTimeValue(this[key]);
        }
    }
}
