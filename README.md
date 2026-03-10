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

<img width="451" height="540" alt="image-4" src="https://github.com/user-attachments/assets/d0e2fbd6-190b-445e-a9a3-5a6e196de914" />


---

### 2. Install [NuGet For Unity](https://github.com/GlitchEnzo/NuGetForUnity) and MessagePack

The SaveSystem uses **MessagePack** for fast binary serialization. Install it before importing the scripts or you'll see compile errors like these:

<img width="1477" height="851" alt="image-5" src="https://github.com/user-attachments/assets/b0ab1fdd-5780-4b52-a081-81255c49f4f6" />


After installing Nuget just search for MessagePack

<img width="1920" height="1041" alt="image-6" src="https://github.com/user-attachments/assets/00b05458-13c3-450a-a06d-25e830d3a73c" />


---

### 3. Set up the Bootstrap GameObject

Create an empty GameObject called `Starters` in your scene and add these three components:

- **ModManagerStarter** — discovers and loads mods from the Mods folder on start
- **SaveSystemStarter** — initializes the save manager with optional auto-save
- **EventBusAutoCleanup** — automatically clears all event listeners on scene unload, preventing ghost listeners

<img width="449" height="605" alt="image-7" src="https://github.com/user-attachments/assets/7561ca39-bd1d-4e7c-969c-0f80b9fbb1fb" />


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

<img width="802" height="760" alt="image-9" src="https://github.com/user-attachments/assets/3b80f7b2-a3f1-4209-b867-ce0aa9e7ffcb" />


### Raise events from anywhere

Events can be raised from any script — MonoBehaviour, plain C# class, ScriptableObject, or mod DLL. No GameObject required:

```csharp
EventBus<PrintTextAndEraseEvent>.Raise();
EventBus<QuitGameEvent>.Raise();
```

### Wire UI buttons to events

Create a caller script with a `Raise(string eventName)` method and wire it to your Button's **OnClick()** in the Inspector:

<img width="562" height="307" alt="image-10" src="https://github.com/user-attachments/assets/1814fb9c-92f2-4316-b66c-b1d9d4bd7f7a" />


In the Button's OnClick, select the caller GameObject, choose `Raise`, and type the event name:

<img width="426" height="110" alt="image-18" src="https://github.com/user-attachments/assets/38960b8f-189b-4631-a416-aadaa4f99eed" />


### Demo — EventBus driving UI

A "PRINT TEXT" button raises `PrintTextAndEraseEvent`, a listener receives it and updates a Canvas Text, then clears it after a few seconds. A "QUIT APPLICATION" button raises `QuitGameEvent` which closes the application:

> The gif below shows the full flow in play mode — button click → event raised → listener reacts → text clears automatically.

![Movie_001-ezgif com-video-to-gif-converter](https://github.com/user-attachments/assets/5ed786fe-a3e8-4058-af3b-6459b759c550)


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

All systems communicate exclusively through the EventBus, nothing holds a direct reference to anything else. This simple tutorial is just a start for your project, since EventBus(es(?)) are better to handle events during gameplay, so use for combat, terrain, etc...

---

## Requirements

- Unity **6000.3.6f1 LTS** or newer(this is the current LTS version that I used, but you can use the code on different versions too)
- MessagePack **3.1.4** via NuGet For Unity
