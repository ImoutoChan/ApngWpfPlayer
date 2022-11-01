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

namespace ImoutoRebirth.Navigator.ApngWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var sources = new[]
            {
                @"69060e4e568c52eef85883be8e7050f7.png",
                @"e310012a65b7327a2aae463b2e8b01bf.png",
            };

            this.ApngPlayer.Source = ApngPlayer.Source == sources[0] ? sources[1] : sources[0];
        }
    }
}
