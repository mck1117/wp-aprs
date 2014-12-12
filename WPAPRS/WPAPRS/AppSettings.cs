using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPAPRS
{
    static class AppSettings
    {
        private static IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        private const string CallsignSettingsKey = "callsign";
        public static string Callsign
        {
            get
            {
                return (string)settings[CallsignSettingsKey];
            }
            set
            {
                settings[CallsignSettingsKey] = value.ToUpperInvariant();
            }
        }

        public static void Save()
        {
            settings.Save();
        }
    }
}
