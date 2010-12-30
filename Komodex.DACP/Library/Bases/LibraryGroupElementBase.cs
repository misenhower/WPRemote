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
using System.ComponentModel;
using System.Collections.Generic;

namespace Komodex.DACP.Library
{
    public abstract class LibraryGroupElementBase : LibraryElementBase
    {
        
        protected override bool ProcessByteKVP(KeyValuePair<string, byte[]> kvp)
        {
            if (base.ProcessByteKVP(kvp))
                return true;

            switch (kvp.Key)
            {
                default:
                    return false;
            }
        }

    }
}
