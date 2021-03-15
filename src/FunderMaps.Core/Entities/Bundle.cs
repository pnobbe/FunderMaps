using FunderMaps.Core.Types.MapLayer;
using System;
using System.ComponentModel.DataAnnotations;

namespace FunderMaps.Core.Entities
{
    /// <summary>
    ///     Represents a bundle of layers made by an organization.
    /// </summary>
    public sealed class Bundle : IdentifiableEntity<Bundle, Guid>
    {
        /// <summary>
        ///     Create new instance.
        /// </summary>
        public Bundle()
            : base(e => e.Id)
        {
        }

        /// <summary>
        ///     Unique identifier.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        ///     References the organization which owns this bundle.
        /// </summary>
        [Required]
        public Guid OrganizationId { get; set; }

        /// <summary>
        ///     Name of this bundle.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        ///     The date this bundle was created.
        /// </summary>
        [Required]
        public DateTime CreateDate { get; set; }

        /// <summary>
        ///     The date this bundle was last updated.
        /// </summary>
        public DateTime? UpdateDate { get; set; }

        /// <summary>
        ///     The date this bundle was deleted.
        /// </summary>
        public DateTime? DeleteDate { get; set; }

        /// <summary>
        ///     The date this bundle was last built.
        /// </summary>
        public DateTime? CompleteDate { get; set; }

        /// <summary>
        ///     Represents the layer id.
        /// </summary>
        [Required]
        public Guid LayerId { get; set; }
    }
}
