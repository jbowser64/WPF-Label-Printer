using DesktopLabelPrinter.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DesktopLabelPrinter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PartsAndLocationsContext _context;
        private const string SettingsFileName = "printer_settings.txt";

        public MainWindow()
        {
            PartsAndLocationsContext context = new PartsAndLocationsContext();
            InitializeComponent();

            FIFODateTextBox.Text = DateTime.Now.ToString("MMMM yyyy");
            _context = context;

            partsAndLocationsGrid.ItemsSource = _context.PartsAndLocations.ToList();

            LoadPrinters();
        }

        private void LoadPrinters()
        {
            try
            {
                // Get all installed printers
                PrinterComboBox.Items.Clear();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    PrinterComboBox.Items.Add(printer);
                }

                // Try to load saved printer preference
                string savedPrinter = LoadSavedPrinter();
                if (!string.IsNullOrEmpty(savedPrinter) && PrinterComboBox.Items.Contains(savedPrinter))
                {
                    PrinterComboBox.SelectedItem = savedPrinter;
                }
                else if (PrinterComboBox.Items.Count > 0)
                {
                    // Default to first printer if no saved preference or saved printer not found
                    PrinterComboBox.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("No printers found on this system. Please install a printer and restart the application.", 
                        "No Printers Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading printers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string LoadSavedPrinter()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                if (File.Exists(settingsPath))
                {
                    return File.ReadAllText(settingsPath).Trim();
                }
            }
            catch (Exception ex)
            {
                // Silently fail if settings can't be loaded
                System.Diagnostics.Debug.WriteLine($"Error loading printer settings: {ex.Message}");
            }
            return string.Empty;
        }

        private void SavePrinter(string printerName)
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                File.WriteAllText(settingsPath, printerName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving printer settings: {ex.Message}");
            }
        }

        private void PrinterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrinterComboBox.SelectedItem != null)
            {
                string selectedPrinter = PrinterComboBox.SelectedItem.ToString();
                SavePrinter(selectedPrinter);
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrinterComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a printer before printing.", "No Printer Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedPrinter = PrinterComboBox.SelectedItem.ToString();
            
            if (string.IsNullOrWhiteSpace(numCopies.Text) || !int.TryParse(numCopies.Text, out int copies))
            {
                MessageBox.Show("Please enter a valid number of copies.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (copies < 1)
            {
                MessageBox.Show("Number of copies must be at least 1.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ensure we have the latest label image with current location override
            string partNum = PartNumberTextBox.Text;
            if (!String.IsNullOrWhiteSpace(partNum))
            {
                string locationOverride = LocationTextBox.Text?.Trim() ?? "";
                try
                {
                    printManager printManager = new printManager();
                    await printManager.PNGDownload(partNum, locationOverride);
                    printManager.Print(copies, selectedPrinter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while preparing to print: {ex.Message}", 
                        "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // If no part number, try to print existing label
                printManager printManager = new printManager();
                printManager.Print(copies, selectedPrinter);
            }
        }

        private void PartNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            string partNum = PartNumberTextBox.Text;


            partsAndDescriptionList.ItemsSource = _context.PartsAndLocations
                .Where(p => p.Material != null && p.Material.Contains(partNum))
                .Select(p => p.Material)
                .Distinct()
                .ToList();
            partsAndDescriptionList.Visibility = Visibility.Visible;
        }

        public void getZPLimage(string? partnum, string? overrideBin = null)
        {
            var parts = from p
                        in _context.PartsAndLocations
                        select p;
            if (!String.IsNullOrEmpty(partnum))
            {

                parts = parts.Where(s => s.Material.Contains(partnum) && s.Sloc.Equals("1000"));
                
                string URL = "";
                foreach (var item in parts)
                {
                    string description = item.Description ?? "No Description";
                    string desc1 = description.Length > 25 ? description.Substring(0, 25) : description;
                    string desc2 = description.Length > 25 ? description.Substring(25) : "";
                    
                    // Use override bin location if provided, otherwise use database bin
                    string binLocation = !String.IsNullOrWhiteSpace(overrideBin) ? overrideBin : (item.Bin ?? "");

                     URL = $"https://api.labelary.com/v1/printers/8dpmm/labels/2x1/0/" +
                        $"%5EXA%5EPW406%5EFT40,52%5EA0N,42,42%5EFH/%5EFD{item.Material}%5EFS%5EFT40,78%5EA0N,25,25%5EFH/%5E" +
                        $"FD{desc1}%5EFS%5EFT40,106%5EA0N,25,25%5EFH/%5E" +
                        $"FD{desc2}%5EFS%5EFT40,140%5EA0N,37,37%5EFH/%5E" +
                        $"FD{binLocation}%5EFS%5EFT275,140%5EA0N,37,37%5EFH/%5EFD%5EFS%5EFT40,180%5EA0N,37,37%5EFH/%5E" +
                        $"FDFIFO: {DateTime.Now.ToString("MMMM yyyy")}%5EFS%5EPQ1,0,1,Y%5EXZ";
                   
                }
                zplPNG.Source = new BitmapImage(new Uri(URL));
            }
            else
            {
                zplPNG.Source = null;
            }
        }
        
        private void LocationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update ZPL preview when location changes, but only if we have a part number
            string partNum = PartNumberTextBox.Text;
            if (!String.IsNullOrWhiteSpace(partNum))
            {
                string locationOverride = LocationTextBox.Text?.Trim() ?? "";
                getZPLimage(partNum, locationOverride);
            }
        }

        private void partsAndDescriptionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (partsAndDescriptionList.SelectedItem != null)
            {
                string partNum = partsAndDescriptionList.SelectedItem.ToString();
               
                try
                {
                    // Get the part from database to populate location field
                    var part = _context.PartsAndLocations
                        .Where(p => p.Material != null && p.Material.Contains(partNum) && p.Sloc == "1000")
                        .FirstOrDefault();
                    
                    // Populate LocationTextBox with database bin if it exists, but only if text box is empty
                    // This allows user to keep their override if they've already entered something
                    if (part != null && String.IsNullOrWhiteSpace(LocationTextBox.Text))
                    {
                        LocationTextBox.Text = part.Bin ?? "";
                    }
                    
                    // Use current location text box value (which may be database value or user override)
                    string locationOverride = LocationTextBox.Text?.Trim() ?? "";
                    
                    printManager printManager = new printManager();
                    printManager.PNGDownload(partNum, locationOverride);
                    getZPLimage(partNum, locationOverride);

                    partsAndLocationsGrid.ItemsSource = _context.PartsAndLocations
                        .Where(p => p.Material != null && p.Material.Contains(partNum))
                        .ToList();

                    PartNumberTextBox.Text = partNum;
                    partsAndDescriptionList.Visibility = Visibility.Collapsed;  
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error has occured. {ex.Message}");
                }
            }
        }

        private void partsAndLocationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
       
    }
}