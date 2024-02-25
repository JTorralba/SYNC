using System.Collections.Concurrent;

using Standard;

class SYNC
{
    private static bool _X;
    private static DEBUG? _DEBUG;
    private static Standard.File? _File;
    private static FileAudit? _FileAudit;

    private static string? _Source;
    private static string? _Target;
    private static string? _Command;

    private static BlockingCollection<FileEvent>? _Queue_FileEvent;

    private static FileSystemWatcher? _FileSystemWatcher;

    private static void Main(string[] args)
    {
        _X = false;
        _DEBUG = new DEBUG();
         _File = new Standard.File();

        _Queue_FileEvent = new BlockingCollection<FileEvent>();
        _FileAudit = new FileAudit(ref _Queue_FileEvent);

        _Source = @Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + '\\' + "Desktop";
        _Target = @_Source + ".SYNC";
        _Command = string.Empty;

        _FileSystemWatcher = new FileSystemWatcher(_Source, filter: "*");

        _FileSystemWatcher.Created += _FileAudit.Created;
        _FileSystemWatcher.Changed += _FileAudit.Changed;
        _FileSystemWatcher.Deleted += _FileAudit.Deleted;
        _FileSystemWatcher.Renamed += _FileAudit.Renamed;

        _FileSystemWatcher.EnableRaisingEvents = true;
        _FileSystemWatcher.IncludeSubdirectories = true;

        Task.Run(() => FileEvent());

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
    }
    private static void FileEvent()
    {
        while (!_Queue_FileEvent.IsCompleted)
        {
            FileEvent _FileEvent = _Queue_FileEvent.Take();

            //Console.WriteLine("FEA: Take() -> {0} {1} {2}", _FileEvent.FullPath, _FileEvent.Action, _FileEvent.NameNew);

            string _Action = string.Empty;
            string _Argument1 = string.Empty;
            string _Argument2 = string.Empty;

            bool _Success = false;
            do
            {
                try
                {
                    switch (_FileEvent.Action)
                    {
                        case "C":
                            _Argument1 = _FileEvent.FullPath;
                            break;
                        case "D":
                            _Argument1 = _FileEvent.FullPath.Replace(_Source + '\\', _Target + '\\');
                            break;
                        case "R":
                            _Argument1 = _FileEvent.NameNew;
                            break;
                        default:
                            break;
                    }

                    if (_File.IsFolder(_Argument1))
                    {
                        _Argument1 = _FileEvent.FullPath.Replace(_Source + '\\', _Target + '\\');
                        DirectoryInfo _DirectoryInfo = new DirectoryInfo(_Argument1);

                        switch (_FileEvent.Action)
                        {
                            case "C":
                                _Action = "CREATE";
                                if (!Directory.Exists(_Argument1))
                                {
                                    Directory.CreateDirectory(_Argument1);
                                }
                                else
                                {
                                    _Action = "SKIP";
                                }
                                break;

                            case "D":
                                _Action = "DELETE";
                                if (Directory.Exists(_Argument1))
                                {
                                    _DirectoryInfo.Delete(true);
                                }
                                else
                                {
                                    _Action = "SKIP";
                                }
                                break;

                            case "R":
                                _Action = "RENAME";
                                _Argument2 = _FileEvent.NameNew.Replace(_Source + '\\', _Target + '\\');
                                if (Directory.Exists(_Argument1) && !Directory.Exists(_Argument2))
                                {
                                    _DirectoryInfo.MoveTo(_Argument2);
                                }
                                else
                                {
                                    _Action = "SKIP";
                                }

                                break;

                            default:
                                break;
                        }
                    }
                    else
                    {
                        FileInfo _FileInfo;

                        if (_FileEvent.Action == "C")
                        {
                            _Argument1 = _FileEvent.FullPath;
                        }
                        else
                        {
                            _Argument1 = _FileEvent.FullPath.Replace(_Source + '\\', _Target + '\\');
                        }

                        _FileInfo = new FileInfo(_Argument1);

                        if (_FileInfo.Exists)
                        {
                            switch (_FileEvent.Action)
                            {
                                case "C":
                                    _Action = "COPY";
                                    _Argument2 = _FileEvent.FullPath.Replace(_Source + '\\', _Target + '\\');
                                    if (!Directory.Exists(Path.GetDirectoryName(_Argument2)))
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(_Argument2));
                                    }
                                    System.IO.File.Copy(_Argument1, _Argument2, true);
                                    break;

                                case "D":
                                    _Action = "DELETE";
                                    _FileInfo.Delete();
                                    break;

                                case "R":
                                    _Action = "RENAME";
                                    _Argument2 = _FileEvent.NameNew.Replace(_Source + '\\', _Target + '\\');
                                    _FileInfo.MoveTo(_Argument2);
                                    break;

                                default:
                                    break;
                            }
                        }
                        else
                        {
                            _Action = "SKIP";
                        }
                    }

                    _Success = true;
                }
                catch (Exception _Exception)
                {
                    if (_X)
                    {
                        _DEBUG.Message("{0}", _Exception.Message.ToString());
                    }
                }

            } while (_Success != true);

            if (_Action != "SKIP")
            {
                Console.WriteLine("{0} {1} {2}", _Action, _Argument1, _Argument2);
            }
        }
    }
}