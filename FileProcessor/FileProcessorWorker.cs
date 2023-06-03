using System.Diagnostics;
using Archiver;
using Statistics;
using System.Text.RegularExpressions;
using FileProcessor.Model;
using Archiver.Model;
using System.Text;

namespace FileProcessor
{
    public static class FileProcessorWorker
    {
        private static readonly int bufferSize = 8192; // Buffer size for reading file portions
        private static readonly int maxThreads = Environment.ProcessorCount; // Number of threads to use for parallel reading
        public static void ProcessFile(ReverseData reverserData)
        {
            string inputFileName = Path.GetFileName(reverserData.InputFile);

            // Step 1: Open the file specified by `fileName`
            if (!File.Exists(reverserData.InputFile))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            try
            {
                var ArchiverHelper = new ArchiverHelper();

                int totalLexemeCount = 0;
                CompresionResult? compresionResult = null;

                // Start the stopwatch
                Stopwatch stopwatch = Stopwatch.StartNew();

                // Step 2: Determine the size of the file
                var fileInfo = new FileInfo(reverserData.InputFile);
                long fileSize = fileInfo.Length;
                reverserData.ExtensionType = fileInfo.Extension;

                Encoding EncodingType = EncodingHelper.DetectEncoding(fileInfo);

                // Step 3: Calculate the number of threads to be used for parallel reading
                int threadCount = Math.Min(maxThreads, (int)Math.Ceiling((double)fileSize / bufferSize));

                // Step 4: Divide the file into equal portions based on the number of threads
                long portionSize = fileSize / threadCount;
                List<string> reversedTextPortions = new List<string>(threadCount);
                List<Thread> threads = new List<Thread>(threadCount);

                // Step 5: For each portion
                for (int i = 0; i < threadCount; i++)
                {
                    long startPosition = i * portionSize;
                    long endPosition = (i == threadCount - 1) ? fileSize : startPosition + portionSize;

                    // Step 5a: Initialize a thread to read the portion of the file in parallel
                    Thread thread = new Thread(() =>
                    {
                        int lexemeCount = 0;
                        // Step 5b: Read the portion of the file using buffering techniques
                        string portionText = ReadFilePortion(reverserData.InputFile, startPosition, endPosition);

                        // Step 5c: Reverse the text of the portion based on `reverseType`
                        string reversedText = ReverseText(portionText, reverserData.ReverseType, reverserData.RemoveSigns, out lexemeCount);

                        // Step 5d: Call the function for archiving and writing the reversed text portion
                        if(threadCount > 1)
                        {
                            ArchiverHelper.WriteInTempFile(reversedText, reverserData.ExtensionType, EncodingType, startPosition, endPosition);
                        }
                        else
                        {
                            ArchiverHelper.ArchiveAndWrite(reverserData.ReverseType, reversedText, reverserData.ExtensionType, EncodingType, reverserData.ArchiveType, reverserData.OutputFile, out compresionResult);
                        }
                        
                        totalLexemeCount += lexemeCount;
                    });

                    threads.Add(thread);
                }

                // Step 6: Wait for all threads to complete
                foreach (Thread thread in threads)
                {
                    thread.Start();
                }

                foreach (Thread thread in threads)
                {
                    thread.Join();
                }

                // Step 7: Merge all portions of the reversed text in the archive or write directly to a file
                // ... Implement the archiving and writing logic here ...

                // Step 8: Write the statistics of the reverse operation to a separate file
                StatisticsHelper.WriteStatistics(reversedTextPortions, reverserData.OutputFile);

                //ArchiverHelper.ArchiveAndWrite(reversedText, reverserData.ArchiveType, OutputFile);
                //Merge all portions of the text
                if (threadCount > 1)
                {
                    ArchiverHelper.ArchiveAndWriteParalel(reverserData.ReverseType, reverserData.ArchiveType, reverserData.OutputFile, reverserData.ExtensionType, EncodingType, out compresionResult);
                }
                // Step 9: Calculate the total lexeme count and total time taken
                // Stop the stopwatch      
                stopwatch.Stop();

                // Get the elapsed time in milliseconds
                TimeSpan totalTimeTaken = TimeSpan.FromSeconds(stopwatch.ElapsedMilliseconds);

                // Step 10: Write the overall statistics to the statistics file
                StatisticsHelper.WriteOverallStatistics(totalLexemeCount, totalTimeTaken, reverserData.OutputFile, compresionResult);

                // Step 11: Display the completion message
                Console.WriteLine("File processing completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during file processing: " + ex.Message);
            }
        }

        public static void ProcessDirectory(ReverseData reverserData, Action<double> updateProgress)
        {
            string[] fileNames = Directory.GetFiles(reverserData.InputDirectory);
            string oneDirectoryBackPath = Path.GetDirectoryName(reverserData.InputDirectory);
            string inputDirectoryName = Path.GetFileName(reverserData.InputDirectory);
            string outputDirectoryPath = Path.Combine(oneDirectoryBackPath, $"i{reverserData.ReverseType[0]}_{inputDirectoryName}_{DateTime.Now.Second}");
            Directory.CreateDirectory(outputDirectoryPath);

            double progressStep = 1.0d / fileNames.Length;
            
            foreach (string fileNameWithPath in fileNames)
            {
                //Task.Delay(100);
                string fileName = Path.GetFileName(fileNameWithPath);
                string tempOutputFileName = $"i{reverserData.ReverseType[0]}_{fileName}";
                reverserData.OutputFile = Path.Combine(outputDirectoryPath, tempOutputFileName);
                reverserData.InputFile = Path.Combine(reverserData.InputDirectory, fileName);
                ProcessFile(reverserData);

                updateProgress(progressStep);
            }
        }

        public static string ReadFilePortion(string fileName, long startPosition, long endPosition)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Seek(startPosition, SeekOrigin.Begin);
                byte[] buffer = new byte[(endPosition - startPosition) + 1];
                fileStream.Read(buffer, 0, buffer.Length);
                return Encoding.Default.GetString(buffer);
            }
        }


        public static string ReverseText(string text, string reverseType, bool removeSigns, out int lexemeCount)
        {
            //text = text.Replace("\n", "");

            switch (reverseType)
            {
                case "char":
                    char[] charArray = text.ToCharArray();
                    List<char> newCharArray = new List<char>();

                    for (int i = charArray.Length - 1; i >= 0; i--)
                    {
                        if (charArray[i] != '\u0000')
                        {
                            if (charArray[i] == '\n' && i - 1 >= 0 && charArray[i - 1] == '\r')
                            {
                                newCharArray.Add('\r');
                                newCharArray.Add('\n');
                                i--;
                            }
                            else
                            {
                                newCharArray.Add(charArray[i]);
                            }
                        }
                    }

                    lexemeCount = charArray.Length;
                    
                    return new string(newCharArray.ToArray());
                    
                    //Array.Reverse(charArray);
                    
                    //return new string(charArray);

                case "word":
                    Regex regexForWords = new Regex(@"[^\p{L}]*\p{Z}[^\p{L}]*");
                    Regex regexSplitByWords = new Regex(@"\p{Z}+");
                    string[] words;
                    
                    if (removeSigns)
                    {
                        words = regexForWords.Replace(text, " ").Split(' ');
                    }
                    else
                    {
                        words = regexSplitByWords.Split(text);
                    }
                    Array.Reverse(words);
                    lexemeCount = words.Count();
                    return string.Join(" ", words);

                case "sentence":
                    // Use regular expression to match sentences with their end signs
                    Regex regexForSentence = new Regex(@"(?<=[\.!\?])\s+");
                    var matches = regexForSentence.Split(text);
                    Array.Reverse(matches);
                    lexemeCount = matches.Count();
                    return string.Join(" ", matches);

                default:
                    throw new ArgumentException("Invalid reverse type.");
            }
        }
    }

}

