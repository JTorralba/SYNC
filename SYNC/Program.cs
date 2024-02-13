using Standard;
using System.Collections.Concurrent;

string _Source = @Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + '\\' + "Desktop";
string _Target = @_Source + ".SYNC";

BlockingCollection<FileEvent> _Queue_FileEvent = new BlockingCollection<FileEvent>();

FileAudit _FileAudit = new FileAudit(ref _Queue_FileEvent);

FileSystemWatcher _FileSystemWatcher = new FileSystemWatcher(_Source, filter: "*");

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
            case "FEA":
                foreach (var _FileEvent in _Queue_FileEvent)
                {
                    Console.WriteLine(_FileEvent.FullPath + ' ' + _FileEvent.Action + ' ' + _FileEvent.NameNew);
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
