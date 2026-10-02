using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class VerifyOtpWindow : Window
    {
        private string _expectedOtp;

        public VerifyOtpWindow(string email, string expectedOtp)
        {
            InitializeComponent();
            _expectedOtp = expectedOtp;
            DescText.Text = $"We simulated sending a 6-digit code to {email}.\n(For testing, your code is: {expectedOtp})";
            
            OtpBox.Focus();
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            if (OtpBox.Text.Trim() == _expectedOtp)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Text = "Invalid code. Please try again.";
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
