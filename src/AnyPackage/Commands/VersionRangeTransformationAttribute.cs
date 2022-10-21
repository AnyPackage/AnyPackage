// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System;
using System.Management.Automation;
using NuGet.Versioning;

namespace AnyPackage.Commands
{
    public class VersionRangeTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            if (inputData is null)
            {
                throw new ArgumentTransformationMetadataException("The argument is null. Provide a valid version range, and then try running the command again.");
            }
            else if (inputData is VersionRange)
            {
                return inputData;
            }
            else if (inputData is string ||
                     inputData is double)
            {
                var input = inputData.ToString();

                // If a minimum inclusive string like 1.0 is passed
                // convert it an exact version match. This is done
                // because the user expects the verbatim version to be used.
                // If the user wishes to use minimum inclusive they
                // will need to use the full syntax of [1.0,]
                if (!input.Contains("*") &&
                    !input.Contains("(") &&
                    !input.Contains("["))
                {
                    input = $"[{input}]";
                }

                try
                {
                    var versionRange = VersionRange.Parse(input);
                    return versionRange;
                }
                catch (Exception ex)
                {
                    throw new ArgumentTransformationMetadataException($"'{inputData}' is not a valid version range.", ex);
                }
            }
            else
            {
                throw new ArgumentTransformationMetadataException($"Unable to convert type '{inputData.GetType()}' into a version range.");
            }
        }
    }
}