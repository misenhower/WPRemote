﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace Komodex.Common.Phone.Controls
{
    public class WizardItem : ContentControl
    {
        public WizardItem()
        {
            DefaultStyleKey = typeof(WizardItem);

            RenderTransform = new TranslateTransform();
        }
    }
}
