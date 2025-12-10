using Kinect_Middleware.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Kinect_Middleware.Views.Pages {
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl {
        UserConfigurationManager manager;

        public String Username {
            get { return UsernameField.Text; }
        }
        public String Password {
            get { return PasswordField.Password; }
        }

        public LoginView() {
            InitializeComponent();

            this.manager = App.Host.Services.GetService<UserConfigurationManager>();

            UsernameField.Text = manager.GetLastUsedUsername();
            LogoutButton.IsEnabled = manager.Authenticated;
        }

        private void Login_Click(object sender, RoutedEventArgs e) {
            manager.LogIn(Username, Password);

            if (manager.Authenticated) {
                LoginButton.IsEnabled = false;
                LogoutButton.IsEnabled = true;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e) {
            manager.LogOut();
            LoginButton.IsEnabled = true;
            LogoutButton.IsEnabled = false;
        }

        private void GoToWebsite_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start(URLs.Website);
        }
    }
}
