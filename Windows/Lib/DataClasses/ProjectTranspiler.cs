using System;
using System.ComponentModel;
using SassyMQ.SSOTME.Lib.RMQActors;
using System.IO;
using System.Diagnostics;
using SSoTme.OST.Lib.CLIOptions;
using SSoTme.OST.Lib.Extensions;
using SassyMQ.Lib.RabbitMQ;

namespace SSoTme.OST.Lib.DataClasses
{
    public partial class ProjectTranspiler
    {
        public Transpiler MatchedTranspiler { get; set; }
        public SSoTmeCLIHandler CLIHandler { get; private set; }

        public ProjectTranspiler()
        {
            this.InitPoco();
        }

        public override string ToString()
        {
            var outputFileName = String.Empty;
            if (!ReferenceEquals(this.CLIHandler, null) && !ReferenceEquals(this.CLIHandler.OutputFileSet, null))
            {
                var outputFileSet = this.CLIHandler.OutputFileSet;
                if (!ReferenceEquals(outputFileSet, null) && System.Linq.Enumerable.Any(outputFileSet.FileSetFiles))
                {
                    outputFileName = String.Format(" ({0})", System.Linq.Enumerable.First(outputFileSet.FileSetFiles).RelativePath);
                }
            }
            var files = String.Format("{0}{1}", this.RelativePath, outputFileName).PadRight(40);
            return String.Format("{0} :: {1}", files, this.CommandLine);
        }

        public ProjectTranspiler(string relativePath, SSOTMEPayload result)
        : this()
        {
            bool localCommand = (ReferenceEquals(result, null));

            this.Name = localCommand ? "LocalCommand" : result.Transpiler.Name;

            this.RelativePath = relativePath.SafeToString().Replace("\\", "/");
            if (Environment.CommandLine.Contains(" "))
            {
                this.CommandLine = Environment.CommandLine.Substring(Environment.CommandLine.IndexOf(" "));
                this.CommandLine = this.CommandLine.Replace("-install", "").Trim();
            }
            else this.CommandLine = String.Empty;

            this.MatchedTranspiler = localCommand ? default(Transpiler) : result.Transpiler;
        }

        internal void Rebuild(SSoTmeProject project)
        {
            Console.WriteLine("RE-transpiling: " + this.RelativePath + ": " + this.Name);
            Console.WriteLine("CommandLine:> ssotme {0}", this.CommandLine);
            var transpileRootDI = new DirectoryInfo(Path.Combine(project.RootPath, this.RelativePath.Trim("\\/".ToCharArray())));
            if (!transpileRootDI.Exists) transpileRootDI.Create();

            Environment.CurrentDirectory = transpileRootDI.FullName;
            var cliHandler = SSoTmeCLIHandler.CreateHandler(this.CommandLine);
            var cliResult = cliHandler.TranspilerProject(this);
            if (cliResult != 0) throw new Exception("Error RE-Transpiling");
        }

        internal void Clean(SSoTmeProject project, bool preserveZFS)
        {
            Console.WriteLine("CLEANING: " + this.RelativePath + ": " + this.Name);
            Console.WriteLine("CommandLine:> ssotme {0}", this.CommandLine);
            Environment.CurrentDirectory = Path.Combine(project.RootPath, this.RelativePath.Trim("\\/".ToCharArray()));
            String zsfFileName = String.Format("{0}.zfs", this.Name.ToTitle().ToLower().Replace(" ", "-"));
            var zfsFI = new FileInfo(zsfFileName);
            if (zfsFI.Exists)
            {
                var zippedFileSet = File.ReadAllBytes(zfsFI.FullName);
                zippedFileSet.CleanZippedFileSet();
                if (!preserveZFS) File.Delete(zfsFI.FullName);
            }
        }

        public void Describe(SSoTmeProject project)
        {
            Console.WriteLine("\n-----------------------------------");
            Console.WriteLine("---- {0}{1}", this.Name, this.IsDisabled ? "    **** DISABLED ****" : "");
            Console.WriteLine("---- .{0}/", this.RelativePath.Replace("\\", "/"));
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("\nCommand Line:> ssotme {0}\n", this.CommandLine);
        }

        public bool IsAtPath(string relativePath)
        {
            relativePath = relativePath.Replace("\\", "/").ToLower();

            return this.RelativePath
                       .SafeToString()
                       .Replace("\\", "/")
                       .ToLower()
                       .StartsWith(relativePath);
        }

        public void LoadInputAndOuputFiles(SSoTmeProject project, bool includeContents)
        {

            var cliHandler = SSoTmeCLIHandler.CreateHandler(this.CommandLine);
            if (!includeContents)
            {
                foreach (var fsf in cliHandler.FileSet.FileSetFiles)
                {
                    fsf.ClearContents();
                }
                cliHandler.inputFileSetXml = String.Empty;
                cliHandler.SSoTmeProject = null;
            }

            if (!ReferenceEquals(this.MatchedTranspiler, null))
            {
                cliHandler.LoadOutputFiles(this.MatchedTranspiler.LowerHyphenName, this.GetProjectRelativePath(project), includeContents);
            }
            this.CLIHandler = cliHandler;
        }

        public String GetProjectRelativePath(SSoTmeProject project)
        {
            var fullPath = Path.Combine(project.RootPath, this.RelativePath.Trim("\\/".ToCharArray()));
            return fullPath.Substring(project.RootPath.Length).Replace("\\", "/");
        }
    }
}