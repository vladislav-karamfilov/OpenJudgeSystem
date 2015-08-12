namespace OJS.Workers.Compilers
{
    using System.Text;

    public class JavaDisassembler : Compiler
    {
        public override string BuildCompilerArguments(string inputFile, string outputFile, string additionalArguments)
        {
            var arguments = new StringBuilder();

            arguments.Append(additionalArguments);

            arguments.AppendFormat(" \"{0}\" > \"{1}\"", inputFile, outputFile);

            return arguments.ToString();
        }

        public override string GetOutputFileName(string inputFileName)
        {
            return inputFileName + ".txt";
        }
    }
}
