namespace NUrumi
{
    /// <summary>
    /// Represents a registry that describes available components in a domain context.
    /// </summary>
    /// <typeparam name="TRegistry">A type of derived registry.</typeparam>
    public abstract class Registry<TRegistry> where TRegistry : Registry<TRegistry>
    {
    }
}