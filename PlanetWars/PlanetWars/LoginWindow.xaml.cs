using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PlanetWars.ServiceReference;

namespace PlanetWars
{
    public partial class LoginWindow: UserControl
    {
        public event EventHandler NoLogin = delegate { };
        public event EventHandler SuccessfulLogin = delegate { };

        public LoginWindow()
        {
            InitializeComponent();
            App.Service.LoginCompleted += Service_LoginCompleted;
            App.Service.RegisterCompleted += Service_RegisterCompleted;
            App.Service.GetLoginHintCompleted += Service_GetLoginHintCompleted;
            var userName = Cookie.GetCookie("username");
            if (String.IsNullOrEmpty(userName)) App.Service.GetLoginHintAsync();
            else NameBox.Text = userName;
            var pass = Cookie.GetCookie("password");
            if (!String.IsNullOrEmpty(pass)) PasswordBox.Password = pass;
        }

        void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            App.Service.LoginAsync(NameBox.Text, PasswordBox.Password);
        }

        void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            App.Service.LoginCompleted -= Service_LoginCompleted;
            App.Service.RegisterCompleted -= Service_RegisterCompleted;
            NoLogin(this, null);
        }

        void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            App.Service.RegisterAsync(NameBox.Text, PasswordBox.Password);
        }

        void Service_GetLoginHintCompleted(object sender, GetLoginHintCompletedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Result) && String.IsNullOrEmpty(NameBox.Text)) NameBox.Text = e.Result;
        }

        void Service_LoginCompleted(object sender, LoginCompletedEventArgs e)
        {
            switch (e.Result) {
                case (LoginResponse.Ok):
                    App.Service.LoginCompleted -= Service_LoginCompleted;
                    App.Service.RegisterCompleted -= Service_RegisterCompleted;
                    // FIXME: use sent values
                    App.UserName = NameBox.Text;
                    App.Password = PasswordBox.Password;
                    Cookie.SetCookie("username", App.UserName);
                    Cookie.SetCookie("password", App.Password);
                    SuccessfulLogin(this, null);
                    break;
                case (LoginResponse.InvalidLogin):
                    MessageBox.Show("Invalid user name.");
                    break;
                case (LoginResponse.InvalidPassword):
                    MessageBox.Show("Invalid password.");
                    break;
                case (LoginResponse.Unregistered):
                    MessageBox.Show("You need to register before logging in.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void Service_RegisterCompleted(object sender, RegisterCompletedEventArgs e)
        {
            switch (e.Result) {
                case RegisterResponse.Ok:
                    MessageBox.Show("You are now registered.");
                    break;
                case RegisterResponse.AlreadyRegistered:
                    MessageBox.Show("You are already registered");
                    break;
                case RegisterResponse.IsSmurf:
                    MessageBox.Show("You have already registered with a different account.");
                    break;
                case RegisterResponse.NotValidSpringLogin:
                    MessageBox.Show("You need to register with your Spring account.");
                    break;
                case RegisterResponse.NotValidSpringPassword:
                    MessageBox.Show("Invalid password. You need to use you Spring password.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void TextBlock_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) App.Service.LoginAsync(NameBox.Text, PasswordBox.Password);
        }
    }
}