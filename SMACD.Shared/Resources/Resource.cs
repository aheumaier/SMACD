using System.Collections.Generic;
using SMACD.Shared.Data;
using YamlDotNet.Serialization;

namespace SMACD.Shared.Resources
{
    /// <summary>
    ///     Represents a Resource resolved to its handler
    /// </summary>
    public abstract class Resource : IModel
    {
        protected Resource()
        {
            ResourceId = Extensions.RandomName();
            SystemCreated = true;
        }

        protected Resource(string resourceId)
        {
            ResourceId = resourceId;
        }

        /// <summary>
        ///     Resource identifier
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        ///     If the Resource was generated by the system
        /// </summary>
        public bool SystemCreated { get; set; }

        /// <summary>
        ///     Get a data-only fingerprint of this Resource (to compare against other resources from files)
        ///     Hash function ignores ResourceId and SystemCreated properties by default!
        /// </summary>
        [YamlIgnore]
        public virtual string DataFingerprint => this.Fingerprint();

        /// <summary>
        ///     Retrieve the resource with given transformations
        /// </summary>
        /// <param name="parameters">Parameters to pass to Resource</param>
        /// <returns></returns>
        public virtual Resource GetWithParameters(IDictionary<string, string> parameters)
        {
            return this;
        }

        public string GetFingerprint() => this.Fingerprint();
    }
}