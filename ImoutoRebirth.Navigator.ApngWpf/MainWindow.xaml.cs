using System;
using System.Windows;

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
                "69060e4e568c52eef85883be8e7050f7.png",
                "e310012a65b7327a2aae463b2e8b01bf.png",
                "59876e976c155e692a06584d9a7a61d5.png",
                "blend sample with dispose op background.png",
                "issue6.png",
                
                // https://philip.html5.org/tests/apng/tests.html
                "013.png",
                "014.png",
                "015.png",
                "016.png",
                "017.png",
                "018.png",
                "019.png",
                "020.png",
                "021.png",
                "022.png",
                "023.png",
            };

            var next = (Array.IndexOf(sources, ApngPlayer.Source) + 1) % sources.Length;

            ApngPlayer.Source = sources[next];
            Title = sources[next];
        }
    }
}
