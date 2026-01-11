using System.Collections.Generic;

namespace Naner.Vendors.Models;

/// <summary>
/// Root configuration object for vendors.json file.
/// </summary>
public class VendorConfiguration
{
    public List<VendorDefinition> Vendors { get; set; } = new();
}
