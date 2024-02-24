using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Standard
{
    public class DEBUG
    {
        public static void Message(string _Message)
        {
            Console.WriteLine("DEBUG: {0}", _Message);
        }
    }

    public class File
    {
        public bool IsFolder(string _FullPath)
        {
            FileAttributes _FileAttributes = 0;

            try
            {
                _FileAttributes = System.IO.File.GetAttributes(_FullPath);
            }
            catch (Exception _Exception)
            {
                return false;
            }

            if ((_FileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                return true;
            }

            return false;
        }

        public bool IsLocked(string _FullPath)
        {
            FileInfo _FileInfo = new FileInfo(_FullPath);

            FileStream _FileStream = null;

            try
            {
                _FileStream = _FileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception _Exception)
            {
                return true;
            }
            finally
            {
                if (_FileStream != null)
                {
                    _FileStream.Close();
                    _FileStream.Dispose();
                }
            }

            return false;
        }
    }

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
        File _File = new File();
        Cryptology _Cryptology = new Cryptology();

        BlockingCollection<FileEvent> _Queue_FileEvent_Reference;

        BlockingCollection<string> _Queue_FileEvents;
        BlockingCollection<FileEvent> _Queue_FileEvent;

        ConcurrentDictionary<string, FileMemo> _Dictionary_FileMemo;

        string NumericSize = "D13";

        public FileAudit(ref BlockingCollection<FileEvent> _Queue_FileEvent_Reference)
        {
            this._Queue_FileEvent_Reference = _Queue_FileEvent_Reference;

            _Queue_FileEvents = new BlockingCollection<string>();
            _Queue_FileEvent = new BlockingCollection<FileEvent>();

            _Dictionary_FileMemo = new ConcurrentDictionary<string, FileMemo>();

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

        void FileScan()
        {
            string[] _Items = Directory.GetFileSystemEntries(@"C:\Users\JTorralba\Desktop", "*", SearchOption.AllDirectories);

            foreach (var _Item in _Items)
            {
                _Queue_FileEvents.Add(_Item);
            }
        }

        void FileEvents()
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

        void FileEvent()
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

        void Rename(string _Key, string _FullPath, string _NameNew)
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
                        }
                    }
                    break;
                case "FED":
                    foreach (var _Key in _Dictionary_FileMemo.Keys.OrderBy(_Key => _Key))
                    {
                        try
                        {
                            Console.WriteLine("{0} {1, -10} {2} {3}", _Dictionary_FileMemo[_Key].Hash, _Dictionary_FileMemo[_Key].Size.ToString(NumericSize), _Dictionary_FileMemo[_Key].Modified, _Key);
                        }
                        catch (Exception _Exception)
                        {
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

    public class Cryptology
    {
        File _File = new File();

        public string FileHash(string _FullPath)
        {
            var _FileInfo = new FileInfo(_FullPath);

            while (_FileInfo.Exists && _File.IsLocked(_FullPath))
            {
            }

            bool _Success = false;

            while (_Success == false)
            {
                try
                {
                    using (FileStream _FileStream = new FileStream(_FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        _Success = true;
                        return FileHash(_FileStream);
                    }
                }
                catch (Exception _Exception)
                {

                }

            }
            return new string('-', 32);
        }

        public string FileHash(FileStream _FileStream)
        {
            StringBuilder _StringBuilder = new StringBuilder();

            if (_FileStream != null)
            {
                _FileStream.Seek(0, SeekOrigin.Begin);

                MD5 _MD5 = MD5CryptoServiceProvider.Create();

                Byte[] _Bytes = _MD5.ComputeHash(_FileStream);

                foreach (Byte _Byte in _Bytes)
                {
                    _StringBuilder.Append(_Byte.ToString("X2"));
                }

                _FileStream.Seek(0, SeekOrigin.Begin);
            }

            if (_FileStream != null)
            {
                _FileStream.Close();
                _FileStream.Dispose();
            }
            
            return _StringBuilder.ToString();
        }
    }
}
