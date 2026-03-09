namespace EventBusSystem
{
    /// <summary>
    /// Marker interface every event must implement.
    /// Best practice: use readonly structs for zero-allocation.
    ///
    ///   public struct Ex_Event : IEvent
    ///   {
    ///       
    ///   }
    /// </summary>
    public interface IEvent { }
}
