using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mayhem.Launcher.UIElements.ProgressBar
{
    /// <summary>
    /// Interaction logic for CustomProgressBar.xaml
    /// </summary>
    public partial class CustomProgressBar : UserControl
    {
        public CustomProgressBar()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(CustomProgressBar),
            new PropertyMetadata(0, new PropertyChangedCallback(
            (obj, chng) =>
            {
                CustomProgressBar current = (CustomProgressBar)obj;
                current.pgc.Width = (int)chng.NewValue;
                switch (current.ToolTipType)
                {
                    case DisplayToolTip.None:
                        current.pgc.ToolTip = null;
                        break;
                    case DisplayToolTip.Valueonly:
                        current.pgc.ToolTip = chng.NewValue;
                        break;
                    case DisplayToolTip.ValuewithPercentage:
                        current.pgc.ToolTip = string.Format("{0} %", chng.NewValue);
                        break;
                    case DisplayToolTip.ValuewithPercentageComplete:
                        current.pgc.ToolTip = string.Format("{0} % Complete", chng.NewValue);
                        break;
                    default:
                        break;
                }

            }
            )));
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public DisplayToolTip ToolTipType
        {
            get;
            set;
        }
        public enum DisplayToolTip : byte
        {
            None,
            Valueonly,
            ValuewithPercentage,
            ValuewithPercentageComplete
        }
    }
}
