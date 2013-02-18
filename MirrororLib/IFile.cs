using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MirrororLib
{
    public interface IFile
    {
        string Name { get; }
        string FullName { get; }
        DateTime LastUpdated { get; }
    }
}
