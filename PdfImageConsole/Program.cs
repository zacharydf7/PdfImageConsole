namespace PdfImageConsole;

class Program
{
    static void Main(string[] args)
    {
        var pdfImageHasher = new PdfImageHasher("C:\\Users\\zacha\\OneDrive\\Desktop\\Samples\\SampleLetter Original MVP1.pdf");
        var imageRectangles = pdfImageHasher.GetImageRectangles();

        foreach (var imageRect in imageRectangles)
        {
            Console.WriteLine($"Page: {imageRect.Key}");
            foreach (var rect in imageRect.Value)
            {
                Console.WriteLine($"X1: {rect.x1}, X2: {rect.x2}, Y1: {rect.y1}, Y2: {rect.y2}");
                Console.WriteLine($"Hash: {pdfImageHasher.GetImageHashAtCoordinates(imageRect.Key, rect.x1, rect.y1, rect.x2, rect.y2)}");
            }
            Console.WriteLine();
        }
    }
}
