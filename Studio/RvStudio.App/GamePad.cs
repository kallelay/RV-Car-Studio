using System.Runtime.InteropServices;

namespace RvStudio.App;

/// <summary>An XInput pad, read when driving. Windows only, and nothing happens without one</summary>
public static class GamePad
{
    [StructLayout(LayoutKind.Sequential)]
    struct State
    {
        public uint Packet;
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    static extern uint GetState(uint index, out State state);

    const ushort A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;
    const short Dead = 5000;
    static ushort last;
    static bool missing;

    public readonly record struct Reading(float Steer, float Throttle, bool Flip, bool Back);

    static float Axis(short v)
    {
        if (Math.Abs((int)v) < Dead) return 0;
        return Math.Clamp((Math.Abs((int)v) - Dead) / (32767f - Dead), 0, 1) * Math.Sign(v);
    }

    /// <summary>Null if no pad is connected. Steering has the game's curve for analogue input: gentle around the middle</summary>
    public static Reading? Read()
    {
        if (!OperatingSystem.IsWindows() || missing) return null;
        State s;
        try
        {
            if (GetState(0, out s) != 0) return null;
        }
        catch (DllNotFoundException) { missing = true; return null; }
        catch (EntryPointNotFoundException) { missing = true; return null; }

        float steer = Axis(s.ThumbLX);
        steer *= Math.Abs(steer);
        float triggers = (s.RightTrigger - s.LeftTrigger) / 255f;
        float throttle = Math.Abs(triggers) > 0.05f ? triggers : Axis(s.ThumbLY);
        ushort pressed = (ushort)(s.Buttons & ~last);
        last = s.Buttons;
        return new Reading(steer, Math.Clamp(throttle, -1, 1), (pressed & X) != 0, (pressed & Y) != 0);
    }
}
