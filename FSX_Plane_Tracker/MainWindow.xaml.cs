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
        static double lon;
        static double lat;
        static double alt;
        enum SIMCONNECT_DATA_DEFINITION_ID
        {
            planeLocation
        }

        struct planeLocation
        {
            public double longitude;
            public double latitude;
            public double altitude;
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
            bool ok = false;
            while (!ok)
            {
                try
                {
                    sc.ReceiveMessage();
                    ok = true;
                }
                catch (Exception)
                {
                    ok = false;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            var thread = new Thread(new ThreadStart(() => ConnectToSimConnect()));
            thread.Start();
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

        private void ConnectToSimConnect()
        {
            while (sc == null)
            {
                try
                {
                    sc = new Microsoft.FlightSimulator.SimConnect.SimConnect("VE_SC_WPF", (IntPtr)0, 0, scReady, 0);
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Error!", "Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                }       
   
            }
            init();
            location();
            var bgThread = new System.Threading.Thread(new System.Threading.ThreadStart(scMessageThread));
            bgThread.IsBackground = true;
            bgThread.Start();
            Thread.Sleep(2000);
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
            planeLocation loc = (planeLocation)data.dwData[0];
            lon = loc.longitude;
            lat = loc.latitude;
            alt = loc.altitude;
            //string test = data.dwData[0].ToString();
            File.AppendAllText("flightlog.txt", "lon: " + lon + ", lat: " + lat + ", alt: " + alt + "\r\n");

        }

        //KOD Z WYKŁADU
        static void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            File.AppendAllText("flightlog.txt", "Connected!\n\r");
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
