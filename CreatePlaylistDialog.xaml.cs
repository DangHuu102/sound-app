using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class CreatePlaylistDialog : Window
    {
        public string PlaylistName { get; private set; } = "";

        public CreatePlaylistDialog()
        {
            InitializeComponent();
            NameBox.Focus();
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
            NameBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) Create_Click(s, null!); };
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            PlaylistName = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(PlaylistName)) return;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
