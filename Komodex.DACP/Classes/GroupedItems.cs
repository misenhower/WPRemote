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
        private static readonly string Groups = "#abcdefghijklmnopqrstuvwxyz";

        private Dictionary<char, GroupItems<T>> itemLookup = new Dictionary<char, GroupItems<T>>();

        protected GroupedItems()
        {
            foreach (char c in Groups)
            {
                GroupItems<T> itemsInGroup = new GroupItems<T>(c.ToString());
                this.Add(itemsInGroup);
                itemLookup.Add(c, itemsInGroup);
            }
        }

        public static GroupedItems<T> HandleResponseNodes(List<KeyValuePair<string, byte[]>> responseNodes, Func<byte[],T> itemGenerator)
        {
            List<T> items = null;
            List<KeyValuePair<string, byte[]>> headers = null;

            foreach (var kvp in responseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl": // Items list
                        items = new List<T>();
                        var itemNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var itemData in itemNodes)
                            items.Add(itemGenerator(itemData.Value));
                        break;
                    case "mshl": // Headers
                        headers = Utility.GetResponseNodes(kvp.Value);
                        break;
                    default:
                        break;
                }
            }

            return Parse(items, headers);
        }

        public static GroupedItems<T> Parse(List<T> items, List<KeyValuePair<string, byte[]>> headers)
        {
            if (items == null || headers == null)
                return null;

            int skip = 0;
            int take = 0;
            char headerChar = 'a';

            var result = new GroupedItems<T>();

            foreach (var header in headers)
            {
                var headerNodes = Utility.GetResponseNodes(header.Value);
                foreach (var node in headerNodes)
                {
                    switch (node.Key)
                    {
                        case "mshc":
                            headerChar = GetKeyChar(node.Value.GetStringValue());
                            break;
                        case "mshi":
                            skip = node.Value.GetInt32Value();
                            break;
                        case "mshn":
                            take = node.Value.GetInt32Value();
                            break;
                        default:
                            break;
                    }
                }

                result.itemLookup[headerChar].AddRange(items.Skip(skip).Take(take));
            }

            return result;
        }

        protected static char GetKeyChar(string input)
        {
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
