using System;
using System.ComponentModel.DataAnnotations;

namespace FunderMaps.Core.Entities
{
    /// <summary>
    ///     Entity representing a map layer.
    /// </summary>
    public sealed class Layer : IdentifiableEntity<Layer, Guid>
    {
        /// <summary>
        ///     Create new instance.
        /// </summary>
        public Layer()
            : base(e => e.Id)
        {
        }

        /// <summary>
        ///     Unique identifier.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        ///     The name of the table referenced by this layer.
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        ///     The human-readable name for this layer.
        /// </summary>
        [Required]
        public string Name { get; set; }

        // FUTURE: Replace object with another entity
        /// <summary>
        ///     Frontend markup and styling.
        /// </summary>
        [Required]
        public object Markup { get; set; }

        /// <summary>
        ///     Get the full class name.
        /// </summary>
        public string Slug => TableName;
    }
}
