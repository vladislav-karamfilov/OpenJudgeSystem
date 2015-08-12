namespace OJS.Workers.Tools.AntiCheat
{
    using System.IO;

    using OJS.Common.Extensions;
    using OJS.Workers.Common;
    using OJS.Workers.Compilers;
    using OJS.Workers.Tools.Similarity;

    public class CSharpCompileDisassemblePlagiarismDetector : CompileDisassemblePlagiarismDetector
    {
        private const string CSharpCompilerAdditionalArguments =
            "/optimize+ /nologo /reference:System.Numerics.dll /reference:PowerCollections.dll";

        private readonly string csharpCompilerPath;
        private readonly string dotNetDisassemblerPath;

        public CSharpCompileDisassemblePlagiarismDetector(string csharpCompilerPath, string dotNetDisassemblerPath)
            : this(csharpCompilerPath, dotNetDisassemblerPath, new SimilarityFinder())
        {
        }

        public CSharpCompileDisassemblePlagiarismDetector(string csharpCompilerPath, string dotNetDisassemblerPath, ISimilarityFinder similarityFinder)
            : base(new CSharpCompiler(), new DotNetDisassembler(), similarityFinder)
        {
            this.csharpCompilerPath = csharpCompilerPath;
            this.dotNetDisassemblerPath = dotNetDisassemblerPath;
        }

        protected override CompileResult CompileCode(string sourceCode)
        {
            var sourceFilePath = FileHelpers.SaveStringToTempFile(sourceCode);
            var compileResult = this.Compiler.Compile(this.csharpCompilerPath, sourceFilePath, CSharpCompilerAdditionalArguments);

            File.Delete(sourceFilePath);

            return compileResult;
        }

        protected override CompileResult DisassembleFile(string compiledFilePath)
        {
            var disassemblerResult = this.Disassembler.Compile(this.dotNetDisassemblerPath, compiledFilePath, null);

            File.Delete(compiledFilePath);

            return disassemblerResult;
        }
    }
}
