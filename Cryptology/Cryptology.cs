using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Standard
{
    public class Cryptology
    {
        private bool _X;
        private DEBUG _DEBUG;
        private Standard.File _File;

        public Cryptology()
        {
            Initialize(false);
        }

        public Cryptology(bool _X)
        {
            Initialize(_X);
        }

        private void Initialize(bool _X)
        {
            this._X = _X;
            _DEBUG = new DEBUG();
            _File = new Standard.File(_X);
        }

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
                    if (_X)
                    {
                        _DEBUG.Message("Cryptology.FileHash", _Exception.Message.ToString());
                    }
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
