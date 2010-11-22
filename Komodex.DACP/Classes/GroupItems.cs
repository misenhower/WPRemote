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

namespace Komodex.DACP
{
    public class GroupItems<T> : List<T>
    {
        public GroupItems(string key)
        {
            Key = key;
        }

        public string Key { get; protected set; }
        public bool HasItems
        {
            get { return Count > 0; }
        }
    }
}
