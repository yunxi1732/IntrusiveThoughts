using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Networking;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;



public enum GameEvents
{
    PlayerDie,
    PlayerRespawn,
    /// <summary>
    /// an event triggered by gameobject entering teleport edge(two param event: Transform, Vector3)
    /// listened by moveable gameobjects
    /// </summary>
    Teleport,

    /// <summary>
    /// an event triggered by player using pocketwatch or the clock tower(no param event)
    /// listened by world related mechanisms
    /// </summary>
    SwitchWorld,

    /// <summary>
    /// an event triggered by player collecting props(single param event: proptype)
    /// listened by ...
    /// </summary>
    PlayerCollectProps,

    WorldSwitchInLastLevel,

    /// <summary>
    /// notifies which UI/2D object is clicked using raycast (single param event: GameObject)
    /// </summary>
    ObjectClicked,
    /// <summary>
    /// notifies which UI/2D object is the cursor entered using raycast (single param event: GameObject)
    /// </summary>
    ObjectHoverEnter,
    /// <summary>
    /// notifies which UI/2D object is the cursor exited using raycast (single param event: GameObject)
    /// </summary>
    ObjectHoverExit,
    /// <summary>
    /// an event triggered by mine gear after explosion (two param event: List<GameObject>, Vector 3)
    /// </summary>
    MineGearExploded,
    /// <summary>
    /// an event triggered proactively to reload a scene by the GameManager(no param event)
    /// </summary>
    SceneReload,
    /// <summary>
    /// an event triggered proactively to load the next scene by the GameManager(no param event)
    /// </summary>
    LoadNextScene,
    /// <summary>
    /// an event triggered proactively to load the previous scene by the GameManager(no param event)
    /// </summary>
    LoadPreviousScene,
    /// <summary>
    /// an event to toggle the air trap (single param event: GameObject)
    /// </summary>
    ToggleAirTrap,
    /// <summary>
    /// no param event
    /// </summary>
    MainMenuEnable,
    /// <summary>
    /// no param event
    /// </summary>
    MainMenuDisable,
    /// <summary>
    /// toggle the visibility of the UI, passing in a true will turn on (single param event: bool)
    /// </summary>
    TogglePocketWatchUI,
    /// <summary>
    /// toggle the input action for Player, passing in a true will turn on (single param event: bool)
    /// </summary>
    TogglePlayerInput,
    /// <summary>
    /// notifies that a gear of certain quantity is used, should only be listened (two param event: int, Proptype)
    /// </summary>
    UseGear,
    /// <summary>
    /// notifies that a gear of certain quantity is used, should be only be triggered (two param event: int, Proptype)
    /// </summary>
    ConsumeGear,
    /// <summary>
    /// switches the looping direction of the looping platform (no param event)
    /// </summary>
    SwitchLoopingPlatformDir,
    /// <summary>
    /// notifies a world change, providing the world type to the listener (single param event: WordType)
    /// </summary>
    WordChanged,
    /// <summary>
    /// query the inventory (pocket watch UI) for the quantity of a Proptype item (single param event: Proptype, return: int)
    /// </summary>
    InventoryQuery,
    /// <summary>
    /// triggered to use the mine skill (no param event)
    /// </summary>
    UseMineSkill,
    /// <summary>
    /// show the letter before the scene load after the player reached destination (no param event)
    /// </summary>
    ShowLetter,
}

public class ErrorSuppress
{
    public static readonly bool suppress = false;
}

public abstract class EventManager1P<T> : MonoBehaviour
{
    private static readonly Dictionary<GameEvents, Action<T>> EventDictionary = new Dictionary<GameEvents, Action<T>>();
    public static void StartListening(GameEvents gameEventName, Action<T> listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            EventDictionary[gameEventName] += listener;
        }
        else
        {
            EventDictionary[gameEventName] = listener;
        }
    }

    public static void StopListening(GameEvents gameEventName, Action<T> listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            EventDictionary[gameEventName] -= listener;
            if (EventDictionary[gameEventName] == null)
            {
                EventDictionary.Remove(gameEventName);
            }
        }
    }

    public static void TriggerEvent(GameEvents gameEventName, T param)
    {
        if (!EventDictionary.TryGetValue(gameEventName, out var action) || action == null)
            return;

        foreach (var handler in action.GetInvocationList())
        {
            var method = handler as Action<T>;
            var target = method?.Target;

            if (target is UnityEngine.Object unityObj && unityObj == null)
            {
                EventDictionary[gameEventName] -= method;

                string senderName = "UnknownSender";
                try
                {
                    var frame = new System.Diagnostics.StackTrace().GetFrame(1);
                    var methodInfo = frame.GetMethod();
                    senderName = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
                }
                catch { }

                string listenerMethod = method?.Method.Name ?? "UnknownMethod";
                if (!ErrorSuppress.suppress) Debug.LogWarning($"[Event: {gameEventName}] Removed destroyed listener, Sender: {senderName}, Listener Owner: <destroyed>, Method: {listenerMethod}");
            }
            else
            {
                try
                {
                    method?.Invoke(param);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Event: {gameEventName}] Exception while invoking listener: {e.Message}");
                }
            }
        }
    }



    public static void CheckEvent(GameEvents gameEventName, Action<T> listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            print("event exist");
        }
        else
        {
            print("event don't exist");
        }
    }
}

public abstract class EventManagerNP : MonoBehaviour
{
    private static readonly Dictionary<GameEvents, Action> EventDictionary = new Dictionary<GameEvents, Action>();
    public static void StartListening(GameEvents gameEventName, Action listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            EventDictionary[gameEventName] += listener;
        }
        else
        {
            EventDictionary[gameEventName] = listener;
        }
    }

    public static void StopListening(GameEvents gameEventName, Action listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            EventDictionary[gameEventName] -= listener;
            if (EventDictionary[gameEventName] == null)
            {
                EventDictionary.Remove(gameEventName);
            }
        }
    }

    public static void TriggerEvent(GameEvents gameEventName)
    {
        if (!EventDictionary.TryGetValue(gameEventName, out var action) || action == null)
            return;

        foreach (var handler in action.GetInvocationList())
        {
            var method = handler as Action;
            var target = method?.Target;

            if (target is UnityEngine.Object unityObj && unityObj == null)
            {
                EventDictionary[gameEventName] -= method;

                string senderName = "UnknownSender";
                try
                {
                    var frame = new System.Diagnostics.StackTrace().GetFrame(1);
                    var methodInfo = frame.GetMethod();
                    senderName = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
                }
                catch { }

                string listenerMethod = method?.Method.Name ?? "UnknownMethod";
                if (!ErrorSuppress.suppress) Debug.LogWarning($"[Event: {gameEventName}] Removed destroyed listener, Sender: {senderName}, Listener Owner: <destroyed>, Method: {listenerMethod}");
            }
            else
            {
                try
                {
                    method?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Event: {gameEventName}] Exception while invoking listener: {e.Message}");
                }
            }
        }
    }



    public static void CheckEvent(GameEvents gameEventName, Action listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            print("event exist" + EventDictionary[gameEventName].GetInvocationList().Length);
        }
        else
        {
            print("event don't exist");
        }
    }
}

public abstract class EventManager2P<T1, T2> : MonoBehaviour
{
    private static readonly Dictionary<GameEvents, Action<T1, T2>> EventDictionary =
        new Dictionary<GameEvents, Action<T1, T2>>();

    public static void StartListening(GameEvents gameEventName, Action<T1, T2> listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            EventDictionary[gameEventName] += listener;
        }
        else
        {
            EventDictionary[gameEventName] = listener;
        }
    }

    public static void StopListening(GameEvents gameEventName, Action<T1, T2> listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            EventDictionary[gameEventName] -= listener;
            if (EventDictionary[gameEventName] == null)
            {
                EventDictionary.Remove(gameEventName);
            }
        }
    }

    public static void TriggerEvent(GameEvents gameEventName, T1 param1, T2 param2)
    {
        if (!EventDictionary.TryGetValue(gameEventName, out var action) || action == null)
            return;

        foreach (var handler in action.GetInvocationList())
        {
            var method = handler as Action<T1, T2>;
            var target = method?.Target;

            if (target is UnityEngine.Object unityObj && unityObj == null)
            {
                EventDictionary[gameEventName] -= method;

                string senderName = "UnknownSender";
                try
                {
                    var frame = new System.Diagnostics.StackTrace().GetFrame(1);
                    var methodInfo = frame.GetMethod();
                    senderName = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
                }
                catch { }

                string listenerMethod = method?.Method.Name ?? "UnknownMethod";
                if (!ErrorSuppress.suppress) Debug.LogWarning($"[Event: {gameEventName}] Removed destroyed listener, Sender: {senderName}, Listener Owner: <destroyed>, Method: {listenerMethod}");
            }
            else
            {
                try
                {
                    method?.Invoke(param1, param2);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Event: {gameEventName}] Exception while invoking listener: {e.Message}");
                }
            }
        }
    }



    public static void CheckEvent(GameEvents gameEventName)
    {
        if (EventDictionary.ContainsKey(gameEventName))
        {
            print("event exists");
        }
        else
        {
            print("event doesn't exist");
        }
    }
}

/// <summary>
/// event manager with a custom returned value
/// </summary>
/// <typeparam name="T1">param 1</typeparam>
/// <typeparam name="T2">param 2</typeparam>
/// <typeparam name="TResult">returned value</typeparam>
public abstract class EventManagerReturn2P<T1, T2, TResult> : MonoBehaviour
{
    private static readonly Dictionary<GameEvents, Func<T1, T2, TResult>> EventDictionary =
        new Dictionary<GameEvents, Func<T1, T2, TResult>>();

    public static void StartListening(GameEvents gameEventName, Func<T1, T2, TResult> listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
            EventDictionary[gameEventName] += listener;
        else
            EventDictionary[gameEventName] = listener;
    }

    public static void StopListening(GameEvents gameEventName, Func<T1, T2, TResult> listener)
    {
        if (!EventDictionary.ContainsKey(gameEventName))
            return;

        EventDictionary[gameEventName] -= listener;

        if (EventDictionary[gameEventName] == null)
            EventDictionary.Remove(gameEventName);
    }

    public static TResult TriggerEvent(GameEvents gameEventName, T1 param1, T2 param2)
    {
        if (!EventDictionary.TryGetValue(gameEventName, out var func) || func == null)
            return default;

        TResult result = default;

        foreach (var handler in func.GetInvocationList())
        {
            var method = handler as Func<T1, T2, TResult>;
            var target = method?.Target;

            if (target is UnityEngine.Object unityObj && unityObj == null)
            {
                EventDictionary[gameEventName] -= method;

                string senderName = "UnknownSender";
                try
                {
                    var frame = new System.Diagnostics.StackTrace().GetFrame(1);
                    var methodInfo = frame.GetMethod();
                    senderName = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
                }
                catch { }

                string listenerMethod = method?.Method.Name ?? "UnknownMethod";

                if (!ErrorSuppress.suppress) Debug.LogWarning(
                    $"[Event: {gameEventName}] Removed destroyed listener, " +
                    $"Sender: {senderName}, Listener Owner: <destroyed>, Method: {listenerMethod}"
                );
            }
            else
            {
                try
                {
                    result = method.Invoke(param1, param2);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[Event: {gameEventName}] Exception while invoking listener: {e.Message}"
                    );
                }
            }
        }

        return result;
    }

    public static void CheckEvent(GameEvents gameEventName)
    {
        Debug.Log(EventDictionary.ContainsKey(gameEventName)
            ? "event exists"
            : "event doesn't exist");
    }
}

/// <summary>
/// event manager with a custom returned value
/// </summary>
/// <typeparam name="T1">param 1</typeparam>
/// <typeparam name="TResult">returned value</typeparam>
public abstract class EventManagerReturn1P<T1, TResult> : MonoBehaviour
{
    private static readonly Dictionary<GameEvents, Func<T1, TResult>> EventDictionary =
        new Dictionary<GameEvents, Func<T1, TResult>>();

    public static void StartListening(GameEvents gameEventName, Func<T1, TResult> listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
            EventDictionary[gameEventName] += listener;
        else
            EventDictionary[gameEventName] = listener;
    }

    public static void StopListening(GameEvents gameEventName, Func<T1, TResult> listener)
    {
        if (!EventDictionary.ContainsKey(gameEventName))
            return;

        EventDictionary[gameEventName] -= listener;

        if (EventDictionary[gameEventName] == null)
            EventDictionary.Remove(gameEventName);
    }

    public static TResult TriggerEvent(GameEvents gameEventName, T1 param1)
    {
        if (!EventDictionary.TryGetValue(gameEventName, out var func) || func == null)
            return default;

        TResult result = default;

        foreach (var handler in func.GetInvocationList())
        {
            var method = handler as Func<T1, TResult>;
            var target = method?.Target;

            if (target is UnityEngine.Object unityObj && unityObj == null)
            {
                EventDictionary[gameEventName] -= method;

                string senderName = "UnknownSender";
                try
                {
                    var frame = new System.Diagnostics.StackTrace().GetFrame(1);
                    var methodInfo = frame.GetMethod();
                    senderName = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
                }
                catch { }

                string listenerMethod = method?.Method.Name ?? "UnknownMethod";

                if (!ErrorSuppress.suppress) Debug.LogWarning(
                    $"[Event: {gameEventName}] Removed destroyed listener, " +
                    $"Sender: {senderName}, Listener Owner: <destroyed>, Method: {listenerMethod}"
                );
            }
            else
            {
                try
                {
                    result = method.Invoke(param1);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[Event: {gameEventName}] Exception while invoking listener: {e.Message}"
                    );
                }
            }
        }

        return result;
    }

    public static void CheckEvent(GameEvents gameEventName)
    {
        Debug.Log(EventDictionary.ContainsKey(gameEventName)
            ? "event exists"
            : "event doesn't exist");
    }
}

/// <summary>
/// event manager with a custom returned value
/// </summary>
/// <typeparam name="TResult">returned value</typeparam>
public abstract class EventManagerReturnNP<TResult> : MonoBehaviour
{
    private static readonly Dictionary<GameEvents, Func<TResult>> EventDictionary =
        new Dictionary<GameEvents, Func<TResult>>();

    public static void StartListening(GameEvents gameEventName, Func<TResult> listener)
    {
        if (EventDictionary.ContainsKey(gameEventName))
            EventDictionary[gameEventName] += listener;
        else
            EventDictionary[gameEventName] = listener;
    }

    public static void StopListening(GameEvents gameEventName, Func<TResult> listener)
    {
        if (!EventDictionary.ContainsKey(gameEventName))
            return;

        EventDictionary[gameEventName] -= listener;

        if (EventDictionary[gameEventName] == null)
            EventDictionary.Remove(gameEventName);
    }

    public static TResult TriggerEvent(GameEvents gameEventName)
    {
        if (!EventDictionary.TryGetValue(gameEventName, out var func) || func == null)
            return default;

        TResult result = default;

        foreach (var handler in func.GetInvocationList())
        {
            var method = handler as Func<TResult>;
            var target = method?.Target;

            if (target is UnityEngine.Object unityObj && unityObj == null)
            {
                EventDictionary[gameEventName] -= method;

                string senderName = "UnknownSender";
                try
                {
                    var frame = new System.Diagnostics.StackTrace().GetFrame(1);
                    var methodInfo = frame.GetMethod();
                    senderName = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
                }
                catch { }

                string listenerMethod = method?.Method.Name ?? "UnknownMethod";

                if (!ErrorSuppress.suppress) Debug.LogWarning(
                    $"[Event: {gameEventName}] Removed destroyed listener, " +
                    $"Sender: {senderName}, Listener Owner: <destroyed>, Method: {listenerMethod}"
                );
            }
            else
            {
                try
                {
                    result = method.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[Event: {gameEventName}] Exception while invoking listener: {e.Message}"
                    );
                }
            }
        }

        return result;
    }

    public static void CheckEvent(GameEvents gameEventName)
    {
        Debug.Log(EventDictionary.ContainsKey(gameEventName)
            ? "event exists"
            : "event doesn't exist");
    }
}

