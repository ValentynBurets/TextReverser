using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

namespace FileProcessor.Model
{
    public class ReverseData
    {
        public ReverseData()
        {
            ReverseType = "char";
            ArchiveType = "none";
            InputFile = "";
            OutputFile = "";
            InputDirectory = "";
            AdditionalSigns = "";
            ExtensionType = "";
        }
        public string ReverseType { get; set; }
        public string ArchiveType { get; set; }
        public string InputFile { get; set; }
        public string InputDirectory { get; set; }
        public string OutputFile { get; set; }
        public bool RemoveSigns { get; set; }
        public string AdditionalSigns { get; set; }
        public string ExtensionType { get; set; }
    }

    [JsonSerializable(typeof(ReverseData))]
    public sealed partial class ReverseDataContext : JsonSerializerContext
    {

    }
}
