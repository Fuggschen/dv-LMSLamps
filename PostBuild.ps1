param(
    [string]$ProjectDir,
    [string]$Configuration
)

# Only run on Release builds
if ($Configuration -ne "Release") {
    Write-Host "Skipping post-build: Not a Release build"
    exit 0
}

Write-Host "Starting post-build process for Release configuration..."

# Set working directory to project root
Set-Location $ProjectDir

# Read info.json to get ID and Version
$infoJson = Get-Content "info.json" | ConvertFrom-Json
$modId = $infoJson.Id
$modVersion = $infoJson.Version

Write-Host "Building package for: $modId v$modVersion"

# Define paths
$distTmpPath = "dist\tmp\$modId"
$distPath = "dist"
$resourcesPath = "resources"
$unityProjectAssetBundlesPath = "LMSLamps.UnityProject\Assets\AssetBundles"
$gameDllPath = "LMSLamps.Game\bin\Release\LMSLamps.Game.dll"
$unityDllPath = "LMSLamps.Unity\bin\Release\LMSLamps.Unity.dll"

# Clean up existing dist folder
if (Test-Path $distPath) {
    Write-Host "Cleaning up existing dist folder..."
    Remove-Item -Path $distPath -Recurse -Force
}

# Create dist/tmp/modname directory
Write-Host "Creating temporary directory: $distTmpPath"
New-Item -Path $distTmpPath -ItemType Directory -Force | Out-Null

# Step 1: Copy DLLs
Write-Host "Copying DLLs..."
if (Test-Path $gameDllPath) {
    Copy-Item -Path $gameDllPath -Destination $distTmpPath
}

if (Test-Path $unityDllPath) {
    Copy-Item -Path $unityDllPath -Destination $distTmpPath
}

# Step 2: Copy AssetBundles to resources folders
Write-Host "Copying AssetBundles to resources folders..."
$assetBundles = @(
    @{Name="bro"; Target="bro"},
    @{Name="bwl"; Target="bwl"},
    @{Name="bwo"; Target="bwo"},
    @{Name="wro"; Target="wro"},
    @{Name="wwl"; Target="wwl"},
    @{Name="wwo"; Target="wwo"}
)

foreach ($bundle in $assetBundles) {
    $sourcePath = Join-Path $unityProjectAssetBundlesPath $bundle.Name
    $targetPath = Join-Path $resourcesPath $bundle.Target
    
    if (Test-Path $sourcePath) {
        if (!(Test-Path $targetPath)) {
            New-Item -Path $targetPath -ItemType Directory -Force | Out-Null
        }
        Copy-Item -Path $sourcePath -Destination (Join-Path $targetPath $bundle.Name) -Force
        Write-Host "  - Copied $($bundle.Name) to $($bundle.Target)"
    } else {
        Write-Host "  - WARNING: AssetBundle $($bundle.Name) not found"
    }
}

# Step 3: Copy info.json
Write-Host "Copying info.json..."
Copy-Item -Path "info.json" -Destination $distTmpPath

# Step 4: Copy resources folders (excluding repository.json)
Write-Host "Copying resource folders..."
$resourceFolders = Get-ChildItem -Path $resourcesPath -Directory

foreach ($folder in $resourceFolders) {
    $targetFolder = Join-Path $distTmpPath $folder.Name
    Copy-Item -Path $folder.FullName -Destination $targetFolder -Recurse -Force
    Write-Host "  - Copied $($folder.Name)"
}

# Step 5: Create zip archive
$archiveName = "${modId}_${modVersion}.zip"
$archivePath = Join-Path $ProjectDir $archiveName

Write-Host "Creating archive: $archiveName"

# Check if 7z is available
$7zPath = $null
$possiblePaths = @(
    "C:\Program Files\7-Zip\7z.exe",
    "C:\Program Files (x86)\7-Zip\7z.exe",
    "7z.exe"  # If in PATH
)

foreach ($path in $possiblePaths) {
    if (Get-Command $path -ErrorAction SilentlyContinue) {
        $7zPath = $path
        break
    }
}

if ($null -eq $7zPath) {
    Write-Host "ERROR: 7z.exe not found. Please install 7-Zip or add it to PATH"
    exit 1
}

# Create archive from the contents of dist/tmp/modname
# Change to the temp directory so files are added at root level
Push-Location $distTmpPath
& $7zPath a -tzip $archivePath "*" -r
Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "Archive created successfully: $archiveName"
} else {
    Write-Host "ERROR: Failed to create archive"
    exit 1
}

# Step 6: Cleanup
Write-Host "Cleaning up temporary files..."
Remove-Item -Path $distPath -Recurse -Force

Write-Host "Post-build process completed successfully!"
Write-Host "Output: $archiveName"
