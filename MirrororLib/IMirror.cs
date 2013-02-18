using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MirrororLib
{
    public interface IMirror : IVolume
    {
        IFile CopyFile(IFile sourceFile);
        void DeleteFile(IFile file);
        void DeleteSubMirror(IMirror subMirror);
        IEnumerable<IMirror> SubMirrors { get; }
        IMirror FindSubMirror(string name);
        IMirror CreateSubMirror(string name);
    }
}
