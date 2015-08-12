namespace OJS.Workers.Tools.AntiCheat
{
    using OJS.Common.Extensions;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Helpers;
    using OJS.Workers.Compilers;
    using OJS.Workers.Tools.Similarity;

    public class JavaCompileDisassemblePlagiarismDetector : CompileDisassemblePlagiarismDetector
    {
        private const string JavaDisassemblerAdditionalArguments = "-c -p";

        private readonly string javaCompilerPath;
        private readonly string javaDisassemblerPath;
        private readonly string workingDirectory;

        public JavaCompileDisassemblePlagiarismDetector(string javaCompilerPath, string javaDisassemblerPath)
            : this(javaCompilerPath, javaDisassemblerPath, new SimilarityFinder())
        {
        }

        public JavaCompileDisassemblePlagiarismDetector(string javaCompilerPath, string javaDisassemblerPath, ISimilarityFinder similarityFinder)
            : base(new JavaCompiler(), new JavaDisassembler(), similarityFinder)
        {
            this.javaCompilerPath = javaCompilerPath;
            this.javaDisassemblerPath = javaDisassemblerPath;
            this.workingDirectory = DirectoryHelpers.CreateTempDirectory();
        }

        ~JavaCompileDisassemblePlagiarismDetector()
        {
            DirectoryHelpers.SafeDeleteDirectory(this.workingDirectory);
        }

        protected override CompileResult CompileCode(string sourceCode)
        {
            var sourceFilePath = JavaCodePreprocessorHelper.PrepareSubmissionFile(sourceCode, this.workingDirectory);
            var compileResult = this.Compiler.Compile(this.javaCompilerPath, sourceFilePath, null);

            return compileResult;
        }

        protected override CompileResult DisassembleFile(string compiledFilePath)
        {
            var disassemblerResult = this.Disassembler.Compile(
                this.javaDisassemblerPath,
                compiledFilePath,
                JavaDisassemblerAdditionalArguments);
            
            return disassemblerResult;
        }
    }
}
