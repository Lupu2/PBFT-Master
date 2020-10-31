using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Cleipnir.ObjectDB
{
    public static class AssemblyHelper
    {
        public static void LoadAllAssembliesFromCurrentFolder()
        {
            var allAssemblies = new List<Assembly>();
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (var dll in Directory.GetFiles(path, "*.dll"))
                allAssemblies.Add(Assembly.LoadFile(dll));
        }
    }
}
