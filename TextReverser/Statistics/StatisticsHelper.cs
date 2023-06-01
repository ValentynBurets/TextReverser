using Archiver.Model;

namespace TextReverser.Statistics
{
    public static class StatisticsHelper
    {
        public static void WriteStatistics(List<string> reversedTextPortions, string outputFile)
        {
            string directoryPath = Path.GetDirectoryName(outputFile);

            using (StreamWriter writer = new StreamWriter(directoryPath + "/porsion_statistics.txt"))
            {
                int portionIndex = 1;
                foreach (string portion in reversedTextPortions)
                {
                    int lexemeCount = GetLexemeCount(portion);
                    TimeSpan timeTaken = GetTimeTaken(portion);
                    writer.WriteLine($"Portion {portionIndex}: Lexeme Count: {lexemeCount}, Time Taken: {timeTaken}");
                    portionIndex++;
                }
            }
        }

        public static int GetLexemeCount(string text)
        {
            // Implement the logic to calculate the lexeme count based on the reverse type
            return text.Length;
        }

        public static TimeSpan GetTimeTaken(string text)
        {
            // Implement the logic to calculate the time taken to reverse the text portion
            return TimeSpan.FromSeconds(1);
        }

        public static int CalculateTotalLexemeCount(List<string> reversedTextPortions)
        {
            int totalLexemeCount = 0;
            foreach (string portion in reversedTextPortions)
            {
                totalLexemeCount += GetLexemeCount(portion);
            }
            return totalLexemeCount;
        }

        public static TimeSpan CalculateTotalTimeTaken(List<string> reversedTextPortions)
        {
            TimeSpan totalTimeTaken = TimeSpan.Zero;
            foreach (string portion in reversedTextPortions)
            {
                totalTimeTaken += GetTimeTaken(portion);
            }
            return totalTimeTaken;
        }

        public static void WriteOverallStatistics(int totalLexemeCount, TimeSpan totalTimeTaken, string outputFile, CompresionResult? compresionResult)
        {
            string directoryPath = Path.GetDirectoryName(outputFile);
            using (StreamWriter writer = new StreamWriter(directoryPath + "/overall_statistics.txt", false))
            {
                writer.WriteLine($"Total Lexeme Count: {totalLexemeCount}");
                writer.WriteLine($"Total Time Taken: {totalTimeTaken}");

                if(compresionResult != null)
                {
                    writer.WriteLine($"Total file size is: {compresionResult.Value.TextFileSize}");

                    writer.WriteLine($"Total compresed file size is: {compresionResult.Value.CompresedFileSize}");
                }
            }
        }
    }
}