using System;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.IO;
using System.Collections.Generic;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf.Canvas.Parser.Data;


namespace PDF_to_HTML
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 1)
            {
                Console.WriteLine("PLEASE ENTER TWO PATHS:");
                Console.WriteLine("1st path is to PDF");
                Console.WriteLine("EX: PDF-to-HTML c:\\temp\\_testPDF.PDF");
                System.Environment.Exit(2);
            }
            var pdfPath = args[0];

            //HTML Header and start of Body for the final HTML file.
            var htmlDocHeadBodyStart = @"
<!doctype html>
<html lang = ""en"">
<head>
	<meta charset =""utf-8"">
	<title> _testPDF</title>
	<meta name = ""description"" content = ""A Test PDF"">
	<meta name = ""author"" content = ""Me"">
	<link rel = ""stylesheet"" href = ""css/styles.css?v=1.0""> 
</head>
	<body>";
            
            //END of HTML Body and file
            var htmlDocBodyEnd = @"
	</body>
</html>";
            
            if (!File.Exists(pdfPath))
            {
                Console.WriteLine("Can't find PDF file: " + pdfPath);
                System.Environment.Exit(2);
            }

            
            string fileName = Path.GetFileNameWithoutExtension(pdfPath);
            string rootPath = Path.GetDirectoryName(pdfPath);
            string outputPath = rootPath + "\\" + fileName + "\\";
            string outputFile = fileName + ".html";
            
            System.IO.Directory.CreateDirectory(outputPath);

            PdfReader pdfReader = new PdfReader(pdfPath);
            PdfDocument pdfDoc = new PdfDocument(pdfReader);

            Console.WriteLine($"Number of pdf {pdfDoc.GetNumberOfPdfObjects()} objects");

            File.WriteAllText(outputPath + outputFile, htmlDocHeadBodyStart);

            //Set up listener based on iText7 to handle objects found in the PDF document
            IEventListener strategyListener = new ImageRenderListener(Path.Combine(@"image{0}.{1}"), outputPath, outputFile);

            //Set up a parser for the PDF document
            PdfCanvasProcessor parser = new PdfCanvasProcessor(strategyListener);

            //Walk thru the pages of the PDF document and pass the objects to the PDF listener
            for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
            {
                parser.ProcessPageContent(pdfDoc.GetPage(page));
                File.AppendAllText(outputPath + outputFile, "</BR>Page: " + page + " of " + pdfDoc.GetNumberOfPages() + "</br><hr></br>");
            }

            pdfDoc.Close();
            pdfReader.Close();

            File.AppendAllText(outputPath + outputFile, htmlDocBodyEnd);
        }
    }

    public class ImageRenderListener : IEventListener
    {
        string format;
        string path;
        string fileName;
        int index = 0;

        public ImageRenderListener(string format, string path, string fileName)
        {
            this.format = format;
            this.path = path;
            this.fileName = fileName;
        }

        public void EventOccurred(IEventData data, EventType type)
        {
            if (data is ImageRenderInfo imageData)
            {
                try
                {
                    //Process images found on the pages
                    PdfImageXObject imageObject = imageData.GetImage();
                    if (imageObject == null)
                    {
                        Console.WriteLine("Image could not be read.");
                    }
                    else
                    {
                        //Save image data to file in output directory
                        File.WriteAllBytes(path + string.Format(format, index, imageObject.IdentifyImageFileExtension()), imageObject.GetImageBytes());
                        //Append image tag and file name to output file
                        File.AppendAllText(path + fileName, "<img src=\"");
                        File.AppendAllText(path + fileName, string.Format(format, index, imageObject.IdentifyImageFileExtension()));
                        File.AppendAllText(path + fileName, "\">");
                        index++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Image could not be read: {0}.", ex.Message);
                }

            }

            if (data is TextRenderInfo textRender)
            {
                string pdfText = textRender.GetText();
                if (pdfText != null)
                {
                    File.AppendAllText(path + fileName, pdfText);
                }
            }
            else if (type == EventType.BEGIN_TEXT) //At start of text event append begining paragraph tag to output file
            {
                File.AppendAllText(path + fileName, "<p>");
            }
            else if (type == EventType.END_TEXT) //At end of text even append end of paragraph tag to output file
            {
                File.AppendAllText(path + fileName, "</p>");
            }
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            return null;
        }
    }
}