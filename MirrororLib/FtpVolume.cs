using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace MirrororLib
{
    public class FtpVolume : IMirror
    {
        string _uri;
        string _userName;
        string _password;

        public FtpVolume(string uri, string userName, string password)
        {
            _uri = uri;
            _userName = userName;
            _password = password;
        }

        public IFile CopyFile(IFile sourceFile)
        {
            string name = sourceFile.Name;
            if (name.Contains('#'))
                return null;
            Uri uri = new Uri(GetConnectionString(name));
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(uri.AbsoluteUri);

            ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
            StreamReader sourceStream = new StreamReader(sourceFile.FullName);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();
            ftpWebRequest.ContentLength = fileContents.Length;

            Stream requestStream = ftpWebRequest.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)ftpWebRequest.GetResponse();

            return new FtpFile(this, sourceFile.Name);
        }

        public void DeleteFile(IFile file)
        {
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(GetConnectionString(file.Name));
            ftpWebRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            FtpWebResponse response = (FtpWebResponse)ftpWebRequest.GetResponse();
        }

        public void DeleteSubMirror(IMirror subMirror)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMirror> SubMirrors
        {
            get
            {
                return from n in GetDirectoryNames() select new FtpVolume(ConcatenateFileOrDirectory(n), _userName, _password);
            }
        }

        public IMirror FindSubMirror(string name)
        {            
            IEnumerable<string> directoryNames = GetDirectoryNames();
            IEnumerable<string> foundDirectoryNames = from s in directoryNames where s == name select s;
            string foundDirectoryName = foundDirectoryNames.FirstOrDefault();
            return (foundDirectoryName == null) ? null : new FtpVolume(string.Format("{0}/{1}", _uri, foundDirectoryName), _userName, _password);
        }

        public IMirror CreateSubMirror(string name)
        {
            Uri uri = new Uri(GetConnectionString(name));
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(uri.AbsoluteUri);
            ftpWebRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
            FtpWebResponse response = (FtpWebResponse)ftpWebRequest.GetResponse();
            return new FtpVolume(ConcatenateFileOrDirectory(name), _userName, _password);
        }

        public string Name
        {
            get
            {
                int index = _uri.LastIndexOf('/');
                return (index == -1) ? _uri : _uri.Substring(index + 1);
            }
        }

        public string FullName
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IFile> Files
        {
            get
            {
                return from f in GetFileNames() select new FtpFile(this, f);
            }
        }

        public IFile FindFile(string fileName)
        {
            if (fileName.Contains('#'))
                return null;

            IEnumerable<string> fileNames = GetFileNames();
            IEnumerable<string> foundFileNames = from s in fileNames where s == fileName select s;
            string foundFileName = foundFileNames.FirstOrDefault();
            return (foundFileName == null) ? null : new FtpFile(this, foundFileName);
        }

        internal string GetConnectionString(string subFileOrDirectory = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ftp://{0}:{1}@{2}", _userName, _password, _uri);
            if (subFileOrDirectory != null)
                sb.AppendFormat("/{0}", subFileOrDirectory);

            return sb.ToString();
        }

        string ConcatenateFileOrDirectory(string subFileOrDirectory)
        {
            return string.Format("{0}/{1}", _uri, subFileOrDirectory);
        }

        private IEnumerable<string> GetDirectoryNames()
        {
            return
                from s in GetFileSystemInfoNames()
                where ((s.Length > 0) && (s.StartsWith("d")))
                select s.Substring(55);
        }

        private IEnumerable<string> GetFileNames()
        {
            return
                from s in GetFileSystemInfoNames()
                where ((s.Length > 0) && (!s.StartsWith("d")))
                select s.Substring(55);
        }

        private IEnumerable<string> GetFileSystemInfoNames()
        {
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(GetConnectionString());

            ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            ftpWebRequest.UseBinary = true;
            StreamReader reader = new StreamReader(((FtpWebResponse)ftpWebRequest.GetResponse()).GetResponseStream());
            return reader.ReadToEnd().Replace("\r\n", "|").Split('|');
        }
    }
}
