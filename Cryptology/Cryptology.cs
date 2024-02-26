using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Standard
{
    public class Cryptology
    {
        private enum DEBUG
        {
            Off,
            Trace,
            Exception,
            On
        }

        private static int _DEBUG = (int) DEBUG.Off;

        private Standard.File _File;

        public Cryptology()
        {
            _File = new Standard.File();
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
                    if (Convert.ToBoolean(_DEBUG & (int) DEBUG.Exception))
                    {
                        Console.WriteLine("Cryptology.FileHash: {0}", _Exception.Message.ToString());
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
