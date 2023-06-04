using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Archiver
{
    public static class EncodingHelper
    {
        public static Encoding DetectEncoding(string fileName)
        {
            // Read the file contents as text
            string fileContents;

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Seek(1, SeekOrigin.Begin);
                byte[] buffer = new byte[50];
                fileStream.Read(buffer, 0, buffer.Length);

                Encoding detectedEncoding = DetectEncoding(buffer);

                // Display the detected encoding
                Console.WriteLine("Detected Encoding: " + detectedEncoding.EncodingName);

                return detectedEncoding;
            }
        }

        static Encoding DetectEncoding(byte[] bytes)
        {
            // Create a memory stream from the byte array
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                // Create a StreamReader to read from the memory stream
                using (StreamReader streamReader = new StreamReader(memoryStream, true))
                {
                    // Read a few characters to detect the encoding
                    streamReader.Peek();

                    // Return the detected encoding
                    return streamReader.CurrentEncoding;
                }
            }
        }
    }
}
