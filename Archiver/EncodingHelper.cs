using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Archiver
{
    public static class EncodingHelper
    {
        public static Encoding DetectEncoding(FileInfo fileInfo)
        {
            // Read the file contents as text
            string fileContents;
            using (StreamReader streamReader = new StreamReader(fileInfo.OpenRead(), true))
            {
                fileContents = streamReader.ReadToEnd();
            }

            // Detect the encoding type based on the byte order mark (BOM)
            Encoding encoding = Encoding.Default;
            if (fileContents.Length >= 2 && fileContents[0] == 0xFEFF)
            {
                encoding = Encoding.Unicode;
            }
            else if (fileContents.Length >= 2 && fileContents[0] == 0xFFFE)
            {
                encoding = Encoding.BigEndianUnicode;
            }
            else if (fileContents.Length >= 3 && fileContents[0] == 0xEF && fileContents[1] == 0xBB && fileContents[2] == 0xBF)
            {
                encoding = Encoding.UTF8;
            }

            return encoding;
        }
    }
}
