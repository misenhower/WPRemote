using Komodex.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    internal static class DacpUtility
    {
        #region Response Parsing

        public static IEnumerable<DacpNode> GetResponseNodes(byte[] data, bool rootNode = false)
        {
            int dataLength = data.Length;
            int location = 0;

            while (location + 8 < dataLength)
            {
                string code = Encoding.UTF8.GetString(data, location, 4);

                // If this is the root node, just assume we need the rest of the data.
                // This fixes issues with malformed responses (such as those from forked-daapd).
                int length;
                if (rootNode)
                    length = dataLength - 8;
                else
                    length = BitUtility.NetworkToHostOrder(BitConverter.ToInt32(data, location + 4));

                byte[] nodeBody = new byte[length];
                Buffer.BlockCopy(data, location + 8, nodeBody, 0, length);

                yield return new DacpNode(code, nodeBody);

                location += 8 + length;
            }
        }

        internal const string DefaultListKey = "mlcl";

        public static IEnumerable<T> GetItemsFromNodes<T>(byte[] data, Func<byte[], T> itemGenerator, string listKey = DefaultListKey)
        {
            return GetItemsFromNodes(GetResponseNodes(data), itemGenerator, listKey);
        }

        public static IEnumerable<T> GetItemsFromNodes<T>(IEnumerable<DacpNode> nodes, Func<byte[], T> itemGenerator, string listKey = DefaultListKey)
        {
            var node = nodes.FirstOrDefault(n => n.Key == listKey);
            if (node == null)
                return Enumerable.Empty<T>();

            return GetResponseNodes(node.Value).Where(n => n.Key == "mlit").Select(n => itemGenerator(n.Value));
        }

        public static IEnumerable<T> GetItemsFromNodes<T>(byte[] data, Func<DacpNodeDictionary, T> itemGenerator, string listKey = DefaultListKey)
        {
            return GetItemsFromNodes(GetResponseNodes(data), d => itemGenerator(DacpNodeDictionary.Parse(d)), listKey);
        }

        public static IEnumerable<T> GetItemsFromNodes<T>(IEnumerable<DacpNode> nodes, Func<DacpNodeDictionary, T> itemGenerator, string listKey = DefaultListKey)
        {
            return GetItemsFromNodes(nodes, d => itemGenerator(DacpNodeDictionary.Parse(d)), listKey);
        }

        #endregion

        #region Grouped Lists

        private const string AlphaGroupChars = "#abcdefghijklmnopqrstuvwxyz";

        public static IDacpList GetAlphaGroupedDacpList<T>(IEnumerable<DacpNode> nodes, Func<byte[], T> itemGenerator, string listKey = DefaultListKey, bool useGroupMinimums = true)
        {
            List<T> items;
            return GetAlphaGroupedDacpList(nodes, itemGenerator, out items, listKey, useGroupMinimums);
        }

        public static IDacpList GetAlphaGroupedDacpList<T>(IEnumerable<DacpNode> nodes, Func<byte[], T> itemGenerator, out List<T> items, string listKey = DefaultListKey, bool useGroupMinimums = true)
        {
            var nodeList = nodes.ToList();
            items = GetItemsFromNodes(nodeList, itemGenerator, listKey).ToList();
            var headers = nodeList.FirstOrDefault(n => n.Key == "mshl");
            IEnumerable<DacpNode> headerNodes = null;
            if (headers != null)
                headerNodes = DacpUtility.GetResponseNodes(headers.Value);

            return GetAlphaGroupedDacpList(items, headerNodes, useGroupMinimums);
        }

        public static IDacpList GetAlphaGroupedDacpList<T>(IEnumerable<DacpNode> nodes, Func<DacpNodeDictionary, T> itemGenerator, string listKey = DefaultListKey, bool useGroupMinimums = true)
        {
            return GetAlphaGroupedDacpList(nodes, d => itemGenerator(DacpNodeDictionary.Parse(d)), listKey, useGroupMinimums);
        }

        public static IDacpList GetAlphaGroupedDacpList<T>(IEnumerable<DacpNode> nodes, Func<DacpNodeDictionary, T> itemGenerator, out List<T> items, string listKey = DefaultListKey, bool useGroupMinimums = true)
        {
            return GetAlphaGroupedDacpList(nodes, d => itemGenerator(DacpNodeDictionary.Parse(d)), out items, listKey, useGroupMinimums);
        }

        public static IDacpList GetAlphaGroupedDacpList<T>(List<T> items, IEnumerable<DacpNode> headers, bool useGroupMinimums = true)
        {
            // Determine whether we need to process the headers
            bool processHeaders = false;

            // Make sure headers were returned
            if (headers != null)
            {
                if (useGroupMinimums)
                {
                    // Only show headers if we have at least 10 items
                    if (items.Count >= 10)
                    {
                        // Also make sure we have more than 1 group
                        headers = headers.ToList();
                        if (((IList)headers).Count >= 2)
                        {
                            processHeaders = true;
                        }
                    }
                }
                else
                {
                    processHeaders = true;
                }
            }

            // If we're not processing headers, just return the items in a list
            if (!processHeaders)
                return items.ToDacpList();

            var result = new DacpList<ItemGroup<T>>(true);

            // Create groups for each letter
            var groupsByChar = new Dictionary<char, ItemGroup<T>>(AlphaGroupChars.Length);
            foreach (char c in AlphaGroupChars)
            {
                var group = new ItemGroup<T>(c.ToString());
                result.Add(group);
                groupsByChar[c] = group;
            }

            // Add the items to their groups
            foreach (var header in headers.Select(n => DacpNodeDictionary.Parse(n.Value)))
            {
                char headerChar = GetKeyChar(header.GetString("mshc"));
                int skip = header.GetInt("mshi");
                int take = header.GetInt("mshn");

                groupsByChar[headerChar].AddRange(items.Skip(skip).Take(take));
            }

            return result;
        }

        private static char GetKeyChar(string input)
        {
            if (string.IsNullOrEmpty(input))
                return '#';

            string formattedString = input.Trim('\0').ToLower();
            if (string.IsNullOrEmpty(formattedString))
                return '#';

            char key = formattedString[0];

            if (key < 'a' || key > 'z')
                key = '#';

            return key;
        }

        #endregion

        #region DACP Data Extension Methods

        public static Int16 GetInt16Value(this byte[] data)
        {
            return BitUtility.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
        }

        public static Int32 GetInt32Value(this byte[] data)
        {
            return BitUtility.NetworkToHostOrder(BitConverter.ToInt32(data, 0));
        }

        public static Int64 GetInt64Value(this byte[] data)
        {
            return BitUtility.NetworkToHostOrder(BitConverter.ToInt64(data, 0));
        }

        public static string GetStringValue(this byte[] data)
        {
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static bool GetBoolValue(byte[] data)
        {
            return !(data[0] == 0);
        }

        public static DateTime GetDateTimeValue(this byte[] data)
        {
            int timestamp = GetInt32Value(data);
            return (new DateTime(1970, 1, 1)).AddSeconds(timestamp);
        }

        #endregion

        #region Other Extension Methods

        public static string EscapeSingleQuotes(string input)
        {
            return input.Replace("'", "\\'");
        }

        public static string QueryEncodeString(string input)
        {
            return Uri.EscapeDataString(DacpUtility.EscapeSingleQuotes(input));
        }

        public static string GetPathAndQueryString(this Uri uri)
        {
            return uri.AbsolutePath + uri.Query;
        }

        internal static DacpList<T> ToDacpList<T>(this IEnumerable<T> collection)
        {
            return new DacpList<T>(false, collection);
        }

        #endregion
    }
}
