using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net;

namespace MirrororLib
{
    public class DriveFile : IFile
    {
        static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        FileInfo _fileInfo;

        public DriveFile(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
            if (!_fileInfo.Exists)
            {
                _log.Error(_fileInfo.FullName + " does not exist");
                throw new System.IO.FileNotFoundException(_fileInfo.FullName);
            }
        }

        #region IFile Members

        public string Name
        {
            get
            {
                return _fileInfo.Name;
            }
        }

        public string FullName
        {
            get
            {
                return _fileInfo.FullName;
            }
        }

        public DateTime LastUpdated
        {
            get
            {
                return _fileInfo.LastWriteTime;
            }
        }

        #endregion
    }
}
