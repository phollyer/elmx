using ElmX.Core.Console;
using ElmX.Elm.App;
using ElmX.Elm.Code;

namespace ElmX.Elm
{
    public class Application : Elm
    {
        public List<string> SourceDirs { get; private set; } = new();

        public Module EntryModule { get; set; }

        public Application(App.Json json, Core.Json elmxJson)
        {

            foreach (string srcDir in json.SourceDirs)
            {
                if (!srcDir.StartsWith(".."))
                {
                    SourceDirs.Add(srcDir);
                }
            }

            ExcludeDirs = elmxJson.AppJson.ExcludeDirs;
            ExcludeFiles = elmxJson.AppJson.ExcludeFiles;

            string entryFile = elmxJson.AppJson.EntryFile;

            EntryModule = new(entryFile);
            EntryModule.ParseImports();

            ModulePaths.Add(EntryModule.FilePath);

            FileList = FindAllFiles(SourceDirs);
        }
    }
}