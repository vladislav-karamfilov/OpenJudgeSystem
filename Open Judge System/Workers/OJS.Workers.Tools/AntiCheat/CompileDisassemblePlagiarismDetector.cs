namespace OJS.Workers.Tools.AntiCheat
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using OJS.Common.Extensions;
    using OJS.Workers.Common;
    using OJS.Workers.Compilers;
    using OJS.Workers.Tools.AntiCheat.Contracts;
    using OJS.Workers.Tools.Similarity;

    public abstract class CompileDisassemblePlagiarismDetector : IPlagiarismDetector
    {
        private readonly ISimilarityFinder similarityFinder;
        private readonly IDictionary<string, string> sourcesCache;

        protected CompileDisassemblePlagiarismDetector(ICompiler compiler, ICompiler disassembler, ISimilarityFinder similarityFinder)
        {
            this.Compiler = compiler;
            this.Disassembler = disassembler;
            this.similarityFinder = similarityFinder;
            this.sourcesCache = new Dictionary<string, string>();
        }

        protected ICompiler Compiler { get; set; }

        protected ICompiler Disassembler { get; set; }

        public PlagiarismResult DetectPlagiarism(string firstSource, string secondSource, IEnumerable<IDetectPlagiarismVisitor> visitors = null)
        {
            string firstFileContent;
            if (!this.GetDisassembledCode(firstSource, out firstFileContent))
            {
                return new PlagiarismResult(0);
            }

            string secondFileContent;
            if (!this.GetDisassembledCode(secondSource, out secondFileContent))
            {
                return new PlagiarismResult(0);
            }

            if (visitors != null)
            {
                foreach (var visitor in visitors)
                {
                    firstFileContent = visitor.Visit(firstFileContent);
                    secondFileContent = visitor.Visit(secondFileContent);
                }
            }

            var differences = this.similarityFinder.DiffText(firstFileContent, secondFileContent, true, true, true);

            var differencesCount = differences.Sum(difference => difference.DeletedA + difference.InsertedB);
            var textLength = firstFileContent.Length + secondFileContent.Length;

            // TODO: Revert the percentage
            var percentage = ((decimal)differencesCount * 100) / textLength;

            return new PlagiarismResult(percentage)
            {
                Differences = differences,
                FirstToCompare = firstFileContent,
                SecondToCompare = secondFileContent
            };
        }

        protected bool GetDisassembledCode(string source, out string disassembledCode)
        {
            if (this.sourcesCache.ContainsKey(source))
            {
                disassembledCode = this.sourcesCache[source];
                return true;
            }

            // TODO: Check for undeleted temporary files.
            disassembledCode = null;

            var sourceFilePath = this.PrepareFileToCompile(source);
            var compileResult = this.CompileCode(sourceFilePath);
            File.Delete(sourceFilePath);
            if (!compileResult.IsCompiledSuccessfully)
            {
                return false;
            }

            var disassemblerResult = this.DisassembleCode(compileResult.OutputFile);
            File.Delete(compileResult.OutputFile);
            if (!disassemblerResult.IsCompiledSuccessfully)
            {
                return false;
            }

            disassembledCode = File.ReadAllText(disassemblerResult.OutputFile);
            this.sourcesCache.Add(source, disassembledCode);
            File.Delete(disassemblerResult.OutputFile);
            return true;
        }

        protected virtual string PrepareFileToCompile(string source)
        {
            return FileHelpers.SaveStringToTempFile(source);
        }

        protected abstract CompileResult CompileCode(string sourceCodeFilePath);

        protected abstract CompileResult DisassembleCode(string compiledCodeFilePath);
    }
}
