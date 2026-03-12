using FitMyBike.Model_Class.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FitMyBike.View_Class
{
    /// <summary>
    /// Logika interakcji dla klasy ResultLabel.xaml
    /// </summary>
    public partial class ResultLabel : UserControl
    {
        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(nameof(Angle), typeof(double), typeof(ResultLabel), new PropertyMetadata(-1.0, ValueChanged));

        double _angle = 0;

        public PoseAngles PoseAnglesReference { get; set; }

        public string Text
        {
            get => nameLabel.Content.ToString();
            set
            {
                nameLabel.Content = value;
            }
        }

        public double Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                valueLabel.Content = _angle.ToString("#.00") + "°";
            }
        }

        public ResultLabel()
        {
            InitializeComponent();
        }

        private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var currentAngleValue = (d as ResultLabel).Angle;
            var newAngle = (double)e.NewValue;
            if (currentAngleValue != newAngle)
            {
                (d as ResultLabel).Angle = newAngle;
            }
        }
    }
}
