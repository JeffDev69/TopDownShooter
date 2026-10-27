using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Keep editor focus and editor-tick timing from swallowing simulated device events.
public sealed class GameplayInputTestScope : IDisposable
{
    private readonly InputSettings previous;
    private readonly InputSettings settings;
    private readonly bool previousRunInBackground;

    public GameplayInputTestScope()
    {
        previous = InputSystem.settings;
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        settings = UnityEngine.Object.Instantiate(previous);
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = settings;
    }

    public void Dispose()
    {
        InputSystem.settings = previous;
        Application.runInBackground = previousRunInBackground;
        UnityEngine.Object.Destroy(settings);
    }

    public static IEnumerator Frames(int count = 2)
    {
        int until = Time.frameCount + count;
        while (Time.frameCount < until) yield return null;
    }
}
