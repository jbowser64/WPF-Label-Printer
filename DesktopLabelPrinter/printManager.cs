using DesktopLabelPrinter.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace DesktopLabelPrinter
{
    public class printManager
    {

        private static PartsAndLocationsContext _context;

        public async Task PNGDownload(string partNum)
        {
            PartsAndLocationsContext context = new PartsAndLocationsContext();

            _context = context;

            var parts = from p
                        in _context.PartsAndLocations
                        select p;
            if (!String.IsNullOrEmpty(partNum))
            {

                parts = parts.Where(s => s.Material.Contains(partNum) && s.Sloc.Equals("1000"));
                string URL = "";
                foreach (var item in parts)
                {
                    string description = item.Description ?? "No Description";
                    string desc1 = description.Length > 25 ? description.Substring(0, 25) : description;
                    string desc2 = description.Length > 25 ? description.Substring(25) : "";
                    string material = item.Material;
                    string bin = item.Bin; 

                    URL = $"https://api.labelary.com/v1/printers/8dpmm/labels/2x1/0/" +
                       $"%5EXA%5EPW406%5EFT40,52%5EA0N,42,42%5EFH/%5EFD{material}%5EFS%5EFT40,78%5EA0N,25,25%5EFH/%5E" +
                       $"FD{desc1}%5EFS%5EFT40,106%5EA0N,25,25%5EFH/%5E" +
                       $"FD{desc2}%5EFS%5EFT40,140%5EA0N,37,37%5EFH/%5E" +
                       $"FD{bin}%5EFS%5EFT275,140%5EA0N,37,37%5EFH/%5EFD%5EFS%5EFT40,180%5EA0N,37,37%5EFH/%5E" +
                       $"FDFIFO: {DateTime.Now.ToString("MMMM yyyy")}%5EFS%5EPQ1,0,1,Y%5EXZ";
                }
                partNum = "";
                string outputFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "label.png");


                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Accept", "image/png");
                        client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                        byte[] imageBytes = await client.GetByteArrayAsync(URL);
                        await File.WriteAllBytesAsync(outputFilePath, imageBytes);
                    }
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Error: An HTTP request error occurred. The server may be unavailable or the URL is invalid.{ex.Message}");

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}");
                }
            }
        }

        public static void Print(int copies)
        {

            short numCopies = Convert.ToInt16(copies);

            string printerName = "ZDesigner ZM400 200 dpi (ZPL) 2";

            string imageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "label.png");
            
            try
            {
                if (!File.Exists(imageFilePath))
                {
                    MessageBox.Show($"Error: The specified image file was not found at '{imageFilePath}'.");
                    return;
                }
                PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                pd.PrintPage += (sender, e) =>
                {
                    try
                    {
                        using (System.Drawing.Image imageToPrint = System.Drawing.Image.FromFile(imageFilePath))
                        {

                            Rectangle marginBounds = e.MarginBounds;
                            e.Graphics.DrawImage(imageToPrint, marginBounds);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while drawing the image: {ex.Message}");
                        e.Cancel = true;
                    }
                };

                pd.DefaultPageSettings.PaperSize = new PaperSize("Label 2x1", 200, 100);


                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                pd.PrinterSettings.Copies = numCopies;

                pd.Print();
            }
            catch (InvalidPrinterException)
            {
                MessageBox.Show($"Error: The printer '{printerName}' was not found. Please ensure the printer is correctly installed on your system.");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}");

            }
        }
    }
}
