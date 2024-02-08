using Standard;

FileAudit _FileAudit = new FileAudit();

FileSystemWatcher _FileSystemWatcher = new FileSystemWatcher(@"C:\Users\JTorralba\Desktop", filter: "*");

_FileSystemWatcher.Created += _FileAudit.Created;
_FileSystemWatcher.Changed += _FileAudit.Changed;
_FileSystemWatcher.Deleted += _FileAudit.Deleted;
_FileSystemWatcher.Renamed += _FileAudit.Renamed;

_FileSystemWatcher.EnableRaisingEvents = true;
_FileSystemWatcher.IncludeSubdirectories = true;

String _Command;

do
{
    _Command = Console.ReadLine();
    if (_Command != null)
    {
        _FileAudit.CLI(_Command);
    }
} while (_Command != null);
