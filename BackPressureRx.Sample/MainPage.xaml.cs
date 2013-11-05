using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
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
using BackPressureRx.Linq;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BackPressureRx.Sample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Subject<int> controller = new Subject<int>();

        public MainPage()
        {
            this.InitializeComponent();

            IControlledObservable<int> c = System.Reactive.Linq.Observable.Range(0, 10000).Controlled();

            c.Windowed(10).Subscribe(x =>
            {
                c.Request(5);
                Debug.WriteLine(x);
            });

            c.Request(5);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            controller.OnNext(int.Parse(this.txtBox.Text));
        }
    }
}
