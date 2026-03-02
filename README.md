# eCommerce Monolith

A modular monolith built with ASP.NET Core, organized around bounded contexts with enforced architectural boundaries.

---

## Project Structure
```
eCommerce-Monolith/
├── eCommerce.sln
├── Directory.Build.props
├── Directory.Packages.props
├── .editorconfig
├── src/
│   ├── Modules/
│   │   ├── Catalog/
│   │   ├── Orders/
│   │   ├── Basket/
│   │   └── Identity/
│   ├── SharedKernel/
│   └── Host/
└── tests/
    ├── ArchitectureTests/
    └── UnitTests/
```

---

## Dependency Graph
```
SharedKernel <- Catalog  --+
SharedKernel <- Orders   --+-- Host  (entry point, runs everything)
SharedKernel <- Basket   --+
SharedKernel <- Identity --+

ArchitectureTests -> Catalog, Orders  (Basket & Identity to be added)
```

**Rules enforced:**
- Modules may only reference `SharedKernel`
- `Host` may reference all modules
- Modules may **not** reference each other

---

## Dev Log

### Day 1 — Workspace & Solution Setup

#### Step 1: Workspace Initialization

Created the root directory and opened it in VS Code.
```powershell
mkdir eCommerce-Monolith

cd eCommerce-Monolith

code .
```

#### Step 2: Launch Integrated Terminal

Opened the VS Code integrated terminal:
- **Windows/Linux:** `Ctrl + backtick`
- **macOS:** `Cmd + backtick`

Verified the terminal path was pointing to the `eCommerce-Monolith` root directory.

#### Step 3: Solution and Directory Scaffolding

Created the master `.sln` solution file and the full directory tree for all bounded contexts.
```powershell
dotnet new sln -n eCommerce

mkdir src/Modules/Catalog, src/Modules/Orders, src/Modules/Basket, src/Modules/Identity, src/SharedKernel, src/Host
mkdir tests/ArchitectureTests, tests/UnitTests
```

- `dotnet new sln -n eCommerce` — creates `eCommerce.sln`, the solution container file
- `mkdir` with comma-separated paths — PowerShell's `mkdir` accepts multiple paths at once and creates parent directories automatically

#### Step 4: Provisioning the Bounded Contexts

Scaffolded isolated class library projects for each domain module, the shared kernel, and the hosting Web API.
```powershell
dotnet new classlib -n Modules.Catalog  -o src/Modules/Catalog
dotnet new classlib -n Modules.Orders   -o src/Modules/Orders
dotnet new classlib -n Modules.Basket   -o src/Modules/Basket
dotnet new classlib -n Modules.Identity -o src/Modules/Identity
dotnet new classlib -n SharedKernel     -o src/SharedKernel
dotnet new webapi   -n Host             -o src/Host
```

- `dotnet new classlib` — creates a class library (not runnable; consumed by other projects)
- `dotnet new webapi` — creates a runnable ASP.NET Core Web API project
- `-n` — sets the project name (also the default root namespace and `.csproj` filename)
- `-o` — sets the output directory

Each command produces a completely independent project with its own `.csproj` file.

#### Step 5: Registering Projects with the Solution

Added all `.csproj` files to the solution so the build system tracks them.
```powershell
dotnet sln add src/Modules/Catalog/Modules.Catalog.csproj
dotnet sln add src/Modules/Orders/Modules.Orders.csproj
dotnet sln add src/Modules/Basket/Modules.Basket.csproj
dotnet sln add src/Modules/Identity/Modules.Identity.csproj
dotnet sln add src/SharedKernel/SharedKernel.csproj
dotnet sln add src/Host/Host.csproj
```

> The `.sln` file is just a container — it doesn't automatically discover projects inside its directory. `dotnet sln add` tells the solution which `.csproj` files belong to it, enabling `dotnet build` from the root and proper VS Code project discovery.

#### Step 6: Enforcing the Dependency Graph

Wired project-to-project references to enforce architectural boundaries.
```powershell
# Modules -> SharedKernel
dotnet add src/Modules/Catalog/Modules.Catalog.csproj   reference src/SharedKernel/SharedKernel.csproj
dotnet add src/Modules/Orders/Modules.Orders.csproj     reference src/SharedKernel/SharedKernel.csproj
dotnet add src/Modules/Basket/Modules.Basket.csproj     reference src/SharedKernel/SharedKernel.csproj
dotnet add src/Modules/Identity/Modules.Identity.csproj reference src/SharedKernel/SharedKernel.csproj

# Host -> All Modules
dotnet add src/Host/Host.csproj reference src/Modules/Catalog/Modules.Catalog.csproj
dotnet add src/Host/Host.csproj reference src/Modules/Orders/Modules.Orders.csproj
dotnet add src/Host/Host.csproj reference src/Modules/Basket/Modules.Basket.csproj
dotnet add src/Host/Host.csproj reference src/Modules/Identity/Modules.Identity.csproj
```

> A `using` statement alone is not sufficient — the compiler needs a `.csproj` reference to locate the correct `.dll`. References are required whenever code crosses project boundaries.

#### Step 7: Architecture Tests Setup

Scaffolded the architecture test project, registered it with the solution, installed NetArchTest, and referenced the modules under test.
```powershell
dotnet new xunit -n ArchitectureTests -o tests/ArchitectureTests
dotnet sln add tests/ArchitectureTests/ArchitectureTests.csproj

dotnet add tests/ArchitectureTests/ArchitectureTests.csproj package NetArchTest.Rules

dotnet add tests/ArchitectureTests/ArchitectureTests.csproj reference src/Modules/Catalog/Modules.Catalog.csproj
dotnet add tests/ArchitectureTests/ArchitectureTests.csproj reference src/Modules/Orders/Modules.Orders.csproj
```

- `dotnet new xunit` — creates an xUnit test project
- `dotnet add ... package` — installs a NuGet package (equivalent to `pip install`)
- `NetArchTest.Rules` — enables automated tests that enforce architecture rules (e.g. fail the build if a module references another module directly)

> Basket and Identity references will be added to ArchitectureTests in a future session.

---

### Day 2 — Enterprise Repository & GitOps Standardization

**Objective:** Lock down repository configuration to prevent drift, enforce strict compilation standards across all bounded contexts, and establish the Trunk-Based Development Git baseline.

#### Step 1: Git Initialization & Artifact Exclusion

Initialize source control and apply standard .NET ignore rules to prevent compiled binaries (`bin/`, `obj/`) from polluting the repository.
```powershell
git init
dotnet new gitignore
```

#### Step 2: Enforce Centralized Package Management

Without a central package manager, each `.csproj` can declare its own NuGet versions — leading to runtime dependency conflicts. `Directory.Packages.props` establishes a single source of truth for all package versions across every module.

Create **`Directory.Packages.props`** in the repo root:
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageVersion Include="xunit" Version="2.5.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.3" />
    
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="8.0.2" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="6.4.0" />
  </ItemGroup>
</Project>

```

Or generate it via PowerShell:
```powershell
$content = @'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageVersion Include="xunit" Version="2.5.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="8.0.2" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="6.4.0" />
  </ItemGroup>
</Project>
'@
Set-Content -Path "Directory.Packages.props" -Value $content -Encoding UTF8
```

> When any module needs a package (e.g. `Microsoft.EntityFrameworkCore`), it declares `<PackageReference Include="..." />` with **no version**. The version is resolved exclusively from this root file.

#### Step 3: Mandate Global Compilation Rules

`Directory.Build.props` forces every current and future module to compile against the same target framework and treats warnings as pipeline-breaking errors — no local overrides allowed.

Create **`Directory.Build.props`** in the repo root:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

Or generate it via PowerShell:
```powershell
$content = @'
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
'@
Set-Content -Path "Directory.Build.props" -Value $content -Encoding UTF8
```

#### Step 4: Deterministic Code Formatting

Scaffold a root-level `.editorconfig` so VS Code and the .NET CLI format code consistently. The CI/CD pipeline will reject code that doesn't conform to these standards.
```powershell
dotnet new editorconfig
```

#### Step 5: Clean the Project Files

Strip the hardcoded version number from `ArchitectureTests.csproj` (set in Day 1) so it defers to `Directory.Packages.props`. No `.csproj` should specify its own version when central management is enabled.
```powershell
$path = "tests/ArchitectureTests/ArchitectureTests.csproj"
$content = Get-Content $path
$content = $content -replace 'Version="1.3.2"', ''
Set-Content -Path $path -Value $content -Encoding UTF8
```

#### Step 6: The Immutable Baseline Commit

Commit this state to `main` to establish your Trunk.
```powershell
# Stage all configuration and structural files
git add .

# Commit the architectural baseline
git commit -m "chore: scaffold modular monolith boundary and centralized config"

# Ensure the primary branch is named 'main'
git branch -M main
```

**What this guarantees:**
- **Zero dependency drift** — no developer can silently upgrade a NuGet package in a single module
- **Strict compilation** — unhandled nullables or unused variables fail the build immediately
- **Trunk-Based ready** — `main` is initialized with a clean, compilable state

---

## Getting Started
```powershell
# Clone and enter the repo
git clone <your-repo-url>
cd eCommerce-Monolith

# Build the entire solution
dotnet build

# Run the host
dotnet run --project src/Host/Host.csproj

# Run architecture tests
dotnet test tests/ArchitectureTests/ArchitectureTests.csproj
```
-----------------