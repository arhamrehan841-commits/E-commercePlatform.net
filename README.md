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
├── infra/
│   └── vpc.yaml
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

## **Day 1 — Workspace & Solution Setup**

#### Step 1: Workspace Initialization

Created the root directory and opened it in VS Code.

```powershell
mkdir eCommerce-Monolith
cd eCommerce-Monolith
code .
```

---

#### Step 2: Launch Integrated Terminal

Opened the VS Code integrated terminal.

- **Windows/Linux:** `Ctrl + backtick`
- **macOS:** `Cmd + backtick`

Verified terminal path was pointing to the `eCommerce-Monolith` root directory.

---

#### Step 3: Solution and Directory Scaffolding

Created the master `.sln` solution file and the physical directory tree for all bounded contexts.

```powershell
dotnet new sln -n eCommerce

mkdir src/Modules/Catalog, src/Modules/Orders, src/Modules/Basket, src/Modules/Identity, src/SharedKernel, src/Host
mkdir tests/ArchitectureTests, tests/UnitTests
```

- `dotnet new sln -n eCommerce` — creates the solution container file `eCommerce.sln`
- `mkdir` with comma-separated paths — PowerShell's `mkdir` accepts multiple paths at once and creates parent directories automatically

---

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

- `dotnet new classlib` — creates a class library project (not runnable, meant to be consumed by other projects)
- `dotnet new webapi` — creates a runnable ASP.NET Core Web API project
- `-n` — sets the project name (also becomes the default root namespace and `.csproj` filename)
- `-o` — sets the output directory where project files are generated

Each `dotnet new` command creates a completely separate, independent project with its own `.csproj` file.

---

#### Step 5: Registering Projects with the Solution

Added all `.csproj` files to the solution file so the build system tracks them.

```powershell
dotnet sln add src/Modules/Catalog/Modules.Catalog.csproj
dotnet sln add src/Modules/Orders/Modules.Orders.csproj
dotnet sln add src/Modules/Basket/Modules.Basket.csproj
dotnet sln add src/Modules/Identity/Modules.Identity.csproj
dotnet sln add src/SharedKernel/SharedKernel.csproj
dotnet sln add src/Host/Host.csproj
```

- The `.sln` file is just a container — it doesn't automatically know about projects created inside its directory
- `dotnet sln add` tells the solution "these `.csproj` files belong to you"
- Required for `dotnet build` to compile everything from the root and for VS Code to display all projects

---

#### Step 6: Enforcing the Dependency Graph

Wired project-to-project references to enforce the architectural rule: modules only know about SharedKernel, and only Host knows about all modules.

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

Dependency rules enforced:
- Modules — can only reference SharedKernel
- Host — can reference all Modules
- Modules — cannot reference each other

> A namespace alone (`using Modules.Catalog;`) is not enough — the compiler needs a reference to know which `.dll` file contains that namespace. References are required when crossing project boundaries.

---

#### Step 7: Architecture Tests Setup

Scaffolded the architecture test project, registered it with the solution, installed the NetArchTest NuGet package, and referenced the modules to be tested.

```powershell
dotnet new xunit -n ArchitectureTests -o tests/ArchitectureTests
dotnet sln add tests/ArchitectureTests/ArchitectureTests.csproj

dotnet add tests/ArchitectureTests/ArchitectureTests.csproj package NetArchTest.Rules

dotnet add tests/ArchitectureTests/ArchitectureTests.csproj reference src/Modules/Catalog/Modules.Catalog.csproj
dotnet add tests/ArchitectureTests/ArchitectureTests.csproj reference src/Modules/Orders/Modules.Orders.csproj
```

- `dotnet new xunit` — creates a test project using the xUnit testing framework
- `dotnet add ... package` — installs a NuGet package (equivalent to `pip install` in Python)
- `NetArchTest.Rules` — library that enables writing automated tests to enforce architecture rules (e.g. fail the build if a module references another module directly)

> Only Catalog and Orders referenced for now — remaining modules (Basket, Identity) to be added later.

---

## **Day 2 — Enterprise Repository & GitOps Standardization**

**Objective:** Lock down the repository configuration to prevent configuration drift, enforce strict compilation standards across all bounded contexts, and establish the Trunk-Based Development Git baseline. In a monorepo, centralized control of dependencies and compiler rules is mandatory to guarantee deterministic CI/CD builds.

Execute the following steps directly in your VS Code PowerShell terminal.

---

#### Step 1: Git Initialization & Artifact Exclusion

You must initialize the source control tracking and immediately apply the standard .NET ignore rules to prevent compiled binaries (`bin/`, `obj/`) from polluting the repository.

```powershell
# Initialize the Git repository
git init

# Generate the standard .NET .gitignore file
dotnet new gitignore
```

---

#### Step 2: Enforce Centralized Package Management

When managing multiple modules, allowing each `.csproj` to dictate its own NuGet package versions leads to dependency conflicts during runtime composition. You must enforce `Directory.Packages.props` to maintain a single source of truth.

Create **`Directory.Packages.props`** in the repo root:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
  </ItemGroup>
</Project>
```

Or execute the following PowerShell command to generate it:

```powershell
$packagesProps = @'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
  </ItemGroup>
</Project>
'@
Set-Content -Path "Directory.Packages.props" -Value $packagesProps -Encoding UTF8
```

> **Architectural Impact:** If the Orders module and the Catalog module both require Entity Framework Core in the future, they will simply declare `<PackageReference Include="Microsoft.EntityFrameworkCore" />` without a version number. The CI pipeline will resolve the version exclusively from this root file.

---

#### Step 3: Mandate Global Compilation Rules

You must eliminate localized compiler settings. The `Directory.Build.props` file will force all current and future modules to compile against the exact same target framework and treat warnings as pipeline-breaking errors.

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

Or execute this command to create the file:

```powershell
$buildProps = @'
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
Set-Content -Path "Directory.Build.props" -Value $buildProps -Encoding UTF8
```

---

#### Step 4: Deterministic Code Formatting

The CI/CD pipeline will reject code that does not match enterprise formatting standards. You must scaffold an `.editorconfig` file at the root level. VS Code and the .NET CLI will use this to format code deterministically.

```powershell
# Generate the standard enterprise EditorConfig
dotnet new editorconfig
```

---

#### Step 5: Clean the Project Files

Because we enabled Central Package Management, we must remove the hardcoded version number from the `ArchitectureTests.csproj` file generated in Day 1.

Execute this script to strip the explicit version, forcing the test project to rely on `Directory.Packages.props`:

```powershell
$testProjPath = "tests/ArchitectureTests/ArchitectureTests.csproj"
$content = Get-Content $testProjPath
$content = $content -replace 'Version="1.3.2"', ''
Set-Content -Path $testProjPath -Value $content -Encoding UTF8
```

---

#### Step 6: The Immutable Baseline Commit

With the structural boundaries and compilation rules physically locked into the file system, you must commit this state to the main branch. This establishes your Trunk.

```powershell
# Stage all configuration and structural files
git add .

# Commit the architectural baseline
git commit -m "chore: scaffold modular monolith boundary and centralized config"

# Ensure the primary branch is named 'main'
git branch -M main
```

**Architecture Validation Checklist**

By executing this sequence, you have guaranteed the following constraints:

- **Zero Dependency Drift** — No developer can secretly upgrade a NuGet package in a single module.
- **Strict Compilation** — The `TreatWarningsAsErrors` flag ensures that unhandled nullables or unused variables will instantly fail the local build and the future CI pipeline.
- **Trunk-Based Ready** — The `main` branch is initialized with a clean, compilable state.

---

## **Day 3 — Infrastructure as Code (IaC) Bootstrapping**

**Objective:** Provision an Amazon Virtual Private Cloud (VPC) using AWS CloudFormation, strictly divided into three tiers to meet enterprise security standards.

**Network Tier Design:**

| Tier | Subnet | Purpose |
|---|---|---|
| Public | `10.0.1.0/24` | NAT Gateway and Application Load Balancer (ALB) — internet accessible |
| Private | `10.0.2.0/24` | .NET API on ECS Fargate — reachable only through the ALB |
| Isolated | `10.0.3.0/24` | SQL Server on Amazon RDS — strictly internal, no internet access in or out |

Execute the following steps in your VS Code terminal.

---

#### Step 1: Scaffold the IaC Directory

Keep your infrastructure definitions right next to your application code.

```powershell
mkdir infra
```

---

#### Step 2: Define the Network Template

Create **`infra/vpc.yaml`** and paste the following declarative infrastructure blueprint. This defines your network boundaries mathematically.

```yaml
AWSTemplateFormatVersion: '2010-09-09'
Description: 'Day 3: Enterprise Multi-Tier VPC for .NET Modular Monolith'

Resources:
  # 1. The Virtual Private Cloud
  MainVPC:
    Type: AWS::EC2::VPC
    Properties:
      CidrBlock: 10.0.0.0/16
      EnableDnsSupport: true
      EnableDnsHostnames: true
      Tags:
        - Key: Name
          Value: eCommerce-VPC

  # 2. Public Subnet (For Load Balancers)
  PublicSubnet:
    Type: AWS::EC2::Subnet
    Properties:
      VpcId: !Ref MainVPC
      CidrBlock: 10.0.1.0/24
      MapPublicIpOnLaunch: true
      AvailabilityZone: !Select [0, !GetAZs '']
      Tags:
        - Key: Name
          Value: eCommerce-Public-Subnet

  # 3. Private Subnet (For .NET API Compute)
  PrivateSubnet:
    Type: AWS::EC2::Subnet
    Properties:
      VpcId: !Ref MainVPC
      CidrBlock: 10.0.2.0/24
      MapPublicIpOnLaunch: false
      AvailabilityZone: !Select [0, !GetAZs '']
      Tags:
        - Key: Name
          Value: eCommerce-Private-Subnet

  # 4. Isolated Subnet (For RDS Database)
  IsolatedSubnet:
    Type: AWS::EC2::Subnet
    Properties:
      VpcId: !Ref MainVPC
      CidrBlock: 10.0.3.0/24
      MapPublicIpOnLaunch: false
      AvailabilityZone: !Select [0, !GetAZs '']
      Tags:
        - Key: Name
          Value: eCommerce-Isolated-Subnet

  # 5. Internet Gateway (To allow external traffic into the Public Subnet)
  InternetGateway:
    Type: AWS::EC2::InternetGateway
    Properties:
      Tags:
        - Key: Name
          Value: eCommerce-IGW

  AttachGateway:
    Type: AWS::EC2::VPCGatewayAttachment
    Properties:
      VpcId: !Ref MainVPC
      InternetGatewayId: !Ref InternetGateway

  # 6. Public Route Table (Routing internet traffic to the IGW)
  PublicRouteTable:
    Type: AWS::EC2::RouteTable
    Properties:
      VpcId: !Ref MainVPC
      Tags:
        - Key: Name
          Value: eCommerce-Public-RouteTable

  PublicRoute:
    Type: AWS::EC2::Route
    DependsOn: AttachGateway
    Properties:
      RouteTableId: !Ref PublicRouteTable
      DestinationCidrBlock: 0.0.0.0/0
      GatewayId: !Ref InternetGateway

  PublicSubnetRouteTableAssociation:
    Type: AWS::EC2::SubnetRouteTableAssociation
    Properties:
      SubnetId: !Ref PublicSubnet
      RouteTableId: !Ref PublicRouteTable

Outputs:
  VpcId:
    Description: "The ID of the provisioned VPC"
    Value: !Ref MainVPC
    Export:
      Name: eCommerce-VpcId
```

---

#### Step 3: Deploy the Infrastructure

You must have the AWS CLI installed and configured with your credentials (`aws configure`). Once authenticated, execute this command from the root of your workspace to deploy the stack to AWS:

```powershell
aws cloudformation deploy `
  --template-file infra/vpc.yaml `
  --stack-name eCommerce-Network-Stack `
  --region us-east-1
```

> You can change `us-east-1` to your preferred AWS region if necessary.

This command packages your YAML, sends it to the AWS API, and deterministically provisions your entire network infrastructure. Deployment is complete when the CLI outputs:

```
Successfully created/updated stack - eCommerce-Network-Stack
```

---


## **Day 4 — CI/CD Pipeline Scaffolding**

**Objective:** Build the automated gatekeeper pipeline today rather than waiting until the end of the project. Every commit will trigger a cloud server that compiles the .NET modular monolith, runs architectural boundary tests, and statically analyzes AWS CloudFormation templates.

**Platform:** GitHub Actions — industry default, deeply integrated, and free.

Two separate pipelines are established:
- **dotnet-ci** — monitors C# domain logic and architecture boundaries
- **iac-ci** — monitors AWS infrastructure definitions

---

#### Commit 1: The .NET Continuous Integration Pipeline

This workflow ensures that nobody can merge code that breaks the compilation rules set up on Day 2, or violates the `NetArchTest` boundary rules set up on Day 1.

**Step 1:** Create the GitHub Actions directory structure.

```powershell
mkdir .github/workflows
```

**Step 2:** Create **`.github/workflows/dotnet-ci.yml`**:

```yaml
name: .NET Enterprise CI

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest

    steps:
    - name: Checkout Repository
      uses: actions/checkout@v4

    - name: Setup .NET 8
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Restore Dependencies
      run: dotnet restore

    - name: Build Solution (Strict)
      # This enforces our Directory.Build.props <TreatWarningsAsErrors> rule
      run: dotnet build --no-restore

    - name: Execute Architecture & Domain Tests
      run: dotnet test --no-build --verbosity normal
```

**Step 3:** Commit the pipeline.

```powershell
git add .github/workflows/dotnet-ci.yml
git commit -m "ci: implement automated .net build and architecture test pipeline"
```

---

#### Commit 2: Infrastructure as Code (IaC) Linting

A broken YAML file can take down the entire cloud network. This pipeline mathematically validates CloudFormation templates before any deployment attempt using `cfn-lint`. It only triggers when files inside `infra/` change, keeping it focused and efficient.

**Step 1:** Create **`.github/workflows/iac-ci.yml`**:

```yaml
name: Infrastructure CI

on:
  push:
    paths:
      - 'infra/**'
  pull_request:
    paths:
      - 'infra/**'

jobs:
  validate-cloudformation:
    name: Lint CloudFormation Templates
    runs-on: ubuntu-latest

    steps:
    - name: Checkout Repository
      uses: actions/checkout@v4

    - name: Setup CloudFormation Linter
      uses: scottbrenner/cfn-lint-action@v2

    - name: Lint VPC & Network Templates
      # The -I flag ignores informational warnings, failing only on real errors
      run: cfn-lint -I infra/*.yaml
```

**Step 2:** Commit the pipeline.

```powershell
git add .github/workflows/iac-ci.yml
git commit -m "ci: integrate cfn-lint for automated cloudformation validation"
```


You now have a multi-pipeline CI setup. One monitors your C# domain logic, and the other monitors your AWS infrastructure definitions.


---

## **Day 5 — Stateful Infrastructure: RDS SQL Server & Secrets Management**

**Objective:** Define the Infrastructure as Code to provision the relational database layer (Amazon RDS SQL Server). In a modular monolith, the database represents the highest risk of architectural coupling — if the Orders module can execute a SQL JOIN directly against the Catalog tables, you have failed to build a modular system and instead built a distributed big ball of mud. Schema segregation will be enforced at the code level on Day 6. Today we build the physical AWS vault to house it.

> **FinOps Note:** Do **not** run `aws cloudformation deploy` for `database.yaml` today. RDS incurs hourly charges even on `t3.micro`. The definition is safely committed to Git. It will be deployed on Day 24 (Integration Testing), tested, and immediately torn down to keep the AWS bill near $0.00.

---

#### Commit 1: The Zero-Trust Database Infrastructure

We do not type a master password into the AWS Console, and we do not hardcode it into YAML. AWS Secrets Manager dynamically generates a highly complex master password during deployment. Only the .NET API will be granted permission to read this secret.

**Step 1:** Create **`infra/database.yaml`**:

```yaml
AWSTemplateFormatVersion: '2010-09-09'
Description: 'Day 5: Enterprise RDS SQL Server & Secrets Management'

Parameters:
  VpcId:
    Type: AWS::EC2::VPC::Id
    Description: "The VPC ID from our Day 3 Network Stack"
  IsolatedSubnetIds:
    Type: List<AWS::EC2::Subnet::Id>
    Description: "The Isolated Subnets for the DB Subnet Group"

Resources:
  # 1. Dynamic Master Password Generation
  DbMasterSecret:
    Type: AWS::SecretsManager::Secret
    Properties:
      Name: eCommerce/Database/MasterCredentials
      Description: "Dynamically generated RDS Master Password"
      GenerateSecretString:
        SecretStringTemplate: '{"username": "admin"}'
        GenerateStringKey: "password"
        PasswordLength: 32
        ExcludeCharacters: '"@/\'

  # 2. Database Security Group (The Vault Door)
  DbSecurityGroup:
    Type: AWS::EC2::SecurityGroup
    Properties:
      GroupDescription: "Allow SQL Server access strictly from the API"
      VpcId: !Ref VpcId
      SecurityGroupIngress:
        # Note: In a production stack, SourceSecurityGroupId would be your ECS Fargate SG.
        # For now, we lock it to the VPC CIDR boundary.
        - IpProtocol: tcp
          FromPort: 1433
          ToPort: 1433
          CidrIp: 10.0.0.0/16
      Tags:
        - Key: Name
          Value: eCommerce-RDS-SG

  # 3. DB Subnet Group (Placing it in the Isolated Tier)
  DbSubnetGroup:
    Type: AWS::RDS::DBSubnetGroup
    Properties:
      DBSubnetGroupDescription: "Subnets isolated from the public internet"
      SubnetIds: !Ref IsolatedSubnetIds

  # 4. The RDS SQL Server Instance (Express Edition for FinOps)
  SqlServerInstance:
    Type: AWS::RDS::DBInstance
    Properties:
      Engine: sqlserver-ex
      DBInstanceClass: db.t3.micro
      AllocatedStorage: 20
      DBSubnetGroupName: !Ref DbSubnetGroup
      VPCSecurityGroups:
        - !Ref DbSecurityGroup
      MasterUsername: !Join [ "", [ "{{resolve:secretsmanager:", !Ref DbMasterSecret, ":SecretString:username}}" ] ]
      MasterUserPassword: !Join [ "", [ "{{resolve:secretsmanager:", !Ref DbMasterSecret, ":SecretString:password}}" ] ]
      PubliclyAccessible: false
      StorageEncrypted: true
      Tags:
        - Key: Name
          Value: eCommerce-ModularMonolith-DB

Outputs:
  DatabaseEndpoint:
    Description: "The connection endpoint for the .NET API"
    Value: !GetAtt SqlServerInstance.Endpoint.Address
```

**Step 2:** Commit the infrastructure definition.

```powershell
git add infra/database.yaml
git commit -m "feat(infra): define rds sql server and dynamic secrets manager vault"
```
---

## **Day 6 — Internal Module Architecture (The Domain Layer)**

**Objective:** Implement Rich Domain Models for the Catalog and Orders modules, ensuring strict encapsulation, data integrity, and zero-trust validation at the core entity level.

---

## 1. Curing Primitive Obsession (Shared Kernel)

Instead of passing loose `decimal` values around the system — which risks adding USD to CAD — we created a Value Object to encapsulate money and currency.

**File: `src/SharedKernel/ValueObjects/Money.cs`**

```csharp
namespace SharedKernel.ValueObjects;

public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency = "USD") => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");

        return new Money(Amount + other.Amount, Currency);
    }
}
```

---

## 2. The Catalog Domain (Strict Encapsulation)

We built the `Product` entity with private setters. Guard clauses were explicitly added to the factory method to ensure a product can never be created with a negative price or an empty description.

**File: `src/Modules/Catalog/Domain/Product.cs`**

```csharp
using SharedKernel.ValueObjects;

namespace Modules.Catalog.Domain;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero();

    // Parameterless constructor required by Entity Framework Core
    private Product() { }

    // Factory method for creating valid products
    public static Product Create(string name, string description, Money price)
    {
        // Guard Clauses to protect domain invariants
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be empty");

        if (price.Amount <= 0)
            throw new ArgumentException("Product price must be greater than zero");

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price
        };
    }

    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            throw new ArgumentException("Price must be greater than zero");

        Price = newPrice;
    }
}
```

---

## 3. The Orders Domain (Aggregate Root)

The `Order` acts as the **Aggregate Root**. It controls all modifications to the items beneath it. A guard clause was added to prevent the creation of "ghost orders" attached to empty customer IDs.

**File: `src/Modules/Orders/Domain/Order.cs`**

```csharp
using SharedKernel.ValueObjects;

namespace Modules.Orders.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Status { get; private set; } = "Pending";

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Parameterless constructor required by Entity Framework Core
    private Order() { }

    public static Order Create(Guid customerId)
    {
        // Guard Clause
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty");

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId
        };
    }

    public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        var item = new OrderItem(Id, productId, productName, unitPrice, quantity);
        _items.Add(item);
    }

    public Money CalculateTotal()
    {
        if (!_items.Any()) return Money.Zero();

        var totalAmount = _items.Sum(i => i.UnitPrice.Amount * i.Quantity);
        var currency = _items.First().UnitPrice.Currency;

        return new Money(totalAmount, currency);
    }
}
```

---

## 4. The OrderItem Entity (Intrinsic Validation)

`OrderItem` is a child entity. It does not have a public factory method because it must only be created via the `Order` aggregate. Exhaustive guard clauses enforce physical reality — for example, quantity cannot be zero.

**File: `src/Modules/Orders/Domain/OrderItem.cs`**

```csharp
using SharedKernel.ValueObjects;

namespace Modules.Orders.Domain;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Money UnitPrice { get; private set; } = Money.Zero();
    public int Quantity { get; private set; }

    // Parameterless constructor required by Entity Framework Core
    private OrderItem() { }

    internal OrderItem(Guid orderId, Guid productId, string productName, Money unitPrice, int quantity)
    {
        // Guard Clauses
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty");

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty");

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty");

        if (unitPrice.Amount < 0)
            throw new ArgumentException("Unit price cannot be negative");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be at least 1");

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
```

---

## 5. Day 6 Git History Summary

By the end of the session, the Git log reflected these atomic commits:

```
feat(domain): introduce money value object to shared kernel to prevent currency mismatch
feat(catalog): implement rich product domain entity with strict encapsulation
feat(orders): implement order aggregate root and order items
fix(catalog): add guard clauses to product factory to enforce price and description invariants
fix(orders): add guard clauses and EF core constructor to order item entity
fix(orders): add guard clause to order factory to enforce valid customer id
```
---

## **Day 7 — Entity Framework Core & Schema Segregation**

**Objective:** Bridge the gap between the pristine C# domain entities (Day 6) and the physical AWS SQL Server vault (Day 5).

In a Modular Monolith, using one massive `AppDbContext` for the entire platform allows developers to write lazy LINQ queries that join the Orders tables directly to the Catalog tables — destroying module boundaries and turning the architecture into a Big Ball of Mud.

The enterprise solution is **Schema Segregation**: a separate `DbContext` per module, with EF Core physically isolating their tables using SQL Server schemas (e.g., `catalog.Products` and `orders.Orders`).

---

## **Commit 1: Centralized EF Core Dependencies**

EF Core NuGet packages are managed centrally so every module uses the exact same version — consistent with the MSBuild configuration established on Day 2.

**Step 1:** Open **`Directory.Packages.props`** at the repo root and add the EF Core packages:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="coverlet.collector" Version="6.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageVersion Include="xunit" Version="2.5.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />

    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.2" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.2" />
  </ItemGroup>
</Project>
```

**Step 2:** Open **`src/Modules/Catalog/Modules.Catalog.csproj`** and **`src/Modules/Orders/Modules.Orders.csproj`** and add the reference — without a version number:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
</ItemGroup>
```

Do this for both the Catalog and Orders `.csproj` files.

**Step 3:** Commit.

```powershell
git add .
git commit -m "build: add ef core sql server dependencies centrally"
```

---

## **Commit 2: The Catalog Database Context**

We create the `CatalogDbContext` and map the `Product` entity. EF Core must also be told how to handle the `Money` Value Object, because SQL Server only understands columns — not C# records.

**Step 1:** Create folder `src/Modules/Catalog/Infrastructure/Data/` and add **`CatalogDbContext.cs`**:

```csharp
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Domain;

namespace Modules.Catalog.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Enforce Schema Segregation
        modelBuilder.HasDefaultSchema("catalog");

        // 2. Map the Product Entity
        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).IsRequired().HasMaxLength(2000);

            // 3. Map the Money Value Object (Owned Entity)
            builder.OwnsOne(p => p.Price, priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("PriceAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                priceBuilder.Property(m => m.Currency)
                    .HasColumnName("PriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });
    }
}
```

**Step 2:** Commit.

```powershell
git add src/Modules/Catalog/Infrastructure/Data/CatalogDbContext.cs
git commit -m "feat(catalog): implement DbContext with catalog schema and map product entity"
```

---

## **Commit 3: The Orders Database Context**

The Orders context is slightly more complex — it has an Aggregate Root (`Order`) and a collection of child entities (`OrderItem`). EF Core must be explicitly told about this strict relationship.

**Step 1:** Create folder `src/Modules/Orders/Infrastructure/Data/` and add **`OrdersDbContext.cs`**:

```csharp
using Microsoft.EntityFrameworkCore;
using Modules.Orders.Domain;

namespace Modules.Orders.Infrastructure.Data;

public class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Enforce Schema Segregation
        modelBuilder.HasDefaultSchema("orders");

        // 2. Map the Order Entity
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CustomerId).IsRequired();
            builder.Property(o => o.Status).IsRequired().HasMaxLength(50);

            // Strictly map the internal collection
            builder.HasMany(o => o.Items)
                   .WithOne()
                   .HasForeignKey(i => i.OrderId)
                   .OnDelete(DeleteBehavior.Cascade); // If order is deleted, delete items
        });

        // 3. Map the OrderItem Entity
        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.ProductId).IsRequired();
            builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            builder.Property(i => i.Quantity).IsRequired();

            // Map the Money Value Object for the item price
            builder.OwnsOne(i => i.UnitPrice, priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("UnitPriceAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                priceBuilder.Property(m => m.Currency)
                    .HasColumnName("UnitPriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });
    }
}
```

**Step 2:** Commit.

```powershell
git add src/Modules/Orders/Infrastructure/Data/OrdersDbContext.cs
git commit -m "feat(orders): implement DbContext with orders schema and map aggregate relationships"
```
---

## **Day 8 — The Application Layer (CQRS & MediatR)**

**Objective:** Decouple the core domain and database from the API edge by implementing the Command Query Responsibility Segregation (CQRS) pattern. Use MediatR as an internal message bus to automatically route requests to their isolated handlers.

---

## **Commit 1: Centralizing the MediatR Dependency**

Enforced the MediatR version globally to ensure all modules in the monolith are strictly synchronized.

**File: `Directory.Packages.props` (Root)**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="MediatR" Version="12.2.0" />
  </ItemGroup>
</Project>
```

**File: `src/Modules/Catalog/Modules.Catalog.csproj` & `src/Modules/Orders/Modules.Orders.csproj`**

```xml
<ItemGroup>
  <PackageReference Include="MediatR" />
</ItemGroup>
```

**Execution:**

```powershell
git add .
git commit -m "build: add mediatR dependency centrally for cqrs implementation"
```

---

## **Commit 2: Implementing a Command (Write Operation)**

Commands mutate state. This use case takes a payload, reconstructs the `Money` Value Object, invokes the `Product` Domain Entity (triggering the guard clauses), and commits it to the SQL database.

**File: `src/Modules/Catalog/Application/Products/Create/CreateProductCommand.cs`**

```csharp
using MediatR;

namespace Modules.Catalog.Application.Products.Create;

// The Request (Data payload)
public record CreateProductCommand(string Name, string Description, decimal PriceAmount, string Currency) : IRequest<Guid>;
```

**File: `src/Modules/Catalog/Application/Products/Create/CreateProductCommandHandler.cs`**

```csharp
using MediatR;
using Modules.Catalog.Domain;
using Modules.Catalog.Infrastructure.Data;
using SharedKernel.ValueObjects;

namespace Modules.Catalog.Application.Products.Create;

internal sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly CatalogDbContext _dbContext;

    public CreateProductCommandHandler(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var price = new Money(request.PriceAmount, request.Currency);
        var product = Product.Create(request.Name, request.Description, price);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
```

**Execution:**

```powershell
git add src/Modules/Catalog/Application/Products/Create/
git commit -m "feat(catalog): implement create product command and handler using mediatR"
```

---

## **Commit 3. Implementing a Query (Read Operation)**

Queries never change data. This use case retrieves a product by its ID. It explicitly declares a nullable return type (`?`) to guarantee type-safety if the ID is not found, and uses EF Core's `AsNoTracking()` for optimal read performance.

**File: `src/Modules/Catalog/Application/Products/GetById/GetProductByIdQuery.cs`**

```csharp
using MediatR;

namespace Modules.Catalog.Application.Products.GetById;

// The ? explicitly enforces that the API must handle a potential 'Not Found' (null) response.
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductResponse?>;

public record ProductResponse(Guid Id, string Name, string Description, decimal PriceAmount, string Currency);
```

**File: `src/Modules/Catalog/Application/Products/GetById/GetProductByIdQueryHandler.cs`**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Infrastructure.Data;

namespace Modules.Catalog.Application.Products.GetById;

internal sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductResponse?>
{
    private readonly CatalogDbContext _dbContext;

    public GetProductByIdQueryHandler(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // AsNoTracking bypasses EF Core's change tracker for high-speed reads
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            return null;
        }

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency);
    }
}
```

**Execution:**

```powershell
git add src/Modules/Catalog/Application/Products/GetById/
git commit -m "feat(catalog): implement get product by id query with strict nullable"
```

---

## **Day 9 — The API Edge & Global Exception Handling**

**Objective:** Expose the Modular Monolith to the outside world. Use .NET 8's `IExceptionHandler` to catch domain errors globally and convert them into secure, standardized RFC 7807 Problem Details (JSON). Wire up the API Controller to route incoming HTTP requests directly into MediatR.

Without this, if a user sends a negative price, the `ArgumentException` thrown by the Domain Entity (Day 6) would crash the request and return an unformatted stack trace to the user — a massive security vulnerability in an enterprise environment.

---

## **Commit 1: The Global Exception Handler**

Placed in the `Host` project, which acts as the outer web shell that loads all modules.

**Step 1:** Create folder `src/Host/Middleware/` and add **`GlobalExceptionHandler.cs`**:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Host.Middleware;

// This class intercepts ANY unhandled exception thrown anywhere in the application
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Securely log the full error and stack trace for internal debugging
        _logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        // 2. Map the Exception type to a specific HTTP Status Code
        var statusCode = exception switch
        {
            // If the Domain throws a Guard Clause exception, it's a Bad Request (400)
            ArgumentException => StatusCodes.Status400BadRequest,

            // Otherwise, it's an unhandled Internal Server Error (500)
            _ => StatusCodes.Status500InternalServerError
        };

        // 3. Construct the RFC 7807 standard JSON response
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == 400 ? "Validation Error" : "Server Error",
            Detail = statusCode == 400 ? exception.Message : "An unexpected error occurred.",
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        // 4. Return the secure response to the client
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Return true to tell ASP.NET that we handled the exception and to stop processing
        return true;
    }
}
```

**Step 2:** Commit.

```powershell
git add src/Host/Middleware/GlobalExceptionHandler.cs
git commit -m "feat(host): implement IExceptionHandler to convert domain exceptions to RFC 7807 problem details"
```

---

## **Commit 2: Wiring up the Host (`Program.cs`)**

Tell the `Host` project to activate the Exception Handler and officially register MediatR so it can build its internal handler dictionary.

**Step 1:** Open **`src/Host/Program.cs`** and update it:

```csharp
using Host.Middleware;
using Modules.Catalog.Application.Products.Create;

var builder = WebApplication.CreateBuilder(args);

// --- 1. REGISTER SERVICES (Dependency Injection) ---

builder.Services.AddControllers();

// Register the Global Exception Handler infrastructure
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Register MediatR (This single line scans the Catalog assembly and finds all handlers)
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
    // Note: We will add the Orders assembly here later!
});

var app = builder.Build();

// --- 2. CONFIGURE THE HTTP PIPELINE ---

// Activate the Exception Handler middleware first, so it wraps all incoming requests
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Step 2:** Commit.

```powershell
git add src/Host/Program.cs
git commit -m "chore(host): register global exception handler and MediatR in DI container"
```

---

## **Commit 3: The API Controller (The Entry Point)**

The controller is intentionally lean. Because MediatR handles the flow and the Domain handles the rules, the Controller only does two things: takes the HTTP request and passes it to the `ISender`.

**Step 1:** Create folder `src/Host/Controllers/` and add **`ProductsController.cs`**:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.Catalog.Application.Products.Create;
using Modules.Catalog.Application.Products.GetById;

namespace Host.Controllers;

[ApiController]
[Route("api/catalog/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender) => _sender = sender;

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        // 1. Write Side: Mutate state and get the new ID
        var productId = await _sender.Send(command, ct);

        // 2. Read Side: Re-use the existing Query logic to fetch the full DTO
        // This ensures the response matches our single source of truth for 'ProductResponse'
        var response = await _sender.Send(new GetProductByIdQuery(productId), ct);

        // 3. REST Contract: 201 Created + Location Header + Full Body
        return CreatedAtAction(nameof(GetProduct), new { id = productId }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProductByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
```

**Step 2:** Commit.

```powershell
git add src/Host/Controllers/ProductsController.cs
git commit -m "feat(host): implement products API controller orchestrating MediatR commands and queries and implement orchestrated post-command query dispatch in controller"
```

## **Day 10 — The Self-Healing Database & Schema Segregation**

**Objective:** Connect the application to a real SQL Server Express instance, implement Design-Time Factories to fix the MediatR/EF tooling conflict, generate physical migration files for both module schemas, and wire up an auto-migrator so the database self-configures on every startup.

---

## **Commit 1: SQL Server Express, Design-Time Factories & Migrations**

### **Step 1: The Connection String**

Moved from `(localdb)` to a full SQL Server Express instance.

**File: `src/Host/appsettings.Development.json`**

```json
{
  "ConnectionStrings": {
    "Database": "Server=.\\SQLEXPRESS;Database=EcommerceDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### **Step 2: The Design-Time Factories (The "MediatR Fix")**

Because MediatR validates handlers at startup, `dotnet ef` commands crash. These factories provide a "backdoor" for the EF tools to see the DB config without booting the whole app.

**File: `src/Modules/Catalog/Infrastructure/Data/CatalogDbContextFactory.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Catalog.Infrastructure.Data;

public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=EcommerceDb;Trusted_Connection=True;TrustServerCertificate=True;");
        return new CatalogDbContext(optionsBuilder.Options);
    }
}
```

**File: `src/Modules/Orders/Infrastructure/Data/OrdersDbContextFactory.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Orders.Infrastructure.Data;

public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=EcommerceDb;Trusted_Connection=True;TrustServerCertificate=True;");
        return new OrdersDbContext(optionsBuilder.Options);
    }
}
```

### **Step 3: Generate the Migration Files**

Run these exact commands from the root folder of the project:

```powershell
# Generate Catalog Migration
dotnet ef migrations add InitialCatalog -p src/Modules/Catalog -s src/Host --context CatalogDbContext -o Infrastructure/Data/Migrations

# Generate Orders Migration
dotnet ef migrations add InitialOrders -p src/Modules/Orders -s src/Host --context OrdersDbContext -o Infrastructure/Data/Migrations
```

**Execution:**

```powershell
git add .
git commit -m "feat(infra): switch to SQL Server Express and add design-time context factories with initial migrations"
```

---

## **Commit 2: Auto-Migration on Startup & DbContext Registration**

### **Step 4: The Auto-Migration Extension**

We taught the application how to "heal" itself by pushing pending schema updates to SQL Server every time it starts.

**File: `src/Host/Extensions/MigrationExtensions.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Infrastructure.Data;
using Modules.Orders.Infrastructure.Data;

namespace Host.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var ordersContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        // Applies pending .cs migrations to the physical database
        catalogContext.Database.Migrate();
        ordersContext.Database.Migrate();
    }
}
```

### **Step 5: The Final `Program.cs` Wiring**

Connected the `DbContext`s, MediatR, and the new Auto-Migrator in the main entry point.

**File: `src/Host/Program.cs`**

```csharp
using Host.Extensions;
using Host.Middleware;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Application.Products.Create;
using Modules.Catalog.Infrastructure.Data;
using Modules.Orders.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 2. Database Registration
var connectionString = builder.Configuration.GetConnectionString("Database");
builder.Services.AddDbContext<CatalogDbContext>(opt => opt.UseSqlServer(connectionString));
builder.Services.AddDbContext<OrdersDbContext>(opt => opt.UseSqlServer(connectionString));

// 3. MediatR Orchestration
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
});

var app = builder.Build();

// 4. Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations(); // <--- This triggers the Day 10 magic
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
```

**Execution:**

```powershell
git add .
git commit -m "feat(host): wire auto-migration on startup and register catalog and orders db contexts in program"
```

---

## **Day 10 Final Summary**

| Area | What Was Done |
|---|---|
| **Infrastructure** | Switched to SQL Server Express for real persistence |
| **Decoupling** | Schema Segregation enforced (`catalog` vs `orders` schemas) |
| **Automation** | Application self-configures on startup via `ApplyMigrations` |
| **Tooling** | Verified physical table existence in SSMS using the Trust Server Certificate bypass |

---

## **Day 11 — Automated Data Seeding & DDD Integration**

**Objective:** Populate the `EcommerceDb` with 50 realistic products using the Bogus library, handle Domain-Driven Design (DDD) encapsulation (Value Objects and Factory Methods), and upgrade the startup pipeline to handle asynchronous database operations.

---

## **Commit 1: Bogus Implementation & DDD Seeding Logic**

### **Step 1: Install the Seeding Tool**

Added the Bogus library to the Catalog module to handle high-fidelity data generation.

```powershell
dotnet add src/Modules/Catalog/Modules.Catalog.csproj package Bogus
```

**Execution:**

```powershell
git add .
git commit -m "chore(catalog): install Bogus package for data seeding"
```

## **Commit 2: Implementing Bogus data seeder for products**

### **Step 1: The Catalog Data Seeder**

Implemented the seeder logic. This required using `.CustomInstantiator` to bypass private constructors and correctly handle the `Money` Value Object.

**File: `src/Modules/Catalog/Infrastructure/Data/CatalogDataSeeder.cs`**

```csharp
using Bogus;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Domain.Products;

namespace Modules.Catalog.Infrastructure.Data;

public static class CatalogDataSeeder
{
    public static async Task SeedAsync(CatalogDbContext context)
    {
        // 1. Safety Check: Only seed if the table is empty
        if (await context.Products.AnyAsync()) return;

        // 2. Define Value Object Generation (Money)
        var moneyFaker = new Faker<Money>()
            .CustomInstantiator(f => new Money(
                f.Random.Decimal(10m, 1000m),
                "USD"));

        // 3. Define Entity Generation via Factory Method
        var productFaker = new Faker<Product>()
            .CustomInstantiator(f => Product.Create(
                f.Commerce.ProductName(),
                f.Commerce.ProductDescription(),
                moneyFaker.Generate()
            ));

        var products = productFaker.Generate(50);

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
```

**Execution:**

```powershell
git add .
git commit -m "feat(catalog): implement Bogus data seeder for products"
```

---

## **Commit 3: Making migrations async & Startup Wiring**

### **Step 1: The Async Migration Extension**

Upgraded the auto-migrator to be asynchronous to support `MigrateAsync` and the new `SeedAsync` database I/O.

**File: `src/Host/Extensions/MigrationExtensions.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Infrastructure.Data;
using Modules.Orders.Infrastructure.Data;

namespace Host.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var ordersContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        // Apply schema updates
        await catalogContext.Database.MigrateAsync();
        await ordersContext.Database.MigrateAsync();

        // Populate with initial data
        await CatalogDataSeeder.SeedAsync(catalogContext);
    }
}
```

**Execution:**

```powershell
git add .
git commit -m "refactor(host): make migrations async and wire up catalog seeder"
```

## **Commit 4: Fixing the namespace of Product.cs from Modules.Catalog.Domain to Modules.Catalog.Domain.Products**

### **Step 1: Final `Program.cs` Async Update**

Modified the entry point to `await` the migration and seeding process before starting the web server.

**File: `src/Host/Program.cs`**

```csharp
// ... (Service registrations)

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Await the async migration and seeding chain
    await app.ApplyMigrationsAsync();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
```

**Execution:**

```powershell
git add .
git commit -m "fix(host): await async database migrations and seeding on startup and fixed the namespace of Product.cs from Modules.Catalog.Domain to Modules.Catalog.Domain.Products
"
```

---

## **Day 11 Final Summary**

| Area | What Was Done |
|---|---|
| **Data Generation** | Integrated Bogus for 50 realistic product entries |
| **DDD Compliance** | Bypassed private constructors using `.CustomInstantiator` |
| **Pipeline** | Refactored auto-migrator to `async Task` for better DB handling |
| **Verification** | Confirmed 50 rows in `catalog.Products` via SSMS |

---

## **Day 12 — The "Reading Room" & DDD Refactoring**

**Objective:** Build a dedicated read pipeline with pagination and search support, restructure the Application layer into a clean `Get` namespace, and shift validation responsibility down into the `Money` Value Object to enforce pure DDD principles.

---

## **Commit 1: Read Pipeline Architecture & Bulk Fetching**

**Files changed:**
- `src/Modules/Catalog/Application/Products/Get/PagedResult.cs` — Created
- `src/Modules/Catalog/Application/Products/Get/GetProducts/GetProductsQuery.cs` — Created
- `src/Modules/Catalog/Application/Products/Get/GetProducts/GetProductsQueryHandler.cs` — Created
- `src/Modules/Catalog/Application/Products/Get/GetById/GetProductByIdQuery.cs` — Moved
- `src/Modules/Catalog/Application/Products/Get/GetById/GetProductByIdQueryHandler.cs` — Moved

Restructured the Application layer to group all Read operations into a dedicated `Get` namespace. Created a generic `PagedResult<T>` DTO for bulk payloads. Built a unified `GetProductsQuery` pipeline utilizing `.AsNoTracking()` to handle filterable, paginated data fetching when a specific ID is not provided.

### **Step 1: `PagedResult.cs`**

**File: `src/Modules/Catalog/Application/Products/Get/PagedResult.cs`**

```csharp
namespace Modules.Catalog.Application.Products.Get;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
```

### **Step 2: `GetProductsQuery.cs`**

**File: `src/Modules/Catalog/Application/Products/Get/GetProducts/GetProductsQuery.cs`**

```csharp
using MediatR;
using Modules.Catalog.Application.Products.Get;

namespace Modules.Catalog.Application.Products.Get.GetProducts;

public record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null
) : IRequest<PagedResult<ProductResponse>>;
```

### **Step 3: `GetProductsQueryHandler.cs`**

**File: `src/Modules/Catalog/Application/Products/Get/GetProducts/GetProductsQueryHandler.cs`**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Infrastructure.Data;
using Modules.Catalog.Application.Products.Get;

namespace Modules.Catalog.Application.Products.Get.GetProducts;

internal sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductResponse>>
{
    private readonly CatalogDbContext _dbContext;

    public GetProductsQueryHandler(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProductResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p => p.Name.Contains(request.SearchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderBy(p => p.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductResponse(
                p.Id, p.Name, p.Description, p.Price.Amount, p.Price.Currency))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>(products, totalCount, request.PageNumber, request.PageSize);
    }
}
```

**Execution:**

```powershell
git add .
git commit -m "refactor(Get): shifted GetproductByIdQuery.cs & GetproductByIdQueryHandler.cs inside Catalog\Application\Products\Get\GetById && feat(Get): created and configured PagedResult.cs as a DTO && feaT(get): configured GetProductsQuery.cs and GetProductsQueryHandler.cs inside Get\GetProducts for fetching products when an id is not specified"
```

---

## **Commit 2: Value Object Validation (Enforcing Pure DDD)**

**Files changed:**
- `src/SharedKernel/ValueObjects/Money.cs`
- `src/Modules/Catalog/Domain/Products/Product.cs`

Shifted currency and price validation logic out of the `Product` aggregate root and down into the `Money` Value Object. `Product` now strictly manages its own state and delegates price integrity to `Money`, adhering to pure DDD principles.

### **Step 1: `Money.cs`**

**File: `src/SharedKernel/ValueObjects/Money.cs`**

```csharp
namespace SharedKernel.ValueObjects;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be exactly 3 characters.");

        Amount = amount;
        Currency = currency.ToUpper();
    }

    public static Money Zero(string currency = "USD") => new(0, currency);
}
```

### **Step 2: `Product.cs`**

**File: `src/Modules/Catalog/Domain/Products/Product.cs`**

```csharp
using SharedKernel.ValueObjects;

namespace Modules.Catalog.Domain.Products;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero();

    private Product() { }

    public static Product Create(string name, string description, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be empty");

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price
        };
    }
}
```

**Execution:**

```powershell
git add .
git commit -m "refactor(Money): Money now handles logic for invalid currency and price and product only handles it's concerned data, this approach enforces DDD"
```
---


## **Day 13 — Zero-Touch Modular Expansion (Orders Module)**

**Objective:** Expand the Orders module into a fully self-contained vertical slice — enabling it to handle Web API concerns, register its own internal dependencies, and expose its own controller — without modifying the Host's core responsibilities.

---

## **Commit 1: Infrastructure & Framework Alignment**

The goal was to enable the Orders module to handle Web API concerns and register its own internal dependencies.

### **Step 1: Framework Reference**

**File: `src/Modules/Orders/Modules.Orders.csproj`**

Added the ASP.NET Core framework reference to unlock Web API types inside the module:

```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

### **Step 2: Module Extension**

**File: `src/Modules/Orders/Infrastructure/OrdersModuleExtensions.cs`**

```csharp
public static IServiceCollection AddOrdersModule(this IServiceCollection services, string connectionString)
{
    services.AddDbContext<OrdersDbContext>(opt => opt.UseSqlServer(connectionString));
    services.AddScoped<IModuleDatabase, OrdersModuleDatabase>();
    services.AddMediatR(config =>
        config.RegisterServicesFromAssembly(typeof(OrdersModuleExtensions).Assembly));
    return services;
}
```

**Execution:**

```powershell
git add .
git commit -m "feat(orders): add ASP.NET Core ref and MediatR registration"
```

---

## **Commit 2: Application & Presentation Encapsulation**

The goal was to move the Orders logic and its entry point into the module, removing the Host's responsibility for these files.

### **Step 3: Create Order Command**

**File: `src/Modules/Orders/Application/Create/CreateOrderCommand.cs`**

```csharp
using MediatR;

namespace Modules.Orders.Application.Create;

public record CreateOrderCommand(Guid CustomerId) : IRequest<Guid>;
```

### **Step 4: Create Order Command Handler**

**File: `src/Modules/Orders/Application/Create/CreateOrderCommandHandler.cs`**

```csharp
using MediatR;

namespace Modules.Orders.Application.Create;

internal sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    public Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct) =>
        Task.FromResult(Guid.NewGuid());
}
```

### **Step 5: Orders Controller**

**File: `src/Modules/Orders/Presentation/OrdersController.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.Orders.Application.Create;

namespace Modules.Orders.Presentation;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var orderId = await sender.Send(command);
        return Created($"/api/orders/{orderId}", new { Id = orderId });
    }
}
```

**Execution:**

```powershell
git add .
git commit -m "feat(orders): implement initial command and API controller

- Created CreateOrderCommand and dedicated handler in separate files.
- Implemented OrdersController with POST endpoint to trigger MediatR.
- Established Presentation-to-Application flow within module boundary."
```
---
## **Day 14 — Shared Infrastructure, Messaging Contracts & Cross-Module Orchestration**

**Objective:** Establish foundational contracts and cross-module shared logic, fulfill the reservation contract within the Catalog module, and connect the Orders module to the synchronous stock reservation flow.

---

## **Phase 1: Shared Infrastructure & Messaging Contracts**

**Goal:** Establish the foundational contracts and cross-module shared logic while reorganizing the core architecture.

### **Step 1: Integration Event Contracts**

Introduced base integration event contracts into `BuildingBlocks` to facilitate decoupled asynchronous messaging.

```powershell
git commit -m "feat(building-blocks): introduce integration event contracts"
```

### **Step 2: Architectural Refactor — Database Directory**

Shifted the `Database` directory from `BuildingBlocks` to `SharedKernel` to better reflect ownership and improve cohesion.

```powershell
git commit -m "refactor(Database): shifted Database directory from BuildingBlocks to SharedKernel"
```

### **Step 3: Stock Reservation Contract**

Defined `IStockReservationContract` within `SharedKernel` to establish the rules for synchronous, cross-module stock reservations.

```powershell
git commit -m "feat(sharedkernel): Introduced IStockReservationContract to facilitate decoupled, synchronous stock reservations."
```

---

## **Phase 2: Catalog Module Reservation Logic & Namespace Fixes**

**Goal:** Fulfill the reservation contract within the Catalog module and clean up minor infrastructure routing issues.

### **Step 4: Namespace Fix — Orders Infrastructure**

Resolved a namespace discrepancy within the Orders module's infrastructure (`OrdersModuleDatabase.cs` and `OrdersModuleExtensions.cs`).

```powershell
git commit -m "fix(orders/infrastructure/data)): fixed the namespaces of OrdersModuleDatabase.cs and OrdersModuleExtensions.cs"
```

### **Step 5: Reservation Pattern & Event Handlers**

Implemented the core reservation pattern (Available/Reserved states) and configured event handlers inside the Catalog module.

```powershell
git commit -m "feat(catalog): implement reservation pattern and event handlers"
```

### **Step 6: DI Container Wiring**

Wired up the DI container in `CatalogModuleExtension.cs` by mapping the new `IStockReservationContract` to the `CatalogReservationService`.

```powershell
git commit -m "refactor(CatalogModuleExtension.cs): added dependency registration..."
```

---

## **Phase 3: Orders Orchestration & Day 15 Prep**

**Goal:** Connect the Orders module to the synchronous reservation flow and set up the next steps.

### **Step 7: Orders Orchestration Pipeline**

Updated the order creation pipeline in the Orders module to orchestrate the distributed transaction in sequence:

1. Reserve Stock
2. Create Order
3. Confirm
4. Publish Event

```powershell
git commit -m "feat(orders): orchestrate order creation with stock reservation"
```

### **Step 8: Day 15 To-Dos**

Documented pending architecture tasks and To-Dos directly in code comments to ensure a smooth transition into Day 15.

```powershell
git commit -m "adding a comment to ensure the 'To dos' for day 15"
```
---
## **Day 15 — Distributed Transactions, Bulk Reservations & Compensating Logic**

**Objective:** Harden the order creation pipeline with multi-item bulk stock reservations, strict validation, compensating rollback transactions for partial failures, and item-level reservation tracking across the Orders and Catalog modules.

---

## **Commit 1: Order Aggregate Root Hardening**

**File: `src/Modules/Orders/Domain/Order.cs`**

Updated the Order Aggregate Root. Added a `List<Guid> ReservationIds` to tie the order to specific inventory locks, added a `LogMessage` string for internal auditing, and built the `MarkAsFailed(reason)` method to enable soft-failures instead of hard-deleting records.

```powershell
git commit -m "refactor(Order.cs): added ReservationId and LogMessage fields, MarkAsFailed method to Order.cs"
```

---

## **Commit 2: EF Core Schema Alignment**

**File: `src/Modules/Orders/Infrastructure/Data/OrdersDbContext.cs`**

Updated the EF Core configuration to match the domain changes. Explicitly mapped the `ReservationId` property so SQL Server enforces it as a required column when writing to the `orders.Orders` table.

```powershell
git commit -m "refactor(OrdersDbContext.cs): added ReservationId to OrdersDbcontext.cs as a required field"
```

---

## **Commit 3: Compensating Transaction for Partial Failures**

**File: `src/Modules/Orders/Application/Create/CreateOrderCommandHandler.cs`**

Built the rollback safety net. Added the `catch` block that triggers `ReleaseReservationsAsync` using `CancellationToken.None` if the database fails to save, ensuring the system doesn't leave permanent "zombie" inventory locks.

```powershell
git commit -m "fix(orders): add compensating transaction for partial failures"
```

---

## **Commit 4: Bulk Stock Reservation Engine**

**Files:**
- `src/Modules/Catalog/Infrastructure/Contracts/CatalogReservationService.cs`
- `src/SharedKernel/Contracts/IStockReservationContract.cs`

Re-engineered the Catalog's reservation engine. Updated `ReserveStockStrictAsync` to accept a list of items, iterate through them to check `AvailableQty`, apply temporary locks, and return a comprehensive `BulkReservationResponse`.

```powershell
git commit -m "Refactor stock reservation to support bulk operations"
```

---

## **Commit 5: StockItem & Reservation DB Mapping**

**File: `src/Modules/Catalog/Infrastructure/Data/CatalogDbContext.cs`**

Bridged the gap between the Catalog domain and the physical database. Added the `DbSet` properties for `StockItems` and `Reservations` and configured their column constraints so EF Core knows how to build the tables.

```powershell
git commit -m "Add StockItem and Reservation mapping in CatalogDbContext"
```

---

## **Commit 6: Strict Multi-Item Order Orchestration & Rollback**

**File: `src/Modules/Orders/Application/Create/CreateOrderCommandHandler.cs`**

Rewrote the primary orchestration loop. The handler now evaluates the `AllReserved` flag — if even one item is missing, it actively halts the order, rolls back any successful partial locks, and throws a validation exception.

```powershell
git commit -m "Implement strict multi-item order creation with stock validation and rollback handling"
```

---

## **Commit 7: Multi-Item Command Contract**

**File: `src/Modules/Orders/Application/Create/CreateOrderCommand.cs`**

Updated the MediatR request contract. Replaced the single `ItemId` parameter with a `List<OrderItemRequest>`, enabling the API to accept multi-item shopping carts in a single payload.

```powershell
git commit -m "feat(ordering): support bulk stock reservation in order creation flow"
```

---

## **Commit 8: Stock Validation Exceptions**

**Files:**
- `src/SharedKernel/Exceptions/StockRejection.cs`
- `src/SharedKernel/Exceptions/StockValidationException.cs`

Created the strict validation mechanisms. Defined the `StockRejection` record to hold missing item details and the `StockValidationException` to carry that list up to the API layer so the frontend can read it.

```powershell
git commit -m "feat(Exceptions): added StockRejection.cs and StockValidationException.cs for strict control of Order"
```

---

## **Commit 9: Reservation Contract — Item-Reservation Pairs**

**File: `src/SharedKernel/Contracts/IStockReservationContract.cs`**

Replaced the flat list of GUIDs in `BulkReservationResponse` with a new `ReservationResult` record. This ensures the Catalog module can explicitly pair a `ProductId` with its newly generated `ReservationId`.

```powershell
git commit -m "feat(sharedkernel): update IStockReservationContract.cs to return item-reservation pairs"
```

---

## **Commit 10: CatalogReservationService — Paired Results**

**File: `src/Modules/Catalog/Infrastructure/Contracts/CatalogReservationService.cs`**

Updated the `ReserveStockStrictAsync` method to fulfill the new contract. When stock is successfully locked, it now constructs and returns the paired `ReservationResult` objects.

```powershell
git commit -m "refactor(catalog): adapt CatalogReservationService.cs to return paired reservation results"
```

---

## **Commit 11: Reservation Tracking Shifted to OrderItem**

**Files:**
- `src/Modules/Orders/Domain/Order.cs`
- `src/Modules/Orders/Domain/OrderItem.cs`

Stripped the `List<Guid> ReservationIds` property out of the `Order` aggregate root entirely. Added `ReservationId` as a core property to the `OrderItem` entity and updated its constructor and guard clauses to require it.

```powershell
git commit -m "refactor(order-domain): shift reservation tracking from order aggregate to order item"
```

---

## **Commit 12: OrdersDbContext — Corrected Schema Mapping**

**File: `src/Modules/Orders/Infrastructure/Data/OrdersDbContext.cs`**

Deleted the broken scalar `builder.Property(o => o.ReservationId)` mapping from the `Order` configuration. Added the correct `ReservationId` mapping to the `OrderItem` configuration to physically enforce the relational schema.

```powershell
git commit -m "fix(OrdersDbContext.cs): map reservation id to order items and removed reservation id's invalid aggregate mapping to order"
```

---

## **Commit 13: Item-Level Reservation Orchestration**

**File: `src/Modules/Orders/Application/Create/CreateOrderCommandHandler.cs`**

Modified the checkout orchestration. It now iterates through the cart, finds the specific `ReservationId` matched to the current item's `ProductId` from the Catalog response, and passes it into the `Order.AddItem()` domain method.

```powershell
git commit -m "refactor(CreateOrderCommandHandler.cs): orchestrate item-level reservations in order creation pipeline"
```
---
> Run `dotnet build` from the root directory. The full vertical slice for the Catalog module is now complete:
> **API Controller** → **MediatR** → **Domain (business rules)** → **EF Core (isolated schema)**
> If anything fails, the Global Exception Handler safely catches it and returns a clean `400 Bad Request`.

---

## **Getting Started**

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

# Deploy network infrastructure (requires AWS CLI configured)
aws cloudformation deploy `
  --template-file infra/vpc.yaml `
  --stack-name eCommerce-Network-Stack `
  --region us-east-1
```