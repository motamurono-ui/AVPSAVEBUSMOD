# AVP — EventBus · SaveSystem · ModSystem
> A modular Unity 6.3 architecture for games that need to scale. Type-safe events, encrypted saves, and runtime mod support — all decoupled and ready to drop into any project.

---

## Systems

| System | Description |
|---|---|
| **EventBus** | Type-safe generic event bus with zero-allocation events and auto-cleanup |
| **SaveSystem** | AES-256 encrypted binary saves using MessagePack serialization |
| **ModSystem** | Runtime mod loading — assets, UI overrides, and .dll gameplay mods |

---

## Getting Started

### 1. Create a new Unity 6.3 project

Open Unity Hub and create a new project using **Unity 6000.3.6f1 LTS** or newer with any Core template.

![New Project](image-4.png)

---

### 2. Install MessagePack via NuGet For Unity

The SaveSystem uses **MessagePack** for fast binary serialization. Install it before importing the scripts or you'll see compile errors like these:

![MessagePack errors](image-5.png)

Install **NuGet For Unity** from the Unity Asset Store, then search for `MessagePack` and install version **3.1.4**:

![NuGet MessagePack](image-6.png)

---

### 3. Set up the Bootstrap GameObject

Create an empty GameObject called `Starters` in your scene and add these three components:

- **ModManagerStarter** — discovers and loads mods from the Mods folder on start
- **SaveSystemStarter** — initializes the save manager with optional auto-save
- **EventBusAutoCleanup** — automatically clears all event listeners on scene unload, preventing ghost listeners

![Bootstrap setup](image-7.png)

That's all the scene setup needed — the systems initialize themselves.

---

## Using the EventBus

### Define your events

Events are plain structs that implement `IEvent`. Add them to a `GameEvents.cs` file:

```csharp
using EventBusSystem;

namespace EventBusSystem.GameEvents
{
    public struct PrintTextAndEraseEvent : IEvent { }
    public struct QuitGameEvent          : IEvent { }
    public struct StartGameEvent         : IEvent { }
}
```

### Listen to events

Use `EventBinding<T>` to subscribe and always register in `OnEnable` / `OnDisable`:

```csharp
//add eventbussystem and eventbussystem.gameevents
using EventBusSystem;
using EventBusSystem.GameEvents;
using UnityEngine;

public class ExampleScript : MonoBehaviour
{
    EventBinding<PrintTextAndEraseEvent> _printtext;
    EventBinding<QuitGameEvent>          _quitgame;

    private void OnEnable()
    {
        _printtext = new EventBinding<PrintTextAndEraseEvent>(PrintText);
        _quitgame  = new EventBinding<QuitGameEvent>(Quit);

        EventBus<PrintTextAndEraseEvent>.Register(_printtext);
        EventBus<QuitGameEvent>.Register(_quitgame);
    }

    private void OnDisable()
    {
        EventBus<PrintTextAndEraseEvent>.Deregister(_printtext);
        EventBus<QuitGameEvent>.Deregister(_quitgame);
    }

    void PrintText() { }
    void Quit()      { }
}
```

![EventBus listener code](image-9.png)

### Raise events from anywhere

Events can be raised from any script — MonoBehaviour, plain C# class, ScriptableObject, or mod DLL. No GameObject required:

```csharp
EventBus<PrintTextAndEraseEvent>.Raise();
EventBus<QuitGameEvent>.Raise();
```

### Wire UI buttons to events

Create a caller script with a `Raise(string eventName)` method and wire it to your Button's **OnClick()** in the Inspector:

![Raise method](image-10.png)

In the Button's OnClick, select the caller GameObject, choose `Raise`, and type the event name:

![Button OnClick setup](image-18.png)

### Demo — EventBus driving UI

A "PRINT TEXT" button raises `PrintTextAndEraseEvent`, a listener receives it and updates a Canvas Text, then clears it after a few seconds. A "QUIT APPLICATION" button raises `QuitGameEvent` which closes the application:

![EventBus UI demo](image-8.png)

> The gif below shows the full flow in play mode — button click → event raised → listener reacts → text clears automatically.

![EventBus demo gif](eventbus-demo.gif)

---

## SaveSystem

The SaveSystem serializes your data with **MessagePack** and encrypts it with **AES-256-CBC**. The encryption key is derived from the device's unique identifier so saves are machine-bound by default.

```csharp
// Save
await SaveManager.Instance.SaveAsync(slotIndex: 0);

// Load
var save = await SaveManager.Instance.LoadAsync(slotIndex: 0);

// Check if slot exists
bool exists = SaveManager.Instance.SlotExists(0);
```

Auto-save is enabled by checking **Enable Auto Save** on the `SaveSystemStarter` component.

---

## ModSystem

Drop a mod folder into `StreamingAssets/Mods/` (builds) or `Assets/Mods/` (editor). The ModManager discovers and loads it automatically on start.

### Mod folder structure

```
Mods/
  my_mod/
    mod.json          ← required
    MyMod.dll         ← optional, gameplay code
    textures/         ← optional, loose textures
    audio/            ← optional, loose audio
    screen_*.uxml     ← optional, UI screen overrides
```

### mod.json

```json
{
  "modId":           "my_mod",
  "modName":         "My Mod",
  "version":         "1.0.0",
  "author":          "You",
  "description":     "A description of your mod.",
  "priority":        10,
  "assetBundleName": "",
  "dllName":         "MyMod.dll",
  "dependencies":    [],
  "tags":            ["gameplay"]
}
```

### Gameplay mods via DLL

Implement `IMod` in a C# Class Library project and compile it to a `.dll`:

```csharp
using ModSystem;

public class MyMod : IMod
{
    public string ModId      => "my_mod";
    public string ModName    => "My Mod";
    public string ModVersion => "1.0.0";
    public bool   ReceivesUpdate => false;

    public void OnLoad(IModContext context)
    {
        context.RegisterOverride("player/move_speed", 12f);
        context.Log("My mod loaded!");
    }

    public void OnUnload() { }
    public void OnUpdate() { }
}
```

---

## Architecture

```
Triplice.EventBus       ← no dependencies
Triplice.Shared         ← MessagePack only
Triplice.SaveSystem     ← EventBus, Shared, MessagePack
Triplice.ModSystem      ← EventBus, SaveSystem, Shared
```

All systems communicate exclusively through the EventBus — nothing holds a direct reference to anything else.

---

## Requirements

- Unity **6000.3.6f1 LTS** or newer
- MessagePack **3.1.4** via NuGet For Unity
- .NET Standard 2.1
