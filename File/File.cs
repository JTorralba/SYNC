using System;
using System.IO;

namespace Standard
{
    public class File
    {
        private enum DEBUG
        {
            Off,
            Trace,
            Exception,
            On
        }

        private static int _DEBUG = (int) DEBUG.Off;

        public File()
        {
        }

        public bool IsFolder(string _FullPath)
        {
            FileAttributes _FileAttributes;

            try
            {
                _FileAttributes = System.IO.File.GetAttributes(_FullPath);
            }
            catch (Exception _Exception)
            {
                if (Convert.ToBoolean(_DEBUG & (int) DEBUG.Exception))
                {
                    Console.WriteLine("File.IsFolder: {0}", _Exception.Message.ToString());
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
                if (Convert.ToBoolean(_DEBUG & (int) DEBUG.Exception))
                {
                    Console.WriteLine("File.IsLocked: {0}", _Exception.Message.ToString());
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
