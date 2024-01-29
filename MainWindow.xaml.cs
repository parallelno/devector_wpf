using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace devector
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        static readonly Hardware hardware = new Hardware();
		private RecentFileManager recentFileManager = new RecentFileManager();
        private FormDebugger form_debugger;
        private FormMemoryMap form_memory_map;

        public MainWindow()
		{
			InitializeComponent();
			recentFileManager.LoadRecentFiles();
			UpdateRecentFilesMenu();

			picture_display.Source = Hardware.display.frame;
		}

        private void UpdateRecentFilesMenu()
        {
            recent_files_menu_item.Items.Clear();
            foreach (var file in recentFileManager.Files)
            {
                var menuItem = new MenuItem
                {
                    Header = file,
                    // Optionally, you can set the Command or use Click event
                };
                menuItem.Click += RecentFileMenuItem_Click;
                recent_files_menu_item.Items.Add(menuItem);
            }
        }

        private void RecentFileMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;
			if (menuItem != null && menuItem.Header != null)
			{
                string filePath = menuItem.Header.ToString();
                if (filePath != null) OpenFile(filePath);
            }
        }

        private void MenuItem_Open(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Binary Files (*.bin;*.rom)|*.bin;*.rom"; // Set filter for .bin and .rom files
			openFileDialog.Title = "Select a Binary File";

			if (openFileDialog.ShowDialog() == true)
			{
				OpenFile(openFileDialog.FileName);
			}
		}

		private void OpenFile(string path)
		{
			// Add your code to open the file
			recentFileManager.AddFile(path);
			UpdateRecentFilesMenu();

			hardware.load_rom(path);

			picture_display.InvalidateVisual();
		}

        private void memory_map_menu_click(object sender, EventArgs e)
        {
            if (form_debugger == null || !form_debugger.IsLoaded)
            {
                form_memory_map = new FormMemoryMap();
                form_memory_map.Show();
            }
            else
            {
                form_memory_map.Focus();
            }
        }

        private void debugger_menu_item_click(object sender, EventArgs e)
        {
            if (form_debugger == null || !form_debugger.IsLoaded)
            {
                form_debugger = new FormDebugger(picture_display);
                form_debugger.Show();
            }
            else
            {
                form_debugger.Focus();
            }
        }
    }
}