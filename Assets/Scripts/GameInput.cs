using UnityEngine;
using UnityEngine.InputSystem;

// Verwerkt invoer van toetsenbord, muis en de bediening op een telefoon op dezelfde plek.
public static class GameInput
{
    private static int interactionFrame = -1;
    public static string InteractHint => MobileControls.Active ? "[USE]" : "[E]";

    public static Vector2 Movement
    {
        get
        {
            Vector2 movement = MobileControls.Active ? MobileControls.Move : Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) movement.y += 1f;
                if (keyboard.sKey.isPressed) movement.y -= 1f;
                if (keyboard.aKey.isPressed) movement.x -= 1f;
                if (keyboard.dKey.isPressed) movement.x += 1f;
            }
            return Vector2.ClampMagnitude(movement, 1f);
        }
    }

    public static bool ReloadPressed => MobileControls.ReloadPressed ||
        (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame);
    public static bool MeleePressed => MobileControls.MeleePressed ||
        (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) ||
        (!MobileControls.Active && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
    public static bool SwitchPressed => MobileControls.SwitchPressed;

    public static bool FireRequested(bool automatic)
    {
        // Zolang de richtknop wordt vastgehouden, blijft het wapen schieten met zijn bestaande vuursnelheid.
        // Negeer muisklikken die de browser nabootst bij aanraking van andere knoppen.
        if (MobileControls.Active) return MobileControls.Firing;
        return Mouse.current != null && (automatic
            ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame);
    }

    public static bool TryInteract()
    {
        bool pressed = MobileControls.InteractPressed ||
            (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        if (!pressed || interactionFrame == Time.frameCount) return false;
        interactionFrame = Time.frameCount;
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => interactionFrame = -1;
}
