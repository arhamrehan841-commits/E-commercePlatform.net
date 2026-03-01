# eCommerce Monolith

A modular monolith built with ASP.NET Core, organized around bounded contexts with enforced architectural boundaries.

---

## Project Structure

```
eCommerce-Monolith/
├── eCommerce.sln
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
SharedKernel ← Catalog  ──┐
SharedKernel ← Orders   ──┤── Host  (entry point, runs everything)
SharedKernel ← Basket   ──┤
SharedKernel ← Identity ──┘

ArchitectureTests → Catalog, Orders  (Basket & Identity to be added)
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
- **Windows/Linux:** `` Ctrl + ` ``
- **macOS:** `` Cmd + ` ``

Verified the terminal path was pointing to the `eCommerce-Monolith` root directory.

#### Step 3: Solution and Directory Scaffolding

Created the master `.sln` solution file and the full directory tree for all bounded contexts.

```powershell
dotnet new sln -n eCommerce

mkdir src/Modules/Catalog, src/Modules/Orders, src/Modules/Basket, src/Modules/Identity, src/SharedKernel, src/Host
mkdir tests/ArchitectureTests, tests/UnitTests
```

- `dotnet new sln -n eCommerce` → creates `eCommerce.sln`, the solution container file
- `mkdir` with comma-separated paths → PowerShell's `mkdir` accepts multiple paths at once and creates parent directories automatically

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

- `dotnet new classlib` → creates a class library (not runnable; consumed by other projects)
- `dotnet new webapi` → creates a runnable ASP.NET Core Web API project
- `-n` → sets the project name (also the default root namespace and `.csproj` filename)
- `-o` → sets the output directory

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

> The `.sln` file is just a container — it doesn't automatically discover projects created inside its directory. `dotnet sln add` tells the solution which `.csproj` files belong to it, enabling `dotnet build` from the root and proper VS Code project discovery.

#### Step 6: Enforcing the Dependency Graph

Wired project-to-project references to enforce architectural boundaries.

```powershell
# Modules → SharedKernel
dotnet add src/Modules/Catalog/Modules.Catalog.csproj   reference src/SharedKernel/SharedKernel.csproj
dotnet add src/Modules/Orders/Modules.Orders.csproj     reference src/SharedKernel/SharedKernel.csproj
dotnet add src/Modules/Basket/Modules.Basket.csproj     reference src/SharedKernel/SharedKernel.csproj
dotnet add src/Modules/Identity/Modules.Identity.csproj reference src/SharedKernel/SharedKernel.csproj

# Host → All Modules
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

- `dotnet new xunit` → creates an xUnit test project
- `dotnet add ... package` → installs a NuGet package (equivalent to `pip install`)
- `NetArchTest.Rules` → enables automated tests that enforce architecture rules (e.g. fail the build if a module references another module directly)

> Basket and Identity references will be added to ArchitectureTests in a future session.

---