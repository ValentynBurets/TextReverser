using Archiver.Model;
using Aspose.Zip;
using Aspose.Zip.Saving;
using Aspose.Zip.SevenZip;
using Microsoft.VisualBasic;
using SharpCompress.Common;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Archiver
{
    public class ArchiverHelper
    {
        private readonly BlockingCollection<PortionParams> SavedTextPortions;

        public ArchiverHelper()
        {
            SavedTextPortions = new BlockingCollection<PortionParams>();
        }

        public void ArchiveAndWrite(string reversedText, string extension, Encoding encodingType, string archiveType, string outputFile, out CompresionResult? compresionResult)
        {
            if (!string.IsNullOrEmpty(archiveType))
            {
                string archiveName = $"{outputFile}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.{archiveType}";
                switch (archiveType.ToLower())
                {
                    case "zip":
                        compresionResult = CreateZipArchive(archiveName, reversedText, extension, encodingType).GetAwaiter().GetResult();
                        break;

                    case "7z":
                        compresionResult = Create7zArchive(archiveName, reversedText, extension, encodingType);
                        break;

                    case "none":
                        // Write the reversed text to a file
                        compresionResult = null;
                        WriteToFile(outputFile, reversedText, extension, encodingType);
                        break;

                    default:
                        throw new ArgumentException("Invalid archive type.");
                }
            }
            else
            {
                compresionResult = null;
                // Write the reversed text to a file
                WriteToFile(outputFile, reversedText, extension, encodingType);
            }
        }
        public void ArchiveAndWriteParalel(string archiveType, string outputFile, string extension, Encoding encodingType, out CompresionResult? compresionResult)
        {
            if (!string.IsNullOrEmpty(archiveType))
            {
                string archiveName = $"{outputFile}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.{archiveType}";

                switch (archiveType.ToLower())
                {
                    case "zip":
                        compresionResult = CreateZipArchiveParalel(archiveName, extension, encodingType);
                        break;

                    case "7z":
                        compresionResult = Create7zArchiveParalel(archiveName, extension, encodingType);
                        break;

                    case "none":
                        compresionResult = null;
                        // Write the reversed text to a file
                        WriteToFileParalel(outputFile, encodingType);
                        break;

                    default:
                        throw new ArgumentException("Invalid archive type.");
                }
            }
            else
            {
                compresionResult = null;
                // Write the reversed text to a file
                WriteToFileParalel(outputFile, encodingType);
            }
        }

        private async Task<CompresionResult> CreateZipArchive(string archiveFilePath, string text, string extension, Encoding encodingType)
        {
            var compresionResult = new CompresionResult();

            using (FileStream fs = new FileStream(archiveFilePath, FileMode.Create))
            {
                using (ZipArchive zipArchive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry = zipArchive.CreateEntry("reversed." + extension);
                    using (StreamWriter writer = new StreamWriter(entry.Open(), encodingType))
                    {
                        await writer.WriteAsync(text);
                    }
                }
            }

            compresionResult.CompresedFileSize = new FileInfo(archiveFilePath).Length;

            return compresionResult;
        }

        public CompresionResult CreateZipArchiveParalel(string archiveFilePath, string extension, Encoding encodingType)
        {
            // Create a folder to store the text file
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderName = Path.GetFileNameWithoutExtension(archiveFilePath);
            string folderFullPath = Path.Combine(folderPath, folderName);
            Directory.CreateDirectory(folderFullPath);

            string fileName = $"text_file." + extension;
            string filePath = Path.Combine(folderFullPath, fileName);

            WriteToFileParalel(filePath, encodingType);
            string archiveFullPath = folderFullPath + ".zip";
            
            // Create a zip archive with the folder
            using (var archive = new Archive())
            {
                archive.CreateEntries(folderFullPath);
                archive.Save(archiveFullPath);
            }
            var compresionResult = new CompresionResult()
            {
                TextFileSize = new FileInfo(filePath).Length,
                CompresedFileSize = new FileInfo(archiveFilePath).Length
            };

            File.Delete(filePath);
            Directory.Delete(folderFullPath);

            return compresionResult;
        }

        public CompresionResult Create7zArchive(string archiveFilePath, string text, string extension, Encoding encodingType)
        {
            // Create a folder to store the text file
            string folderPath = Path.GetDirectoryName(archiveFilePath);
            string folderName = Path.GetFileNameWithoutExtension(archiveFilePath);
            string folderFullPath = Path.Combine(folderPath, folderName);
            Directory.CreateDirectory(folderFullPath);

            // Write the text to a file within the folder
            string textFilePath = Path.Combine(folderFullPath, "text.txt");
            File.WriteAllText(textFilePath, text, encodingType);

            // Create a 7z archive with the folder
            var compresionSettings = new SevenZipLZMACompressionSettings();
            //compresionSettings.DictionarySize = int.MaxValue;
            using (var archive = new SevenZipArchive(new SevenZipEntrySettings(compresionSettings)))
            {
                archive.CreateEntries(folderFullPath);
                archive.Save(archiveFilePath);
            }

            var compresionResult = new CompresionResult()
            {
                TextFileSize = new FileInfo(textFilePath).Length,
                CompresedFileSize = new FileInfo(archiveFilePath).Length
            };

            File.Delete(textFilePath);
            Directory.Delete(folderFullPath);

            return compresionResult;
        }

        public CompresionResult Create7zArchiveParalel(string archiveFilePath, string extension, Encoding encodingType)
        {
            // Create a folder to store the text file
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderName = Path.GetFileNameWithoutExtension(archiveFilePath);
            string folderFullPath = Path.Combine(folderPath, folderName);
            Directory.CreateDirectory(folderFullPath);

            string fileName = $"text_file." + extension;
            string filePath = Path.Combine(folderFullPath, fileName);

            WriteToFileParalel(filePath, encodingType);
            string archiveFullPath = folderFullPath + ".7z";
            // Create a 7z archive with the folder
            using (var archive = new SevenZipArchive(new SevenZipEntrySettings(new SevenZipLZMACompressionSettings())))
            {
                archive.CreateEntries(folderFullPath);
                archive.Save(archiveFullPath);
            }

            var compresionResult = new CompresionResult()
            {
                TextFileSize = new FileInfo(filePath).Length,
                CompresedFileSize = new FileInfo(archiveFilePath).Length
            };

            File.Delete(filePath);
            Directory.Delete(folderFullPath);

            return compresionResult;
        }

        private void WriteToFile(string fileName, string text, string extension, Encoding encodingType)
        {
            using (StreamWriter writer = new StreamWriter(fileName, true, encodingType))
            {
                writer.Write(text);
            }
        }

        public void WriteToFileParalel(string fileName, Encoding encodingType)
        {
            var sortedSavedTextPortions = SavedTextPortions.OrderBy(item => item.EndPosition);

            using (StreamWriter writer = new StreamWriter(fileName, true, encodingType))
            {
                for (int i = sortedSavedTextPortions.Count() - 1; i >= 0; i--)
                {
                    var filePath = Path.Combine(sortedSavedTextPortions.ElementAt(i).FolderPath, sortedSavedTextPortions.ElementAt(i).FileName);

                    using (StreamReader reader = new StreamReader(filePath, encodingType))
                    {
                        string fileContent = reader.ReadToEnd();
                        writer.Write(fileContent);
                    }

                    // Delete the file
                    File.Delete(filePath);
                }
            }
            //Directory.Delete(sortedSavedTextPortions.ElementAt(0).FolderPath);       
        }

        public void WriteInTempFile(string reversedText, string extension, Encoding EncodingType, long startPosition, long endPosition)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "temp_files");
            string fileName = $"file_{startPosition}.{extension}";
            string filePath = Path.Combine(folderPath, fileName);

            Directory.CreateDirectory(folderPath);

            using (StreamWriter writer = new StreamWriter(filePath, true, EncodingType))
            {
                writer.Write(reversedText);
            }

            var portionParams = new PortionParams()
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                FileName = fileName,
                FolderPath = folderPath
            };

            SavedTextPortions.Add(portionParams);
        }
    }
}