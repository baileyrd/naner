param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

# This hook is called during environment initialization
# to set up Java-specific environment variables and PATH

$javaHome = Join-Path $Context.NanerRoot "vendor\java"

if (Test-Path $javaHome) {
    # Set JAVA_HOME
    $env:JAVA_HOME = $javaHome
    $env:JDK_HOME = $javaHome

    # Add Java bin to PATH
    $javaBin = Join-Path $javaHome "bin"
    if ($env:PATH -notlike "*$javaBin*") {
        $env:PATH = "$javaBin;$env:PATH"
    }

    # Set CLASSPATH
    $env:CLASSPATH = ".;$javaHome\lib\*"
}
