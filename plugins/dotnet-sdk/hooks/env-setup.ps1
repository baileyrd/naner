param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

# This hook is called during environment initialization
# to set up .NET-specific environment variables and PATH

$dotnetRoot = Join-Path $Context.NanerRoot "vendor\dotnet"

if (Test-Path $dotnetRoot) {
    # Set DOTNET_ROOT
    $env:DOTNET_ROOT = $dotnetRoot

    # Add dotnet to PATH
    if ($env:PATH -notlike "*$dotnetRoot*") {
        $env:PATH = "$dotnetRoot;$env:PATH"
    }

    # Add .NET global tools to PATH
    $dotnetTools = Join-Path $Context.NanerRoot "home\.dotnet\tools"
    if ($env:PATH -notlike "*$dotnetTools*") {
        $env:PATH = "$dotnetTools;$env:PATH"
    }

    # Disable telemetry and first-run experience
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
    $env:DOTNET_MULTILEVEL_LOOKUP = "0"

    # Set NuGet packages directory
    $env:NUGET_PACKAGES = Join-Path $Context.NanerRoot "home\.nuget\packages"
}
