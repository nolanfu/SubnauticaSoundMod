using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Mono.Cecil;

namespace Installer 
{
    class Program
    {
        static int Main(string[] args)
        {
            Install installer = new Install(args);
            return installer.Up();
        }
    }

    class Install 
    {
        #region Constants

        public static readonly string SteamAppID = "264710";
        public static readonly string[] PermanentBinaries =
        {
            "SubnauticaSoundMod.dll", "0Harmony.dll"
        };
        public static readonly string[] TemporaryBinaries =
        {
            "Assembly-CSharp.Mod.mm.dll", "MonoMod.exe", "Mono.Cecil.dll", "Mono.Cecil.Mdb.dll", "Mono.Cecil.Pdb.dll"
        };

        public static readonly string[] ExpectedBinaries = PermanentBinaries.Concat(TemporaryBinaries).ToArray();

		public static BindingFlags AllBindings = BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.Instance
			| BindingFlags.Static
			| BindingFlags.GetField
			| BindingFlags.SetField
			| BindingFlags.GetProperty
			| BindingFlags.SetProperty;

        public static readonly int MonomodTimeout = 60;

        #endregion

        #region Variables

        string installPath;
        string installPathOverride;
        string logPrefix;

        List<string> binariesToRemove;

        #endregion

        #region constructor

        public Install(string[] args)
        {
            installPath = "";
            if (args.Length > 0)
            {
                installPathOverride = args[0];
            } else
            {
                installPathOverride = null;
            }
            logPrefix = "";
            binariesToRemove = new List<string>();
        }

        #endregion

        #region Paths

        string GetDefaultInstallPath()
        {
            string steamPath = "";
            object regKey = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null);
            if (regKey != null)
            {
                steamPath = regKey.ToString();
            }
            else
            {
                Log("Can't find Steam in Windows registry.");
            }
            return steamPath + @"/steamapps/common/Subnautica";
        }

        string GetManagedPath()
        {
            return installPath + @"/Subnautica_Data/Managed";
        }

        string GetStreamingAssetsPath()
        {
            return installPath + @"/Subnautica_Data/StreamingAssets";
        }

        #endregion

        public int Up()
        {
            Reset();
            Log("Installing SubnauticaSoundMod.");
            Indent();
            string[][] steps =
            {
                new string[] { "ConfirmInstallPath", "Couldn't find Steam directory. Can't install." },
                new string[] { "ConfirmPackagedBinaries", "Some installation files missing. Can't install." },
                new string[] { "InstallDlls", "Failed to install dlls." },
                new string[] { "ConfirmPatchesApplied", "Failed to apply patch." },
                new string[] { "InstallSoundBanks", "Failed to install sound banks." }
            };
            foreach (string[] step in steps) {
                MethodInfo method = GetType().GetMethod(step[0], AllBindings);
                bool result = Convert.ToBoolean(method.Invoke(this, new object[] { }));
                if (!result)
                {
                    Unindent();
                    Log(step[1]);
                    return 1;
                }
            }
            Unindent();
            Log("Installation complete. Have fun!");
            return 0;
        }

        #region Steps

        bool ConfirmInstallPath()
        {
            if (installPathOverride != null)
            {
                installPath = installPathOverride;
            }
            else
            {
                installPath = GetDefaultInstallPath();
            }
            if (!System.IO.Directory.Exists(installPath))
            {
                return false;
            }
            if (!System.IO.Directory.Exists(GetManagedPath()))
            {
                return false;
            }
            if (!System.IO.Directory.Exists(GetStreamingAssetsPath()))
            {
                return false;
            }
            return true;
        }


        bool ConfirmPackagedBinaries()
        {
            foreach (string fileName in ExpectedBinaries)
            {
                string path = @"bin/" + fileName;
                if (!System.IO.File.Exists(path))
                {
                    Log("Can't find required installation file '{0}'.", path);
                    return false;
                }
            }
            return true;
        }

        bool InstallDlls()
        {
            return CopyFiles(PermanentBinaries, @"bin/", GetManagedPath());
        }

        bool ConfirmPatchesApplied()
        {
            return ConfirmPatchApplied("Assembly-CSharp.dll", "Assembly-CSharp.original.dll", "SubnauticaSoundMod.InstallStamp");
        }

        bool InstallSoundBanks()
        {
            string[] fileNames = System.IO.Directory.GetFiles(@"banks/").Select(path => System.IO.Path.GetFileName(path)).ToArray();
            return CopyFiles(fileNames, @"banks/", GetStreamingAssetsPath());
        }

        #endregion

        #region Patching

        bool ConfirmPatchApplied(string targetFile, string backupName, string alreadyInstalledType)
        {
            string targetPath = GetManagedPath() + "/" + targetFile;
            if (!System.IO.File.Exists(targetPath))
            {
                Log("Couldn't find {0} to patch.", targetFile);
                return false;
            }
            Log("Checking to see whether {0} needs to be patched...", targetFile);
            if (!NeedsPatch(targetPath, alreadyInstalledType))
            {
                Log("{0} is already patched.", targetFile);
                return true;
            }
            if (!ApplyPatch(targetFile, backupName))
            {
                return false;
            }
            Log("{0} successfully patched.", targetFile);
            return true;
        }

        bool NeedsPatch(string targetPath, string alreadyInstalledType)
        {
            ModuleDefinition module = ModuleDefinition.ReadModule(targetPath);
            foreach (TypeDefinition type in module.Types)
            {
                if (type.FullName == alreadyInstalledType)
                {
                    module.Dispose();
                    return false;
                }
            }
            module.Dispose();
            return true;
        }
        
        bool ApplyPatch(string targetFile, string backupName)
        {
            if (!CopyFiles(TemporaryBinaries, @"bin/", GetManagedPath()))
            {
                return false;
            }
            Log("Applying patch to {0}...", targetFile);
            Indent();
            if (!RunMonomod(targetFile))
            {
                Unindent();
                return false;
            }
            RemoveFiles(TemporaryBinaries, GetManagedPath());

            Log("Swapping patched version of {0} in, moving original to {1}", targetFile, backupName);
            System.IO.File.Move(GetManagedPath() + "/" + targetFile, GetManagedPath() + "/" + backupName);
            System.IO.File.Move(GetManagedPath() + "/MONOMODDED_" + targetFile, GetManagedPath() + "/" + targetFile);
            Unindent();
            return true;
        }

        bool RunMonomod(string targetFile)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(GetManagedPath() + "/MonoMod.exe", targetFile);
            startInfo.WorkingDirectory = GetManagedPath();
            Log(startInfo.WorkingDirectory);
            System.Diagnostics.Process monomod = System.Diagnostics.Process.Start(startInfo);
            monomod.WaitForExit(MonomodTimeout * 1000);
            if (!monomod.HasExited)
            {
                Log("Monomod still not finished after {0} seconds. Killing and bailing out.", MonomodTimeout);
                monomod.Kill();
                monomod.Dispose();
                return false;
            }
            if (monomod.ExitCode != 0)
            {
                Log("Failure: Monomod exited with code {0}", monomod.ExitCode);
                monomod.Dispose();
                return false;
            }
            monomod.Dispose();
            return true;
        }

        #endregion

        #region Helpers

        void Reset()
        {
            binariesToRemove.Clear();
        }

        bool CopyFiles(string[] fileNames, string prefix, string destinationFolder)
        {
            Log("Copying files to {0}", destinationFolder);
            Indent();
            foreach (string fileName in fileNames)
            {
                try
                {
                    Log("Copying {0}", fileName);
                    System.IO.File.Copy(prefix + fileName, destinationFolder + @"/" + fileName, true);
                } catch (Exception exception)
                {
                    Unindent();
                    Log("Failed to copy {0} to its destination.", fileName);
                    Log(exception.ToString());
                    return false;
                }
            }
            Unindent();
            return true;
        }

        bool RemoveFiles(string[] fileNames, string targetFolder)
        {
            Log("Removing temporary files from {0}", targetFolder);
            Indent();
            foreach (string fileName in fileNames)
            {
                try
                {
                    System.IO.File.Delete(targetFolder + "/" + fileName);
                }
                catch (Exception exception)
                {
                    Log("Warning: failed to remove file {0}", fileName);
                    Log(exception.ToString());
                }
            }
            Unindent();
            return true;
        }

        #endregion

        #region Logging

        public void Log(string message, params object [] args)
        {
            Console.WriteLine(logPrefix + message, args);
        }

        void Indent()
        {
            logPrefix += "  ";
        }

        void Unindent()
        {
            logPrefix = logPrefix.Substring(0, logPrefix.Length - 2);
        }

        #endregion
    }
}
