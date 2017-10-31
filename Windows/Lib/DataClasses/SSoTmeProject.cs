using System;
using System.ComponentModel;
using SassyMQ.SSOTME.Lib.RMQActors;
using System.IO;
using Newtonsoft.Json;
using SassyMQ.Lib.RabbitMQ;
using System.Linq;
using SSoTme.OST.Lib.Extensions;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;

namespace SSoTme.OST.Lib.DataClasses
{
    public partial class SSoTmeProject
    {
        public SSoTmeProject()
        {
            this.InitPoco();
        }

        public static void Init()
        {
            var currentProject = TryToLoad(new DirectoryInfo(Environment.CurrentDirectory));
            if (!ReferenceEquals(currentProject, null))
            {
                throw new Exception(String.Format("Project has already been initialized in: {0}", currentProject.RootPath));
            }
            else
            {
                var newProject = new SSoTmeProject();
                newProject.RootPath = Environment.CurrentDirectory;
                newProject.Name = Path.GetFileName(Environment.CurrentDirectory);
                newProject.Save();
                Console.WriteLine("SSoTme Project Created and Initialized Successfully.");
            }
        }

        public void Save()
        {
            this.Save(new DirectoryInfo(this.RootPath));
        }

        private void Save(DirectoryInfo rootDI)
        {
            this.CheckUniqueIDs();
            string projectJson = JsonConvert.SerializeObject(this);
            File.WriteAllText(this.GetProjectFileName(), projectJson);
        }

        public void CheckUniqueIDs()
        {
            IEnumerable<ProjectTranspiler> dupTranspilers = GetDuplicateTranpsilers();
            while (dupTranspilers.Any())
            {
                var firstDup = dupTranspilers.First();
                firstDup.ProjectTranspilerId = Guid.NewGuid();
                dupTranspilers = GetDuplicateTranpsilers();
            }
        }

        private IEnumerable<ProjectTranspiler> GetDuplicateTranpsilers()
        {
            return this.ProjectTranspilers.Where(pt => this.ProjectTranspilers.Where(otherPT => otherPT != pt).Any(anyOtherPT => anyOtherPT.ProjectTranspilerId == pt.ProjectTranspilerId));
        }

        protected String GetProjectFileName()
        {
            return GetProjectFI().FullName;
        }
        protected FileInfo GetProjectFI()
        {
            return GetProjectFIAt(new DirectoryInfo(this.RootPath));
        }

        protected static FileInfo GetProjectFIAt(DirectoryInfo rootDI)
        {
            return new FileInfo(Path.Combine(rootDI.FullName, "SSoTmeProject.json"));
        }

        private static SSoTmeProject Load(FileInfo projectFI)
        {
            var projectJson = File.ReadAllText(projectFI.FullName);
            var ssotmeProject = JsonConvert.DeserializeObject<SSoTmeProject>(projectJson);
            ssotmeProject.RootPath = projectFI.Directory.FullName;
            if (String.IsNullOrEmpty(ssotmeProject.Name))
            {
                ssotmeProject.Name = Path.GetFileName(ssotmeProject.RootPath);
            }

            return ssotmeProject;
        }

        public static SSoTmeProject LoadOrFail(DirectoryInfo dirToCheck)
        {
            var proj = TryToLoad(dirToCheck);

            if (ReferenceEquals(proj, null))
            {
                throw new Exception(String.Format("\nSSoTmeProject could not be found in {0}.  \n\nPlease run `>ssotme -init` from the root of your project to initialize the SSoTme Project.", dirToCheck.FullName));
            }
            else return proj;
        }
        public static SSoTmeProject TryToLoad(DirectoryInfo dirToCheck)
        {
            FileInfo projectFI = GetProjectFIAt(dirToCheck);

            if (projectFI.Exists) return SSoTmeProject.Load(projectFI);
            else
            {
                // Try parent
                if (ReferenceEquals(dirToCheck.Parent, null)) return default(SSoTmeProject);
                else return TryToLoad(dirToCheck.Parent);
            }
        }

        private static SSoTmeProject Load(DirectoryInfo rootDI)
        {
            return Load(GetProjectFIAt(rootDI));
        }

        public void AddSetting(string setting)
        {
            var partsOfSetting = setting.SafeToString().Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var settingName = partsOfSetting.FirstOrDefault();
            var settingValue = String.Join(String.Empty, partsOfSetting.Skip(1));

            if (string.IsNullOrEmpty(settingName)) throw new Exception("Settings must be in the format of 'name=value'");
            else
            {
                this.ProjectSettings.Add(new ProjectSetting()
                {
                    Name = settingName,
                    Value = settingValue
                });
                Console.WriteLine("Added Setting: {0}: '{1}'", settingName, settingValue);
            }
        }

        internal void ListSettings()
        {
            Console.WriteLine("\nSETTINGS: ");

            if (this.ProjectSettings.Any())
            {
                foreach (var projectSetting in this.ProjectSettings)
                {
                    Console.WriteLine("    - {0} = {1}", projectSetting.Name, projectSetting.Value);
                }
            }
            else Console.WriteLine("NO settings added to the project yet.");
        }

        public void CheckResults()
        {

            Console.WriteLine("Updating transpilers/inputs/outputs and project flow...");

            // Load each tranpspiler and load it's input and output files.
            foreach (var projectTranspiler in this.ProjectTranspilers)
            {
                Environment.CurrentDirectory = Path.Combine(this.RootPath, projectTranspiler.RelativePath.Trim("\\/".ToCharArray()));

                projectTranspiler.LoadInputAndOuputFiles(this, false);
            }

            var json = JsonConvert.SerializeObject(this);
            json = String.Format("{{ \"{0}\" : {1} }}", this.GetName(), json);
            var xml = json.JsonToXml();
            DirectoryInfo di = new DirectoryInfo(Path.Combine(this.RootPath, "DSPXml"));
            if (!di.Exists) di.Create();

            File.WriteAllText(Path.Combine(di.FullName, "SSoTmeProject.spxml"), xml.OuterXml);
        }

        public void CreateDocs()
        {
            this.CheckResults();

            Console.WriteLine("Updating Docs from latest SPXml results.");

            DirectoryInfo di = new DirectoryInfo(Path.Combine(this.RootPath, "DSPXml"));
            if (!di.Exists) di.Create();

            Environment.CurrentDirectory = di.FullName;

            ProcessStartInfo psi = new ProcessStartInfo("ssotme");
            psi.WorkingDirectory = di.FullName;

            psi.Arguments = "spxml-to-detailed-spxml -i \"./SSoTmeProject.spxml\"";
            var p = Process.Start(psi);
            p.WaitForExit(100000);
            if (!p.HasExited) throw new Exception("Failed waiting for Detailed SP Xml to be created.");
            else
            {
                psi.Arguments = "detailed-spxml-to-html-docs -i \"./SSoTmeProject.dspxml\"";
                p = Process.Start(psi);

                p.WaitForExit(100000);
                if (!p.HasExited) throw new Exception("Failed waiting for Docs to be created.");
                else
                {
                    Console.WriteLine("Analyze completed");
                }
            }

        }

        public string GetName()
        {
            if (String.IsNullOrEmpty(this.Name)) return Path.GetFileName(this.RootPath);
            else return this.Name;
        }

        public void Describe(string relativePath = "")
        {
            Console.WriteLine("\n==========================================");
            Console.WriteLine("======  {0}", this.Name);  
            Console.WriteLine("======    {0}", this.RootPath);
            Console.WriteLine("==========================================");

            Console.WriteLine();

            this.ListSettings();

            Console.WriteLine();


            Console.WriteLine("\nTRANSPILERS: ");
            var matchingProjectTranspilers = this.ProjectTranspilers.ToList();

            if (!String.IsNullOrEmpty(relativePath))
            {
                relativePath = this.GetProjectRelativePath(relativePath);
                matchingProjectTranspilers = matchingProjectTranspilers.Where(wherePT => wherePT.IsAtPath(relativePath)).ToList();
            }

            foreach (var projectTranspiler in matchingProjectTranspilers)
            {
                projectTranspiler.Describe(this);
            }
        }

        public void Install(SSOTMEPayload result)
        {
            string relativePath = this.GetProjectRelativePath(Environment.CurrentDirectory);

            var projectTranspiler = new ProjectTranspiler(relativePath, result);

            this.IntegrateNewTranspiler(projectTranspiler);

            this.Save();
        }

        internal void Update(ProjectTranspiler projectTranspiler, SSOTMEPayload result)
        {
            projectTranspiler.MatchedTranspiler = ReferenceEquals(result, null) ? default(Transpiler) : result.Transpiler;
            this.IntegrateExistingTranspiler(projectTranspiler);

            this.Save();
        }

        private string GetProjectRelativePath(String fullPath)
        {
            var relativePathDI = new DirectoryInfo(fullPath);
            var rootPathDI = new DirectoryInfo(this.RootPath);
            var relativePath = relativePathDI.FullName.Substring(rootPathDI.FullName.Length);
            return relativePath.Replace("\\", "/");
        }

        public void Rebuild(string buildPath, bool includeDisabled)
        {
            var currentDirectory = Environment.CurrentDirectory;
            try
            {
                var relativePath = this.GetProjectRelativePath(buildPath);
                var matchingProjectTranspilers = this.ProjectTranspilers.Where(wherePT => wherePT.IsAtPath(relativePath));
                foreach (var pt in matchingProjectTranspilers)
                {
                    if (!pt.IsDisabled || includeDisabled) pt.Rebuild(this);
                    else Console.WriteLine("\n\n - SKIPPING DISABLED TRANSPILER: {0}\n - {1}\n - {2}\n\n", pt.Name, pt.RelativePath, pt.CommandLine);
                }
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }
        }


        public void Clean(bool preserveZFS)
        {
            this.Clean(this.RootPath, preserveZFS);
        }

        public void Clean(string cleanPath, bool preserveZFS)
        {
            var currentDirectory = Environment.CurrentDirectory;
            try
            {
                var relativePath = this.GetProjectRelativePath(cleanPath);
                var matchingProjectTranspilers = this.ProjectTranspilers.Where(wherePT => wherePT.IsAtPath(relativePath));
                foreach (var pt in matchingProjectTranspilers)
                {
                    pt.Clean(this, preserveZFS);
                }
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }
        }

        public void Rebuild(bool includeDisabled)
        {
            this.Rebuild(this.RootPath, includeDisabled);
        }


        private void IntegrateExistingTranspiler(ProjectTranspiler projectTranspiler)
        {
            this.IntegrateTranspiler(projectTranspiler, false);
        }

        private void IntegrateNewTranspiler(ProjectTranspiler projectTranspiler)
        {
            this.IntegrateTranspiler(projectTranspiler, true);
        }

        private void IntegrateTranspiler(ProjectTranspiler projectTranspiler, bool addIfMissing)
        {
            var matchingTranspiler = this.ProjectTranspilers.FirstOrDefault(fodPT => (fodPT.Name == projectTranspiler.Name) && (fodPT.RelativePath == projectTranspiler.RelativePath));
            int firstIndex = -1;
            while (!ReferenceEquals(matchingTranspiler, null))
            {
                if (firstIndex == -1) firstIndex = this.ProjectTranspilers.IndexOf(matchingTranspiler);
                this.ProjectTranspilers.Remove(matchingTranspiler);
                matchingTranspiler = this.ProjectTranspilers.FirstOrDefault(fodPT => (fodPT.Name == projectTranspiler.Name) && (fodPT.RelativePath == projectTranspiler.RelativePath));
            }
            if (firstIndex >= 0) this.ProjectTranspilers.Insert(firstIndex, projectTranspiler);
            else if (addIfMissing) this.ProjectTranspilers.Add(projectTranspiler);
        }


        public void RemoveSetting(string setting)
        {
            var partsOfSetting = setting.SafeToString().Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var settingName = partsOfSetting.FirstOrDefault();
            var settingValue = String.Join(String.Empty, partsOfSetting.Skip(1));

            if (string.IsNullOrEmpty(settingName)) throw new Exception("Setting name not provided - unable to remove.");
            else
            {
                var matchingSetting = this.ProjectSettings.FirstOrDefault(fodSetting => fodSetting.Name.Equals(settingName, StringComparison.OrdinalIgnoreCase));
                if (ReferenceEquals(matchingSetting, null)) throw new Exception(String.Format("Can't find matching setting: {0}", settingName));
                else
                {
                    this.ProjectSettings.Remove(matchingSetting);
                    Console.WriteLine("Successfully Removed Setting: {0}: '{1}'", matchingSetting.Name, matchingSetting.Value);
                }
            }
        }
    }
}