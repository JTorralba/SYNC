using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Standard
{
    public class FileEvent
    {
        public string FullPath;
        public string Action;
        public string NameNew;

        public FileEvent(string _FullPath, string _Action, string _NameNew)
        {
            this.FullPath = _FullPath;
            this.Action = _Action;
            this.NameNew = _NameNew;
        }
    }

    public class FileMemo
    {
        public string Hash;
        public long Size;
        public DateTime Modified;

        public FileMemo(string _Hash, long _Size, DateTime _Modified)
        {
            this.Hash = _Hash;
            this.Size = _Size;
            this.Modified = _Modified;
        }
    }

    public class FileAudit
    {
        private bool _X;
        private DEBUG _DEBUG;
        private File _File;
        private Cryptology _Cryptology;

        private BlockingCollection<FileEvent> _Queue_FileEvent_Reference;
        private BlockingCollection<string> _Queue_FileEvents;
        private BlockingCollection<FileEvent> _Queue_FileEvent;

        private ConcurrentDictionary<string, FileMemo> _Dictionary_FileMemo;

        private string _NumericSize;

        public FileAudit(ref BlockingCollection<FileEvent> _Queue_FileEvent_Reference)
        {
            Initialize(ref _Queue_FileEvent_Reference, false);
        }

        public FileAudit(ref BlockingCollection<FileEvent> _Queue_FileEvent_Reference, bool _X)
        {
            Initialize(ref _Queue_FileEvent_Reference, _X);
        }

        private void Initialize(ref BlockingCollection<FileEvent> _Queue_FileEvent_Reference, bool _X)
        {
            this._X = _X;
            _DEBUG = new DEBUG();
            _File = new File(_X);
            _Cryptology = new Cryptology(_X);

            this._Queue_FileEvent_Reference = _Queue_FileEvent_Reference;
            _Queue_FileEvents = new BlockingCollection<string>();
            _Queue_FileEvent = new BlockingCollection<FileEvent>();

            _Dictionary_FileMemo = new ConcurrentDictionary<string, FileMemo>();

            _NumericSize = "D13";

            Task.Run(() => FileEvents());
            Task.Run(() => FileEvent());

            FileScan();
        }

        public void Created(Object _Object, FileSystemEventArgs _FileSystemEventArgs)
        {
            _Queue_FileEvents.Add(_FileSystemEventArgs.FullPath);
        }

        public void Changed(Object _Object, FileSystemEventArgs _FileSystemEventArgs)
        {
            _Queue_FileEvents.Add(_FileSystemEventArgs.FullPath);
        }

        public void Deleted(Object _Object, FileSystemEventArgs _FileSystemEventArgs)
        {
            //Reference Only ----------------------------------------------------------------------
            //
            //foreach (var _Event in _Queue_FileEvents)
            //{
            //    bool _ReQueue = false;

            //    string _ReQueue_Event = "";

            //    if (_Event.Contains(_FileSystemEventArgs.FullPath + '\\'))
            //    {
            //        _ReQueue = false;
            //    }
            //    else
            //    {
            //        if (_Event == _FileSystemEventArgs.FullPath)
            //        {
            //            _ReQueue = false;
            //        }
            //        else
            //        {
            //            _ReQueue = true;
            //            _ReQueue_Event = _FileSystemEventArgs.FullPath;
            //        }
            //    }

            //    if (_ReQueue)
            //    {
            //        _Queue_FileEvents.Add(_ReQueue_Event);
            //    }
            //}

            _Queue_FileEvent.Add(new FileEvent(_FileSystemEventArgs.FullPath, "D", ""));
        }

        public void Renamed(Object _Object, RenamedEventArgs _RenamedEventArgs)
        {
            foreach (var _Event in _Queue_FileEvents)
            {
                bool _ReQueue = false;

                string _ReQueue_Event = "";

                if (_Event.Contains(_RenamedEventArgs.OldFullPath + '\\'))
                {
                    _ReQueue = true;
                    _ReQueue_Event = _Event.Replace(_RenamedEventArgs.OldFullPath + '\\', _RenamedEventArgs.FullPath + '\\');
                }
                else
                {
                    if (_Event == _RenamedEventArgs.OldFullPath)
                    {
                        _ReQueue = true;
                        _ReQueue_Event = _RenamedEventArgs.FullPath;
                    }
                }

                if (_ReQueue)
                {
                    _Queue_FileEvents.Add(_ReQueue_Event);
                }
            }

            _Queue_FileEvent.Add(new FileEvent(_RenamedEventArgs.OldFullPath, "R", _RenamedEventArgs.FullPath));
        }

        private void FileScan()
        {
            string[] _Items = Directory.GetFileSystemEntries(@"C:\Users\JTorralba\Desktop", "*", SearchOption.AllDirectories);

            foreach (var _Item in _Items)
            {
                _Queue_FileEvents.Add(_Item);
            }
        }

        private void FileEvents()
        {
            while (!_Queue_FileEvents.IsCompleted)
            {
                string _FullPath = _Queue_FileEvents.Take();

                //Console.WriteLine("FEM: Take() -> {0}", _FullPath);

                FileInfo _FileInfo = new FileInfo(_FullPath);

                string _FileHash = "";
                long _FileSize = 0;
                DateTime _FileModified = DateTime.Now;
                FileMemo _FileMemo;

                if (!_FileInfo.Exists && !_File.IsLocked(_FullPath))
                {
                    continue;
                }
                else
                {
                    if (_File.IsFolder(_FullPath))
                    {
                        _FileHash = new string('-', 32);
                        _FileSize = 0;
                        _FileModified = _FileInfo.CreationTime;
                    }
                    else
                    {
                        if (_FileInfo.Exists)
                        {
                            _FileHash = _Cryptology.FileHash(_FullPath);
                            _FileSize = _FileInfo.Length;
                            _FileModified = _FileInfo.LastAccessTime;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                _FileMemo = new FileMemo(_FileHash, _FileSize, _FileModified);

                //Console.WriteLine("{0} {1, -10} {2} {3}", _FileHash, _FileSize.ToString(NumericSize), _FileModified, _FullPath);

                if (_Dictionary_FileMemo.TryGetValue(_FullPath, out FileMemo _Record))
                {
                    if (_Record != null)
                    {
                        if ((_FileHash != _Record.Hash) && (_FileSize != 0))
                        {
                            _Dictionary_FileMemo.AddOrUpdate(_FullPath, _FileMemo, (Key, _OldValue) => _FileMemo);
                            _Queue_FileEvent.Add(new FileEvent(_FullPath, "C", ""));
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        if (_File.IsFolder(_FullPath))
                        {
                            _Dictionary_FileMemo.AddOrUpdate(_FullPath, _FileMemo, (Key, _OldValue) => _FileMemo);
                            _Queue_FileEvent.Add(new FileEvent(_FullPath, "C", ""));
                        }
                    }
                }
                else
                {
                    _Dictionary_FileMemo.AddOrUpdate(_FullPath, _FileMemo, (Key, _OldValue) => _FileMemo);
                    _Queue_FileEvent.Add(new FileEvent(_FullPath, "C", ""));
                }
            }
        }

        private void FileEvent()
        {
            while (!_Queue_FileEvent.IsCompleted)
            {
                FileEvent _FileEvent = _Queue_FileEvent.Take();

                //Console.WriteLine("FEO: Take() -> {0} {1}", _FileEvent.FullPath, _FileEvent.Action, _FileEvent.NameNew);

                string _Hash = new string('-', 32);
                long _Size = 0;
                DateTime _Modified = DateTime.Now;
                string _NameNew = "";

                if (_FileEvent.Action == "R")
                {
                    string[] _Split = _FileEvent.NameNew.Split('\\');
                    string _Base = string.Join("\\", _Split.Take(_Split.Length - 1));
                    _NameNew = _Split.Last();
                }

                FileInfo _FileInfo = new FileInfo(_FileEvent.FullPath);

                if (!_FileInfo.Exists && !_File.IsLocked(_FileEvent.FullPath))
                {
                    continue;
                }
                else
                {
                    if (_File.IsFolder(_FileEvent.FullPath))
                    {
                    }
                    else
                    {
                        if (_FileInfo.Exists)
                        {
                            _Dictionary_FileMemo.TryGetValue(_FileEvent.FullPath, out FileMemo _X);
                            FileMemo _Y = new FileMemo(_X.Hash, _X.Size, _FileInfo.LastWriteTime);
                            _Dictionary_FileMemo.AddOrUpdate(_FileEvent.FullPath, _Y, (Key, _OldValue) => _Y);
                        }
                    }
                }

                FileMemo _Record = null;

                if (_FileEvent.Action == "R")
                {
                    if (_Dictionary_FileMemo.TryGetValue(_FileEvent.NameNew, out _Record))
                    {
                        _Hash = _Record.Hash;
                        _Size = _Record.Size;
                        _Modified = _Record.Modified;
                    }
                }
                else
                {
                    if (_Dictionary_FileMemo.TryGetValue(_FileEvent.FullPath, out _Record))
                    {
                        if (_Record == null)
                        {
                        }
                        else
                        {
                            _Hash = _Record.Hash;
                            _Size = _Record.Size;
                            _Modified = _Record.Modified;
                        }
                    }
                }

                //Console.WriteLine("{0} {1, -10} {2} {3} {4} {5}", _Hash, _Size.ToString(NumericSize), _Modified, _FileEvent.FullPath, _FileEvent.Action, _NameNew);

                _Queue_FileEvent_Reference.Add(new FileEvent(_FileEvent.FullPath, _FileEvent.Action, _FileEvent.NameNew));

                switch (_FileEvent.Action)
                {
                    case "D":
                        List<string> _ToDelete = _Dictionary_FileMemo.Keys.Where(Key => Key.Contains(_FileEvent.FullPath)).ToList();
                        _ToDelete.ForEach(Key => _Dictionary_FileMemo.TryRemove(Key, out FileMemo _Remove_Delete));
                        break;
                    case "R":
                        if (_File.IsFolder(_FileEvent.NameNew))
                        {
                            List<string> _ToRename = _Dictionary_FileMemo.Keys.Where(Key => Key.Contains(_FileEvent.FullPath + '\\')).ToList();
                            _ToRename.ForEach(Key => Rename(Key, _FileEvent.FullPath, _FileEvent.NameNew));
                        }
                        _Dictionary_FileMemo.TryGetValue(_FileEvent.FullPath, out _Record);
                        _Dictionary_FileMemo.TryAdd(_FileEvent.NameNew, _Record);
                        _Dictionary_FileMemo.TryRemove(_FileEvent.FullPath, out FileMemo _Remove_Rename);
                        break;
                    case "C":
                        break;
                    default:
                        break;
                }
            }
        }

        private void Rename(string _Key, string _FullPath, string _NameNew)
        {
            _Dictionary_FileMemo.TryGetValue(_Key, out FileMemo _Record);
            _Dictionary_FileMemo.TryAdd(_Key.Replace(_FullPath, _NameNew), _Record);
            _Dictionary_FileMemo.TryRemove(_Key, out FileMemo _Remove);
        }

        public void CLI(string _Command)
        {
            switch (_Command.ToUpper())
            {
                case "FEM":
                    foreach (var _Item in _Queue_FileEvents)
                    {
                        try
                        {
                            Console.WriteLine("{0}", _Item);
                        }
                        catch (Exception _Exception)
                        {
                            if (_X)
                            {
                                _DEBUG.Message("CLI.FEM", _Exception.Message.ToString());
                            }
                        }
                    }
                    break;
                case "FEO":
                    foreach (var _Item in _Queue_FileEvent)
                    {
                        try
                        {
                            Console.WriteLine("{0} {1}", _Item.FullPath, _Item.Action);
                        }
                        catch (Exception _Exception)
                        {
                            if (_X)
                            {
                                _DEBUG.Message("CLI.FEO", _Exception.Message.ToString());
                            }
                        }
                    }
                    break;
                case "FED":
                    foreach (var _Key in _Dictionary_FileMemo.Keys.OrderBy(_Key => _Key))
                    {
                        try
                        {
                            Console.WriteLine("{0} {1, -10} {2} {3}", _Dictionary_FileMemo[_Key].Hash, _Dictionary_FileMemo[_Key].Size.ToString(_NumericSize), _Dictionary_FileMemo[_Key].Modified, _Key);
                        }
                        catch (Exception _Exception)
                        {
                            if (_X)
                            {
                                _DEBUG.Message("CLI.FED", _Exception.Message.ToString());
                            }
                        }
                    }
                    break;
                case "FES":
                    FileScan();
                    break;
                default:
                    break;
            }
        }
    }
}
