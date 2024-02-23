using Standard;
using System.Collections.Concurrent;
using System.IO;

Standard.File _File = new Standard.File();

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

Task.Run(() => FileEvent());

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

void FileEvent()
{
    while (!_Queue_FileEvent.IsCompleted)
    {
        FileEvent _FileEvent = _Queue_FileEvent.Take();

        //Console.WriteLine("FEO: Take() -> {0} {1} {2}", _FileEvent.FullPath, _FileEvent.Action, _FileEvent.NameNew);

        Thread.Sleep(0000);

        string _Action = "";
        string _Argument1 = "";
        string _Argument2 = "";

        switch (_FileEvent.Action)
        {
            case "D":
                _Action = "delete";
                _Argument1 = _FileEvent.FullPath.Replace(_Source + '\\', _Target + '\\');

                if (_File.IsFolder(_Argument1))
                {
                    DirectoryInfo _DirectoryInfo = new DirectoryInfo(_Argument1);

                    bool _Success = false;

                    while (_Success == false)
                    {
                        try
                        {
                            _DirectoryInfo.Delete();
                            _Success = true;
                        }
                        catch (Exception E)
                        {
                        }
                    }
                }
                else
                {
                    FileInfo _FileInfoD = new FileInfo(_Argument1);

                    if (_FileInfoD.Exists)
                    {
                        while (_File.IsLocked(_Argument1))
                        {
                        }

                        bool _Success = false;

                        while (_Success == false)
                        {
                            try
                            {
                                _FileInfoD.Delete();
                                _Success = true;
                            }
                            catch (Exception E)
                            {
                            }
                        }
                    }

                }
                break;

            case "R":
                _Action = "rename";
                _Argument1 = _FileEvent.FullPath.Replace(_Source + '\\', _Target + '\\');
                _Argument2 = _FileEvent.NameNew.Replace(_Source + '\\', _Target + '\\');

                if (_File.IsFolder(_Argument1))
                {
                    DirectoryInfo _DirectoryInfo = new DirectoryInfo(_Argument1);

                    bool _Success = false;

                    while (_Success == false)
                    {
                        try
                        {
                            _DirectoryInfo.MoveTo(_Argument2);
                            _Success = true;
                        }
                        catch (Exception E)
                        {
                        }
                    }
                }
                else
                {
                    FileInfo _FileInfoR = new FileInfo(_Argument1);
                    if (_FileInfoR.Exists)
                    {
                        while (_File.IsLocked(_Argument1))
                        {
                        }

                        bool _Success = false;

                        while (_Success == false)
                        {
                            try
                            {
                                _FileInfoR.MoveTo(_Argument2);
                                _Success = true;
                            }
                            catch (Exception E)
                            {
                            }
                        }
                    }
                }
                break;

            case "C":

                _Argument1 = _FileEvent.FullPath;

                if (_File.IsFolder(_Argument1))
                {
                    _Action = "create";
                    _Argument1 = _FileEvent.FullPath.Replace(_Source + '\\', _Target + '\\');

                    bool _Success = false;

                    while (_Success == false)
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(_Argument1);
                            _Success = true;
                        }
                        catch (Exception E)
                        {
                        }
                    }
                }
                else
                {
                    FileInfo _FileInfoC = new FileInfo(_Argument1);
                    if (_FileInfoC.Exists)
                    {
                        while (_File.IsLocked(_Argument1))
                        {
                        }

                        _Action = "copy";
                        _Argument2 = _FileEvent.FullPath.Replace(_Source + '\\', _Target + '\\');

                        Directory.CreateDirectory(Path.GetDirectoryName(_Argument2));

                        bool _Success = false;

                        while (_Success == false)
                        {
                            try
                            {
                                System.IO.File.Copy(_Argument1, _Argument2, true);
                                _Success = true;
                            }
                            catch (Exception E)
                            {
                            }
                        }
                    }
                }

                break;

            default:
                break;
        }

        Console.WriteLine("{0} {1} {2}", _Action, _Argument1, _Argument2);
    }

    _FileAudit.CLI("FEM");
}
