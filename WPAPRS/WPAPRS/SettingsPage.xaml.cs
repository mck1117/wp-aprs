using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace WPAPRS
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            txtCallsign.Text = AppSettings.Callsign;
            txtCallsign_TextChanged(txtCallsign, null);
        }


        private static bool IsValidCallsign(string call)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(call, "[A-Z]{1,2}[0-9][A-Z]{1,3}");
        }

        private void txtCallsign_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Force the contents to be uppercase
            int cursorLoc = txtCallsign.SelectionStart;
            txtCallsign.Text = txtCallsign.Text.ToUpperInvariant();
            txtCallsign.SelectionStart = cursorLoc;

            // Update the validity indicator
            bool isValid = IsValidCallsign(txtCallsign.Text);
            txtCallsignValid.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;

            btnSave.IsEnabled = isValid;
        }

        private void cancelAppBarIcon_Click(object sender, EventArgs e)
        {
            // Return to the previous page without saving.
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                throw new InvalidOperationException("Something has gone way-bad wrong.");
            }
        }

        private void saveAppBarIcon_Click(object sender, EventArgs e)
        {
            /// TODO: save the settings here.
            AppSettings.Callsign = txtCallsign.Text;
            AppSettings.Save();

            // Return to previous after saving settings.
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                throw new InvalidOperationException("Something has gone way-bad wrong.");
            }
        }
    }
}