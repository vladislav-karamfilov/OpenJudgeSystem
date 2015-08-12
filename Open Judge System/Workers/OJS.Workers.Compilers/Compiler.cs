﻿namespace OJS.Workers.Compilers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;

    using OJS.Common.Models;
    using OJS.Workers.Common;

    /// <summary>
    /// Defines the base of the work with compilers algorithm and allow the subclasses to implement some of the algorithm parts.
    /// </summary>
    /// <remarks>Template method design pattern is used.</remarks>
    public abstract class Compiler : ICompiler
    {
        private const int CompilerProcessExitTimeOut = 5000;

        public static ICompiler CreateCompiler(CompilerType compilerType)
        {
            switch (compilerType)
            {
                case CompilerType.None:
                    return null;
                case CompilerType.CSharp:
                    return new CSharpCompiler();
                case CompilerType.CPlusPlusGcc:
                    return new CPlusPlusCompiler();
                case CompilerType.MsBuild:
                    return new MsBuildCompiler();
                case CompilerType.Java:
                    return new JavaCompiler();
                default:
                    throw new ArgumentException("Unsupported compiler.");
            }
        }

        public CompileResult Compile(string compilerPath, string inputFile, string additionalArguments)
        {
            if (compilerPath == null)
            {
                throw new ArgumentNullException("compilerPath");
            }

            if (inputFile == null)
            {
                throw new ArgumentNullException("inputFile");
            }

            if (!File.Exists(compilerPath))
            {
                return new CompileResult(false, string.Format("Compiler not found! Searched in: {0}", compilerPath));
            }

            if (!File.Exists(inputFile))
            {
                return new CompileResult(false, string.Format("Input file not found! Searched in: {0}", inputFile));
            }

            // Move source file if needed
            string newInputFilePath = this.RenameInputFile(inputFile);
            if (newInputFilePath != inputFile)
            {
                File.Move(inputFile, newInputFilePath);
                inputFile = newInputFilePath;
            }

            // Build compiler arguments
            var outputFile = this.GetOutputFileName(inputFile);
            var arguments = this.BuildCompilerArguments(inputFile, outputFile, additionalArguments);

            // Find compiler directory
            var directoryInfo = new FileInfo(compilerPath).Directory;
            if (directoryInfo == null)
            {
                return new CompileResult(false, string.Format("Compiler directory is null. Compiler path value: {0}", compilerPath));
            }

            // Prepare process start information
            var processStartInfo =
                new ProcessStartInfo(compilerPath)
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = directoryInfo.ToString(),
                    Arguments = arguments
                };
            this.UpdateCompilerProcessStartInfo(processStartInfo);

            // Execute compiler
            var compilerOutput = ExecuteCompiler(processStartInfo);

            outputFile = this.ChangeOutputFileAfterCompilation(outputFile);

            // Delete input file
            if (File.Exists(newInputFilePath))
            {
                File.Delete(newInputFilePath);
            }

            // Check results and return CompilerResult instance
            if (!File.Exists(outputFile))
            {
                // Compiled file is missing
                return new CompileResult(false, string.Format("Compiled file is missing. Compiler output: {0}", compilerOutput));
            }

            if (!string.IsNullOrWhiteSpace(compilerOutput))
            {
                // Compile file is ready but the compiler has something on standard error (possibly compile warnings)
                return new CompileResult(true, compilerOutput, outputFile);
            }

            // Compilation is ready without warnings
            return new CompileResult(outputFile);
        }

        public virtual string RenameInputFile(string inputFile)
        {
            return inputFile;
        }

        public virtual string GetOutputFileName(string inputFileName)
        {
            return inputFileName + ".exe";
        }

        public virtual string ChangeOutputFileAfterCompilation(string outputFile)
        {
            return outputFile;
        }

        public abstract string BuildCompilerArguments(string inputFile, string outputFile, string additionalArguments);

        public virtual void UpdateCompilerProcessStartInfo(ProcessStartInfo processStartInfo)
        {
        }

        private static string ExecuteCompiler(ProcessStartInfo compilerProcessStartInfo)
        {
            var outputBuilder = new StringBuilder();
            var errorOutputBuilder = new StringBuilder();

            using (var process = new Process())
            {
                process.StartInfo = compilerProcessStartInfo;

                using (var outputWaitHandle = new AutoResetEvent(false))
                {
                    using (var errorWaitHandle = new AutoResetEvent(false))
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                outputBuilder.AppendLine(e.Data);
                            }
                        };

                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                errorOutputBuilder.AppendLine(e.Data);
                            }
                        };

                        var started = process.Start();
                        if (!started)
                        {
                            return "Could not start compiler.";
                        }

                        if (compilerProcessStartInfo.RedirectStandardOutput)
                        {
                            process.BeginOutputReadLine();
                        }

                        if (compilerProcessStartInfo.RedirectStandardError)
                        {
                            process.BeginErrorReadLine();
                        }

                        var exited = process.WaitForExit(CompilerProcessExitTimeOut);
                        if (!exited)
                        {
                            process.Kill();
                        }

                        outputWaitHandle.WaitOne(100);
                        errorWaitHandle.WaitOne(100);
                    }
                }
            }

            var output = outputBuilder.ToString().Trim();
            var errorOutput = errorOutputBuilder.ToString().Trim();

            var compilerOutput = string.Format("{0}{1}{2}", output, Environment.NewLine, errorOutput).Trim();
            return compilerOutput;
        }
    }
}
