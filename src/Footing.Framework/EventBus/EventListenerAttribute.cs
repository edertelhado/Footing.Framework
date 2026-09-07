namespace Footing.Framework.EventBus;

[AttributeUsage(AttributeTargets.Method)]
public class EventListenerAttribute : Attribute 
{ 
    public int Priority { get; set; } = 0;
}