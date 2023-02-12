namespace NUrumi
{
    /// <summary>
    /// Describes a group of entities.
    /// </summary>
    public interface IGroup
    {
        /// <summary>
        /// Raises when any component of the group was added or removed to entity.
        /// </summary>
        /// <param name="entityIndex">An index of changed entity.</param>
        /// <param name="added">If true - component was added; otherwise - removed.</param>
        void Update(int entityIndex, bool added);
    }
}