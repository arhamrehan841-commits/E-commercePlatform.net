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

### **Day 1 — Workspace & Solution Setup**

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

### **Day 2 — Enterprise Repository & GitOps Standardization**

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

### **Day 3 — Infrastructure as Code (IaC) Bootstrapping**

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


### **Day 4 — CI/CD Pipeline Scaffolding**

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

### **Day 5 — Stateful Infrastructure: RDS SQL Server & Secrets Management**

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

### **Day 6 — Internal Module Architecture (The Domain Layer)**

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