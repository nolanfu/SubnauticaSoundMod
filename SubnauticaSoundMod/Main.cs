using System;
using System.Diagnostics;
using System.Collections.Generic;
using FMODUnity;

namespace SubnauticaSoundMod
{
    public static class Main
    {
        #region Variables

        public static readonly string NativeGUIDFileName = "NativeGUIDs.txt";
        public static readonly string ModGUIDFileName = "SoundModGUIDs.txt";

        public static readonly string ModEventPrefix = "event:/sound_mod";
        public static readonly string ModBankPrefix = "bank:/";
        public static readonly string ModBusPrefix = "bus:/";

        public static Dictionary<string, string> ModEvents = new Dictionary<string, string>();
        public static Dictionary<string, string> ModBuses = new Dictionary<string, string>();
        public static Dictionary<string, string> NativeBuses = new Dictionary<string, string>();
        public static List<string> ModBanks = new List<string>();

        public static Dictionary<string, string> Replacements = new Dictionary<string, string>();
        public static Dictionary<string, string> GuidMap = new Dictionary<string, string>();

        private static bool initHasBeenRun = false;

        #endregion

        #region Init

        public static void Init()
        {
            if (initHasBeenRun)
            {
                return;
            }
            ReadModGUIDFile();
            InitReplacements();
            Patches.Setup.PatchAll();
            initHasBeenRun = true;
        }

        #endregion

        #region Helpers

        public static string NativeGUIDsPath()
        {
            return UnityEngine.Application.streamingAssetsPath + "/" + NativeGUIDFileName;
        }

        public static string ModGUIDsPath()
        {
            return UnityEngine.Application.streamingAssetsPath + "/" + ModGUIDFileName;
        }

        public static string GetBankPath(string bankName)
        {
            return string.Format("{0}/{1}.bank", UnityEngine.Application.streamingAssetsPath, bankName);
        }

        #endregion

        #region Sound Bank Metadata

        public static void ReadModGUIDFile()
        {
            if (!System.IO.File.Exists(ModGUIDsPath()))
            {
                Log.Info("No {0} file found to load replacements from. No sounds will be modified on this run.", ModGUIDFileName);
                return;
            }

            string[] guidLines = System.IO.File.ReadAllLines(ModGUIDsPath());
            foreach (string line in guidLines)
            {
                if (line.Contains(" " + ModEventPrefix))
                {
                    string[] parts = line.Replace(" " + ModEventPrefix, "|").Split('|');
                    ModEvents[parts[1]] = parts[0];
                }
                if (line.Contains(" " + ModBusPrefix))
                {
                    string[] parts = line.Replace(" " + ModBusPrefix, "|").Split('|');
                    ModBuses[parts[1]] = parts[0];
                }
                if (line.Contains(" " + ModBankPrefix))
                {
                    string bankName = line.Replace(" " + ModBankPrefix, "|").Split('|')[1];
                    if (System.IO.File.Exists(GetBankPath(bankName)) && bankName != "Master Bank")
                    {
                        ModBanks.Add(bankName);
                    }
                }
            }
        }

        public static FMOD.Studio.Bus[] AllBuses()
        {
            FMOD.Studio.Bank[] banks = { };
            object banksResult = FMODUnity.RuntimeManager.StudioSystem.getBankList(out banks);
            List<FMOD.Studio.Bus> output = new List<FMOD.Studio.Bus>();
            foreach (FMOD.Studio.Bank bank in banks)
            {
                int busCount = 0;
                bank.getBusCount(out busCount);
                if (busCount > 0)
                {
                    FMOD.Studio.Bus[] buses = { };
                    bank.getBusList(out buses);
                    foreach (FMOD.Studio.Bus bus in buses)
                    {
                        output.Add(bus);
                    }
                }
            }
            return output.ToArray();
        }

        public static void WriteNativeGUIDFile()
        {
            List<string> output = new List<string>();
            foreach (FMOD.Studio.Bus bus in AllBuses())
            {
                Guid busid = new Guid();
                bus.getID(out busid);
                string busPathString = "";
                bus.getPath(out busPathString);
                output.Add(busid.ToString() + " " + busPathString);
            }
            System.IO.File.WriteAllLines(NativeGUIDsPath(), output.ToArray());
        }

        public static void RecordNativeBuses()
        {
            List<string> output = new List<string>();
            foreach (FMOD.Studio.Bus bus in AllBuses())
            {
                Guid busid = new Guid();
                bus.getID(out busid);
                string busPathString = "";
                bus.getPath(out busPathString);
                NativeBuses[busPathString.Replace(ModBusPrefix, "")] = busid.ToString();
            }
        }

        public static void OverwriteModBanksWithNativeGUIDs()
        {
            if (NativeBuses.Count == 0 || ModBuses.Count == 0)
            {
                return;
            }
            foreach (string bankName in Main.ModBanks.ToArray())
            {
                string bankPath = GetBankPath(bankName);
                Stopwatch timer = new Stopwatch();
                timer.Start();
                Log.Info("Loading {0} for bus GUID rewriting...", bankName);
                byte[] bankData = System.IO.File.ReadAllBytes(bankPath);
                // FIXME: Converting to a hex string, doing string replacement, and the converting
                // back to byte[] is embarrassingly inefficient.
                Log.Info("  Converting to hex string...");
                string bankString = BytesToHexString(bankData);
                foreach (KeyValuePair<string, string> pair in ModBuses)
                {
                    if (NativeBuses.ContainsKey(pair.Key))
                    {
                        string modBusGuidString = BytesToHexString(new Guid(pair.Value).ToByteArray());
                        string nativeBusGuidString = BytesToHexString(new Guid(NativeBuses[pair.Key]).ToByteArray());
                        int count = System.Text.RegularExpressions.Regex.Matches(bankString, modBusGuidString).Count;
                        if (count > 0)
                        {
                            Log.Info("  Replacing {0} references to bus '/{1}' ({2} -> {3})...", count, pair.Key, modBusGuidString, nativeBusGuidString);
                            bankString = bankString.Replace(modBusGuidString, nativeBusGuidString);
                        }
                    }
                }
                Log.Info("  Converting back to bytes...");
                byte[] result = HexStringToBytes(bankString);
                Log.Info("  Writing back to file...");
                System.IO.File.WriteAllBytes(bankPath, result);
                timer.Stop();
                Log.Info("  Finished in {0} seconds.", System.Convert.ToDouble(timer.ElapsedMilliseconds) / 1000.0);
            }
        }

        public static string BytesToHexString(byte[] data)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            foreach (byte b in data)
            {
                result.Append(b.ToString("x2"));
            }
            return result.ToString();
        }

        public static byte[] HexStringToBytes(string data)
        {
            int length = data.Length / 2;
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);
            }
            return result;
        }

        public static void LoadModBanks()
        {
            Settings settings = Settings.Instance;
            foreach (string bank in ModBanks.ToArray())
            {
                Log.Info("Adding {0} to bank list.", bank);
                settings.Banks.Add(bank);
                try
                {
                    RuntimeManager.LoadBank(bank, settings.AutomaticSampleLoading);
                }
                catch (BankLoadException exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }
            RuntimeManager.WaitForAllLoads();
        }

        #endregion

        #region Event Replacement

        public static void InitReplacements()
        {
            Log.Info("Adding event replacements...");
            foreach (KeyValuePair<string, string> pair in ModEvents)
            {
                AddSoundReplacement(pair.Key, pair.Value);
            }
            Log.Info("Finished setting up event replacements.");
        }

        public static void AddSoundReplacement(string eventPath, string guidString)
        {
            string moddedPath = ModEventPrefix + eventPath.ToLower();
            Log.Info("  Adding replacement: {0} -> {1} : {2}", "event:" + eventPath, moddedPath, guidString);
            Replacements["event:" + eventPath.ToLower()] = moddedPath;
            GuidMap[moddedPath] = guidString;
        }

        public static void ConvertEventPath(ref string eventPath)
        {
            if (Replacements.ContainsKey(eventPath.ToLower()))
            {
                Log.Info("Replacing {0} with {1}", eventPath, Main.Replacements[eventPath.ToLower()]);
                eventPath = Replacements[eventPath.ToLower()];
            }
            if (GuidMap.ContainsKey(eventPath.ToLower()))
            {
                Log.Info("Converting {0} to GUID {1}", eventPath, Main.GuidMap[eventPath.ToLower()]);
                eventPath = GuidMap[eventPath.ToLower()]; 
            }
        }

        #endregion
    }


}
