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

            GoToMain();
        }

        private void SignUpLink_Click(object sender, RoutedEventArgs e)
        {
            var signUp = new SignUpWindow();
            signUp.Show();
            this.Hide();
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
            // Tìm MainWindow đang ẩn và show lại, tránh tạo instance mới
            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w is MainWindow mw)
                {
                    mw.Show();
                    this.Hide();
                    return;
                }
            }
            // Nếu không tìm thấy thì tạo mới
            var main = new MainWindow();
            main.Show();
            this.Hide();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
