using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSX_Plane_Tracker
{
    public class Plane
    {
        public double Longitude { get; private set; }
        public double Latitude { get; private set; }
        public double Altitude { get; private set; }
        public double Airspeed { get; private set; }
        public double Pitch { get; private set; }
        public double Bank { get; private set; }
        public double Delta { get; private set; }
        public double Turn { get; private set; }
        public double Heading { get; private set; }
        public double VerticalSpeed { get; private set; }

        public void Update(PlaneStruct planeStructure)
        {
            Longitude = planeStructure.longitude;
            Latitude = planeStructure.latitude;
            Altitude = planeStructure.altitude;
            Airspeed = planeStructure.airspeed;
            Pitch = planeStructure.pitch;
            Bank = planeStructure.bank;
            Delta = planeStructure.delta;
            Turn = planeStructure.turn;
            Heading = planeStructure.heading;
            VerticalSpeed = planeStructure.verticalSpeed;
        }
    }

    public struct PlaneStruct
    {
        public double longitude;
        public double latitude;
        public double altitude;
        public double airspeed;
        public double pitch;
        public double bank;
        public double delta;
        public double turn;
        public double heading;
        public double verticalSpeed;
    }
}
