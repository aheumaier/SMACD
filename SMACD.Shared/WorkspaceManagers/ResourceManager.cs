﻿using Microsoft.Extensions.Logging;
using SMACD.Shared.Data;
using SMACD.Shared.Resources;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.Shared.WorkspaceManagers
{
    /// <summary>
    /// Manages Resources and their fingerprints
    /// </summary>
    public class ResourceManager
    {
        private static readonly Lazy<ResourceManager> _instance = new Lazy<ResourceManager>(() => new ResourceManager());
        public static ResourceManager Instance => _instance.Value;

        public delegate bool ResourceCollisionDelegate(Resource originalResource, Resource newResource);

        /// <summary>
        /// Fired when a Resource is successfully registered
        /// </summary>
        public event EventHandler<Resource> ResourceRegistered;

        /// <summary>
        /// Fired when a Resource has a collision with another Resource's ID
        /// </summary>
        public event ResourceCollisionDelegate ResourceIdCollision;

        /// <summary>
        /// Fired when a Resource has a collision with another Resource's fingerprint
        /// </summary>
        public event ResourceCollisionDelegate ResourceFingerprintCollision;

        private ConcurrentDictionary<string, Resource> ResourcesById { get; set; } = new ConcurrentDictionary<string, Resource>();
        private ConcurrentDictionary<string, Resource> ResourcesByFingerprint { get; set; } = new ConcurrentDictionary<string, Resource>();
        private ILogger Logger { get; } = Extensions.LogFactory.CreateLogger("ResourceManager");

        private ResourceManager()
        {
        }

        public bool ContainsId(string resourceId) => ResourcesById.ContainsKey(resourceId);

        public bool ContainsFingerprint(string fingerprint) => ResourcesByFingerprint.ContainsKey(fingerprint);

        public bool ContainsPointer(ResourcePointerModel pointer) => ResourcesById.ContainsKey(pointer.ResourceId);

        public Resource GetById(string resourceId) => ContainsId(resourceId) ? ResourcesById[resourceId] : null;

        public Resource GetByFingerprint(string fingerprint) => ContainsFingerprint(fingerprint) ? ResourcesByFingerprint[fingerprint] : null;

        public Resource GetByPointer(ResourcePointerModel pointer) => ContainsPointer(pointer) ? ResourcesById[pointer.ResourceId]?.GetWithParameters(pointer.ResourceParameters) : null;

        public Resource Search(Predicate<Resource> selector) => ResourcesById.Values.FirstOrDefault(r => selector(r));

        public T GetById<T>(string resourceId) where T : Resource => (T)GetById(resourceId);

        public T GetByFingerprint<T>(string fingerprint) where T : Resource => (T)GetByFingerprint(fingerprint);

        public T GetByObjectFingerprint<T>(T existingObject) where T : Resource => (T)GetByFingerprint(existingObject.DataFingerprint);

        public T GetByPointer<T>(ResourcePointerModel pointer) where T : Resource => (T)ResourcesById[pointer.ResourceId]?.GetWithParameters(pointer.ResourceParameters);

        public T Search<T>(Predicate<T> selector) where T : Resource => (T)ResourcesById.Values.FirstOrDefault(r => r is T && selector((T)r));

        /// <summary>
        /// Update the hash code cache
        /// </summary>
        public void UpdateCache()
        {
            Logger.LogDebug("=== EXPIRING AND REGENERATING RESOURCE HASH CACHE FOR {0} ITEMS ===", ResourcesByFingerprint.Count);
            ResourcesByFingerprint.Clear();
            Parallel.ForEach(ResourcesById, r => ResourcesByFingerprint.TryAdd(r.Value.DataFingerprint, r.Value));
        }

        /// <summary>
        /// Register a new Resource
        /// </summary>
        /// <param name="resource">Resource to register</param>
        /// <returns></returns>
        public Resource Register(Resource resource)
        {
            if (ContainsId(resource.ResourceId)) // Contains resource by Id
            {
                var existing = GetById(resource.ResourceId);
                Logger.LogError("Attempted to register resource ID {0} which already exists", resource.ResourceId);
                if (!(ResourceIdCollision?.Invoke(existing, resource)).GetValueOrDefault(false)) // if FALSE or NULL, break -- otherwise override stop behavior
                    return null;
            }
            if (ContainsFingerprint(resource.DataFingerprint)) // Contains resource by fingerprint
            {
                var existing = GetByFingerprint(resource.DataFingerprint);
                Logger.LogError("Attempted to register resource {0} fingerprint {1} which already exists at resource {2}", resource.ResourceId, resource.DataFingerprint, existing.ResourceId);
                if (!(ResourceFingerprintCollision?.Invoke(existing, resource)).GetValueOrDefault(false)) // if FALSE or NULL, break -- otherwise override stop behavior
                    return null;
            }

            ResourcesById.TryAdd(resource.ResourceId, resource);
            ResourcesByFingerprint.TryAdd(resource.DataFingerprint, resource);

            Logger.LogDebug("Registered Resource {0} with fingerprint {1}", resource.ResourceId, resource.DataFingerprint);
            ResourceRegistered?.Invoke(this, resource);
            return resource;
        }
    }
}