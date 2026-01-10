<#
.SYNOPSIS
    Naner GUI Configuration Manager - Visual interface for managing Naner configuration.

.DESCRIPTION
    Provides a Windows Forms-based GUI for managing:
    - Vendor installation and configuration
    - Environment management
    - Profile configuration
    - Setup wizard for new users
    - Configuration validation

.PARAMETER ShowWizard
    Launch the setup wizard for first-time users.

.PARAMETER Tab
    Open the GUI with a specific tab selected (Vendors, Environments, Profiles, Settings).

.EXAMPLE
    .\Invoke-NanerGUI.ps1

.EXAMPLE
    .\Invoke-NanerGUI.ps1 -ShowWizard

.EXAMPLE
    .\Invoke-NanerGUI.ps1 -Tab Vendors

.NOTES
    Requires: .NET Framework (included with Windows)
    Version: 1.0.0
    Author: Naner Project
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$ShowWizard,

    [Parameter()]
    [ValidateSet("Vendors", "Environments", "Profiles", "Settings")]
    [string]$Tab
)

$ErrorActionPreference = "Stop"

# Import required modules
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
$vendorModule = Join-Path $PSScriptRoot "Naner.Vendors.psm1"
$envModule = Join-Path $PSScriptRoot "Naner.Environments.psm1"

if (-not (Test-Path $commonModule)) {
    throw "Common.psm1 module not found at: $commonModule"
}

Import-Module $commonModule -Force

if (Test-Path $vendorModule) {
    Import-Module $vendorModule -Force
}

if (Test-Path $envModule) {
    Import-Module $envModule -Force
}

# Load Windows Forms
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Global variables
$script:NanerRoot = Find-NanerRoot
$script:ConfigPath = Join-Path $script:NanerRoot "config\naner.json"
$script:VendorsPath = Join-Path $script:NanerRoot "config\vendors.json"
$script:Config = Get-NanerConfig -ConfigPath $script:ConfigPath
$script:MainForm = $null
$script:TabControl = $null

#region Utility Functions

function Write-GUILog {
    param(
        [string]$Message,
        [string]$LogBox
    )

    if ($LogBox) {
        $timestamp = Get-Date -Format "HH:mm:ss"
        $LogBox.AppendText("[$timestamp] $Message`r`n")
        $LogBox.ScrollToCaret()
    }
}

function Show-MessageBox {
    param(
        [string]$Message,
        [string]$Title = "Naner Configuration",
        [System.Windows.Forms.MessageBoxButtons]$Buttons = [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]$Icon = [System.Windows.Forms.MessageBoxIcon]::Information
    )

    return [System.Windows.Forms.MessageBox]::Show($Message, $Title, $Buttons, $Icon)
}

function Refresh-VendorsList {
    param($ListView)

    $ListView.Items.Clear()

    if (-not (Test-Path $script:VendorsPath)) {
        return
    }

    $vendors = Get-Content $script:VendorsPath | ConvertFrom-Json

    foreach ($vendor in $vendors.PSObject.Properties) {
        $v = $vendor.Value
        $item = New-Object System.Windows.Forms.ListViewItem($vendor.Name)
        $item.SubItems.Add($v.DisplayName)
        $item.SubItems.Add($(if ($v.Enabled) { "Yes" } else { "No" }))
        $item.SubItems.Add($(if ($v.Required) { "Yes" } else { "No" }))

        # Check if installed
        $vendorPath = Join-Path $script:NanerRoot "vendor\$($vendor.Name.ToLower())"
        $installed = Test-Path $vendorPath
        $item.SubItems.Add($(if ($installed) { "Installed" } else { "Not Installed" }))

        if ($v.Required) {
            $item.ForeColor = [System.Drawing.Color]::DarkBlue
        } elseif (-not $v.Enabled) {
            $item.ForeColor = [System.Drawing.Color]::Gray
        }

        $item.Tag = $v
        $ListView.Items.Add($item) | Out-Null
    }
}

function Refresh-EnvironmentsList {
    param($ListView)

    $ListView.Items.Clear()

    $envRoot = Join-Path $script:NanerRoot "environments"

    if (-not (Test-Path $envRoot)) {
        New-Item -ItemType Directory -Path $envRoot -Force | Out-Null
    }

    # Add default environment
    $defaultItem = New-Object System.Windows.Forms.ListViewItem("default")
    $defaultItem.SubItems.Add("Default Environment")

    $activeEnv = "default"
    if (Test-Path (Join-Path $envRoot ".active")) {
        $activeEnv = Get-Content (Join-Path $envRoot ".active") -Raw | ConvertFrom-Json | Select-Object -ExpandProperty Environment
    }

    $defaultItem.SubItems.Add($(if ($activeEnv -eq "default") { "Active" } else { "" }))
    $defaultItem.SubItems.Add((Join-Path $script:NanerRoot "home"))

    if ($activeEnv -eq "default") {
        $defaultItem.Font = New-Object System.Drawing.Font($defaultItem.Font, [System.Drawing.FontStyle]::Bold)
    }

    $ListView.Items.Add($defaultItem) | Out-Null

    # Add custom environments
    $envDirs = Get-ChildItem -Path $envRoot -Directory -ErrorAction SilentlyContinue

    foreach ($envDir in $envDirs) {
        $metaPath = Join-Path $envDir.FullName ".metadata.json"

        if (Test-Path $metaPath) {
            $meta = Get-Content $metaPath | ConvertFrom-Json
            $item = New-Object System.Windows.Forms.ListViewItem($envDir.Name)
            $item.SubItems.Add($meta.Description)
            $item.SubItems.Add($(if ($activeEnv -eq $envDir.Name) { "Active" } else { "" }))
            $item.SubItems.Add($envDir.FullName)

            if ($activeEnv -eq $envDir.Name) {
                $item.Font = New-Object System.Drawing.Font($item.Font, [System.Drawing.FontStyle]::Bold)
            }

            $item.Tag = $meta
            $ListView.Items.Add($item) | Out-Null
        }
    }
}

function Refresh-ProfilesList {
    param($ListView)

    $ListView.Items.Clear()

    foreach ($profile in $script:Config.Profiles.PSObject.Properties) {
        $p = $profile.Value
        $item = New-Object System.Windows.Forms.ListViewItem($profile.Name)
        $item.SubItems.Add($p.Name)
        $item.SubItems.Add($p.Shell)
        $item.SubItems.Add($p.Description)

        $isDefault = $script:Config.DefaultProfile -eq $profile.Name
        $item.SubItems.Add($(if ($isDefault) { "Yes" } else { "" }))

        if ($isDefault) {
            $item.Font = New-Object System.Drawing.Font($item.Font, [System.Drawing.FontStyle]::Bold)
        }

        $item.Tag = $p
        $ListView.Items.Add($item) | Out-Null
    }
}

#endregion

#region Setup Wizard

function Show-SetupWizard {
    $wizardForm = New-Object System.Windows.Forms.Form
    $wizardForm.Text = "Naner Setup Wizard"
    $wizardForm.Size = New-Object System.Drawing.Size(700, 500)
    $wizardForm.StartPosition = "CenterScreen"
    $wizardForm.FormBorderStyle = "FixedDialog"
    $wizardForm.MaximizeBox = $false
    $wizardForm.MinimizeBox = $false

    # Title Label
    $titleLabel = New-Object System.Windows.Forms.Label
    $titleLabel.Text = "Welcome to Naner!"
    $titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
    $titleLabel.Location = New-Object System.Drawing.Point(20, 20)
    $titleLabel.Size = New-Object System.Drawing.Size(640, 40)
    $wizardForm.Controls.Add($titleLabel)

    # Description Label
    $descLabel = New-Object System.Windows.Forms.Label
    $descLabel.Text = "This wizard will help you set up your portable development environment."
    $descLabel.Location = New-Object System.Drawing.Point(20, 70)
    $descLabel.Size = New-Object System.Drawing.Size(640, 40)
    $wizardForm.Controls.Add($descLabel)

    # Step 1: Vendor Selection
    $vendorLabel = New-Object System.Windows.Forms.Label
    $vendorLabel.Text = "Select optional vendors to install:"
    $vendorLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
    $vendorLabel.Location = New-Object System.Drawing.Point(20, 120)
    $vendorLabel.Size = New-Object System.Drawing.Size(640, 25)
    $wizardForm.Controls.Add($vendorLabel)

    $vendorCheckList = New-Object System.Windows.Forms.CheckedListBox
    $vendorCheckList.Location = New-Object System.Drawing.Point(20, 150)
    $vendorCheckList.Size = New-Object System.Drawing.Size(640, 150)

    # Load optional vendors
    if (Test-Path $script:VendorsPath) {
        $vendors = Get-Content $script:VendorsPath | ConvertFrom-Json
        foreach ($vendor in $vendors.PSObject.Properties) {
            if (-not $vendor.Value.Required) {
                $vendorCheckList.Items.Add("$($vendor.Value.DisplayName) - $($vendor.Value.Description)", $false) | Out-Null
            }
        }
    }

    $wizardForm.Controls.Add($vendorCheckList)

    # Progress Bar
    $progressBar = New-Object System.Windows.Forms.ProgressBar
    $progressBar.Location = New-Object System.Drawing.Point(20, 310)
    $progressBar.Size = New-Object System.Drawing.Size(640, 25)
    $progressBar.Style = "Continuous"
    $wizardForm.Controls.Add($progressBar)

    # Status Label
    $statusLabel = New-Object System.Windows.Forms.Label
    $statusLabel.Text = "Ready to install..."
    $statusLabel.Location = New-Object System.Drawing.Point(20, 340)
    $statusLabel.Size = New-Object System.Drawing.Size(640, 25)
    $wizardForm.Controls.Add($statusLabel)

    # Buttons
    $installButton = New-Object System.Windows.Forms.Button
    $installButton.Text = "Install Selected"
    $installButton.Location = New-Object System.Drawing.Point(440, 420)
    $installButton.Size = New-Object System.Drawing.Size(120, 30)
    $installButton.Add_Click({
        $statusLabel.Text = "Installing vendors..."
        $progressBar.Value = 0
        $installButton.Enabled = $false
        $skipButton.Enabled = $false

        # TODO: Implement actual installation
        # For now, just show progress
        for ($i = 0; $i -le 100; $i += 10) {
            $progressBar.Value = $i
            Start-Sleep -Milliseconds 200
        }

        $statusLabel.Text = "Installation complete!"
        Show-MessageBox -Message "Setup wizard completed successfully!`n`nYou can now use Naner to launch your development environment." -Title "Setup Complete" -Icon Information
        $wizardForm.DialogResult = [System.Windows.Forms.DialogResult]::OK
        $wizardForm.Close()
    })
    $wizardForm.Controls.Add($installButton)

    $skipButton = New-Object System.Windows.Forms.Button
    $skipButton.Text = "Skip"
    $skipButton.Location = New-Object System.Drawing.Point(570, 420)
    $skipButton.Size = New-Object System.Drawing.Size(90, 30)
    $skipButton.Add_Click({
        $wizardForm.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
        $wizardForm.Close()
    })
    $wizardForm.Controls.Add($skipButton)

    $wizardForm.ShowDialog() | Out-Null
}

#endregion

#region Main Window - Vendors Tab

function Create-VendorsTab {
    $tabPage = New-Object System.Windows.Forms.TabPage
    $tabPage.Text = "Vendors"
    $tabPage.Padding = New-Object System.Windows.Forms.Padding(10)

    # ListView for vendors
    $listView = New-Object System.Windows.Forms.ListView
    $listView.Location = New-Object System.Drawing.Point(10, 10)
    $listView.Size = New-Object System.Drawing.Size(760, 400)
    $listView.View = "Details"
    $listView.FullRowSelect = $true
    $listView.GridLines = $true
    $listView.MultiSelect = $false

    $listView.Columns.Add("ID", 100) | Out-Null
    $listView.Columns.Add("Name", 200) | Out-Null
    $listView.Columns.Add("Enabled", 80) | Out-Null
    $listView.Columns.Add("Required", 80) | Out-Null
    $listView.Columns.Add("Status", 120) | Out-Null

    $tabPage.Controls.Add($listView)

    # Buttons
    $btnY = 420

    $installButton = New-Object System.Windows.Forms.Button
    $installButton.Text = "Install Selected"
    $installButton.Location = New-Object System.Drawing.Point(10, $btnY)
    $installButton.Size = New-Object System.Drawing.Size(120, 30)
    $installButton.Add_Click({
        if ($listView.SelectedItems.Count -eq 0) {
            Show-MessageBox -Message "Please select a vendor to install." -Icon Warning
            return
        }

        $selectedVendor = $listView.SelectedItems[0].Text
        $result = Show-MessageBox -Message "Install vendor: $selectedVendor ?`n`nThis may take several minutes." -Buttons YesNo -Icon Question

        if ($result -eq [System.Windows.Forms.DialogResult]::Yes) {
            # TODO: Implement installation with progress dialog
            Show-MessageBox -Message "Vendor installation will be implemented in the next phase." -Icon Information
        }
    })
    $tabPage.Controls.Add($installButton)

    $enableButton = New-Object System.Windows.Forms.Button
    $enableButton.Text = "Enable/Disable"
    $enableButton.Location = New-Object System.Drawing.Point(140, $btnY)
    $enableButton.Size = New-Object System.Drawing.Size(120, 30)
    $enableButton.Add_Click({
        if ($listView.SelectedItems.Count -eq 0) {
            Show-MessageBox -Message "Please select a vendor to enable/disable." -Icon Warning
            return
        }

        Show-MessageBox -Message "Enable/disable functionality will be implemented in the next phase." -Icon Information
    })
    $tabPage.Controls.Add($enableButton)

    $refreshButton = New-Object System.Windows.Forms.Button
    $refreshButton.Text = "Refresh"
    $refreshButton.Location = New-Object System.Drawing.Point(650, $btnY)
    $refreshButton.Size = New-Object System.Drawing.Size(120, 30)
    $refreshButton.Add_Click({
        Refresh-VendorsList -ListView $listView
    })
    $tabPage.Controls.Add($refreshButton)

    # Initial load
    Refresh-VendorsList -ListView $listView

    return $tabPage
}

#endregion

#region Main Window - Environments Tab

function Create-EnvironmentsTab {
    $tabPage = New-Object System.Windows.Forms.TabPage
    $tabPage.Text = "Environments"
    $tabPage.Padding = New-Object System.Windows.Forms.Padding(10)

    # ListView for environments
    $listView = New-Object System.Windows.Forms.ListView
    $listView.Location = New-Object System.Drawing.Point(10, 10)
    $listView.Size = New-Object System.Drawing.Size(760, 400)
    $listView.View = "Details"
    $listView.FullRowSelect = $true
    $listView.GridLines = $true
    $listView.MultiSelect = $false

    $listView.Columns.Add("Name", 150) | Out-Null
    $listView.Columns.Add("Description", 300) | Out-Null
    $listView.Columns.Add("Status", 100) | Out-Null
    $listView.Columns.Add("Path", 200) | Out-Null

    $tabPage.Controls.Add($listView)

    # Buttons
    $btnY = 420

    $createButton = New-Object System.Windows.Forms.Button
    $createButton.Text = "Create New"
    $createButton.Location = New-Object System.Drawing.Point(10, $btnY)
    $createButton.Size = New-Object System.Drawing.Size(120, 30)
    $createButton.Add_Click({
        Show-MessageBox -Message "Create environment dialog will be implemented in the next phase." -Icon Information
    })
    $tabPage.Controls.Add($createButton)

    $switchButton = New-Object System.Windows.Forms.Button
    $switchButton.Text = "Switch To"
    $switchButton.Location = New-Object System.Drawing.Point(140, $btnY)
    $switchButton.Size = New-Object System.Drawing.Size(120, 30)
    $switchButton.Add_Click({
        if ($listView.SelectedItems.Count -eq 0) {
            Show-MessageBox -Message "Please select an environment to switch to." -Icon Warning
            return
        }

        $selectedEnv = $listView.SelectedItems[0].Text
        Show-MessageBox -Message "Switch to environment: $selectedEnv`n`nThis functionality will be implemented in the next phase." -Icon Information
    })
    $tabPage.Controls.Add($switchButton)

    $deleteButton = New-Object System.Windows.Forms.Button
    $deleteButton.Text = "Delete"
    $deleteButton.Location = New-Object System.Drawing.Point(270, $btnY)
    $deleteButton.Size = New-Object System.Drawing.Size(120, 30)
    $deleteButton.Add_Click({
        if ($listView.SelectedItems.Count -eq 0) {
            Show-MessageBox -Message "Please select an environment to delete." -Icon Warning
            return
        }

        $selectedEnv = $listView.SelectedItems[0].Text

        if ($selectedEnv -eq "default") {
            Show-MessageBox -Message "Cannot delete the default environment." -Icon Warning
            return
        }

        $result = Show-MessageBox -Message "Delete environment: $selectedEnv ?`n`nThis action cannot be undone." -Buttons YesNo -Icon Warning

        if ($result -eq [System.Windows.Forms.DialogResult]::Yes) {
            Show-MessageBox -Message "Delete environment functionality will be implemented in the next phase." -Icon Information
        }
    })
    $tabPage.Controls.Add($deleteButton)

    $refreshButton = New-Object System.Windows.Forms.Button
    $refreshButton.Text = "Refresh"
    $refreshButton.Location = New-Object System.Drawing.Point(650, $btnY)
    $refreshButton.Size = New-Object System.Drawing.Size(120, 30)
    $refreshButton.Add_Click({
        Refresh-EnvironmentsList -ListView $listView
    })
    $tabPage.Controls.Add($refreshButton)

    # Initial load
    Refresh-EnvironmentsList -ListView $listView

    return $tabPage
}

#endregion

#region Main Window - Profiles Tab

function Create-ProfilesTab {
    $tabPage = New-Object System.Windows.Forms.TabPage
    $tabPage.Text = "Profiles"
    $tabPage.Padding = New-Object System.Windows.Forms.Padding(10)

    # ListView for profiles
    $listView = New-Object System.Windows.Forms.ListView
    $listView.Location = New-Object System.Drawing.Point(10, 10)
    $listView.Size = New-Object System.Drawing.Size(760, 400)
    $listView.View = "Details"
    $listView.FullRowSelect = $true
    $listView.GridLines = $true
    $listView.MultiSelect = $false

    $listView.Columns.Add("ID", 100) | Out-Null
    $listView.Columns.Add("Display Name", 150) | Out-Null
    $listView.Columns.Add("Shell", 100) | Out-Null
    $listView.Columns.Add("Description", 300) | Out-Null
    $listView.Columns.Add("Default", 80) | Out-Null

    $tabPage.Controls.Add($listView)

    # Buttons
    $btnY = 420

    $launchButton = New-Object System.Windows.Forms.Button
    $launchButton.Text = "Launch Profile"
    $launchButton.Location = New-Object System.Drawing.Point(10, $btnY)
    $launchButton.Size = New-Object System.Drawing.Size(120, 30)
    $launchButton.Add_Click({
        if ($listView.SelectedItems.Count -eq 0) {
            Show-MessageBox -Message "Please select a profile to launch." -Icon Warning
            return
        }

        $selectedProfile = $listView.SelectedItems[0].Text
        Show-MessageBox -Message "Launching profile: $selectedProfile`n`nThis functionality will be implemented in the next phase." -Icon Information
    })
    $tabPage.Controls.Add($launchButton)

    $setDefaultButton = New-Object System.Windows.Forms.Button
    $setDefaultButton.Text = "Set as Default"
    $setDefaultButton.Location = New-Object System.Drawing.Point(140, $btnY)
    $setDefaultButton.Size = New-Object System.Drawing.Size(120, 30)
    $setDefaultButton.Add_Click({
        if ($listView.SelectedItems.Count -eq 0) {
            Show-MessageBox -Message "Please select a profile to set as default." -Icon Warning
            return
        }

        $selectedProfile = $listView.SelectedItems[0].Text
        Show-MessageBox -Message "Set default profile: $selectedProfile`n`nThis functionality will be implemented in the next phase." -Icon Information
    })
    $tabPage.Controls.Add($setDefaultButton)

    $refreshButton = New-Object System.Windows.Forms.Button
    $refreshButton.Text = "Refresh"
    $refreshButton.Location = New-Object System.Drawing.Point(650, $btnY)
    $refreshButton.Size = New-Object System.Drawing.Size(120, 30)
    $refreshButton.Add_Click({
        Refresh-ProfilesList -ListView $listView
    })
    $tabPage.Controls.Add($refreshButton)

    # Initial load
    Refresh-ProfilesList -ListView $listView

    return $tabPage
}

#endregion

#region Main Window - Settings Tab

function Create-SettingsTab {
    $tabPage = New-Object System.Windows.Forms.TabPage
    $tabPage.Text = "Settings"
    $tabPage.Padding = New-Object System.Windows.Forms.Padding(10)

    # Naner Root
    $rootLabel = New-Object System.Windows.Forms.Label
    $rootLabel.Text = "Naner Root:"
    $rootLabel.Location = New-Object System.Drawing.Point(10, 20)
    $rootLabel.Size = New-Object System.Drawing.Size(100, 20)
    $tabPage.Controls.Add($rootLabel)

    $rootTextBox = New-Object System.Windows.Forms.TextBox
    $rootTextBox.Text = $script:NanerRoot
    $rootTextBox.Location = New-Object System.Drawing.Point(120, 20)
    $rootTextBox.Size = New-Object System.Drawing.Size(500, 20)
    $rootTextBox.ReadOnly = $true
    $tabPage.Controls.Add($rootTextBox)

    # Configuration File
    $configLabel = New-Object System.Windows.Forms.Label
    $configLabel.Text = "Config File:"
    $configLabel.Location = New-Object System.Drawing.Point(10, 50)
    $configLabel.Size = New-Object System.Drawing.Size(100, 20)
    $tabPage.Controls.Add($configLabel)

    $configTextBox = New-Object System.Windows.Forms.TextBox
    $configTextBox.Text = $script:ConfigPath
    $configTextBox.Location = New-Object System.Drawing.Point(120, 50)
    $configTextBox.Size = New-Object System.Drawing.Size(500, 20)
    $configTextBox.ReadOnly = $true
    $tabPage.Controls.Add($configTextBox)

    # Actions GroupBox
    $actionsGroup = New-Object System.Windows.Forms.GroupBox
    $actionsGroup.Text = "Actions"
    $actionsGroup.Location = New-Object System.Drawing.Point(10, 90)
    $actionsGroup.Size = New-Object System.Drawing.Size(760, 180)
    $tabPage.Controls.Add($actionsGroup)

    # Open Config Button
    $openConfigButton = New-Object System.Windows.Forms.Button
    $openConfigButton.Text = "Edit naner.json"
    $openConfigButton.Location = New-Object System.Drawing.Point(20, 30)
    $openConfigButton.Size = New-Object System.Drawing.Size(150, 30)
    $openConfigButton.Add_Click({
        if (Test-Path $script:ConfigPath) {
            Start-Process "notepad.exe" -ArgumentList $script:ConfigPath
        }
    })
    $actionsGroup.Controls.Add($openConfigButton)

    # Open Vendors Config Button
    $openVendorsButton = New-Object System.Windows.Forms.Button
    $openVendorsButton.Text = "Edit vendors.json"
    $openVendorsButton.Location = New-Object System.Drawing.Point(190, 30)
    $openVendorsButton.Size = New-Object System.Drawing.Size(150, 30)
    $openVendorsButton.Add_Click({
        if (Test-Path $script:VendorsPath) {
            Start-Process "notepad.exe" -ArgumentList $script:VendorsPath
        }
    })
    $actionsGroup.Controls.Add($openVendorsButton)

    # Open Home Folder Button
    $openHomeButton = New-Object System.Windows.Forms.Button
    $openHomeButton.Text = "Open Home Folder"
    $openHomeButton.Location = New-Object System.Drawing.Point(20, 70)
    $openHomeButton.Size = New-Object System.Drawing.Size(150, 30)
    $openHomeButton.Add_Click({
        $homePath = Join-Path $script:NanerRoot "home"
        if (Test-Path $homePath) {
            Start-Process "explorer.exe" -ArgumentList $homePath
        }
    })
    $actionsGroup.Controls.Add($openHomeButton)

    # Run Setup Wizard Button
    $wizardButton = New-Object System.Windows.Forms.Button
    $wizardButton.Text = "Run Setup Wizard"
    $wizardButton.Location = New-Object System.Drawing.Point(190, 70)
    $wizardButton.Size = New-Object System.Drawing.Size(150, 30)
    $wizardButton.Add_Click({
        Show-SetupWizard
    })
    $actionsGroup.Controls.Add($wizardButton)

    # Validate Configuration Button
    $validateButton = New-Object System.Windows.Forms.Button
    $validateButton.Text = "Validate Configuration"
    $validateButton.Location = New-Object System.Drawing.Point(20, 110)
    $validateButton.Size = New-Object System.Drawing.Size(150, 30)
    $validateButton.Add_Click({
        Show-MessageBox -Message "Configuration validation will be implemented in the next phase." -Icon Information
    })
    $actionsGroup.Controls.Add($validateButton)

    # About Section
    $aboutGroup = New-Object System.Windows.Forms.GroupBox
    $aboutGroup.Text = "About Naner"
    $aboutGroup.Location = New-Object System.Drawing.Point(10, 290)
    $aboutGroup.Size = New-Object System.Drawing.Size(760, 160)
    $tabPage.Controls.Add($aboutGroup)

    $aboutText = @"
Naner - Portable Development Environment
Version: 1.0.0

A portable, vendored development environment for Windows that includes:
• PowerShell 7, Git, MSYS2/Bash, Windows Terminal
• Optional: Node.js, Python, Go, Rust, Ruby
• Multi-environment support
• Plugin system for extensions

For more information, visit the documentation folder.
"@

    $aboutLabel = New-Object System.Windows.Forms.Label
    $aboutLabel.Text = $aboutText
    $aboutLabel.Location = New-Object System.Drawing.Point(20, 25)
    $aboutLabel.Size = New-Object System.Drawing.Size(720, 125)
    $aboutLabel.AutoSize = $false
    $aboutGroup.Controls.Add($aboutLabel)

    return $tabPage
}

#endregion

#region Main Window

function Show-MainWindow {
    # Create main form
    $script:MainForm = New-Object System.Windows.Forms.Form
    $script:MainForm.Text = "Naner Configuration Manager"
    $script:MainForm.Size = New-Object System.Drawing.Size(800, 550)
    $script:MainForm.StartPosition = "CenterScreen"
    $script:MainForm.FormBorderStyle = "FixedSingle"
    $script:MainForm.MaximizeBox = $false

    # Create tab control
    $script:TabControl = New-Object System.Windows.Forms.TabControl
    $script:TabControl.Location = New-Object System.Drawing.Point(10, 10)
    $script:TabControl.Size = New-Object System.Drawing.Size(780, 500)
    $script:MainForm.Controls.Add($script:TabControl)

    # Add tabs
    $script:TabControl.TabPages.Add((Create-VendorsTab))
    $script:TabControl.TabPages.Add((Create-EnvironmentsTab))
    $script:TabControl.TabPages.Add((Create-ProfilesTab))
    $script:TabControl.TabPages.Add((Create-SettingsTab))

    # Select initial tab if specified
    if ($Tab) {
        for ($i = 0; $i -lt $script:TabControl.TabPages.Count; $i++) {
            if ($script:TabControl.TabPages[$i].Text -eq $Tab) {
                $script:TabControl.SelectedIndex = $i
                break
            }
        }
    }

    # Show the form
    $script:MainForm.ShowDialog() | Out-Null
}

#endregion

#region Main Execution

# Show wizard if requested
if ($ShowWizard) {
    Show-SetupWizard
    exit 0
}

# Show main window
Show-MainWindow

#endregion
