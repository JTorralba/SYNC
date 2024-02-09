using Standard;
using System.Xml.Linq;

List<FileEvent> _FileEvents = new List<FileEvent>();

FileAudit _FileAudit = new FileAudit(ref _FileEvents);

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
        switch (_Command.ToUpper())
        {
            case ".":
                foreach (var _FileEvent in _FileEvents)
                {
                    Console.WriteLine(_FileEvent.FullPath + ' ' + _FileEvent.Action + ' ' + _FileEvent.FullPathNew);
                }
                break;
            case "X":
                Environment.Exit(0);
                break;
            default:
                _FileAudit.CLI(_Command);
                break;
        }
    }
} while (_Command != null);
