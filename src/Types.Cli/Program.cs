using System;
using System.Linq;
using System.IO;
using Types.Core;
using Types.Core.Specs;

namespace Types.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var searchPath = args[0];
            var armPaths = Directory.GetDirectories(searchPath, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(searchPath, path))
                .Select(path => path.Split(Path.DirectorySeparatorChar))
                .Where(segments => segments.Length == 6)
                .Where(segments => segments[0] == "specification" && segments[2] == "resource-manager")
                .Select(segments => new ArmFolderSpec(Path.Combine(searchPath, Path.Combine(segments)), segments[1], segments[3], segments[4], segments[5]));

            foreach (var armPath in armPaths)
            {
                var parser = new ArmSpecsProcessor(armPath);
            }
        }
    }
}