using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Komodex.WP7DACPRemote.Controls
{
    public partial class StarRating : UserControl
    {
        public StarRating()
        {
            InitializeComponent();

            star1.Tag = 1;
            star2.Tag = 2;
            star3.Tag = 3;
            star4.Tag = 4;
            star5.Tag = 5;

            SetStars(Rating);
        }

        #region Star Manipulation

        private double zeroPointPosition = double.NaN;
        private double starWidth = 0;
        private int currentStars = 0;

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            base.OnManipulationStarted(e);

            starWidth = star1.ActualWidth;
            double starsWidth = starContainer.ActualWidth;
            double layoutWidth = LayoutRoot.ActualWidth;
            zeroPointPosition = (layoutWidth - starsWidth) / 2;

            SetStars(e.ManipulationOrigin.X);
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);

            SetStars(e.ManipulationOrigin.X);
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            base.OnManipulationCompleted(e);

            zeroPointPosition = double.NaN;
            Rating = currentStars;
        }

        private void SetStars(double x, bool setRatingProperty = false)
        {
            if (double.IsNaN(zeroPointPosition))
                return;

            double relativePos = x - zeroPointPosition;
            int newStars = (int)Math.Ceiling(relativePos / starWidth);
            SetStars(newStars, setRatingProperty);
        }

        private void SetStars(int count, bool setRatingProperty = false)
        {
            if (count < 0)
                count = 0;
            else if (count > 5)
                count = 5;

            double off, on;
            off = StarOffOpacity;
            on = StarOnOpacity;

            star1.Opacity = (count >= 1) ? on : off;
            star2.Opacity = (count >= 2) ? on : off;
            star3.Opacity = (count >= 3) ? on : off;
            star4.Opacity = (count >= 4) ? on : off;
            star5.Opacity = (count >= 5) ? on : off;


            if (setRatingProperty && currentStars != count)
                Rating = count;

            currentStars = count;
        }

        #endregion

        #region Dependency Properties

        #region Rating

        public static readonly DependencyProperty RatingProperty = DependencyProperty.Register(
            "Rating", typeof(int), typeof(StarRating), new PropertyMetadata(3, OnRatingChanged));

        private static void OnRatingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            StarRating sr = (StarRating)obj;
            int newValue = (int)e.NewValue;

            sr.SetStars(newValue);
        }

        /// <summary>
        /// The star rating value. 0 &lt;= value &lt;= 5
        /// </summary>
        public int Rating
        {
            get { return (int)GetValue(RatingProperty); }
            set { SetValue(RatingProperty, value); }
        }

        #endregion

        #region ContinuousUpdate

        public static readonly DependencyProperty ContinuousUpdateProperty = DependencyProperty.Register(
            "ContinuousUpdate", typeof(bool), typeof(StarRating), new PropertyMetadata(false));

        /// <summary>
        /// If this is true, the Rating property will be updated continuously as the user modifies
        /// the value by dragging across the stars.  If this is false, the property will only be
        /// updated once manipulation is complete.
        /// </summary>
        public bool ContinuousUpdate
        {
            get { return (bool)GetValue(ContinuousUpdateProperty); }
            set { SetValue(ContinuousUpdateProperty, value); }
        }

        #endregion

        #region StarOnOpacity / StarOffOpacity

        public static readonly DependencyProperty StarOnOpacityProperty = DependencyProperty.Register(
            "StarOnOpacity", typeof(double), typeof(StarRating), new PropertyMetadata(1.0, OnStarOpacityChanged));

        public static readonly DependencyProperty StarOffOpacityProperty = DependencyProperty.Register(
            "StarOffOpacity", typeof(double), typeof(StarRating), new PropertyMetadata(0.5, OnStarOpacityChanged));

        private static void OnStarOpacityChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            StarRating sr = (StarRating)obj;
            double newValue = (double)e.NewValue;

            sr.SetStars(sr.currentStars);
        }

        /// <summary>
        /// The opacity for stars that are "on"
        /// </summary>
        public double StarOnOpacity
        {
            get { return (double)GetValue(StarOnOpacityProperty); }
            set { SetValue(StarOnOpacityProperty, value); }
        }

        /// <summary>
        /// The opacity for stars that are "off"
        /// </summary>
        public double StarOffOpacity
        {
            get { return (double)GetValue(StarOffOpacityProperty); }
            set { SetValue(StarOffOpacityProperty, value); }
        }

        #endregion

        #endregion
    }
}
