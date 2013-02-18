using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MirrororLib
{
    public interface ISource : IVolume
    {
        DateTime LastModified { get; }
        IEnumerable<ISource> SubSources { get; }
    }
}
