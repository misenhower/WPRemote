using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Komodex.Remote.Converters
{
    public class CurrentDACPItemVisibilityConverter : DependencyObject
    {
        private void Update()
        {
            if (CurrentSongID != 0 && DACPItem != null && DACPItem.ID == CurrentSongID)
            {
                IndicatorVisibility = Visibility.Visible;
                TextTrimming = TextTrimming.WordEllipsis;
            }
            else
            {
                IndicatorVisibility = Visibility.Collapsed;
                TextTrimming = TextTrimming.None;
            }
        }

        #region CurrentSongID

        public static readonly DependencyProperty CurrentSongIDProperty =
            DependencyProperty.Register("CurrentSongID", typeof(int), typeof(CurrentDACPItemVisibilityConverter), new PropertyMetadata(0, CurrentSongIDPropertyChanged));

        public int CurrentSongID
        {
            get { return (int)GetValue(CurrentSongIDProperty); }
            set { SetValue(CurrentSongIDProperty, value); }
        }

        private static void CurrentSongIDPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CurrentDACPItemVisibilityConverter converter = (CurrentDACPItemVisibilityConverter)d;
            converter.Update();
        }

        #endregion

        #region DACPItem

        public static readonly DependencyProperty DACPItemProperty =
            DependencyProperty.Register("DACPItem", typeof(DACPItem), typeof(CurrentDACPItemVisibilityConverter), new PropertyMetadata(null, DACPItemPropertyChanged));

        public DACPItem DACPItem
        {
            get { return (DACPItem)GetValue(DACPItemProperty); }
            set { SetValue(DACPItemProperty, value); }
        }

        private static void DACPItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CurrentDACPItemVisibilityConverter converter = (CurrentDACPItemVisibilityConverter)d;
            converter.Update();
        }

        #endregion

        #region IndicatorVisibility

        public static readonly DependencyProperty IndicatorVisibilityProperty =
            DependencyProperty.Register("IndicatorVisibility", typeof(Visibility), typeof(CurrentDACPItemVisibilityConverter), new PropertyMetadata(Visibility.Collapsed));

        public Visibility IndicatorVisibility
        {
            get { return (Visibility)GetValue(IndicatorVisibilityProperty); }
            set { SetValue(IndicatorVisibilityProperty, value); }
        }

        #endregion

        #region TextTrimming

        public static readonly DependencyProperty TextTrimmingProperty =
            DependencyProperty.Register("TextTrimming", typeof(TextTrimming), typeof(CurrentDACPItemVisibilityConverter), new PropertyMetadata(TextTrimming.None));

        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        #endregion
    }
}
