using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using UnityEngine;
using FMODUnity;

namespace SubnauticaSoundMod
{
    namespace Patches
    {
        class Setup
        {
            private static readonly HarmonyInstance harmony = HarmonyInstance.Create("com.bovinesensibilities.subnautica.soundmod");

            public static void PatchAll()
            {
                // Enabling this creates a log file on your desktop, showing the emitted IL instructions.
                // HarmonyInstance.DEBUG = false;

                Log.Info("Applying SubnauticaSoundMod patches...");

                // We can't use Harmony's nice annotations to apply these because some of the arguments are by ref,
                // and MakeByRefType(), ref, out, etc. don't work in annotations.
                PatchWithClass(
                    Harmony.AccessTools.Method(typeof(global::FMODUWE), "GetEventDescription", new[] { typeof(string), typeof(FMOD.Studio.EventDescription).MakeByRefType() }),
                    typeof(Patches.FMODUWE.GetEventDescriptionPatch)
                );
                PatchWithClass(
                    Harmony.AccessTools.Method(typeof(FMODUnity.RuntimeManager), "GetEventDescription", new[] { typeof(string) }),
                    typeof(Patches.RuntimeManager.GetEventDescriptionPatch)
                );
                PatchWithClass(
                    Harmony.AccessTools.Method(typeof(FMODUnity.RuntimeManager), "Initialiase", new[] { typeof(bool) }),
                    typeof(Patches.RuntimeManager.InitialiasePatch)
                );
                Log.Info("Completed patching using " + Assembly.GetExecutingAssembly().FullName);
            }

            public static void PatchWithClass(MethodInfo target, Type patchClass)
            {
                if (target == null)
                {
                    Log.Error("  Null target passed in for patch class " + patchClass.ToString() + ". Doing nothing.");
                    return;
                }
                Log.Info("  Patching " + target.ToString() + ".");
                MethodInfo prefix = Harmony.AccessTools.Method(patchClass, "Prefix");
                MethodInfo postfix = Harmony.AccessTools.Method(patchClass, "Postfix");
                harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
            }
        }

        #region Harmony Patches

        namespace FMODUWE
        {
            class GetEventDescriptionPatch
            {
                static void Prefix(ref string eventPath, FMOD.Studio.EventDescription eventDesc)
                {
                    // Log.Info("FMODUWE.GetEventDescription - eventPath: {0}", eventPath.ToString());
                    Main.ConvertEventPath(ref eventPath);
                }

                static void Postfix(string eventPath, FMOD.Studio.EventDescription eventDesc)
                {

                }
            }
        }

        namespace RuntimeManager
        {
            class GetEventDescriptionPatch
            {
                static void Prefix(ref string path)
                {
                    // Log.Info("RuntimeManager.GetEventDescription - path: {0}", path.ToString());
                    Main.ConvertEventPath(ref path);
                }

                static void Postfix(string path)
                {

                }

                public static void TryOut(string eventPath)
                {
                    FMOD.Studio.EventDescription mydesc = FMODUnity.RuntimeManager.GetEventDescription(eventPath);
                    if (mydesc != null)
                    {
                        Guid descId = new Guid();
                        mydesc.getID(out descId);
                        Log.Info("GUID from FMODUnity.RuntimeManager.GetEventDescription('{0}'): {1}", eventPath, descId.ToString());
                    }
                }
            }

            class InitialiasePatch // Oh FMOD.
            {
                static void Prefix(bool forceNoNetwork)
                {
                }

                static void Postfix(bool forceNoNetwork)
                {
                    Main.WriteNativeGUIDFile();
                    Main.RecordNativeBuses();
                    Main.OverwriteModBanksWithNativeGUIDs();
                    Main.LoadModBanks();
                    // Debugging.DescribeBanks();
                    // Debugging.TestPatches();
                }
            }
        }

        #endregion
    }
}
