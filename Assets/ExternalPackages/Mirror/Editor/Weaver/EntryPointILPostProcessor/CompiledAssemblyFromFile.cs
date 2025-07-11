// tests use WeaveAssembler, which uses AssemblyBuilder to Build().
// afterwards ILPostProcessor weaves the build.
// this works on windows, but build() does not run ILPP on mac atm.
// we need to manually invoke ILPP with an assembly from file.
//
// this is in Weaver folder becuase CompilationPipeline can only be accessed
// from assemblies with the name "Unity.*.CodeGen"

using System.IO;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Mirror.Weaver
{
    public class CompiledAssemblyFromFile : ICompiledAssembly
    {
        private readonly string assemblyPath;

        public CompiledAssemblyFromFile(string assemblyPath)
        {
            this.assemblyPath = assemblyPath;
            var peData = File.ReadAllBytes(assemblyPath);
            var pdbFileName = Path.GetFileNameWithoutExtension(assemblyPath) + ".pdb";
            var pdbData = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assemblyPath), pdbFileName));
            InMemoryAssembly = new InMemoryAssembly(peData, pdbData);
        }

        public string Name => Path.GetFileNameWithoutExtension(assemblyPath);
        public string[] References { get; set; }
        public string[] Defines { get; set; }
        public InMemoryAssembly InMemoryAssembly { get; }
    }
}