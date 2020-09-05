using System;
using System.IO;
using Microsoft.OpenApi.Readers;
using Types.Core.Specs;

namespace Types.Core
{
    public class ArmSpecsProcessor
    {
        private readonly OpenApiStreamReader openApiReader = new OpenApiStreamReader();

        public ArmSpecsProcessor(ArmFolderSpec spec)
        {
            foreach (var file in Directory.GetFiles(spec.FullPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                using (var reader = new StreamReader(file))
                {
                    var document = openApiReader.Read(reader.BaseStream, out var diagnostic);

                    ResourceParser.Parse(document);
                }
            }
        }
    }
}
