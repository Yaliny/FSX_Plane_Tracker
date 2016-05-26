using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace FSX_Plane_Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SimConnect sc = null;

        public MainWindow()
        {
            InitializeComponent();

            var thread = new Thread(new ThreadStart(() => ConnectToSimConnect()));
            thread.Start();
        }

        private void ConnectToSimConnect()
        {
            while (sc == null)
            {
                try
                {
                    sc = new SimConnect("testtest", IntPtr.Zero, 0, null, 0);

                    button.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        new Action(
                            delegate ()
                            {
                                button.Content = "Connected!";
                            }
                    ));
                }
                catch (Exception)
                {
                    //MessageBox.Show(this, "Error!", "Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Thread.Sleep(2000);
            }
        }

        private void button_Click(object sender, RoutedEventArgs eargs)
        {
            //ConnectToSimConnect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sc != null)
            {
                sc.Dispose();
            }
        }
    }
}
