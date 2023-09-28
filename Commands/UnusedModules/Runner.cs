using ElmX.Core;
using ElmX.Core.Console;
using ElmX.Elm;

namespace ElmX.Commands.UnusedModules
{
    class Runner : RunnerBase
    {
        /// <summary>
        /// Run the unused-modules command in the current directory.
        /// </summary>
        /// <param name="options">
        /// The command line options.
        /// </param>
        public void Run(Options options)
        {
            if (options.ShowHelp)
            {
                Help.ShowUnusedModulesOptions();
                Environment.Exit(0);
            }

            List<string> unusedModules = new();

            if (!options.Show && !options.Delete && !options.Pause && !options.Rename)
            {
                Writer.EmptyLine();
                Writer.WriteLine("You asked me to find the unused modules, but you didn't tell me what to do with them. Please use the -h or --help option to see what your options are.");
                Environment.Exit(0);
            }

            Writer.Clear();
            Writer.WriteLine("I will now search for unused modules.");

            if (ElmJson.json.projectType == ProjectType.Application && ElmJson.json.Application != null)
            {
                Elm.Application application = new(ElmJson.json.Application, ElmX_Json);
                unusedModules = Modules.FindUnused(application);
            }
            else if (ElmJson.json.projectType == ProjectType.Package && ElmJson.json.Package != null)
            {
                Elm.Package package = new(ElmJson.json.Package, ElmX_Json);
                unusedModules = Modules.FindUnused(package);
            }
            else
            {
                Writer.EmptyLine();
                Writer.WriteLine("I could not find the source directories for this project.");
                Environment.Exit(0);
            }

            if (unusedModules.Count == 0)
            {
                Writer.EmptyLine();
                Writer.WriteLine("I did not find any unused modules.");
                Environment.Exit(0);
            }

            if (options.Show && options.Rename)
            {
                Writer.EmptyLine();
                Writer.WriteLine("You asked me to show you the unused modules and then rename them. I will do that now.");
                Writer.EmptyLine();
                Writer.WriteLines(unusedModules);

                Rename(unusedModules);

                Environment.Exit(0);
            }

            if (options.Show && options.Pause)
            {
                Writer.EmptyLine();
                Writer.WriteLine("You asked me to show you the unused modules and then pause before deleting them. I will do that now.");
                Writer.EmptyLine();
                Writer.WriteLines(unusedModules);

                PauseUnusedModules(unusedModules);

                Environment.Exit(0);
            }

            if (options.Show && options.Delete)
            {
                Writer.EmptyLine();
                Writer.WriteLine("You asked me to show you the unused modules and then delete them. I will do that now.");
                Writer.EmptyLine();
                Writer.WriteLines(unusedModules);

                DeleteUnusedModules(unusedModules);

                Environment.Exit(0);
            }

            if (options.Delete)
            {
                Writer.EmptyLine();
                Writer.WriteLine("You asked me to delete the unused modules. I will do that now.");

                DeleteUnusedModules(unusedModules);

                Environment.Exit(0);
            }

            if (options.Pause)
            {
                Writer.EmptyLine();
                Writer.WriteLine("You asked me to pause before deleting the unused modules. I will do that now.");

                PauseUnusedModules(unusedModules);

                Environment.Exit(0);
            }

            if (options.Rename)
            {
                Writer.EmptyLine();
                Writer.WriteLine("You asked me to rename the unused modules. I will do that now.");

                Rename(unusedModules);

                Environment.Exit(0);
            }

            if (options.Show)
            {
                Writer.EmptyLine();
                Writer.WriteLine("You asked me to show you the unused modules. I will do that now.");
                Writer.EmptyLine();
                Writer.WriteLines(unusedModules);

                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Delete the unused modules.  
        /// </summary>
        /// <param name="files">
        /// The list of unused modules.
        /// </param>
        private void DeleteUnusedModules(List<string> files)
        {
            foreach (string file in files)
            {
                File.Delete(file);
            }

        }

        /// <summary>
        /// Pause before deleting the unused modules. Request permission before deleting each module.
        /// </summary>
        /// <param name="files">
        /// The list of unused modules.
        /// </param>
        private void PauseUnusedModules(List<string> files)
        {
            foreach (string file in files)
            {
                MaybeDeleteFile(file);
            }
        }

        /// <summary>
        /// Request permission before deleting a module.
        /// </summary>
        /// <param name="file">
        /// The module to delete.
        /// </param>
        private void MaybeDeleteFile(string file)
        {
            Writer.EmptyLine();
            Writer.WriteLine($"Should I delete the following file? (y/n/(q)uit)");
            Writer.WriteLine(file);

            string key = Reader.ReadKey();

            if (key == "y")
            {
                File.Delete(file);
                Writer.WriteLine("Deleted.");
            }
            else if (key == "q")
            {
                Writer.EmptyLine();
                Writer.WriteLine("Exiting...");
                Environment.Exit(0);
            }
            else if (key == "n")
            {
                Writer.EmptyLine();
                Writer.WriteLine("Skipping...");
            }
        }

        /// <summary>
        /// Rename the unused modules by prepending a tilde (~) to the filename.
        /// </summary>
        /// <param name="files">
        /// The list of unused modules.
        /// </param>
        private void Rename(List<string> files)
        {
            foreach (string file in files)
            {
                string? path = System.IO.Path.GetDirectoryName(file);
                string? filename = System.IO.Path.GetFileName(file);

                if (filename != null && path != null)
                {
                    string newFile = System.IO.Path.Combine(path, $"~{filename}");

                    File.Move(file, newFile);

                    Writer.WriteLine($"Renamed: {file} -> ~{filename}");
                }
                else if (filename != null)
                {
                    File.Move(file, "~" + filename);

                    Writer.WriteLine($"Renamed: {file} -> ~{filename}");
                }
                else
                {
                    Writer.WriteLine($"Unable to rename {file}");
                }
            }
        }
    }

}