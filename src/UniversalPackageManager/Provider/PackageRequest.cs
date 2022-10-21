// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using NuGet.Versioning;

namespace UniversalPackageManager.Provider
{
    /// <summary>
    /// The <c>PackageRequest</c> class is used
    /// to send information to the package provider.
    /// </summary>
    public sealed class PackageRequest : Request
    {
        /// <summary>
        /// Gets the package name.
        /// </summary>
        public string Name { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets the package version range.
        /// </summary>
        public VersionRange? Version { get; internal set; }

        /// <summary>
        /// Gets the package source name.
        /// </summary>
        public string? Source { get; internal set; }

        /// <summary>
        /// Gets if should include prerelease versions.
        /// </summary>
        public bool Prerelease { get; internal set; }

        /// <summary>
        /// Gets the package if passed in via <c>InputObject</c> parameter.
        /// </summary>
        public PackageInfo? Package { get; internal set; }

        /// <summary>
        /// Gets the destination path for Save-Package
        /// and package path for Publish-Package.
        /// </summary>
        public string? Path { get; internal set; }

        internal bool PassThru { get; set; }
        internal bool TrustSource { get; set; }

        private Dictionary<Guid, List<string>> _trustedRepositories = new Dictionary<Guid, List<string>>();
        private bool _yesToAll;
        private bool _noToAll;

        internal PackageRequest(PSCmdlet command) : base(command) { }

        public bool IsMatch(string name)
        {
            var wildcardPattern = WildcardPattern.Get(Name, WildcardOptions.IgnoreCase);
            return wildcardPattern.IsMatch(name);
        }

        public bool IsMatch(NuGetVersion version)
        {
            if (version.IsPrerelease && !Prerelease)
            {
                return false;
            }
            
            if (Version is not null)
            {
                return Version.Satisfies(version);
            }

            return false;
        }

        public bool IsMatch(string name, NuGetVersion version)
        {
            return IsMatch(name) && IsMatch(version);
        }

        /// <summary>
        /// Prompts the user if they want to install a package from an untrusted source.
        /// </summary>
        /// <param name="source">Source name.</param>
        /// <returns>
        /// Returns true if the user accepted or false if the user rejected.
        /// </returns>
        public bool PromptUntrustedSource(string source)
        {
            if (TrustSource) { return true; }
            if (_yesToAll) { return _yesToAll; }
            if (_noToAll) { return _noToAll; }

            // TODO: Consider changing to current culture case insensitive comparison.
            if (ProviderInfo is not null &&
                _trustedRepositories.TryGetValue(ProviderInfo.Id, out var trustedRepositories) &&
                trustedRepositories.Where(x => x.ToLower() == source.ToLower()).Count() > 0)
            {
                return true;
            }

            var query = string.Format("You are installing packages from an untrusted source. If you trust this source, change its Trusted value by running the Set-PackageSource cmdlet. Are you sure you want to install the PSresource from '{0}'?", source);

            var trusted = Cmdlet.ShouldContinue(
                query: query,
                caption: "Untrusted Source",
                hasSecurityImpact: true,
                yesToAll: ref _yesToAll,
                noToAll: ref _noToAll
            );

            if (ProviderInfo is not null && trusted)
            {
                _trustedRepositories[ProviderInfo.Id].Add(source);
            }

            return trusted;
        }

        /// <summary>
        /// Writes the package to the pipeline.
        /// </summary>
        /// <param name="package">The package.</param>
        public void WritePackage(PackageInfo package)
        {
            HasWriteObject = true;

            if (PassThru)
            {
                Cmdlet.WriteObject(package);
            }
        }

        /// <summary>
        /// Writes the packages to the pipeline.
        /// </summary>
        /// <param name="package">The packages.</param>
        public void WritePackage(IEnumerable<PackageInfo> package) => WritePackage(package);

        /// <summary>
        /// Writes the package to the pipeline.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="version">The package version.</param>
        /// <param name="description">The package description.</param>
        /// <param name="source">The package source.</param>
        /// <param name="metadata">Additional metadata about the package.</param>
        /// <param name="dependencies">The package dependencies.</param>
        public void WritePackage(string name,
                                 NuGetVersion version,
                                 string description = "",
                                 PackageSourceInfo? source = null,
                                 Hashtable? metadata = null,
                                 IEnumerable<PackageDependency>? dependencies = null)
        {
            var package = NewPackageInfo(name, version, description, source, metadata, dependencies);
            WritePackage(package);
        }

        /// <summary>
        /// Creates a new <c>PackageSourceInfo</c>.
        /// </summary>
        /// <param name="name">The package source name.</param>
        /// <param name="location">The package source location.</param>
        /// <param name="trusted">If the package source is trusted or not.</param>
        /// <param name="metadata">Additional metadata about the package source.</param>
        public PackageSourceInfo NewSourceInfo(string name,
                                                       string location,
                                                       bool trusted = false,
                                                       Hashtable? metadata = null)
        {
            return new PackageSourceInfo(name, location, ProviderInfo!, trusted, metadata);
        }

        private PackageInfo NewPackageInfo(string name,
                                           NuGetVersion version,
                                           string description = "",
                                           PackageSourceInfo? source = null,
                                           Hashtable? metadata = null,
                                           IEnumerable<PackageDependency>? dependencies = null)
        {
            return new PackageInfo(name, version, description, ProviderInfo!, source, metadata, dependencies);
        }
    }
}