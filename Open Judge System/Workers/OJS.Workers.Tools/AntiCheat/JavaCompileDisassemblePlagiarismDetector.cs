namespace OJS.Workers.Tools.AntiCheat
{
    using System;

    using OJS.Common.Models;
    using OJS.Workers.Common;
    using OJS.Workers.Compilers;
    using OJS.Workers.Tools.Similarity;

    public class JavaCompileDisassemblePlagiarismDetector : CompileDisassemblePlagiarismDetector
    {
        private readonly string javaCompilerPath;
        private readonly string javaDisassemblerPath;

        public JavaCompileDisassemblePlagiarismDetector(string javaCompilerPath, string javaDisassemblerPath)
            : this(javaCompilerPath, javaDisassemblerPath, new SimilarityFinder())
        {
        }

        public JavaCompileDisassemblePlagiarismDetector(string javaCompilerPath, string javaDisassemblerPath, ISimilarityFinder similarityFinder)
            : base(new JavaCompiler(), new JavaDisassembler(), similarityFinder)
        {
            this.javaCompilerPath = javaCompilerPath;
            this.javaDisassemblerPath = javaDisassemblerPath;
        }

        protected override CompileResult CompileCode(string sourceCodeFilePath)
        {
            throw new NotImplementedException();
        }

        protected override CompileResult DisassembleCode(string compiledCodeFilePath)
        {
            throw new NotImplementedException();
        }
    }
}
