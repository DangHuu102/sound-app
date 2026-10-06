using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DatabaseManager.InitializeDatabase();

            EmailBox.Text = "Enter your username or email";
            EmailBox.Foreground = System.Windows.Media.Brushes.Gray;

            EmailBox.PreviewMouseLeftButtonDown += (s, e) => EmailBox.Focus();
            PasswordBox.PreviewMouseLeftButtonDown += (s, e) => PasswordBox.Focus();

            MouseLeftButtonDown += (s, e) => 
            { 
                if (e.OriginalSource is System.Windows.DependencyObject depObj)
                {
                    var current = depObj;
                    while (current != null)
                    {
                        if (current is System.Windows.Controls.TextBox || current is System.Windows.Controls.PasswordBox || current is System.Windows.Controls.Button)
                        {
                            return; // Dừng lại, để cho control xử lý
                        }
                        current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                    }
                }

                if (e.ButtonState == MouseButtonState.Pressed)
                    DragMove(); 
            };
        }

        private void ClearPlaceholder(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            if (tb.Text == "Enter your username or email")
            {
                tb.Text = "";
                tb.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void RestorePlaceholder(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = "Enter your username or email";
                tb.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private bool _isSyncing = false;

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isSyncing)
            {
                _isSyncing = true;
                VisiblePasswordBox.Text = PasswordBox.Password;
                _isSyncing = false;
            }
        }

        private void VisiblePasswordBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_isSyncing)
            {
                _isSyncing = true;
                PasswordBox.Password = VisiblePasswordBox.Text;
                _isSyncing = false;
            }
        }

        private void ShowPassword_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PasswordBox.Visibility = Visibility.Collapsed;
            VisiblePasswordBox.Visibility = Visibility.Visible;
            VisiblePasswordBox.Focus();
            VisiblePasswordBox.Select(VisiblePasswordBox.Text.Length, 0);
        }

        private void ShowPassword_MouseUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
            VisiblePasswordBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Focus();
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string input = EmailBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(input) || input == "Enter your username or email")
            { ShowError("Please enter your username or email."); return; }

            if (string.IsNullOrWhiteSpace(password))
            { ShowError("Please enter your password."); return; }

            var user = DatabaseManager.Login(input, password);
            if (user == null)
            { ShowError("Incorrect username/email or password."); return; }

            App.CurrentUser = user;
            GoToMain();
        }

        private void SignUpLink_Click(object sender, RoutedEventArgs e)
        {
            var signUp = new SignUpWindow();
            signUp.Show();
            this.Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            GoToMain();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void GoToMain()
        {
            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w is MainWindow mw)
                {
                    mw.UpdateUserUI();
                    mw.Show();
                    this.Close();
                    return;
                }
            }
            var main = new MainWindow();
            main.Show();
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
