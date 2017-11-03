using SubnauticaSoundMod;

// Adds a call to SubnauticaSoundMod.Main.Init() after GameInput.Awake() is finished executing.
public class GameInput
{
    extern private void orig_Awake();

    private void Awake()
    {
        orig_Awake();
        Main.Init();
    }
}

namespace SubnauticaSoundMod
{
    class InstallStamp
    {
        public static readonly bool Installed = true;
    }
}
