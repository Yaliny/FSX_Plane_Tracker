using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Threading;
using System.Windows;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Collections.Generic;
using System.Configuration;

namespace FSX_Plane_Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Members
        private static SimConnect sc = null;
        private static readonly Plane plane = new Plane();
        static Map flightmap;
        static Location center;
        static Location currentLocation;
        static double zoomLevel;
        static bool pause;
        static LocationCollection route = new LocationCollection();
        EventWaitHandle scReady = new EventWaitHandle(false, EventResetMode.AutoReset);
        #endregion

        #region Enums
        enum SIMCONNECT_DATA_DEFINITION_ID
        {
            planeLocation
        }

        enum DATA_REQUEST_ID { REQUEST_1 }
        enum EVENT_ID
        {
            EVENT_PAUSE,
            EVENT_BRAKES
        };
        #endregion

        #region Simconnect Message
        private void scMessageThread()
        {
            while (true)
            {
                scReady.WaitOne();
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(
                        delegate()
                        {
                            scMessageProcess();
                        }
                    ));
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
        #endregion

        #region C-tor
        public MainWindow()
        {
            InitializeComponent();
            var key = ConfigurationManager.AppSettings["BingMapsCredentialsProvider"];
            if (String.IsNullOrEmpty(key))
            {
                return;
            }

            viewmap.CredentialsProvider = new ApplicationIdCredentialsProvider(key);
            //viewmap.CredentialsProvider = new ApplicationIdCredentialsProvider(ConfigurationManager.AppSettings["BingMapsCredentialsProvider"].ToString());
            //mainWindow.Height = 600;

            var thread = new Thread(new ThreadStart(ConnectToSimConnect));
            thread.Start();
            center = viewmap.Center;
            flightmap = viewmap;
            currentLocation = new Location();
            pause = false;
        }
        #endregion

        #region Setup 
        void setupPulling()
        {
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Plane Altitude", "kilometers", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "AIRSPEED TRUE", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "ATTITUDE INDICATOR PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "ATTITUDE INDICATOR BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Delta Heading Rate", "degrees per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Turn Coordinator Ball", "number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Heading Indicator", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "Vertical Speed", "ft/min", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.AddToDataDefinition(SIMCONNECT_DATA_DEFINITION_ID.planeLocation, "GPS GROUND SPEED", "m/s", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            sc.MapClientEventToSimEvent(EVENT_ID.EVENT_PAUSE, "pause_toggle");
            sc.MapClientEventToSimEvent(EVENT_ID.EVENT_BRAKES, "brakes");

            sc.RegisterDataDefineStruct<PlaneStruct>(SIMCONNECT_DATA_DEFINITION_ID.planeLocation);
            sc.RequestDataOnSimObject(DATA_REQUEST_ID.REQUEST_1, SIMCONNECT_DATA_DEFINITION_ID.planeLocation, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, 0, 0, 0, 0);
        }

        static void setupEvents()
        {
            sc.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simconnect_OnRecvOpen);
            sc.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);
            sc.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvException);
            sc.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(simconnect_OnRecvSimobjectData);
            sc.OnRecvEvent += new SimConnect.RecvEventEventHandler(Sc_OnRecvEvent);
        }

        private static void Sc_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            switch ((EVENT_ID)data.uEventID)
            {
                case EVENT_ID.EVENT_PAUSE:
                    pause = !pause;
                    break;
                case EVENT_ID.EVENT_BRAKES:

                    break;
            }
        }
        #endregion

        #region Connect to Simconnect
        private void ConnectToSimConnect()
        {
            while (sc == null)
            {
                try
                {
                    sc = new SimConnect("VE_SC_WPF", (IntPtr)0, 0, scReady, 0);
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                    delegate ()
                    {
                        mainWindow.Title = mainWindow.Title.Replace("Disconnected", "Connected");
                    }));
                    }
                catch (Exception)
                {

                }

                Thread.Sleep(2000);
            }

            setupEvents();
            setupPulling();

            var pullingThread = new Thread(new ThreadStart(scMessageThread));
            pullingThread.IsBackground = false;
            pullingThread.Start();

            var updatingViewThread = new Thread(new ThreadStart(updateViewThread));
            pullingThread.IsBackground = false;
            updatingViewThread.Start();
        }
        #endregion

        #region Window Closing
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closeConnection();
        }
        #endregion

        #region Simconnect On Recv functions
        static void simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            var planeStructure = (PlaneStruct)data.dwData[0];
            plane.Update(planeStructure);

            center.Latitude = plane.Latitude;
            center.Longitude = plane.Longitude;
            if (currentLocation.Latitude != center.Latitude && currentLocation.Longitude != center.Longitude)
            {
                zoomLevel = 19 - Math.Log(plane.Altitude * 5.508, 2);
                flightmap.SetView(center, zoomLevel);
                currentLocation = flightmap.Center;
                route.Add(flightmap.Center);
                MapPolyline polyline = new MapPolyline();
                polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                polyline.StrokeThickness = 5;
                polyline.Opacity = 0.7;
                polyline.Locations = route;
                flightmap.Children.Add(polyline);
            }
        }
        
        static void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
        }

        static void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            closeConnection();
        }

        static void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
        }
        #endregion
        
        #region Close connection
        static private void closeConnection()
        {
            if (sc != null)
            {
                sc.Dispose();
                sc = null;
            }          
        }
        #endregion

        #region Update
        void updateViewThread()
        {
            while(true)
            {
                try
                {
                    updateView();
                }
                catch(Exception)
                {
                }

                Thread.Sleep(2000);
            }
        }

        void updateView()
        {
            if (pause)
            {
                return;
            }
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                delegate ()
                {
                    //var source = DataSource.Create(plane.x, plane.y);
                    airSpeed.Value = plane.Airspeed;
                    groundSpeed.Value = plane.GroundSpeed;
                    pitch.Value = plane.Pitch;
                    tilt.Value = plane.Bank;
                    //airspeedMonitor.
                    LineGraph line;
                    List<Point> p = new List<Point>();
                    for(int i = 0; i < 100 && i < plane.x.Count; i++)
                    {
                        p.Add(new Point(plane.x[i], plane.y[i]));
                    }

                    EnumerableDataSource<Point> ds = new EnumerableDataSource<Point>(p);

                    //ds.SetXMapping(x => x);
                    //ds.SetYMapping(y => plane.y);
                    ds.SetXYMapping(x => x);
                    line = new LineGraph(ds);
                    line.LinePen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2);
                    //plotter.Children.RemoveAll(x =>  x is LineGraph );
                    foreach (var item in plotter.Children)
                    {
                        if (item is LineGraph)
                        {
                            plotter.Children.Remove(item);
                            break;
                        }
                    }
                    plotter.Children.Add(line);
                    plotter.FitToView();
                    //var chartLine = plotter.AddLineChart();
                    //line = new LineGraph()
                    //plotter.addLineChart(DataSource.Create)
                    debug.Text = String.Format("TRUE AS = {0}\nPITCH DEG = {1}\nBANK DEG = {2}\nHEADING DELTA = {3}\nTURN = {4}\nHEADING INDICATOR = {5}\nVERT SPEED = {6}", plane.Airspeed, plane.Pitch, plane.Bank, plane.Delta, plane.Turn, plane.Heading, plane.VerticalSpeed);
                }
            ));
        }
        #endregion
    }
}
