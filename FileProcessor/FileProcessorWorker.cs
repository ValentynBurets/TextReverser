using System.Diagnostics;
using Archiver;
using Statistics;
using System.Text.RegularExpressions;
using FileProcessor.Model;
using Archiver.Model;
using System.Text;
using SharpCompress.Common;
using System.Collections.Concurrent;
using System;

namespace FileProcessor
{
    public static class FileProcessorWorker
    {
        private static readonly int bufferSize = 8192; // Buffer size for reading file portions
        private static readonly int maxThreads = Environment.ProcessorCount; // Number of threads to use for parallel reading
        public static BlockingCollection<double> reversalTimeOfTenKiloBites = new BlockingCollection<double>();
        private static Mutex mutexTimeSync = new Mutex();

        public static void ProcessFile(ReverseData reverserData, Action<long> updateFileSizeLeft, Func<double>? updateProgress, Stopwatch stopWatchProces, Action<TimeSpan> updateTimeSpent)
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

                Encoding EncodingType = EncodingHelper.DetectEncoding(reverserData.InputFile);

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
                        Stopwatch stopWatchPortionTime = Stopwatch.StartNew();

                        int lexemeCount = 0;
                        // Step 5b: Read the portion of the file using buffering techniques
                        string portionText = ReadFilePortion(reverserData.InputFile, startPosition, endPosition);

                        // Step 5c: Reverse the text of the portion based on `reverseType`
                        string reversedText = ReverseText(portionText, reverserData.ReverseType, reverserData.RemoveSigns, out lexemeCount, reverserData.AdditionalSigns);

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
                        
                        stopWatchPortionTime.Stop();
                        updateFileSizeLeft(portionSize);

                        double timeSpentToReverse = (double)stopWatchPortionTime.ElapsedMilliseconds * 1024 / portionSize;
                        reversalTimeOfTenKiloBites.Add(timeSpentToReverse);

                        updateTimeSpent(stopWatchProces.Elapsed);
                        
                        if (updateProgress != null)
                        {
                            updateProgress();
                        }
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
                // StatisticsHelper.WriteStatistics(reversedTextPortions, reverserData.OutputFile);

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
                TimeSpan totalTimeTaken = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);

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

        public static async Task ProcessDirectory(ReverseData reverserData, Func<double>? updateProgress, Action<TimeSpan> updateTimeLeftLable, Stopwatch stopWatchProces, Action<TimeSpan> updateTimeSpent)
        {
            string[] fileNames = Directory.GetFiles(reverserData.InputDirectory);
            string oneDirectoryBackPath = Path.GetDirectoryName(reverserData.InputDirectory);
            string inputDirectoryName = Path.GetFileName(reverserData.InputDirectory);
            string outputDirectoryPath = Path.Combine(oneDirectoryBackPath, $"I{reverserData.ReverseType[0]}_{inputDirectoryName}");

            int index = 1;
            var tempFolderPath = new StringBuilder(outputDirectoryPath);

            while (Directory.Exists(Path.Combine(oneDirectoryBackPath, tempFolderPath.ToString())))
            {
                index++;
                tempFolderPath.Clear();
                tempFolderPath.Append(outputDirectoryPath + "_" + index);
            }

            if(index >= 2)
            {
                outputDirectoryPath += "_" + index;
            }

            Directory.CreateDirectory(outputDirectoryPath);
            
            DirectoryInfo directoryInfo = new DirectoryInfo(reverserData.InputDirectory);
            long totalSizeLeft = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);

            Action<long> updateFileSizeLeft = (long portionSize) =>
            {
                totalSizeLeft -= portionSize;

                mutexTimeSync.WaitOne();

                long reversalTimeOfTenKiloBitesCount = reversalTimeOfTenKiloBites.Count;
                if (reversalTimeOfTenKiloBitesCount % 300 == 0 && reversalTimeOfTenKiloBitesCount > 100)
                {
                    double averageValue;

                    if (reversalTimeOfTenKiloBitesCount < 2000)
                    {
                        averageValue = reversalTimeOfTenKiloBites
                                                    .Where(item => !double.IsInfinity(item))
                                                    .Average();
                    }
                    else
                    {
                        averageValue = reversalTimeOfTenKiloBites
                                                    .Skip(reversalTimeOfTenKiloBites.Count - 1000).Take(1000)
                                                    .Where(item => !double.IsInfinity(item))
                                                    .Average();
                    }

                    TimeSpan averageTimeSpan = TimeSpan.FromMilliseconds(averageValue * (totalSizeLeft / 1024));

                    updateTimeLeftLable(averageTimeSpan);
                }
                mutexTimeSync.ReleaseMutex();
            };


            foreach (string fileNameWithPath in fileNames)
            {
                string currentFileNameWithPath = fileNameWithPath;
                string fileName = Path.GetFileName(currentFileNameWithPath);
                string tempOutputFileName = $"I{reverserData.ReverseType[0]}_{fileName}";
                reverserData.OutputFile = Path.Combine(outputDirectoryPath, tempOutputFileName);
                reverserData.InputFile = Path.Combine(reverserData.InputDirectory, fileName);
                await Task.Run(() => { ProcessFile(reverserData, updateFileSizeLeft, null, stopWatchProces, updateTimeSpent); });
                
                if(updateProgress != null)
                {
                    updateProgress();
                }
            }

            reversalTimeOfTenKiloBites = new BlockingCollection<double>();
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


        public static string ReverseText(string text, string reverseType, bool removeSigns, out int lexemeCount, string additionalSigns)
        {
            switch (reverseType)
            {
                case "char":
                { 
                    if (removeSigns)
                    {
                        Regex regexForPunctuation = new Regex(@"\p{P}");

                        string textWithoutSigns = regexForPunctuation.Replace(text, "");

                        char[] resCharArray = textWithoutSigns.ToCharArray();
                        Array.Reverse(resCharArray);
                        lexemeCount = resCharArray.Length;

                        return string.Join("", resCharArray);
                    }
                    else
                    {
                        char[] charArray = text.ToCharArray();
                        List<char> newCharArray = new List<char>();

                        for (int i = charArray.Length - 1; i >= 0; i--)
                        {
                            if (charArray[i] != '\u0000')
                            {
                                if (charArray[i] == '\n' && i - 1 >= 0 && charArray[i - 1] == '\r')
                                {
                                    newCharArray.AddRange(Environment.NewLine);
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
                    }
                }
                case "word":
                {
                    //regular exprethion for deleting sings
                    Regex regexForWords = new Regex(@"[^\p{L}\p{IsCJKUnifiedIdeographs}]+");

                    //regular expression for splitting into words
                    Regex regexSplitByWords = new Regex(@"(\S+[\p{P}\s]*)|[\t]+|[\r\n]+");

                    //regular expretion for splitiong into words
                    Regex regexSplitByLineBreak = new Regex(@"[\r\n]+");

                    //regular expretion for splitiong into words
                    Regex regexSplitByTab = new Regex(@"[\t]+");

                    //regular expretion for detecting non-letter signs
                    Regex regexNonLetter = new Regex(@"[a-zA-Z]");

                    //end of sentence or punctuation marck
                    var rexExpForPunctuationAndSentenceTepmString = new StringBuilder("");
                    rexExpForPunctuationAndSentenceTepmString.Append(@"[.!?…⁉️⁈‼️⁇,:;\r\n\t\s");
                    rexExpForPunctuationAndSentenceTepmString.Append(additionalSigns.Replace(" ", ""));
                    rexExpForPunctuationAndSentenceTepmString.Append("]+");
                    Regex regexForPunctuationAndSentence = new Regex(@rexExpForPunctuationAndSentenceTepmString.ToString());

                    //letters, numbers and signs(end of sentence or punctuation marcks)
                    var regexForSignsAndLettersTempString = new StringBuilder("");
                    regexForSignsAndLettersTempString.Append(@"[.!?…⁉️⁈‼️⁇,:;\r\n\t\s-_0-9'`’""\t]+|[");
                    regexForSignsAndLettersTempString.Append(additionalSigns.Replace(" ", ""));
                    regexForSignsAndLettersTempString.Append("]+|");
                    regexForSignsAndLettersTempString.Append("[a-zA-Z]+|");

                    regexForSignsAndLettersTempString.Append(@"[\p{IsArabic}\p{IsCJKUnifiedIdeographs}\p{IsCyrillic}\p{IsHebrew}\p{IsHiragana}\p{IsKatakana}\p{IsDevanagari}\p{IsTelugu}\p{IsThai}\p{IsGreek}\p{L}]+");

                    Regex regexForSignsAndLetters = new Regex(regexForSignsAndLettersTempString.ToString());

                    List<string> words = new List<string>();

                    if (removeSigns)
                    {
                        words.AddRange(regexForWords.Replace(text, "").Split(' '));
                    }
                    else
                    {
                        words.AddRange(regexSplitByWords.Split(text));
                        words.RemoveAll(string.IsNullOrWhiteSpace);
                    }

                    List<string> wordesInverted = new List<string>();
                    for (int i = words.Count - 1; i >= 0; i--)
                    {
                        StringBuilder tempWord = new StringBuilder(string.Join("", regexForSignsAndLetters.Matches(words[i])));
                        //String tempWord = String.Join("", regexForSignsAndLetters.Matches(words[i]));
                        var matchSignsFound = regexForPunctuationAndSentence.Matches(tempWord.ToString());

                        if (matchSignsFound != null && matchSignsFound.Count >= 1)
                        {
                            if(matchSignsFound.Count == 1)
                            {
                                BackSwipe(tempWord, matchSignsFound[0].Value);
                            }
                            if(matchSignsFound.Count == 2)
                            {
                                FrontSwipe(tempWord, matchSignsFound[0].Value);
                                BackSwipe(tempWord, matchSignsFound[1].Value);
                            }
                        }

                        wordesInverted.Add(tempWord.ToString());
                    }

                    lexemeCount = words.Count();
                    string wordRes = string.Join("", wordesInverted);

                    return wordRes.Replace("\u0000", "");
                }
                case "sentence":
                {
                    // Use regular expression to match sentences with their end signs
                    Regex regexForSentence = new Regex(@"[\.!\?…⁉️⁈‼️⁇]");
                    var matches = regexForSentence.Split(text);
                    Array.Reverse(matches);
                    lexemeCount = matches.Count();
                    string sentencesRes = string.Join(" ", matches);

                    return sentencesRes.Replace("\u0000", "");
                }
                default:
                    throw new ArgumentException("Invalid reverse type.");
            }
        }

        private static void FrontSwipe(StringBuilder tempWord, string matchSignsList)
        {
            if (matchSignsList != null && matchSignsList.Length > 0)
            {
                int tempEndIndex = tempWord.Length - 2;
                int tempWordSize = tempWord.Length - 1;

                for (int matchArrayIndex = matchSignsList.Length - 1, singsSwipedCount = 0; matchArrayIndex >= 0; matchArrayIndex--, singsSwipedCount++)
                {
                    int indexOfSign = tempWord.ToString().IndexOf(matchSignsList[matchArrayIndex].ToString());

                    if (indexOfSign >= tempEndIndex)
                    {
                        tempWord.Remove(indexOfSign, 1);
                        tempWord.Insert(tempWordSize - indexOfSign + singsSwipedCount, matchSignsList[matchArrayIndex].ToString());
                        tempEndIndex--;
                    }
                }
            }
        }
        
        private static void BackSwipe(StringBuilder tempWord, string matchSignsList)
        {
            if (matchSignsList != null && matchSignsList.Length > 0)
            {
                int tempEndIndex = tempWord.Length - 2;
                int tempWordSize = tempWord.Length - 1;

                for (int matchArrayIndex = matchSignsList.Length - 1, singsSwipedCount = 0; matchArrayIndex >= 0; matchArrayIndex--, singsSwipedCount++)
                {
                    int indexOfSign = tempWord.ToString().LastIndexOf(matchSignsList[matchArrayIndex].ToString());

                    if (indexOfSign >= tempEndIndex)
                    {
                        tempWord.Remove(indexOfSign, 1);
                        tempWord.Insert(tempWordSize - indexOfSign + singsSwipedCount, matchSignsList[matchArrayIndex].ToString());
                        tempEndIndex--;
                    }
                }
            }
        }
    }

}

