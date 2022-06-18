using Newtonsoft.Json;
using SassyMQ.Lib.RabbitMQ;
using SassyMQ.Lib.RabbitMQ.Payload;
using SSoTme.OST.Lib.CLIOptions;
using SSoTme.OST.Lib.DataClasses;
using SSoTme.OST.Lib.Extensions;
using SSoTme.OST.Lib.SassySDK.Derived;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SassyMQ.SSOTME.Lib.RMQActors
{
    public class SSOTMEPayload : StandardPayload<SSOTMEPayload>, ISSOTMEPayload
    {
        static SSOTMEPayload()
        {
            SSOTMEExtensions.FileWritten += CDVExtensions_FileWritten;
        }

        private static void CDVExtensions_FileWritten(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(sender.SafeToString());
            if (fi.Name == "output.txt" || fi.Name == "search.txt") Console.WriteLine(File.ReadAllText(fi.FullName));
            Console.WriteLine(sender.SafeToString());
        }

        public string GetParameterByIndex(int parameterIndex)
        {
            if (!this.CLIParams.Skip(parameterIndex).Any()) throw new Exception(string.Format("Parameter {0} not found.", parameterIndex));
            else
            {
                var param = this.CLIParams.Skip(parameterIndex).First();
                return param.Substring(param.IndexOf("=") + 1);
            }
        }

        public bool HasParamNamed(string paramName)
        {
            return this.CLIParams.Any(anyParam => anyParam.StartsWith(String.Format("{0}=", paramName)));
        }

        public string GetParameterByName(string paramName)
        {
            if (!this.HasParamNamed(paramName)) throw new Exception(string.Format("Parameter {0} not found.", paramName));
            else
            {
                var param = this.CLIParams.First(firstParam => firstParam.StartsWith(String.Format("{0}=", paramName)));
                return param.Substring(param.IndexOf("=") + 1);
            }
        }


        public SSOTMEPayload()
        {
            this.Settings = new Dictionary<string, string>();
            this.CLIParams = new List<string>();
        }


        public Guid AccessToken { get; set; }
        public string EmailAddress { get; set; }
        public TranspilerHost TranspilerHost { get; set; }
        public string InstanceName { get; set; }
        public bool IsDirectMessage { get; set; }
        public string RecoverEmail { get; set; }
        public string RegisterEmailAddress { get; set; }
        public string RegisterName { get; set; }
        public Dictionary<String, String> Settings { get; set; }

        public string GetSetting(string settingName, string defaultStringValue)
        {
            if (!this.Settings.ContainsKey(settingName) || String.IsNullOrEmpty(this.Settings[settingName]))
            {
                return defaultStringValue;
            }
            else return this.Settings[settingName];
        }

        public string AuthToken { get; set; }
        public string ScreenName { get; set; }
        public string Secret { get; set; }
        public Transpiler Transpiler { get; set; }
        public TranspileRequest TranspileRequest { get; set; }
        public TranspilerInstance TranspilerInstance { get; set; }
        public string TranspilerName { get; set; }
        public List<Transpiler> Transpilers { get; set; }
        public string BaseRoutingKey { get; set; }
        public bool RegisterEmailSent { get; set; }
        public bool IsNewAccount { get; set; }
        public List<PublicAccount> PublicAccounts { get; set; }

        public string CLIAccount { get; set; }
        public List<string> CLIInput { get; set; }
        public string CLIInputFileContents { get; set; }
        public string CLIInputFileSetJson { get; set; }
        public string CLIInputFileSetXml { get; set; }
        public string CLIOutput { get; set; }
        public List<String> CLIParams { get; set; }
        public String CLITranspiler { get; set; }
        public int CLIWaitTimeout { get; set; }
        public List<FileType> FileTypes { get; set; }
        public SSoTmeProject SSoTmeProject { get; set; }
        public SSOTMEKey SSoTmeKey { get; set; }
        public bool ReturnJson { get; set; }

        public void CleanFileSet()
        {
            if (this.Transpiler is null) return;
            else
            {
                FileInfo zfsFI = GetZFSFI();
                if (zfsFI.Exists)
                {
                    var previousFileSet = File.ReadAllBytes(zfsFI.FullName);
                    previousFileSet.CleanZippedFileSet();
                }
            }
        }




        public override string ToString()
        {
            return String.Format("{0} - {1}", this.SenderId, this.RoutingKey);
        }

        public int SaveFileSet(bool skipClean)
        {
            if (!skipClean) this.CleanFileSet();

            // Save the file set to the disk
            var fileSetXml = this.TranspileRequest.ZippedOutputFileSet.UnzipToString();
            var tempFI = new FileInfo(String.Format("tempFileSet_{0}.xml", Guid.NewGuid()));
            File.WriteAllText(tempFI.FullName, fileSetXml);

            SSOTMEExtensions.SplitFileSetFile(tempFI.FullName, tempFI.Directory.FullName);
            tempFI.Delete();

            this.SavePreviousFileSet(fileSetXml);

            return 0;
        }

        public void SavePreviousFileSet(string fileSetXml)
        {
            if (this.Transpiler is null) return;
            else
            {
                var zfsFI = this.GetZFSFI();
                if (!zfsFI.Directory.Exists) zfsFI.Directory.Create();
                File.WriteAllBytes(zfsFI.FullName, fileSetXml.Zip());
            }
        }

        private FileInfo GetZFSFI()
        {
            var curDir = Environment.CurrentDirectory;
            var relPath = curDir.Substring(this.SSoTmeProject.RootPath.Length);

            var ssotmeDI = new DirectoryInfo(String.Format("{0}/.ssotme", this.SSoTmeProject.RootPath));
            if (!ssotmeDI.Exists)
            {
                ssotmeDI.Create();
                ssotmeDI.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }

            var zfsFileName = String.Format("{0}/{1}/{2}.zfs", ssotmeDI.FullName, relPath, this.Transpiler.LowerHyphenName, "", "");
            //var zfsFileName = String.Format("{0}.zfs", this.Transpiler.LowerHyphenName);
            var zfsFI = new FileInfo(zfsFileName);
            return zfsFI;
        }

        internal void SaveCLIOptions(SSoTmeCLIHandler sSoTmeCLIHandler)
        {
            this.CLIAccount = sSoTmeCLIHandler.account;
            this.CLIInput = sSoTmeCLIHandler.input;
            this.CLIInputFileContents = sSoTmeCLIHandler.inputFileContents;
            this.CLIInputFileSetXml = sSoTmeCLIHandler.inputFileSetXml;
            this.CLIOutput = sSoTmeCLIHandler.output;
            this.CLIParams = sSoTmeCLIHandler.parameters;
            this.CLITranspiler = sSoTmeCLIHandler.transpiler;
            this.CLIWaitTimeout = sSoTmeCLIHandler.waitTimeout;
        }
    }
}

