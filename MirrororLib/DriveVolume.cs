using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net;

namespace MirrororLib
{
    public class DriveVolume : ISource, IMirror
    {
        static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        DirectoryInfo _pathInfo;

        public DriveVolume(DirectoryInfo directoryInfo)
        {
            _pathInfo = directoryInfo;
        }

        public DriveVolume(string path) : this(new DirectoryInfo(path))
        {
        }

        #region IVolume Members

        public string Name
        {
            get
            {
                return _pathInfo.Name;
            }
        }

        public string FullName
        {
            get
            {
                return _pathInfo.FullName;
            }
        }

        public DateTime LastModified
        {
            get
            {
                return RecursiveLastModified(_pathInfo);
            }
        }

        public IEnumerable<IFile> Files
        {
            get
            {
                return from n in _pathInfo.GetFiles() select (IFile)new DriveFile(n);
            }
        }

        public IFile FindFile(string fileName)
        {
            FileInfo fileInfo = FindFileSystemInfo(fileName) as FileInfo;
            return (fileInfo == null) ? null : new DriveFile(fileInfo);
        }

        #endregion

        #region ISource Members

        public IEnumerable<ISource> SubSources
        {
            get
            {
                return from n in _pathInfo.GetDirectories() select (ISource)new DriveVolume(n);
            }
        }

        #endregion

        #region IMirror Members

        public IFile CopyFile(IFile sourceFile)
        {
            string fullDestPath = MakeFullPath(sourceFile.Name);
            File.Copy(sourceFile.FullName, fullDestPath, true);
            return new DriveFile(new FileInfo(fullDestPath));
        }

        public void DeleteFile(IFile file)
        {
            File.Delete(file.FullName);
        }

        public void DeleteSubMirror(IMirror subMirror)
        {
            DriveVolume subDriveVolume = (DriveVolume)subMirror;
            try
            {
                Directory.Delete(subDriveVolume.FullName, true);
            }
            catch (UnauthorizedAccessException uae)
            {
                // subfile might be readonly.
            }
        }

        public IEnumerable<IMirror> SubMirrors
        {
            get
            {
                return from n in _pathInfo.GetDirectories() select (IMirror)new DriveVolume(n);
            }
        }

        public IMirror FindSubMirror(string name)
        {
            DirectoryInfo directoryInfo = FindFileSystemInfo(name) as DirectoryInfo;
            return (directoryInfo == null) ? null : new DriveVolume(directoryInfo);
        }

        public IMirror CreateSubMirror(string name)
        {
            return new DriveVolume(_pathInfo.CreateSubdirectory(name));
        }

        #endregion

        private FileSystemInfo FindFileSystemInfo(string fileSystemInfoName)
        {
            FileSystemInfo retVal;
            FileSystemInfo[] fileSystemInfos = _pathInfo.GetFileSystemInfos(fileSystemInfoName);
            int foundFileSystemInfoCount = fileSystemInfos.Length;
            if (foundFileSystemInfoCount == 0)
                retVal = null;
            else if (foundFileSystemInfoCount == 1)
                retVal = fileSystemInfos[0];
            else
                throw new ApplicationException("More that one item matches " + fileSystemInfoName + " in " + _pathInfo.FullName);

            return retVal;
        }

        private DateTime RecursiveLastModified(FileSystemInfo fsi)
        {
            DateTime retVal = fsi.LastWriteTime;

            if (fsi is DirectoryInfo)
            {
                try
                {
                    foreach (FileSystemInfo sfsi in ((DirectoryInfo)fsi).GetFileSystemInfos())
                    {
                        DateTime lastUpdated = RecursiveLastModified(sfsi);
                        retVal = (retVal > lastUpdated) ? retVal : lastUpdated;
                    }
                }
                catch (UnauthorizedAccessException uae)
                {
                    // log
                }
            }

            return retVal;
        }

        private string MakeFullPath(string fileName)
        {
            StringBuilder sb = new StringBuilder(_pathInfo.FullName);
            if (!_pathInfo.FullName.EndsWith("\\"))
                sb.Append('\\');
            sb.Append(fileName);
            return sb.ToString();
        }
    }
}
