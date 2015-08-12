namespace OJS.Workers.Tools.AntiCheat
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using OJS.Workers.Common;
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
            string firstDisassembledCode;
            if (!this.GetDisassembledCode(firstSource, out firstDisassembledCode))
            {
                return new PlagiarismResult(0);
            }

            string secondDisassembledCode;
            if (!this.GetDisassembledCode(secondSource, out secondDisassembledCode))
            {
                return new PlagiarismResult(0);
            }

            if (visitors != null)
            {
                foreach (var visitor in visitors)
                {
                    firstDisassembledCode = visitor.Visit(firstDisassembledCode);
                    secondDisassembledCode = visitor.Visit(secondDisassembledCode);
                }
            }

            var differences = this.similarityFinder.DiffText(firstDisassembledCode, secondDisassembledCode, true, true, true);

            var differencesCount = differences.Sum(difference => difference.DeletedA + difference.InsertedB);
            var textLength = firstDisassembledCode.Length + secondDisassembledCode.Length;

            // TODO: Revert the percentage
            var percentage = ((decimal)differencesCount * 100) / textLength;

            return new PlagiarismResult(percentage)
            {
                Differences = differences,
                FirstToCompare = firstDisassembledCode,
                SecondToCompare = secondDisassembledCode
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

            var compileResult = this.CompileCode(source);
            if (!compileResult.IsCompiledSuccessfully)
            {
                return false;
            }

            var disassemblerResult = this.DisassembleFile(compileResult.OutputFile);
            if (!disassemblerResult.IsCompiledSuccessfully)
            {
                return false;
            }

            disassembledCode = File.ReadAllText(disassemblerResult.OutputFile);
            this.sourcesCache.Add(source, disassembledCode);
            FileHelpers.SafeDelete(disassemblerResult.OutputFile);
            return true;
        }

        protected abstract CompileResult CompileCode(string sourceCode);

        protected abstract CompileResult DisassembleFile(string compiledFilePath);
    }
}
