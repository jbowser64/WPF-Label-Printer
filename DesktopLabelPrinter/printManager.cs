using DesktopLabelPrinter.Data;
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
    }
}
