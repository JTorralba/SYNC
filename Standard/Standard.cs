using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Standard
{
    public class DEBUG
    {
        public static void Message(String Message)
        {
            Console.WriteLine("DEBUG: {0}", Message);
        }
    }

    public class File
    {
        public bool IsDirectory(String _FullPath)
        {
            FileAttributes _FileAttributes = 0;

            try
            {
                _FileAttributes = System.IO.File.GetAttributes(_FullPath);
            }
            catch (Exception E)
            {
                return false;
            }

            if ((_FileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                return true;
            }

            return false;
        }

        public bool IsLocked(String _FullPath)
        {
            FileInfo _FileInfo = new FileInfo(_FullPath);

            FileStream _FileStream = null;

            try
            {
                _FileStream = _FileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception E)
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

    public class Cryptology
    {
        Standard.File _File = new Standard.File();

        public String FileHash(String _FullPath)
        {
            var _FileInfo = new FileInfo(_FullPath);

            while (_FileInfo.Exists && _File.IsLocked(_FullPath))
            {
            }

            using (FileStream _FileStream = new FileStream(_FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return FileHash(_FileStream);
            }
        }

        public String FileHash(FileStream _FileStream)
        {
            StringBuilder _Hash_String = new StringBuilder();

            if (_FileStream != null)
            {
                _FileStream.Seek(0, SeekOrigin.Begin);

                MD5 _MD5 = MD5CryptoServiceProvider.Create();

                Byte[] _Hash = _MD5.ComputeHash(_FileStream);

                foreach (Byte _Byte in _Hash)
                {
                    _Hash_String.Append(_Byte.ToString("X2"));
                }

                _FileStream.Seek(0, SeekOrigin.Begin);
            }

            if (_FileStream != null)
            {
                _FileStream.Close();
                _FileStream.Dispose();
            }
            
            return _Hash_String.ToString();
        }
    }
}
