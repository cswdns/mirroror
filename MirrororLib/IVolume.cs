using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MirrororLib
{
    public interface IVolume
    {
        string Name { get; }
        string FullName { get; }
        IEnumerable<IFile> Files { get; }
        IFile FindFile(string fileName);
    }
}
