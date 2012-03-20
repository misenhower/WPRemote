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
using Microsoft.Phone.Controls;
using System.Text.RegularExpressions;

namespace Komodex.Common.Phone.Controls
{
    public class NumericTextBox : PhoneTextBox
    {
        public NumericTextBox()
        {
            InputScope = new InputScope();
            InputScope.Names.Add(new InputScopeName() { NameValue = InputScopeNameValue.Number });
            TextChanged += new TextChangedEventHandler(NumericTextBox_TextChanged);
        }

        void NumericTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int selectionStart = SelectionStart;
            int textLen = Text.Length;

            Text = Regex.Replace(Text, "\\D", string.Empty);

            int newTextLen = Text.Length;
            if (newTextLen < textLen && selectionStart > 0)
                selectionStart--;

            SelectionStart = selectionStart;
        }

        public int? IntValue
        {
            get
            {
                int result;
                if (int.TryParse(Text, out result))
                    return result;
                return null;
            }
            set { Text = value.ToString(); }
        }
    }
}
