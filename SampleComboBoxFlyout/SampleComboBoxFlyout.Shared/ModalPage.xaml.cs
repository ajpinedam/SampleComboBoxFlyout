using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SampleComboBoxFlyout
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ModalPage : Page
    {
        public ModalPage()
        {
            this.InitializeComponent();
            Loaded += ModalPage_Loaded;
        }

        private void ModalPage_Loaded(object sender, RoutedEventArgs e)
        {
            var t = this.ShowLocalVisualTree(50);
        }

        public string[] Tests { get; } = new string[] {
            "1",
            "2",
            "3",
        };

    }
}
