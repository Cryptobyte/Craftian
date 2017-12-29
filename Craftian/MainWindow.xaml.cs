using Craftian.Minecraft;
using MahApps.Metro.Controls.Dialogs;
using MojangSharp.Api;
using MojangSharp.Endpoints;
using MojangSharp.Responses;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Craftian
{
    /// <inheritdoc cref="MahApps.Metro.Controls.MetroWindow" />
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Profile Properties

        private bool _autoLogin;
        private DispatcherTimer _connectionCheck;
        private Mojang.Profile _userProfile;

        #endregion

        #region Interface Properties

        /// <summary>
        /// Is Login available, used to indicate if the 
        /// servers used to login to Mojang are reachable
        /// </summary>
        public bool IsLoginAvailable {
            get => (bool)GetValue(IsLoginAvailableProperty);
            set => SetValue(IsLoginAvailableProperty, value);
        }

        /// <summary>
        /// Is the user logged in to their Mojang account
        /// </summary>
        public bool IsLoggedIn {
            get => (bool)GetValue(IsLoggedInProperty);
            set => SetValue(IsLoggedInProperty, value);
        }

        /// <summary>
        /// Is there a mod being dragged over the window
        /// </summary>
        public bool IsModOver {
            get => (bool)GetValue(IsModOverProperty);
            set => SetValue(IsModOverProperty, value);
        }

        /// <summary>
        /// Is there a mod being dragged over the mod button
        /// </summary>
        public bool IsModOverDrop {
            get => (bool)GetValue(IsModOverDropProperty);
            set => SetValue(IsModOverDropProperty, value);
        }

        /// <summary>
        /// Status of the connection to various Mojang services
        /// </summary>
        public int ConnectionStatus {
            get => (int)GetValue(ConnectionStatusProperty);
            set => SetValue(ConnectionStatusProperty, value);
        }

        public static readonly DependencyProperty IsLoginAvailableProperty =
           DependencyProperty.Register("IsLoginAvailable", typeof(bool),
             typeof(MainWindow), new UIPropertyMetadata(false));

        public static readonly DependencyProperty IsLoggedInProperty =
            DependencyProperty.Register("IsLoggedIn", typeof(bool),
                typeof(MainWindow), new UIPropertyMetadata(false));

        public static readonly DependencyProperty IsModOverProperty =
            DependencyProperty.Register("IsModOver", typeof(bool),
                typeof(MainWindow), new UIPropertyMetadata(false));

        public static readonly DependencyProperty IsModOverDropProperty =
            DependencyProperty.Register("IsModOverDrop", typeof(bool),
                typeof(MainWindow), new UIPropertyMetadata(false));

        public static readonly DependencyProperty ConnectionStatusProperty =
            DependencyProperty.Register("ConnectionStatus", typeof(int),
                typeof(MainWindow), new UIPropertyMetadata(0));

        #endregion

        /// <summary>
        /// List of Jump servers to use in the UI
        /// </summary>
        private ObservableCollection<Server> _servers;

        private async void ShowLogoutDialog(object sender, RoutedEventArgs e)
        {
            // Show logout confirm dialog
            var result = await this.ShowMessageAsync(
                Properties.Resources.str_Logout_Title, 
                Properties.Resources.str_Logout_Message, 
                MessageDialogStyle.AffirmativeAndNegative
            ); 

            // If user canceled logout, return
            if (result != MessageDialogResult.Affirmative)
                return;

            // Invalidate user access token
            await new Invalidate(_userProfile.AccessToken).PerformRequest();

            // Delete profile from disk
            _userProfile.Remove();

            // Reset globals for blank session
            IsLoggedIn = false;
            _userProfile = null;
            _autoLogin = false;

            // Turn off auto login
            Properties.Settings.Default.AutoLoadAccount = false;
            Properties.Settings.Default.AutoLoadAccountName = string.Empty;
            Properties.Settings.Default.Save();

            // Change Login text back to default
            BtnLogin.Content = Properties.Resources.str_Login;
        }

        private async void ShowLoginDialog(object sender, RoutedEventArgs e)
        {
            // If user is logged in show logout dialog
            if (IsLoggedIn)
            {
                ShowLogoutDialog(sender, null);
                return;
            }

            // Store login message so we can add to it if there are issues
            var loginMessage = Properties.Resources.str_Login_Message;

            // If connection status has trouble add to login dialog
            if (ConnectionStatus < 2)
                loginMessage += "\n" + Properties.Resources.err_Connectivity_Mojang;

            // Show login dialog and read the result
            var result = await this.ShowLoginAsync(
                Properties.Resources.str_Login_Title, 
                loginMessage,

                new LoginDialogSettings {
                    UsernameWatermark = Properties.Resources.plc_Email,
                    PasswordWatermark = Properties.Resources.plc_Password,
                    ColorScheme = MetroDialogOptions.ColorScheme,
                    NegativeButtonVisibility = Visibility.Visible,
                    RememberCheckBoxVisibility = Visibility.Visible,
                    RememberCheckBoxText = Properties.Resources.cBox_AutoLogin
                }
            );

            // If the result is null, return
            if (result == null)
                return;

            // Show progress while logging in
            var controller = await this.ShowProgressAsync(
                Properties.Resources.prog_Login_Title, 
                Properties.Resources.prog_Login_Message
            );

            // There is no way to determine the progress of the login request
            controller.SetIndeterminate();
            
            /*
             * Pass user credentials into authentication request
             * so that we don't store them in memory for long
             */
            var authResponse = await new Authenticate(
                new Credentials {
                    Username = result.Username,
                    Password = result.Password

            }).PerformRequest();

            // If authentication request was successful
            if (authResponse.IsSuccess)
            {
                IsLoggedIn = true;

                // Create new Profilel object to store basic information
                _userProfile = new Mojang.Profile(
                    result.Username,
                    authResponse.SelectedProfile.PlayerName,
                    authResponse.User.Uuid,
                    authResponse.AccessToken,
                    authResponse.SelectedProfile.Legacy != null && 
                    authResponse.SelectedProfile.Legacy.Value ? "legacy" : "mojang"
                );

                // Set login button text to player's player name
                BtnLogin.Content = _userProfile.PlayerName;

                // Set auto login variable to result of the check box
                _autoLogin = result.ShouldRemember;

                // Close progress dialog
                await controller.CloseAsync();
            }
            else // login was unsuccessful
            {
                // Close progress dialog
                await controller.CloseAsync();

                // Show login error dialog
                await this.ShowMessageAsync(
                    Properties.Resources.str_FailedLogin_Title,
                    Properties.Resources.str_FailedLogin_Message + 
                    "\n\n" + authResponse.Error.ErrorMessage
                );
            }
        }

        /// <summary>
        /// Gets a more dynamic timeout for the connectivity check timer 
        /// so that we don't query Mojang more than we have to
        /// </summary>
        /// <returns>Timeout between connectivity checks</returns>
        public int GetTimeout()
        {
            return ConnectionStatus < 2 ? 5 : 3;
        }

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private async void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            // Make all default directories for the app
            Directories.MakeDefaults();

            /*
             * Force a connection check before the timer starts 
             * so that players can play the game without waiting
             * for timers to clear
             */
            Timer_Connection_Tick(null, null);

            /*
             * Client token is saved statically so that we can
             * invalidate, validate, etc. with the same token
             */
            Requester.ClientToken = ConfigurationManager.AppSettings.Get("ClientToken");

            /*
             * Check if a user has logged in before and selected
             * the check box to automatically login when the app
             * starts
             */
            _autoLogin = Properties.Settings.Default.AutoLoadAccount;

            /*
             * If the user has opted to automatically login we
             * can handle the login process in the background
             * to save time
             */
            if (_autoLogin)
            {
                try
                {
                    // Try to load the user profile from the save file
                    _userProfile = Mojang.Profile.Load(
                        Properties.Settings.Default.AutoLoadAccountName
                    );
                }
                catch // Normally FileNotFoundException (!)
                {
                    // Turn auto login off locally if we failed to load the file
                    _autoLogin = false;
                    _userProfile = null;

                    // Turn auto login off application wide if we failed to load the file
                    Properties.Settings.Default.AutoLoadAccount = false;
                    Properties.Settings.Default.AutoLoadAccountName = string.Empty;
                    Properties.Settings.Default.Save();
                    
                    // Show login failed dialog to user if auto login has failed
                    await this.ShowMessageAsync(
                        Properties.Resources.str_FailedLogin_Title,
                        Properties.Resources.str_FailedLogin_Message
                    );

                    return;
                }

                // Try to validate the user's access token from the loaded profile
                var validateResponse = await new Validate(_userProfile.AccessToken).PerformRequest();

                // Access token validation was successful
                if (validateResponse.IsSuccess)
                {
                    // Update global to indicate user is logged in
                    IsLoggedIn = true;

                    // Login must be available because we just logged in
                    IsLoginAvailable = true;

                    // Change login button text to player's player name
                    BtnLogin.Content = _userProfile.PlayerName;
                }
                else
                {
                    /*
                     * If the validation did not work, try to refresh the token instead
                     * which is the same thing that the stock Minecraft launcher does
                     */
                    var refreshResponse = await new Refresh(_userProfile.AccessToken).PerformRequest();

                    // If token refresh was successful
                    if (refreshResponse.IsSuccess)
                    {
                        // Update global to indicate user is logged in
                        IsLoggedIn = true;

                        // Login must be available because we just logged in
                        IsLoginAvailable = true;

                        // Change login button text to player's player name
                        BtnLogin.Content = _userProfile.PlayerName;
                    }
                    else
                    {
                        // Turn auto login off locally if we failed to refresh the access token
                        _autoLogin = false;
                        _userProfile = null;

                        // Turn auto login off application wide if we failed to refresh the access token
                        Properties.Settings.Default.AutoLoadAccount = false;
                        Properties.Settings.Default.AutoLoadAccountName = string.Empty;
                        Properties.Settings.Default.Save();

                        // Show login failed dialog to user if auto login has failed
                        await this.ShowMessageAsync(
                            Properties.Resources.str_FailedLogin_Title,
                            Properties.Resources.str_FailedLogin_Message +
                            "\n\nResponse: { " + refreshResponse.Error.ErrorMessage + " }"
                        );
                    }
                }
            }

            /*
             * Setup a timer to check the connection to Mojang, etc.
             * every so often which is primarily used to update the UI
             */
            _connectionCheck = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(GetTimeout())
            };

            _connectionCheck.Tick += Timer_Connection_Tick;
            _connectionCheck.Start();
        }

        private async void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /*
             * If the user has opted to automatically login
             * we can save the profile information to a file
             * for easier login next time. No private information
             * is saved here, only tokens to refresh on next launch
             */
            if (_autoLogin)
            {
                Properties.Settings.Default.AutoLoadAccount = true;
                Properties.Settings.Default.AutoLoadAccountName = _userProfile.PlayerName;
                Properties.Settings.Default.Save();

                await _userProfile.Save();
            }
            else
            {
                Properties.Settings.Default.AutoLoadAccount = false;
                Properties.Settings.Default.AutoLoadAccountName = string.Empty;
                Properties.Settings.Default.Save();
            }
        }

        private async void Timer_Connection_Tick(object sender, EventArgs e)
        {
            #region Check Connection to Mojang Services

            var status = await new ApiStatus().PerformRequest();

            if (!status.IsSuccess)
            {
                IsLoginAvailable = false;
                ConnectionStatus = 0;

                if (_connectionCheck != null)
                    _connectionCheck.Interval = 
                        TimeSpan.FromSeconds(GetTimeout());

                return;
            }

            if (status.MojangAccounts == ApiStatusResponse.Status.Unavailable ||
                status.MojangAuthenticationService == ApiStatusResponse.Status.Unavailable)
            {
                IsLoginAvailable = false;
                ConnectionStatus = 0;

                if (_connectionCheck != null)
                    _connectionCheck.Interval =
                        TimeSpan.FromSeconds(GetTimeout());

                return;
            }

            if (status.MojangAccounts == ApiStatusResponse.Status.SomeIssues ||
                status.MojangAuthenticationService == ApiStatusResponse.Status.SomeIssues)
            {
                IsLoginAvailable = true;
                ConnectionStatus = 1;

                if (_connectionCheck != null)
                    _connectionCheck.Interval =
                        TimeSpan.FromSeconds(GetTimeout());

                return;
            }

            if (status.MojangAccounts == ApiStatusResponse.Status.Available &&
                status.MojangAuthenticationService == ApiStatusResponse.Status.Available)
            {
                IsLoginAvailable = true;
                ConnectionStatus = 2;

                if (_connectionCheck != null)
                    _connectionCheck.Interval =
                        TimeSpan.FromSeconds(GetTimeout());

            }

            #endregion
        }

        #region Drag / Drop

        private static bool IsModFiles(string[] files)
        {
            return files.All(file => file == null || Path.GetExtension(file).Equals(".jar"));
        }

        private void Button_Package_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && 
                IsModFiles((string[])e.Data.GetData(DataFormats.FileDrop))) {

                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            if (IsModOverDrop) IsModOverDrop = false;
        }

        private void Button_Package_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                IsModFiles((string[])e.Data.GetData(DataFormats.FileDrop))) {

                if (!IsModOverDrop) IsModOverDrop = true;

                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Button_Package_DragLeave(object sender, DragEventArgs e)
        {
            if (IsModOverDrop) IsModOverDrop = false;

            e.Handled = true;
        }

        private void Button_Package_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                IsModFiles((string[])e.Data.GetData(DataFormats.FileDrop))) {

                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void MainWindow1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                IsModFiles((string[])e.Data.GetData(DataFormats.FileDrop))) {

                if (!IsModOver) IsModOver = true;

                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void MainWindow1_DragLeave(object sender, DragEventArgs e)
        {
            if (IsModOver) IsModOver = false;

            e.Handled = true;
        }

        private void MainWindow1_Drop(object sender, DragEventArgs e)
        {
            if (IsModOver) IsModOver = false;

            e.Handled = true;
        }

        #endregion

        private void Button_Package_Click(object sender, RoutedEventArgs e)
        {
            MainTabs.SelectedIndex = 0;
        }

        private void Button_Play_Click(object sender, RoutedEventArgs e)
        {
            /*

            var launcher = new Launcher();

            launcher.Launch(
                _userProfile,
                new Version("1.12.2")
            );

            */
        }

        private void Button_Skins_Click(object sender, RoutedEventArgs e)
        {
            MainTabs.SelectedIndex = 2;
        }

        private void Button_Go_Home(object sender, RoutedEventArgs e)
        {
            MainTabs.SelectedIndex = 1;
        }

        private async void About_Jump_Servers(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync(
                Properties.Resources.abt_JumpServers_Title,
                Properties.Resources.abt_JumpServers
            );
        }
    }
}
