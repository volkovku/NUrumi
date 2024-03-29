﻿namespace NUrumi
{
    /// <summary>
    /// Describes a callback of entity update.
    /// </summary>
    public interface IUpdateCallback
    {
        /// <summary>
        /// Raises when any component of was added or removed to entity.
        /// </summary>
        /// <param name="entityIndex">An index of changed entity.</param>
        /// <param name="added">If true - component was added; otherwise - removed.</param>
        void BeforeChange(int entityIndex, bool added);

        /// <summary>
        /// Raises when any component of was added or removed to entity.
        /// </summary>
        /// <param name="entityIndex">An index of changed entity.</param>
        /// <param name="added">If true - component was added; otherwise - removed.</param>
        void AfterChange(int entityIndex, bool added);
    }
}