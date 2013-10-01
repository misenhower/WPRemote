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
using System.Collections.Generic;
using System.Linq;
using Komodex.DACP.Library;

namespace Komodex.DACP
{
    public class GroupedItems<T> : List<GroupItems<T>>
    {
        private const string AlphaGroupChars = "#abcdefghijklmnopqrstuvwxyz";

        public static GroupedItems<T> GetAlphaGroupedItems(IEnumerable<DACPNode> nodes, Func<byte[], T> itemGenerator)
        {
            List<T> items;
            return GetAlphaGroupedItems(nodes, itemGenerator, out items);
        }

        public static GroupedItems<T> GetAlphaGroupedItems(IEnumerable<DACPNode> nodes, Func<byte[], T> itemGenerator, out List<T> items)
        {
            var nodeList = nodes.ToList();
            items = DACPUtility.GetListFromNodes(nodeList, itemGenerator);
            var headers = nodeList.FirstOrDefault(n => n.Key == "mshl");
            IEnumerable<DACPNode> headerNodes = null;
            if (headers != null)
                headerNodes = DACPUtility.GetResponseNodes(headers.Value);

            return GetAlphaGroupedItems(items, headerNodes);
        }

        public static GroupedItems<T> GetAlphaGroupedItems(List<T> items, IEnumerable<DACPNode> headers)
        {
            GroupedItems<T> result = new GroupedItems<T>();

            // If we don't have any header nodes, just return one group that has all the items
            if (headers == null)
            {
                result.Add(new GroupItems<T>(string.Empty));
                result[0].AddRange(items);
                return result;
            }

            // Create groups for each letter
            var groupsByChar = new Dictionary<char, GroupItems<T>>(AlphaGroupChars.Length);
            foreach (char c in AlphaGroupChars)
            {
                GroupItems<T> group = new GroupItems<T>(c.ToString());
                result.Add(group);
                groupsByChar[c] = group;
            }

            // Add the items to their groups
            foreach (var header in headers.Select(n => DACPNodeDictionary.Parse(n.Value)))
            {
                char headerChar = GetKeyChar(header.GetString("mshc"));
                int skip = header.GetInt("mshi");
                int take = header.GetInt("mshn");

                groupsByChar[headerChar].AddRange(items.Skip(skip).Take(take));
            }

            return result;
        }

        protected static char GetKeyChar(string input)
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

    }
}
