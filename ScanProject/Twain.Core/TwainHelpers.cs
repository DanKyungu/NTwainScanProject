using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanProject.Twain.Core
{
    public class TwainHelpers
    {

        public static string ConvertImageFromBmpToJpg(string bmpFile, string destinationPath, string destinationFileName)
        {
            var destPath = destinationPath;
            var destFile = destinationFileName;

            var bmpFileInfo = new FileInfo(bmpFile);

            if (!bmpFileInfo.Exists)
                return string.Empty;

            var fullConvertedImagePath = $"{Path.Combine(destPath, destFile)}.jpg";
            var destInfo = new FileInfo(fullConvertedImagePath);

            if (!Directory.Exists(destInfo.DirectoryName))
                Directory.CreateDirectory(destInfo.DirectoryName);

            var convertedImage = Image.FromFile(bmpFileInfo.FullName);
            FileInfo fileInfo = new FileInfo(fullConvertedImagePath);

            convertedImage.Save(fullConvertedImagePath, ImageFormat.Jpeg);
            //bmpFileInfo.Delete();

            return fullConvertedImagePath;
        }
    }
}
