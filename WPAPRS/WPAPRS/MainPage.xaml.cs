using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPAPRS.Resources;

using Windows.Devices.Geolocation;

namespace WPAPRS
{
    public partial class MainPage : PhoneApplicationPage
    {
        Geolocator loc;
        APRSAudioStreamSource audio;


        // Constructor
        public MainPage()
        {
            InitializeComponent();

            audio = new APRSAudioStreamSource(48000);

            me.SetSource(audio);
            me.Play();

            loc = new Geolocator();
            loc.DesiredAccuracy = PositionAccuracy.High;
            loc.MovementThreshold = 0;
            loc.ReportInterval = 10000;
            loc.PositionChanged += loc_PositionChanged;
        }

        void loc_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Geoposition pos = args.Position;

            Packet packet = new APRSPacket(AppSettings.Callsign, "AWP001", new String[] { "WIDE1-1", "WIDE2-2" },
                APRSPacket.PositionIcons.Phone, pos.Coordinate, " Acc: " + pos.Coordinate.Accuracy + ", Altacc: " + pos.Coordinate.AltitudeAccuracy + ", Type: " + pos.Coordinate.PositionSource);

            System.Diagnostics.Debug.WriteLine("Send packet " + packet.ToString());

            //audio.EnqueuePacketForTransmission(packet);
        }
        
        private void settingsAppBarIcon_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }
    }
}