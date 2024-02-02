using System;

namespace Record
{
    public class FileEvent
    {
        public String FullPath;
        public String Action;
        public String FullPathNew;

        public FileEvent(String _FullPath, String _Action, String _FullPathNew)
        {
            this.FullPath = _FullPath;
            this.Action = _Action;
            this.FullPathNew = _FullPathNew;
        }
    }

    public class FileMemo
    {
        public String Hash;
        public long Size;
        public DateTime Modified;

        public FileMemo(String _Hash, long _Size, DateTime _Modified)
        {
            this.Hash = _Hash;
            this.Size = _Size;
            this.Modified = _Modified;
        }
    }
}
