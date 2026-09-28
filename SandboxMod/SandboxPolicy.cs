namespace KV3.Sandbox;

public static class SandboxPolicy
{
    public static bool IsSessionAllowed(bool compatible, int mode, bool hasLocal)
    {
        return compatible && mode == 2 && hasLocal;
    }

    public static bool CanAct(bool sessionAllowed, bool meeting, bool inAnimation)
    {
        return sessionAllowed && !meeting && !inAnimation;
    }

    public static bool CanUseOnlineNoClip(bool compatible, int mode, bool hasLocal,
        bool meeting, bool inAnimation)
    {
        return compatible && mode == 1 && hasLocal && !meeting && !inAnimation;
    }

    public static bool CanTarget(bool sessionAllowed, bool isLocal, bool isDummy,
        bool disconnected, bool dead, bool killing)
    {
        return sessionAllowed && !isLocal && isDummy && !disconnected && (!killing || !dead);
    }
}
