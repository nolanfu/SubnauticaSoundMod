using System;

namespace SubnauticaSoundMod
{
    class Debugging
    {
        public static void DescribeBanks()
        {
            Log.Info("*** Begin outputting sound bank metadata ***");
            FMOD.Studio.Bank[] banks = { };
            object banksResult = FMODUnity.RuntimeManager.StudioSystem.getBankList(out banks);
            Log.Info("Return value of getBankList: {0}", banksResult.ToString());
            foreach (FMOD.Studio.Bank bank in banks)
            {
                Guid bankid = new Guid();
                bank.getID(out bankid);
                string bankPathString = "";
                bank.getPath(out bankPathString);
                int eventCount = 0;
                bank.getEventCount(out eventCount);
                int busCount = 0;
                bank.getBusCount(out busCount);
                Log.Info("Bank ID: {0} - path: {1} - event count: {2} - bus count: {3}", bankid, bankPathString, eventCount, busCount);

                if (eventCount > 0)
                {
                    Log.Info("Event list for {0}:", bankPathString);
                    DescribeBankEvents(bank);
                }

                if (busCount > 0)
                {
                    Log.Info("Bus list for {0}:", bankPathString);
                    DescribeBankBuses(bank);
                }
            }
            Log.Info("*** Finish outputting sound bank metadata ***");
        }

        public static void DescribeBankEvents(FMOD.Studio.Bank bank)
        {
            FMOD.Studio.EventDescription[] descs = { };
            bank.getEventList(out descs);
            foreach (FMOD.Studio.EventDescription desc in descs)
            {
                Guid eventid = new Guid();
                desc.getID(out eventid);
                string eventPathString = "";
                desc.getPath(out eventPathString);
                Log.Info("  Event: {0} - path: {1}", eventid.ToString(), eventPathString);
            }
        }

        public static void DescribeBankBuses(FMOD.Studio.Bank bank)
        {
            FMOD.Studio.Bus[] buses = { };
            bank.getBusList(out buses);
            foreach (FMOD.Studio.Bus bus in buses)
            {
                Guid busid = new Guid();
                bus.getID(out busid);
                string busPathString = "";
                bus.getPath(out busPathString);
                Log.Info("  Bus: {0} - path: {1}", busid.ToString(), busPathString);
            }
        }

        public static void TestPatches()
        {
            Patches.RuntimeManager.GetEventDescriptionPatch.TryOut("event:/sound_mod/player/jump");
        }
    }
}
