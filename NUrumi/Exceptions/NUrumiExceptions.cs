namespace NUrumi.Exceptions
{
    public static class NUrumiExceptions
    {
        public static NUrumiComponentNotFoundException ComponentNotFound(
            int entityIndex,
            IComponent component,
            IField field)
        {
            return new NUrumiComponentNotFoundException(
                "Entity does not have component (" +
                $"entity_index={entityIndex}," +
                $"component_name={component.GetType().Name}," +
                $"component_field={field.Name})");
        }
    }
}