﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Ambermoon
{
    internal class Configuration : IConfiguration
    {
        public ScreenRatio ScreenRatio { get; set; } = ScreenRatio.Ratio16_10;
        public int? Width { get; set; } = 1280;
        public int? Height { get; set; } = 800;
        public bool Fullscreen { get; set; } = false;
        public bool UseDataPath { get; set; } = false;
        public string DataPath { get; set; } = ExecutableDirectoryPath;
        public SaveOption SaveOption { get; set; } = SaveOption.ProgramFolder;
        public int GameVersionIndex { get; set; } = 0;
        public bool LegacyMode { get; set; } = false;
        public bool Music { get; set; } = true;
        public bool FastBattleMode { get; set; } = false;

        public static readonly string FallbackConfigDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ambermoon");

        public static string ExecutableDirectoryPath
        {
            get
            {
                bool isWindows = System.Environment.OSVersion.Platform == System.PlatformID.Win32NT;

                var assemblyPath = Process.GetCurrentProcess().MainModule.FileName;

                if (assemblyPath.EndsWith("dotnet"))
                {
                    assemblyPath = Assembly.GetExecutingAssembly().Location;
                }

                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

                if (isWindows)
                {
                    if (assemblyDirectory.EndsWith("Debug") || assemblyDirectory.EndsWith("Release") || assemblyDirectory.EndsWith("netcoreapp3.1"))
                    {
                        string projectFile = Path.GetFileNameWithoutExtension(assemblyPath) + ".csproj";

                        var root = new DirectoryInfo(assemblyDirectory);

                        while (root.Parent != null)
                        {
                            if (File.Exists(Path.Combine(root.FullName, projectFile)))
                                break;

                            root = root.Parent;

                            if (root.Parent == null) // we could not find it (should not happen)
                                return assemblyDirectory;
                        }

                        return root.FullName;
                    }
                    else
                    {
                        return assemblyDirectory;
                    }
                }
                else
                {
                    return assemblyDirectory;
                }
            }
        }

        public static Configuration Load(string filename, Configuration defaultValue = null)
        {
            if (!File.Exists(filename))
                return defaultValue;

            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        }

        public void Save(string filename)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            File.WriteAllText(filename, JsonConvert.SerializeObject(this,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                })
            );
        }
    }
}
