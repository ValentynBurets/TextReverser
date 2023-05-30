namespace Archiver.Model
{
    internal struct PortionParams
    {
        public long StartPosition { get; set; }
        public long EndPosition { get; set; }
        public string FileName { get; set; }
        public string FolderPath { get; set; }
    }
}
