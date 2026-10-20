# Packaging & Distribution Guide

**Phase 4: Automated NuGet Package Generation**

This document explains how the project is packaged, versioned, and distributed as a NuGet package.

---

## Table of Contents

1. [Architecture](#architecture)
2. [Versioning Strategy](#versioning-strategy)
3. [Build & Pack Workflow](#build--pack-workflow)
4. [Package Contents](#package-contents)
5. [Distribution](#distribution)
6. [Version Bump Procedure](#version-bump-procedure)
7. [Troubleshooting](#troubleshooting)

---

## Architecture

### Project Structure

```
Ravelaso.UiPath.Selenium/
├─ Ravelaso.UiPath.Selenium.Core/          ← Source library (no pack logic)
├─ Ravelaso.UiPath.Selenium.Tests/         ← Tests (no pack logic)
├─ Ravelaso.UiPath.Selenium.Package/       ← NEW: Packaging proxy
│  └─ Ravelaso.UiPath.Selenium.Package.csproj
├─ Version.props                             ← NEW: Central versioning
├─ dist/                                     ← NEW: Output folder
│  └─ Ravelaso.UiPath.Selenium.*.nupkg
└─ Ravelaso.UiPath.Selenium.slnx
```

### Philosophy

- **Core** remains agnostic to packaging (no NuGet awareness)
- **Package** project acts as a **wrapper/proxy** that:
  - References Core
  - Copies transitive dependencies
  - Includes runtime assets (selenium-manager.exe)
  - Applies versioning from Version.props
  - Generates `.nupkg` on build (Release only)
- **Version.props** is the **single source of truth** for versioning

---

## Versioning Strategy

### Release Build

**Configuration:** `-c Release`  
**Version:** `{Major}.{Minor}.{Patch}` (e.g., `0.1.0`)  
**Source:** `Version.props`  
**Auto-pack:** Yes (`GeneratePackageOnBuild=true` in Release)

```powershell
dotnet build -c Release
# Output: dist/Ravelaso.UiPath.Selenium.0.1.0.nupkg
```

### Debug Build

**Configuration:** `-c Debug`  
**Version:** `{Major}.{Minor}.{Patch}-dev.{YYYYMMDD}.{XXXXX}` (e.g., `0.1.0-dev.20261020.12345`)  
**Source:** `Version.props` + auto-generated timestamp  
**Auto-pack:** No (must use `dotnet pack`)

```powershell
dotnet pack -c Debug
# Output: dist/Ravelaso.UiPath.Selenium.0.1.0-dev.20261020.12345.nupkg
```

### Version.props Format

**File:** `C:\...\Ravelaso.UiPath.Selenium\Version.props`

```xml
<Project>
  <PropertyGroup>
    <!-- Semantic Versioning -->
    <VersionMajor>0</VersionMajor>
    <VersionMinor>1</VersionMinor>
    <VersionPatch>0</VersionPatch>
    <VersionPrefix>$(VersionMajor).$(VersionMinor).$(VersionPatch)</VersionPrefix>

    <!-- NuGet Metadata -->
    <PackageId>Ravelaso.UiPath.Selenium</PackageId>
    <Title>Ravelaso.UiPath.Selenium</Title>
    <!-- ... -->
  </PropertyGroup>
</Project>
```

---

## Build & Pack Workflow

### Single-Step Release Build

```bash
cd C:\Data\Dev\Ravelaso.UiPath.Selenium

# 1. Clean (optional)
dotnet clean -c Release

# 2. Build + Pack (automatic)
dotnet build -c Release
```

**What happens:**
1. ✅ Core library compiles (net8.0 + net10.0)
2. ✅ Tests compile
3. ✅ Package project compiles + triggers pack
4. ✅ Runtime assets (runtimes/) copied to bin/
5. ✅ `.nupkg` generated in `dist/`

**Output:**
```
dist/Ravelaso.UiPath.Selenium.0.1.0.nupkg (7.81 MB)
```

---

### Debug Build (Manual Pack)

```bash
dotnet build -c Debug          # Compiles only, no pack
dotnet pack -c Debug           # Generate .nupkg with -dev suffix
```

**Output:**
```
dist/Ravelaso.UiPath.Selenium.0.1.0-dev.20261020.12345.nupkg
```

---

## Package Contents

### Structure

```
Ravelaso.UiPath.Selenium.0.1.0.nupkg
│
├─ lib/
│  ├─ net8.0/                           ← UiPath Studio 25.10
│  │  ├─ Ravelaso.UiPath.Selenium.Core.dll
│  │  ├─ Ravelaso.UiPath.Selenium.Core.xml (documentation)
│  │  ├─ Ravelaso.UiPath.Selenium.Package.dll
│  │  ├─ Selenium.WebDriver.dll (4.51.0)
│  │  ├─ Selenium.Support.dll (4.51.0)
│  │  └─ selenium-manager.exe ✅ CRITICAL
│  │
│  └─ net10.0/                          ← Local development
│     └─ (identical to net8.0)
│
├─ docs/
│  ├─ README.md
│  ├─ api.md
│  ├─ locators.md
│  ├─ plan.md
│  ├─ QUICKSTART.md
│  └─ CHANGELOG.md
│
├─ README.md (root)
├─ LICENSE
├─ .nuspec (NuGet metadata)
└─ [Content_Types].xml
```

### Key Assets

| Asset | Purpose | Why Included |
|---|---|---|
| **Core.dll** | Main library | Actual implementation |
| **Core.xml** | IntelliSense docs | IDE tooltips |
| **Package.dll** | Proxy wrapper | Metadata/version host |
| **selenium-manager.exe** | Selenium asset | ✅ **Solves the error in UiPath** |
| **WebDriver.dll** | Selenium core | Browser automation |
| **Support.dll** | Selenium support | SelectElement, etc. |

---

## Distribution

### To UiPath Studio 25.10

**Method 1: NuGet Local Feed**
```bash
# Copy .nupkg to local folder
Copy-Item dist\Ravelaso.UiPath.Selenium.0.1.0.nupkg C:\MyPackages\

# Add to Studio's package sources
# Settings → NuGet → Add source
```

**Method 2: Direct DLL Import**
```bash
# Extract .nupkg and copy DLL
Expand-Archive dist\Ravelaso.UiPath.Selenium.0.1.0.nupkg extracted\
Copy-Item extracted\lib\net8.0\* C:\MyProject\Libraries\
```

### To Production Machines

**No Admin Required (Key Benefit):**
- Deploy `.nupkg` to all machines via **NuGet feed** or **file share**
- No need to install `selenium-manager.exe` separately ✅
- NuGet restores all assets automatically

---

## Version Bump Procedure

### Patch Version (Bugfix)

Example: `0.1.0` → `0.1.1`

```xml
<!-- Version.props -->
<VersionPatch>1</VersionPatch>
```

```bash
dotnet build -c Release
# Output: dist/Ravelaso.UiPath.Selenium.0.1.1.nupkg
```

### Minor Version (Feature)

Example: `0.1.0` → `0.2.0`

```xml
<!-- Version.props -->
<VersionMinor>2</VersionMinor>
<VersionPatch>0</VersionPatch>  <!-- Reset -->
```

```bash
dotnet build -c Release
# Output: dist/Ravelaso.UiPath.Selenium.0.2.0.nupkg
```

### Major Version (Breaking Change)

Example: `0.1.0` → `1.0.0`

```xml
<!-- Version.props -->
<VersionMajor>1</VersionMajor>
<VersionMinor>0</VersionMinor>  <!-- Reset -->
<VersionPatch>0</VersionPatch>  <!-- Reset -->
```

```bash
dotnet build -c Release
# Output: dist/Ravelaso.UiPath.Selenium.1.0.0.nupkg
```

---

## Key Files Explained

### Version.props

**Purpose:** Central versioning + NuGet metadata  
**Location:** Project root  
**Edited By:** Release engineer before bumping version  
**Imported By:** Package.csproj

```xml
<Import Project="..\Version.props" />

<PropertyGroup>
    <Version>$(VersionPrefix)</Version>
    <VersionSuffix Condition="'$(Configuration)' == 'Debug'">
        dev.$(BuildTimestamp).$(BuildCounter)
    </VersionSuffix>
</PropertyGroup>
```

### Package.csproj

**Purpose:** Packaging proxy + transitive dependency resolver  
**Location:** `Ravelaso.UiPath.Selenium.Package/`  
**Key Features:**
- References Core (pulls all dependencies)
- Copies runtime assets (selenium-manager.exe)
- Auto-packs on Release build only
- Outputs to `dist/` folder

**Critical Settings:**
```xml
<!-- Only auto-pack in Release -->
<GeneratePackageOnBuild Condition="'$(Configuration)' == 'Release'">true</GeneratePackageOnBuild>

<!-- Include runtime assets -->
<Target Name="CopyRuntimesToPackage" AfterTargets="Build">
    <!-- Copy runtimes/ folder for packing -->
</Target>

<ItemGroup>
    <None Include="$(OutputPath)runtimes\**\*" Pack="true" 
          PackagePath="runtimes\%(RecursiveDir)" />
</ItemGroup>
```

---

## Troubleshooting

### Q: `.nupkg` is missing `selenium-manager.exe`

**Symptoms:** NuGet restore works, but at runtime: `Selenium Manager binary not found`

**Diagnosis:**
```powershell
cd dist
Expand-Archive Ravelaso.UiPath.Selenium.0.1.0.nupkg extracted\
ls extracted\lib\net8.0\

# Check for selenium-manager.exe
```

**Solution:**
1. Ensure `Package.csproj` includes the Copy target
2. Run `dotnet clean` then `dotnet build -c Release`
3. Verify `bin/Release/net8.0/runtimes/win/native/selenium-manager.exe` exists
4. Retry pack

### Q: Version suffix not applied to Debug build

**Symptoms:** `dotnet pack -c Debug` produces `0.1.0.nupkg` (no `-dev` suffix)

**Diagnosis:**
```xml
<!-- Check Package.csproj -->
<Version>$(VersionPrefix)</Version>
<VersionSuffix Condition="'$(Configuration)' == 'Debug'">
    dev.$(BuildTimestamp).$(BuildCounter)
</VersionSuffix>
```

**Solution:**
1. Ensure both properties are in PropertyGroup
2. Verify `$(BuildTimestamp)` and `$(BuildCounter)` are defined
3. Run `dotnet pack -c Debug --force` to regenerate

### Q: Build takes too long (regenerating .nupkg)

**Symptoms:** `dotnet build -c Release` rebuilds everything

**Diagnosis:** Core or Tests have changes, Package is rebuilt

**Solution:**
- Only clean when necessary: `dotnet clean -c Release`
- For incremental builds: `dotnet build -c Release` (skips unchanged projects)

### Q: `.nupkg` size is larger than expected

**Expected:** ~7.8 MB (includes runtime assets for all platforms)

**Symptoms:** .nupkg is >100 MB

**Diagnosis:** Likely has debug symbols or multiple architecture copies

**Solution:**
```xml
<!-- Package.csproj -->
<IncludeSymbols>false</IncludeSymbols>
<IncludeSource>false</IncludeSource>
```

---

## CI/CD Integration (Future)

### GitHub Actions Workflow

```yaml
name: Build & Pack

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - run: dotnet build -c Release
      
      - run: dotnet test -c Release
      
      - uses: actions/upload-artifact@v3
        with:
          name: nupkg
          path: dist/*.nupkg
```

### Pushing to NuGet.org (Phase 5)

```bash
dotnet nuget push dist/Ravelaso.UiPath.Selenium.0.1.0.nupkg \
  --api-key ${{ secrets.NUGET_API_KEY }} \
  --source https://api.nuget.org/v3/index.json
```

---

## Checklist: Before Release

- [ ] Update `Version.props` with new version
- [ ] Run `dotnet build -c Release`
- [ ] Verify `.nupkg` in `dist/` folder
- [ ] Extract and inspect `.nupkg` contents (especially `selenium-manager.exe`)
- [ ] Update `CHANGELOG.md` with release notes
- [ ] Commit and tag git: `git tag v0.1.0`
- [ ] Push `.nupkg` to distribution location (NuGet, file share, etc.)
- [ ] Test installation in target environment (UiPath Studio)

---

## Summary

| Task | Command | Output |
|---|---|---|
| **Bump version** | Edit `Version.props` | N/A |
| **Build Release** | `dotnet build -c Release` | `dist/*.nupkg` |
| **Build Debug** | `dotnet build -c Debug` | No .nupkg |
| **Pack Debug** | `dotnet pack -c Debug` | `dist/*-dev.*.nupkg` |
| **Inspect .nupkg** | `Expand-Archive -Path ... -DestinationPath ...` | Extracted folder |
| **Distribute** | Copy `.nupkg` to NuGet feed or file share | Ready for deployment |

---

**Next Phase:** NuGet.org publishing (Phase 5)
