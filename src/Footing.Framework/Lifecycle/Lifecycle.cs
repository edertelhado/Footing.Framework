namespace Footing.Framework.Lifecycle;

[AttributeUsage(AttributeTargets.Method)]
public class PostConstructAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class PreDestroyAttribute : Attribute { }
