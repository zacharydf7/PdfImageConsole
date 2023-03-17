using pdftron.Filters;
using pdftron.PDF;
using pdftron;
using System.Security.Cryptography;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace PdfImageConsole;

public class PdfImageHasher
{
    private readonly string _pdfPath;

    public PdfImageHasher(string pdfPath)
	{
        _pdfPath = pdfPath;
    }

    public void HashPdfImages() 
    {
        // Set your PDFTron license key
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        var pdfTronLicenseKey = configuration["PDFTron:LicenseKey"]!;
        PDFNet.Initialize(pdfTronLicenseKey);

        try
        {
            // Load the input PDF file
            using PDFDoc doc = new PDFDoc(_pdfPath);
            int imageCount = 0;

            // Iterate through the pages in the PDF document
            for (PageIterator itr = doc.GetPageIterator(); itr.HasNext(); itr.Next())
            {
                Page page = itr.Current();

                // Create an ElementReader to read elements from the page
                ElementReader reader = new ElementReader();
                reader.Begin(page);

                // Iterate through the elements on the page
                for (Element element = reader.Next(); element != null; element = reader.Next())
                {
                    // Check if the element is an image or an inline image
                    if (element.GetType() == Element.Type.e_image || element.GetType() == Element.Type.e_inline_image)
                    {
                        imageCount++;

                        // Extract the image object from the element
                        Image img = new Image(element.GetXObject());

                        // Get the image data as a Filter object
                        Filter imageDataFilter = img.GetImageData();

                        // Create a MemoryStream to store the image data
                        MemoryStream ms = new MemoryStream();

                        // Read the image data from the FilterReader and write it to the MemoryStream
                        using FilterReader filterReader = new FilterReader(imageDataFilter);
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = filterReader.Read(buffer)) > 0)
                        {
                            ms.Write(buffer, 0, bytesRead);
                        }

                        // Convert the MemoryStream content to a byte array
                        byte[] imageData = ms.ToArray();

                        // Compute the SHA-256 hash of the image data
                        string hash;
                        using SHA256 sha256 = SHA256.Create();
                        byte[] hashBytes = sha256.ComputeHash(imageData);
                        hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                        // Print the image hash to the console
                        Console.WriteLine($"Image {imageCount}: {hash}");
                    }
                }

                // Cleanup: End the ElementReader
                reader.End();
            }

            // Print the total number of images found in the PDF
            Console.WriteLine($"Found {imageCount} images in the PDF.");
        }
        catch (Exception ex)
        {
            // Print any errors that occurred while processing the PDF
            Console.WriteLine($"Error processing PDF: {ex.Message}");
        }
    }

    public string? GetImageHashAtCoordinates(int pageNumber, double x1, double y1, double x2, double y2)
    {
        // Set your PDFTron license key
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        var pdfTronLicenseKey = configuration["PDFTron:LicenseKey"]!;
        PDFNet.Initialize(pdfTronLicenseKey);

        string? imageHash = null;

        try
        {
            using PDFDoc doc = new PDFDoc(_pdfPath);
            Page page = doc.GetPage(pageNumber);

            if (page == null) return null;

            Rect searchRect = new Rect(x1, y1, x2, y2);
            ElementReader reader = new ElementReader();
            reader.Begin(page);

            for (Element element = reader.Next(); element != null; element = reader.Next())
            {
                if (element.GetType() == Element.Type.e_image || element.GetType() == Element.Type.e_inline_image)
                {
                    Rect imgRect = new Rect();
                    bool success = element.GetBBox(imgRect);

                    if (success && imgRect.IntersectRect(imgRect, searchRect))
                    {
                        Image img = new Image(element.GetXObject());
                        Filter imageDataFilter = img.GetImageData();
                        MemoryStream ms = new MemoryStream();
                        FilterReader filterReader = new FilterReader(imageDataFilter);

                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = filterReader.Read(buffer)) > 0)
                        {
                            ms.Write(buffer, 0, bytesRead);
                        }

                        byte[] imageData = ms.ToArray();

                        // Compute the SHA-256 hash of the image data
                        using SHA256 sha256 = SHA256.Create();
                        byte[] hashBytes = sha256.ComputeHash(imageData);
                        imageHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                        break;
                    }
                }
            }
            reader.End();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error processing PDF: {e.Message}");
        }
        return imageHash;
    }

    public Dictionary<int, List<Rect>> GetImageRectangles()
    {

        // Set your PDFTron license key
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        var pdfTronLicenseKey = configuration["PDFTron:LicenseKey"]!;
        PDFNet.Initialize(pdfTronLicenseKey);

        Dictionary<int, List<Rect>> imageRectangles = new Dictionary<int, List<Rect>>();

        try
        {
            PDFDoc doc = new PDFDoc(_pdfPath);

            for (PageIterator itr = doc.GetPageIterator(); itr.HasNext(); itr.Next())
            {
                Page page = itr.Current();
                int pageNumber = itr.GetPageNumber();
                ElementReader reader = new ElementReader();
                reader.Begin(page);

                List<Rect> rectsOnPage = new List<Rect>();

                for (Element element = reader.Next(); element != null; element = reader.Next())
                {
                    if (element.GetType() == Element.Type.e_image || element.GetType() == Element.Type.e_inline_image)
                    {
                        Rect imgRect = new Rect();
                        bool success = element.GetBBox(imgRect);
                        if (success)
                        {
                            rectsOnPage.Add(imgRect);
                        }
                    }
                }

                if (rectsOnPage.Count > 0)
                {
                    imageRectangles.Add(pageNumber, rectsOnPage);
                }

                reader.End();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error processing PDF: {e.Message}");
        }

        return imageRectangles;
    }
}
