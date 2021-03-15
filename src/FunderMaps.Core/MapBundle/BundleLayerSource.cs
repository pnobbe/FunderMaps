using FunderMaps.Core.Entities;
using FunderMaps.Core.Threading.Command;

namespace FunderMaps.Core.MapBundle
{
    /// <summary>
    ///     Bundle layer source specialization.
    /// </summary>
    internal class BundleLayerSource : SqlLayerSource
    {
        private readonly string layerOutputName;

        /// <summary>
        ///     Create new instance.
        /// </summary>
        public BundleLayerSource(Bundle bundle, Layer layer, string workspace)
        {
            Workspace = workspace;
            layerOutputName = layer.Slug;
            Query = $@"
                SELECT  s.*
                FROM    maplayer.{layer.TableName} AS s
                JOIN    application.organization AS o ON o.id = '{bundle.OrganizationId}'
                WHERE   ST_Intersects(o.fence, s.geom)";
        }

        public override void Imbue(CommandInfo commandInfo)
        {
            base.Imbue(commandInfo);

            commandInfo.ArgumentList.Add("-nln");
            commandInfo.ArgumentList.Add(layerOutputName);
        }
    }
}
