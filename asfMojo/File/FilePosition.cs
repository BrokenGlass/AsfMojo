
namespace AsfMojo.File
{
    /// <summary>
    /// The position within a media file and the delta to the requested position
    /// </summary>
    public class FilePosition
    {
        public string FileName { get; private set; }
        public FileMediaType MediaType { get; private set; }

        public long FileOffset { get; private set; }
        public uint TimeOffset { get; private set; }
        public int Delta { get; private set; }

        public FilePosition(string fileName, uint timeOffset, long fileOffset, FileMediaType mediaType = FileMediaType.Video, int delta=0)
        {
            FileName = fileName;
            TimeOffset = timeOffset;
            FileOffset = fileOffset;
            MediaType = mediaType;
            Delta = delta;
        }
    }
}
