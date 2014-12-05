using System;
using System.Collections.Generic;
using System.Text;

using Windows.Devices.Geolocation;

namespace WPAPRS
{
    public class APRSPacket : Packet
    {
        public enum PositionIcons : byte
        {
            PoliceStation = (byte)'!',
            NoSymbol = (byte)'"',
            Digi = (byte)'#',
            Phone = (byte)'$',
            DXCluster = (byte)'%',
            HFGateway = (byte)'&',
            Plane = (byte)'\'',
            MobSatStn = (byte)'(',
            Snomobile = (byte)')',
            RedCross = (byte)'*',
            BoyScout = (byte)'+',
            Home = (byte)',',
            X = (byte)'.',
            RedDot = (byte)'/'
        }

        private static byte[] BuildPayload(Geocoordinate position, string comments, PositionIcons positionIcon)
        {
            StringBuilder msg = new StringBuilder();

            // Indicate that we are only a TNC, not APRS messaging.
            msg.Append("/");

            // Append the time
            msg.Append(DateTime.Now.ToUniversalTime().ToString("HHmmss"));
            msg.Append("h");    // Indicates that we're using zulu time

            // Append the position
            // We do it in the format DDMM.mm (degrees, minutes, hundredths of a minute)
            double lat = position.Latitude;
            char latSign = lat > 0 ? 'N' : 'S';
            lat = lat > 0 ? lat : -lat;
            int latDeg = (int)lat;
            float latMin = (float)((lat - latDeg) * 60);
            msg.Append(latDeg);
            if (latMin < 10)
            {
                msg.Append('0');
            }
            msg.Append(latMin.ToString("F2"));
            msg.Append(latSign);

            msg.Append('/');

            // Same for longitude
            double lon = position.Longitude;
            char lonSign = lon > 0 ? 'E' : 'W';
            lon = lon > 0 ? lon : -lon;
            int lonDeg = (int)lon;
            float lonMin = (float)((lon - lonDeg) * 60);
            if (lonDeg < 100)
            {
                msg.Append('0');
            }
            msg.Append(lonDeg);
            if(lonMin < 10)
            {
                msg.Append('0');
            }
            msg.Append(lonMin.ToString("F2"));
            msg.Append(lonSign);

            // APRS Symbol
            msg.Append((char)positionIcon);

            // Course
            msg.Append((int)position.Heading);
            msg.Append('/');

            // Speed in knots.  There are 1852m per nm
            msg.Append((int)(position.Speed * (3600.0 / 1852)));

            // Altitude in feet
            msg.Append("/A=");
            msg.Append(((int)(position.Altitude * 3.2808399)).ToString("D6"));

            msg.Append(comments);

            return Encoding.UTF8.GetBytes(msg.ToString());
        }

        public APRSPacket(string source, string destination, string[] path, PositionIcons positionIcon, Geocoordinate position, string comments)
            : base(destination, source, path, Packet.AX25_CONTROL_APRS, Packet.AX25_PROTOCOL_NO_LAYER_3, BuildPayload(position, comments, positionIcon))
        {

        }
    }
}