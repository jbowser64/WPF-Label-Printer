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
        public MainWindow()
        {
            PartsAndLocationsContext context = new PartsAndLocationsContext();
            InitializeComponent();

            FIFODateTextBox.Text = DateTime.Now.ToString("MMMM yyyy");
            _context = context;

            partsAndLocationsGrid.ItemsSource = _context.PartsAndLocations.ToList();

        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            printManager printManager = new printManager();
            int copies = Convert.ToInt32(numCopies.Text);
            
            printManager.Print(copies);
           
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

        public void getZPLimage(string? partnum)
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

                     URL = $"https://api.labelary.com/v1/printers/8dpmm/labels/2x1/0/" +
                        $"%5EXA%5EPW406%5EFT40,52%5EA0N,42,42%5EFH/%5EFD{item.Material}%5EFS%5EFT40,78%5EA0N,25,25%5EFH/%5E" +
                        $"FD{desc1}%5EFS%5EFT40,106%5EA0N,25,25%5EFH/%5E" +
                        $"FD{desc2}%5EFS%5EFT40,140%5EA0N,37,37%5EFH/%5E" +
                        $"FD{item.Bin}%5EFS%5EFT275,140%5EA0N,37,37%5EFH/%5EFD%5EFS%5EFT40,180%5EA0N,37,37%5EFH/%5E" +
                        $"FDFIFO: {DateTime.Now.ToString("MMMM yyyy")}%5EFS%5EPQ1,0,1,Y%5EXZ";
                   
                }
                zplPNG.Source = new BitmapImage(new Uri(URL));
            }
            else
            {
                zplPNG.Source = null;
            }
        }

        private void partsAndDescriptionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //move this again? figure out wtf is going on here

            if (partsAndDescriptionList.SelectedItem != null)
            {
                string partNum = partsAndDescriptionList.SelectedItem.ToString();
               
                try
                {
                    printManager printManager = new printManager();
                    printManager.PNGDownload(partNum);
                    getZPLimage(partNum);

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
                //partsAndDescriptionList.Items.Clear();
            }


        }

        private void partsAndLocationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
       
    }
}