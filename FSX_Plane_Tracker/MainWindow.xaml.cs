using Microsoft.FlightSimulator.SimConnect;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Globalization;
using Microsoft.Maps.MapControl;
using System.Windows.Controls;
using System.IO;

namespace FSX_Plane_Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static SimConnect sc = null;
        private const int WM_USER_SIMCONNECT = 0;
        static string lon = null;
        static string lat = null;
        static string alt = null;
        enum SIMCONNECT_DATA_DEFINITION_ID
        {
            planeLocation
        }
        struct planeLocation
        {
            public float longitude;
            public float latitude;
            public float altitude;
        }
        enum DATA_REQUEST_ID { REQUEST_1 }
        //receiving messages
        System.Threading.EventWaitHandle scReady = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset);
        public delegate void MyDelegate();
        private void scMessageThread()
        {
            while (true)
            {
                scReady.WaitOne();
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new MyDelegate(scMessageProcess));
            }
        }

        private void scMessageProcess()
        {
            sc.ReceiveMessage();
        }

        public MainWindow()
        {
            InitializeComponent();

            var thread = new Thread(new ThreadStart(() => ConnectToSimConnect()));
            thread.Start();
            init();
            location();
        }

        void location()
        {
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Plane Altitude", "meters", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.RegisterDataDefineStruct<planeLocation>(SIMCONNECT_DATA_DEFINITION_ID.planeLocation);
            sc.RequestDataOnSimObject(DATA_REQUEST_ID.REQUEST_1, SIMCONNECT_DATA_DEFINITION_ID.planeLocation, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, 0, 0, 0, 0);
            sc.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(simconnect_OnRecvSimobjectData);
        }

        //private static void relocate(string lon, string lat, string alt)
        //{
        //    // Parse the information of the button's Tag property
        //    string[] tagInfo = ((Button)sender).Tag.ToString().Split(' ');
        //    Location  center = (Location)locConverter.ConvertFrom(tagInfo[0]);
        //    double zoom = System.Convert.ToDouble(tagInfo[1]);
            
        //    // Set the map view
        //    flightmap.SetView(center, zoom);

        //    Location mapCenter = flightmap.Center;
        //    mapLongitude = Convert.ToDouble(lon);
        //    double mapLatitude = Convert.ToDouble(lat);
        //    Location center = (mapLongitude, mapLatitude);
        //}

        private void ConnectToSimConnect()
        {
            while (sc == null)
            {
                try
                {
                    sc = new SimConnect("testtest", IntPtr.Zero, WM_USER_SIMCONNECT, null, 0);
                    button.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        new Action(
                            delegate()
                            {
                                button.Content = "Connected!";
                            }
                    ));
                    
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Error!", "Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Thread.Sleep(2000);
            }
        }

        private void button_Click(object sender, RoutedEventArgs eargs)
        {
            ConnectToSimConnect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closeConnection();
        }
   
        static void simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            lon = data.dwData[0].ToString();
            lat = data.dwData[1].ToString();
            alt = data.dwData[2].ToString();
            File.AppendAllText("flightlog.txt", "lon: " + lon + ", lat: " + lat + ", alt: " + alt);
            //relocate(lon, lat, alt);
        }

        //KOD Z WYKŁADU
        static void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            MessageBox.Show("Podłączyłem się do FSX / ESP. Naciśnij dowolny klawisz, aby zamknąć połączenie.");
        }

        static void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            MessageBox.Show("FSX / ESP zakończył połączenie.");
            closeConnection();
        }

        static void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            MessageBox.Show("Przypełzł wyjątek z FSX / ESP: " + data.dwException);
        }

        static private void closeConnection()
        {
            if (sc != null)
            {
                sc.Dispose();
                sc = null;
                MessageBox.Show("Połączenie z FSX / ESP zamknięte.");
            }          
        }

        static void init()
        {
            sc.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simconnect_OnRecvOpen);
            sc.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);
            sc.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvException);
        }






    }
}
