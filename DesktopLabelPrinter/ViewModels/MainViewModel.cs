using DesktopLabelPrinter.Data;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DesktopLabelPrinter.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly PartsAndLocationsContext _context;
        private readonly printManager _printManager;
        private const string SettingsFileName = "printer_settings.txt";

        private string _partNumber = string.Empty;
        private string _location = string.Empty;
        private string _fifoDate = DateTime.Now.ToString("MMMM yyyy");
        private string _numCopies = "1";
        private string? _selectedPrinter;
        private ObservableCollection<string> _availablePrinters = new();
        private ObservableCollection<string> _partsSuggestions = new();
        private ObservableCollection<PartsAndLocation> _partsAndLocations = new();
        private PartsAndLocation? _selectedPart;
        private string? _selectedPartNumber;
        private BitmapImage? _zplPreviewImage;
        private bool _isPartsListVisible = false;

        public MainViewModel()
        {
            _context = new PartsAndLocationsContext();
            _printManager = new printManager();

            // Initialize PrintCommand first before LoadPrinters, since setting SelectedPrinter will call RaiseCanExecuteChanged
            PrintCommand = new RelayCommand(async _ => await ExecutePrintAsync(), _ => CanPrint());

            LoadPrinters();
            LoadPartsAndLocations();
        }

        #region Properties

        public string PartNumber
        {
            get => _partNumber;
            set
            {
                if (SetProperty(ref _partNumber, value))
                {
                    OnPartNumberChanged();
                }
            }
        }

        public string Location
        {
            get => _location;
            set
            {
                if (SetProperty(ref _location, value))
                {
                    UpdateZPLPreview();
                }
            }
        }

        public string FIFODate
        {
            get => _fifoDate;
            set
            {
                if (SetProperty(ref _fifoDate, value))
                {
                    UpdateZPLPreview();
                }
            }
        }

        public string NumCopies
        {
            get => _numCopies;
            set
            {
                if (SetProperty(ref _numCopies, value))
                {
                    PrintCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public string? SelectedPrinter
        {
            get => _selectedPrinter;
            set
            {
                if (SetProperty(ref _selectedPrinter, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        SavePrinter(value);
                    }
                    PrintCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<string> AvailablePrinters
        {
            get => _availablePrinters;
            set => SetProperty(ref _availablePrinters, value);
        }

        public ObservableCollection<string> PartsSuggestions
        {
            get => _partsSuggestions;
            set => SetProperty(ref _partsSuggestions, value);
        }

        public ObservableCollection<PartsAndLocation> PartsAndLocations
        {
            get => _partsAndLocations;
            set => SetProperty(ref _partsAndLocations, value);
        }

        public PartsAndLocation? SelectedPart
        {
            get => _selectedPart;
            set => SetProperty(ref _selectedPart, value);
        }

        public string? SelectedPartNumber
        {
            get => _selectedPartNumber;
            set
            {
                if (SetProperty(ref _selectedPartNumber, value) && !string.IsNullOrEmpty(value))
                {
                    ExecutePartSelected(value);
                }
            }
        }

        public BitmapImage? ZplPreviewImage
        {
            get => _zplPreviewImage;
            set => SetProperty(ref _zplPreviewImage, value);
        }

        public bool IsPartsListVisible
        {
            get => _isPartsListVisible;
            set => SetProperty(ref _isPartsListVisible, value);
        }

        #endregion

        #region Commands

        public RelayCommand PrintCommand { get; }

        #endregion

        #region Private Methods

        private void LoadPrinters()
        {
            try
            {
                AvailablePrinters.Clear();
                
                // Check if PrinterSettings.InstalledPrinters is accessible
                if (PrinterSettings.InstalledPrinters == null)
                {
                    MessageBox.Show("Unable to access printer settings. Please check your system configuration.",
                        "Printer Access Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (string? printer in PrinterSettings.InstalledPrinters)
                {
                    // Skip null printer names
                    if (!string.IsNullOrEmpty(printer))
                    {
                        AvailablePrinters.Add(printer);
                    }
                }

                // Try to load saved printer preference
                string savedPrinter = LoadSavedPrinter();
                if (!string.IsNullOrEmpty(savedPrinter) && AvailablePrinters.Contains(savedPrinter))
                {
                    SelectedPrinter = savedPrinter;
                }
                else if (AvailablePrinters.Count > 0)
                {
                    SelectedPrinter = AvailablePrinters[0];
                }
                else
                {
                    MessageBox.Show("No printers found on this system. Please install a printer and restart the application.",
                        "No Printers Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading printers: {ex.Message}\n\nStack Trace: {ex.StackTrace}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPartsAndLocations()
        {
            try
            {
                var parts = _context.PartsAndLocations.ToList();
                PartsAndLocations = new ObservableCollection<PartsAndLocation>(parts);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading parts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void OnPartNumberChanged()
        {
            if (string.IsNullOrWhiteSpace(PartNumber))
            {
                PartsSuggestions.Clear();
                IsPartsListVisible = false;
                ZplPreviewImage = null;
                return;
            }

            // Update suggestions
            var suggestions = _context.PartsAndLocations
                .Where(p => p.Material != null && p.Material.Contains(PartNumber))
                .Select(p => p.Material!)
                .Distinct()
                .ToList();

            PartsSuggestions = new ObservableCollection<string>(suggestions);
            IsPartsListVisible = suggestions.Count > 0;

            // Update preview
            UpdateZPLPreview();
        }

        private void UpdateZPLPreview()
        {
            if (string.IsNullOrWhiteSpace(PartNumber))
            {
                ZplPreviewImage = null;
                return;
            }

            GetZPLImage(PartNumber, Location, FIFODate);
        }

        private void GetZPLImage(string? partNum, string? overrideBin = null, string? overrideFIFODate = null)
        {
            if (string.IsNullOrEmpty(partNum))
            {
                ZplPreviewImage = null;
                return;
            }

            var parts = _context.PartsAndLocations
                .Where(s => s.Material != null && s.Material.Contains(partNum) && s.Sloc != null && s.Sloc.Equals("1000"));

            string? url = null;
            foreach (var item in parts)
            {
                string description = item.Description ?? "No Description";
                string desc1 = description.Length > 25 ? description.Substring(0, 25) : description;
                string desc2 = description.Length > 25 ? description.Substring(25) : "";

                // Use override bin location if provided, otherwise use database bin
                string binLocation = !string.IsNullOrWhiteSpace(overrideBin) ? overrideBin : (item.Bin ?? "");
                string fifoDate = !string.IsNullOrWhiteSpace(overrideFIFODate) ? overrideFIFODate : DateTime.Now.ToString("MMMM yyyy");

                url = $"https://api.labelary.com/v1/printers/8dpmm/labels/2x1/0/" +
                    $"%5EXA%5EPW406%5EFT40,52%5EA0N,42,42%5EFH/%5EFD{item.Material}%5EFS%5EFT40,78%5EA0N,25,25%5EFH/%5E" +
                    $"FD{desc1}%5EFS%5EFT40,106%5EA0N,25,25%5EFH/%5E" +
                    $"FD{desc2}%5EFS%5EFT40,140%5EA0N,37,37%5EFH/%5E" +
                    $"FD{binLocation}%5EFS%5EFT275,140%5EA0N,37,37%5EFH/%5EFD%5EFS%5EFT40,180%5EA0N,37,37%5EFH/%5E" +
                    $"FDFIFO: {fifoDate}%5EFS%5EPQ1,0,1,Y%5EXZ";
                break; // Only need first match
            }

            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    ZplPreviewImage = new BitmapImage(new Uri(url));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading preview image: {ex.Message}");
                    ZplPreviewImage = null;
                }
            }
            else
            {
                ZplPreviewImage = null;
            }
        }

        private void ExecutePartSelected(string? partNum)
        {
            if (string.IsNullOrEmpty(partNum))
                return;

            try
            {
                // Get the part from database to populate location field
                var part = _context.PartsAndLocations
                    .Where(p => p.Material != null && p.Material.Contains(partNum) && p.Sloc == "1000")
                    .FirstOrDefault();

                // Populate Location with database bin if it exists, but only if location is empty
                // This allows user to keep their override if they've already entered something
                if (part != null && string.IsNullOrWhiteSpace(Location))
                {
                    Location = part.Bin ?? "";
                }

                // Update parts grid
                var filteredParts = _context.PartsAndLocations
                    .Where(p => p.Material != null && p.Material.Contains(partNum))
                    .ToList();

                PartsAndLocations = new ObservableCollection<PartsAndLocation>(filteredParts);

                // Update part number and hide suggestions
                PartNumber = partNum;
                IsPartsListVisible = false;

                // Update preview
                UpdateZPLPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error has occurred. {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanPrint()
        {
            return !string.IsNullOrEmpty(SelectedPrinter) &&
                   int.TryParse(NumCopies, out int copies) &&
                   copies >= 1;
        }

        private async Task ExecutePrintAsync()
        {
            if (string.IsNullOrEmpty(SelectedPrinter))
            {
                MessageBox.Show("Please select a printer before printing.", "No Printer Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NumCopies) || !int.TryParse(NumCopies, out int copies))
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

            // Ensure we have the latest label image with current location and FIFO date override
            if (!string.IsNullOrWhiteSpace(PartNumber))
            {
                try
                {
                    await _printManager.PNGDownload(PartNumber, Location, FIFODate);
                    _printManager.Print(copies, SelectedPrinter);
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
                try
                {
                    _printManager.Print(copies, SelectedPrinter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while printing: {ex.Message}",
                        "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}

