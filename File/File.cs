using System;
using System.IO;

namespace Standard
{
    public class File
    {
        private bool _X;
        private DEBUG _DEBUG;

        public File()
        {
            Initialize(false);
        }

        public File(bool _X)
        {
            Initialize(_X);
        }

        private void Initialize(bool _X)
        {
            this._X = _X;
            _DEBUG = new DEBUG();
        }

        public bool IsFolder(string _FullPath)
        {
            FileAttributes _FileAttributes = 0;

            try
            {
                _FileAttributes = System.IO.File.GetAttributes(_FullPath);
            }
            catch (Exception _Exception)
            {
                if (_X)
                {
                    _DEBUG.Message("File.IsFolder", _Exception.Message.ToString());
                }
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
                if (_X)
                {
                    _DEBUG.Message("File.IsLocked", _Exception.Message.ToString());
                }
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
}
