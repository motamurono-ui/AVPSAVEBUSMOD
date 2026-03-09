namespace EventBusSystem.SaveSystem
{
    //this events are for Saves, if you want to add more logic here you can, but try to keep it simple and focused on the save system
    //also organize the events in folders too, for example, for a status system use another script file and folder, also pay attention to the assemblies
    public struct GameSavedEvent : IEvent { public int SlotIndex; public string SlotName; public long TimestampUtc; }
    public struct GameLoadedEvent : IEvent { public int SlotIndex; public string SlotName; public long TimestampUtc; }
    public struct SaveDeletedEvent : IEvent { public int SlotIndex; }
    public struct AutoSaveBegunEvent : IEvent { public int SlotIndex; }
    public struct SaveSystemReadyEvent : IEvent { }

    public struct SaveErrorEvent : IEvent
    {
        public SaveErrorType ErrorType;
        public string Message;
        public int SlotIndex;
    }

    public enum SaveErrorType
    {
        SerializationFailed, EncryptionFailed, DecryptionFailed,
        FileWriteFailed, FileReadFailed, FileNotFound,
        VersionMismatch, CorruptedData,
    }
}