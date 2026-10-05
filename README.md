# 📊 InsightFlow

> A full-stack business information and performance management application built with .NET MAUI, ASP.NET Core Web API, Entity Framework Core, and MySQL.

## 📖 About InsightFlow

**InsightFlow** is a business information and performance management application designed to help organisations capture, organise, monitor, and analyse operational information through a centralised system.

The application brings together information such as employees, departments, customers, products and services, sales, daily operational records, business value, and performance data.

InsightFlow transforms this information into dashboards, reports, charts, performance information, and forecasting functionality that can support organisational monitoring and decision-making.

---

## ✨ Main Features

- Secure employee authentication
- Administrator, Manager, and Employee roles
- Employee account management
- Employee activation and deactivation
- Department management
- Daily operational record capture
- Customer management
- Product and service management
- Sales management
- Company-wide dashboard
- Employee and department performance monitoring
- Reports and analytics
- Bar, pie, and line charts
- Business value trend analysis
- PDF report generation
- Forecasting functionality
- Historical record preservation

---

# 🔐 Authentication

Users log into InsightFlow using three credentials:

```text
Company Email
      +
Employee ID
      +
Password
```

After successful authentication, the ASP.NET Core API generates a JWT used to access protected functionality.

Passwords are hashed using ASP.NET Core `PasswordHasher<Employee>` and are not stored as readable plain-text passwords.

### Login Interface

![InsightFlow Login](docs/screenshots/Login_png.png)

---

# 👥 User Roles

InsightFlow supports three main roles.

## 👨‍💼 Administrator

The **Administrator has company-wide oversight of InsightFlow**.

Administrator functionality includes access to areas such as:

- Company-wide dashboards
- Employee Management
- Employee account creation
- Employee activation and deactivation
- Password resets
- Role assignment
- Department information
- Customer information
- Products and services
- Sales information
- Company-wide reports
- Employee performance
- Department performance
- Business analytics
- Overall organisational performance

The Administrator can create Manager and Employee accounts through **Employee Management**.

### Administrator Dashboard

The Administrator Dashboard provides a company-wide overview of organisational activity and performance.

![InsightFlow Administrator Dashboard](docs/screenshots/AdminDashboard.png)

### Employee Management

Administrators can manage employee accounts, roles, departments, and account status through the Employee Management interface.

![InsightFlow Employee Management](docs/screenshots/Employee%20Management.png)

## 👔 Manager

Managers have management-level access according to the functionality and authorization available within InsightFlow.

Manager functionality can include:

- Management dashboards
- Performance information
- Reports
- Operational information
- Business activity information

## 👨‍💻 Employee

Employees use employee-level functionality such as:

- Employee dashboard
- Daily operational record capture
- Viewing permitted information
- Employee-specific functionality

---

# 📈 Reports & Business Analytics

InsightFlow converts operational records into useful business information for monitoring and decision-making.

Reporting and analytics functionality includes:

- Company-wide reporting
- Department performance analysis
- Employee performance analysis
- Business value analysis
- Record and activity analysis
- Bar charts
- Pie charts
- Line charts
- Business value trends
- Filtering and reporting
- PDF report generation

### Report Overview

![InsightFlow Report](docs/screenshots/previous_report_test.png)

### Report Analytics

![InsightFlow Report Analytics](docs/screenshots/Previous_report_testing.png)

---

# 🪪 Employee IDs

InsightFlow uses structured Employee IDs based on organisational departments.

Examples:

```text
IF-MGT-0001    Management
IF-FIN-0001    Finance
IF-IT-0001     Information Technology
IF-HR-0001     Human Resources
IF-SAL-0001    Sales
```

For example:

```text
IF-MGT-0001
│   │    │
│   │    └── Employee number
│   └─────── Department code
└─────────── InsightFlow
```

---

# 🔑 First-Time Administrator Login

InsightFlow does **not** publish or hard-code an Administrator password.

When InsightFlow is started with a new database, the application creates the initial Administrator account using a password supplied securely by the person installing the application.

The initial Administrator account is:

```text
Company Email:
admin@insightflow.co.za

Employee ID:
IF-MGT-0001

Password:
The value configured locally as SeedAdmin:Password
```

The installer chooses their own password.

For example:

```bash
dotnet user-secrets set "SeedAdmin:Password" "YOUR_SECURE_INITIAL_ADMIN_PASSWORD"
```

`YOUR_SECURE_INITIAL_ADMIN_PASSWORD` is only a placeholder.

Do **not** enter that text literally. Replace it with your own secure password.

The password is stored in local ASP.NET Core User Secrets and is not committed to GitHub.

---

# 🚀 First-Time Setup

When a developer, lecturer, or potential employer clones InsightFlow, the setup process is:

```text
Clone Repository
       ↓
Configure MySQL
       ↓
Configure User Secrets
       ↓
Start InsightFlow.API
       ↓
Entity Framework Applies Migrations
       ↓
Departments Are Created
       ↓
Initial Administrator Is Created
       ↓
Login as Administrator
       ↓
Create Manager / Employee Accounts
       ↓
Test InsightFlow
```

---

# 🔒 Configure User Secrets

Navigate to the API project:

```bash
cd InsightFlow.API
```

Configure your MySQL connection string:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_MYSQL_CONNECTION_STRING"
```

Configure your JWT signing key:

```bash
dotnet user-secrets set "Jwt:Key" "YOUR_SECURE_JWT_SIGNING_KEY"
```

Configure your initial Administrator password:

```bash
dotnet user-secrets set "SeedAdmin:Password" "YOUR_SECURE_INITIAL_ADMIN_PASSWORD"
```

The private configuration has this structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_MYSQL_CONNECTION_STRING"
  },
  "Jwt": {
    "Key": "YOUR_SECURE_JWT_SIGNING_KEY"
  },
  "SeedAdmin": {
    "Password": "YOUR_SECURE_INITIAL_ADMIN_PASSWORD"
  }
}
```

> ⚠️ **Never commit real values for these settings to GitHub.**

---

# 👨‍💼 Logging In for the First Time

After starting `InsightFlow.API` with a new database, log in using:

```text
Company Email:
admin@insightflow.co.za

Employee ID:
IF-MGT-0001

Password:
The password YOU configured as SeedAdmin:Password
```

Once the Administrator exists, changing `SeedAdmin:Password` does **not** change that existing Administrator's password.

The seed password is used only when the initial Administrator needs to be created.

---

# 👥 Creating Manager and Employee Accounts

After the first Administrator login:

```text
Administrator
      ↓
Employee Management
      ↓
Create Employee
      ↓
Enter Employee Details
      ↓
Select Department
      ↓
Assign Role
 ├── Manager
 └── Employee
      ↓
Account Created
```

The newly created account can then use its own:

```text
Company Email
Employee ID
Password
```

to log into InsightFlow.

---

# 🔄 Employee Account Lifecycle

InsightFlow preserves historical information when an employee leaves the organisation.

Instead of deleting the employee:

```text
Active Employee
      ↓
Administrator Deactivates Account
      ↓
Employee Cannot Log In
      ↓
Historical Records Remain
      ↓
Reports and Analytics Remain Accurate
```

Inactive accounts can be reactivated when appropriate.

This approach helps preserve historical business information while preventing inactive employees from accessing the system.

---

# 🧠 Architecture

```text
┌──────────────────────────────┐
│       .NET MAUI Client       │
│          C# + XAML           │
└──────────────┬───────────────┘
               │
          HTTPS / JSON
               │
               ▼
┌──────────────────────────────┐
│   ASP.NET Core Web API       │
│                              │
│ Authentication               │
│ Authorization                │
│ Business Logic               │
│ Reporting                    │
│ Data Processing              │
└──────────────┬───────────────┘
               │
       Entity Framework Core
               │
               ▼
┌──────────────────────────────┐
│            MySQL             │
│                              │
│ Employees                    │
│ Departments                  │
│ Customers                    │
│ Products                     │
│ Sales                        │
│ Daily Records                │
└──────────────────────────────┘
```

---

# 🛠️ Technology Stack

| Area | Technology |
|---|---|
| Language | C# |
| Framework | .NET 8 |
| Frontend | .NET MAUI |
| UI | XAML |
| Backend | ASP.NET Core Web API |
| Database | MySQL |
| ORM | Entity Framework Core |
| MySQL Provider | Pomelo |
| Authentication | JWT |
| Password Security | ASP.NET Core PasswordHasher |
| Secure Client Storage | .NET MAUI SecureStorage |
| API | REST / JSON |
| API Testing | Swagger / OpenAPI |
| PDF Reporting | QuestPDF |
| Version Control | Git |
| Repository | GitHub |
| IDE | Visual Studio |

---

# ▶️ Running the Application

## 1. Requirements

Install:

- .NET 8 SDK
- Visual Studio
- .NET MAUI workload
- ASP.NET Core development tools
- MySQL Server
- Git

## 2. Configure MySQL

Create an empty MySQL database for InsightFlow.

Configure its connection string through User Secrets.

## 3. Configure Private Settings

Configure:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
SeedAdmin:Password
```

using ASP.NET Core User Secrets.

## 4. Start the API

Run:

```text
InsightFlow.API
```

The API applies the configured Entity Framework migrations and prepares the database.

## 5. Login as Administrator

Use:

```text
Email:       admin@insightflow.co.za
Employee ID: IF-MGT-0001
Password:    Your SeedAdmin:Password value
```

for the initial login on a new database.

## 6. Start the Client

Run:

```text
InsightFlow
```

as the .NET MAUI project.

> The current client configuration is intended for local development. Deployment to hosted environments or physical mobile devices requires the API endpoint to be configured appropriately.

---

# 🔒 Security

InsightFlow implements security practices including:

- Password hashing
- JWT authentication
- Role-based authorization
- Protected API endpoints
- Secure client-side token storage
- Employee account activation/deactivation
- Private database credentials
- Private JWT signing keys
- Private initial Administrator password
- Historical record preservation

The GitHub repository contains:

```text
Source Code                         ✅
Database Models                     ✅
Controllers                         ✅
Services                            ✅
Migrations                          ✅
Database Seeder                     ✅
Non-sensitive Configuration         ✅

Database Password                   ❌
JWT Signing Key                     ❌
SeedAdmin:Password actual value     ❌
Employee Passwords                  ❌
```

---

# 📁 Repository Structure

```text
InsightFlow/
│
├── InsightFlow/
│   └── .NET MAUI Client
│
├── InsightFlow.API/
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Migrations/
│   └── Services/
│
├── docs/
│   └── screenshots/
│       ├── Login_png.png
│       ├── AdminDashboard.png
│       ├── Employee Management.png
│       ├── previous_report_test.png
│       └── Previous_report_testing.png
│
├── InsightFlow.sln
├── .gitignore
├── .gitattributes
└── README.md
```

---

# 🔮 Future Improvements

- [ ] Hosted production API
- [ ] Cloud database
- [ ] Android deployment
- [ ] iOS deployment
- [ ] Enhanced password recovery
- [ ] Email notifications
- [ ] Additional analytics
- [ ] More advanced forecasting
- [ ] Automated testing
- [ ] CI/CD pipeline
- [ ] Production deployment

---

# ⚠️ Project Notice

InsightFlow is primarily a **learning and portfolio project**.

It demonstrates full-stack development using C#, .NET MAUI, ASP.NET Core, MySQL, Entity Framework Core, JWT authentication, reporting, analytics, and software security practices.

---

# 👨‍💻 Developer

**Martin Hleketani**

BSc Information Technology Student  
North-West University

InsightFlow forms part of my software development portfolio and demonstrates practical experience in:

- Full-stack software development
- C# and .NET development
- REST API development
- Relational database design
- Authentication and authorization
- Business information systems
- Data analysis and reporting
- System design

---

# 📄 License

This project is currently maintained as a development and portfolio project.

All rights reserved.