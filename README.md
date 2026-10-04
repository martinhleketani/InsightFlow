# 📊 InsightFlow

> A business information and performance management application designed to transform operational data into meaningful reports, analytics, and decision-support information.

---

## 📖 About InsightFlow

**InsightFlow** is a business management and analytics application developed to help organisations capture, organise, monitor, and analyse operational information through a centralised system.

The application brings together important business information such as employees, departments, customers, products, sales, daily records, and performance data.

Instead of relying on disconnected records and manual analysis, InsightFlow provides a structured environment for managing business information and presenting that information through dashboards, reports, performance monitoring, and forecasting functionality.

InsightFlow was developed as a practical software development project and forms part of my growing portfolio of C# and .NET applications.

---

## 🎯 Aim of the Project

The main aim of **InsightFlow** is to provide a centralised business information system that helps organisations transform operational data into meaningful information that can support monitoring, analysis, and decision-making.

The system aims to make it easier to:

- Capture and manage business information
- Maintain employee and organisational records
- Record daily operational data
- Monitor employee and business performance
- Manage customer and product information
- Analyse sales information
- Generate useful reports
- Identify patterns and trends
- Support forecasting
- Present important information through dashboards

The broader goal is to demonstrate how software can transform everyday operational data into useful information that managers and employees can use to better understand organisational performance.

---

## ✨ Core Features

### 🔐 Authentication and User Access

InsightFlow includes an authentication system for controlling access to the application.

The authentication system supports:

- Employee login
- JWT-based authentication
- Password hashing
- Role-based access
- Administrator accounts
- Employee accounts
- Account status checking
- Protected API endpoints

Users authenticate using:

```text
Company Email
+
Employee ID
+
Password
```

After successful authentication, the API generates a JWT that can be used when accessing protected functionality.

---

### 👥 Employee Management

InsightFlow provides functionality for managing employee-related information.

This includes:

- Employee records
- Employee accounts
- Employee IDs
- Employee names
- Company email addresses
- Job titles
- Department allocation
- Employee roles
- Employee account status
- Employee performance information

This allows important workforce information to be maintained within a central system.

---

### 🏢 Department Management

Employees can be associated with organisational departments.

The initial database seeding process creates departments including:

- Sales
- Finance
- Human Resources
- Information Technology
- Marketing
- Operations
- Customer Service
- Procurement
- Management

Each department has its own department code.

Examples include:

```text
IF-SAL
IF-FIN
IF-HR
IF-IT
IF-MKT
IF-OPS
IF-CS
IF-PRC
IF-MGT
```

Department information helps organise employees and operational information according to different areas of the organisation.

---

### 📝 Daily Records

InsightFlow allows operational information to be captured through daily records.

These records provide structured historical information that can contribute to:

- Activity monitoring
- Performance analysis
- Historical comparisons
- Reports
- Dashboards
- Management insights

---

### 📦 Product Management

The system contains functionality for maintaining product information.

Product information can contribute to operational management, sales records, reporting, and business analysis.

---

### 🤝 Customer Management

InsightFlow provides functionality for maintaining customer information.

Customer records form part of the organisation's operational information and can be connected to other business activities within the system.

---

### 💰 Sales Management

Sales information can be captured and managed within InsightFlow.

Sales information can contribute to:

- Business reports
- Performance analysis
- Historical comparisons
- Dashboards
- Forecasting
- Management decision support

---

### 📊 Dashboard

InsightFlow includes dashboard functionality for presenting important business information in an understandable format.

Instead of requiring users to manually examine individual database records, dashboards provide a higher-level view of business activity and performance.

---

### 📈 Employee Performance

The system includes functionality for monitoring and presenting employee performance information.

Performance information can help identify:

- Strong performance
- Areas requiring attention
- Performance trends
- Changes over time
- Employee activity

This allows operational information to contribute to a clearer understanding of individual and organisational performance.

---

### 🔮 Forecasting

InsightFlow includes forecasting functionality to explore how historical business information can be used to estimate future performance.

This extends the application beyond simple record keeping by introducing analytical and decision-support functionality.

```text
Historical Data
      ↓
Data Analysis
      ↓
Patterns & Trends
      ↓
Forecast
      ↓
Decision Support
```

---

### 📑 Reports

InsightFlow includes reporting functionality for presenting stored business information in a structured format.

Reports can assist users in understanding:

- Historical activity
- Sales information
- Employee performance
- Operational information
- Business trends

The project also includes functionality related to PDF report generation.

---

### 📥 Data Import

The application includes data import functionality to support bringing existing information into the system rather than requiring all information to be entered manually.

This can simplify the process of populating the system with existing business data.

---

## 🧠 System Architecture

InsightFlow follows a client-server architecture.

```text
┌────────────────────────────────┐
│          InsightFlow           │
│          .NET MAUI             │
│                                │
│        XAML Interface          │
└───────────────┬────────────────┘
                │
                │ HTTP / JSON
                ▼
┌────────────────────────────────┐
│       InsightFlow.API          │
│     ASP.NET Core Web API       │
│                                │
│ Authentication                 │
│ Business Logic                 │
│ Data Processing                │
│ Reporting                      │
└───────────────┬────────────────┘
                │
                │ Entity Framework Core
                ▼
┌────────────────────────────────┐
│         MySQL Database         │
│                                │
│ Employees                      │
│ Departments                    │
│ Customers                      │
│ Products                       │
│ Sales                          │
│ Daily Records                  │
│ Performance Data               │
└────────────────────────────────┘
```

The **.NET MAUI application** provides the user interface.

The **ASP.NET Core Web API** handles authentication, business logic, data processing, reporting, and communication with the database.

**Entity Framework Core** provides the data access layer between the API and the database.

The **MySQL database** stores the application's operational information.

---

## 🛠️ Technology Stack

| Area | Technology |
|---|---|
| Programming Language | C# |
| Application Framework | .NET |
| Frontend | .NET MAUI |
| User Interface | XAML |
| Backend | ASP.NET Core Web API |
| Database | MySQL |
| ORM | Entity Framework Core |
| Authentication | JWT |
| Password Security | ASP.NET Core PasswordHasher |
| API Architecture | REST |
| Data Format | JSON |
| API Testing | Swagger / OpenAPI |
| Reporting | PDF Reporting |
| Version Control | Git |
| Repository Hosting | GitHub |
| Development Environment | Visual Studio |

---

# 🧪 Demo Accounts

InsightFlow automatically creates demonstration accounts when a new database is initialised.

These accounts allow developers, lecturers, recruiters, and other reviewers to test the application from both an **Administrator** and **Employee** perspective.

---

## 👨‍💼 Administrator Demo Account

| Field | Value |
|---|---|
| Role | Administrator |
| Company Email | `admin@insightflow.co.za` |
| Employee ID | `IF-MGT-0001` |
| Password | `InsightAdmin123!` |
| Department | Management |
| Job Title | System Administrator |

The Administrator account is intended to demonstrate management and administrative functionality within InsightFlow.

Depending on the available functionality, the administrator can interact with areas such as:

- Employee management
- Department information
- Customer information
- Product information
- Sales information
- Employee performance
- Reports
- Dashboards
- Business analytics

---

## 👨‍💻 Employee Demo Account

| Field | Value |
|---|---|
| Role | Employee |
| Company Email | `employee@insightflow.co.za` |
| Employee ID | `IF-IT-0001` |
| Password | `InsightEmployee123!` |
| Department | Information Technology |
| Job Title | IT Employee |

The Employee account allows InsightFlow to be tested from the perspective of a standard employee.

This allows reviewers to experience the functionality and permissions available to an employee rather than an administrator.

> **Important:** These accounts are demonstration accounts intended for local development, testing, and portfolio evaluation only. They must not be used as production credentials.

---

## 🔐 How Demo Login Works

InsightFlow requires three pieces of information when a user signs in:

```text
Company Email
      +
Employee ID
      +
Password
      ↓
Authentication API
      ↓
Find Employee
      ↓
Check Account Status
      ↓
Verify Password Hash
      ↓
Generate JWT
      ↓
Authenticated User
```

The authentication API first searches for an employee using both the:

```text
Company Email
Employee ID
```

If the employee exists, the system checks whether the employee account is active.

The submitted password is then verified against the password hash stored in the database.

If authentication succeeds, the API generates a JWT.

The token contains information such as:

- Account ID
- Employee ID
- Employee name
- Company email
- Role
- Department ID
- Department name
- Department code

This information can then be used by the application when working with authenticated users and role-based functionality.

---

## 🔑 Password Security

InsightFlow does **not** store employee passwords as readable plain-text passwords in the database.

ASP.NET Core's:

```text
PasswordHasher<Employee>
```

is used when creating the demo accounts.

The process can be represented as:

```text
Password
    ↓
PasswordHasher<Employee>
    ↓
Password Hash
    ↓
MySQL Database
```

During login:

```text
Entered Password
      ↓
PasswordHasher Verification
      ↓
Stored Password Hash
      ↓
Match?
 ├── Yes → Login
 └── No  → Unauthorized
```

The passwords documented in this README are intentionally provided because they belong to demonstration accounts.

They should never be reused for production users.

---

# 🌱 Automatic Database Seeding

InsightFlow contains a database seeder that prepares important initial information when the API is started.

The seeding process includes:

```text
InsightFlow.API Starts
        ↓
Apply Pending Migrations
        ↓
Check Departments
        ↓
Create Departments If Required
        ↓
Find Management Department
        ↓
Find Information Technology Department
        ↓
Check Administrator
   ├── Exists → Keep Existing Account
   └── Missing → Create Administrator
        ↓
Check Demo Employee
   ├── Exists → Keep Existing Account
   └── Missing → Create Employee
        ↓
Hash Passwords
        ↓
Save Changes
```

The seeder checks whether the demo users already exist before attempting to create them.

This prevents duplicate demo accounts from being added every time the API starts.

---

# ▶️ Running InsightFlow

## 1. Clone the Repository

Clone the InsightFlow repository and open:

```text
InsightFlow.sln
```

in Visual Studio.

---

## 2. Configure MySQL

Ensure that MySQL is installed and running.

InsightFlow requires a MySQL database for storing application information.

The database connection credentials are intentionally not included in the GitHub repository.

---

## 3. Configure Development Secrets

Sensitive configuration should be stored using **ASP.NET Core User Secrets**.

Private configuration includes values such as:

```text
ConnectionStrings:DefaultConnection

Jwt:Key
```

The JWT signing key and database credentials should never be committed to the repository.

Non-sensitive JWT configuration can remain in the normal application configuration.

For example:

```json
{
  "Jwt": {
    "Issuer": "InsightFlow.API",
    "Audience": "InsightFlow.MAUI",
    "ExpiryMinutes": 60
  }
}
```

---

## 4. Database Migrations

When the API starts, InsightFlow's database seeder applies pending Entity Framework Core migrations.

This ensures that the database structure is prepared before the initial development information is seeded.

---

## 5. Start the API

Run the:

```text
InsightFlow.API
```

project.

During development, Swagger can be used to inspect and test the API endpoints.

---

## 6. Test Authentication with Swagger

The login endpoint is:

```text
POST /api/Auth/login
```

The login request requires:

```json
{
  "companyEmail": "admin@insightflow.co.za",
  "employeeId": "IF-MGT-0001",
  "password": "InsightAdmin123!"
}
```

A successful authentication request returns information including:

```text
Login status
JWT
Token expiry
Employee details
Employee role
Department information
```

---

## 7. Start the .NET MAUI Application

Run the:

```text
InsightFlow
```

.NET MAUI project.

---

## 8. Administrator Login

Use:

```text
Company Email:
admin@insightflow.co.za

Employee ID:
IF-MGT-0001

Password:
InsightAdmin123!
```

---

## 9. Employee Login

Use:

```text
Company Email:
employee@insightflow.co.za

Employee ID:
IF-IT-0001

Password:
InsightEmployee123!
```

The two accounts allow the application to be evaluated using different user roles.

---

# 🔑 JWT Authentication

InsightFlow uses JSON Web Tokens (JWT) for API authentication.

After successful authentication:

```text
Login Request
      ↓
AuthController
      ↓
Employee Found
      ↓
Password Verified
      ↓
JWT Generated
      ↓
Token Returned
      ↓
Authenticated API Requests
```

The JWT includes claims representing information about the authenticated employee.

These include:

```text
Account ID
Employee ID
Name
Company Email
Role
Department ID
Department
Department Code
```

The JWT signing key is obtained from private application configuration and is not hardcoded into the application's source code.

---

# 📁 Repository Structure

```text
InsightFlow/
│
├── InsightFlow/
│   │
│   └── .NET MAUI Client
│
├── InsightFlow.API/
│   │
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Migrations/
│   ├── Services/
│   └── ASP.NET Core Web API
│
├── InsightFlow.sln
│
├── .gitignore
│
└── README.md
```

---

## 📱 InsightFlow Client

The `InsightFlow` project contains the client-side application.

This includes functionality such as:

- XAML pages
- Application navigation
- Authentication interfaces
- Dashboards
- Data-entry interfaces
- Employee interfaces
- Reports
- API communication
- User interface logic

---

## 🌐 InsightFlow.API

The `InsightFlow.API` project contains the server-side functionality.

This includes:

- API controllers
- Authentication
- JWT generation
- Database access
- Entity models
- Entity Framework Core
- Business logic
- Data services
- Reporting services
- Database migrations
- Database seeding

---

# 🔒 Security

Security is an important part of the InsightFlow architecture.

Sensitive development information should **not** be stored directly in the GitHub repository.

This includes:

- Real database passwords
- Database connection credentials
- JWT signing keys
- API keys
- Email account passwords
- Production credentials
- Other private secrets

During local development, private ASP.NET Core configuration is stored separately using **ASP.NET Core User Secrets**.

The repository should therefore contain:

```text
GitHub Repository
│
├── Application source code       ✅
├── Database models               ✅
├── Controllers                   ✅
├── Services                      ✅
├── UI code                       ✅
├── Database migrations           ✅
├── Database seeder               ✅
├── Non-sensitive configuration   ✅
│
└── Private secrets               ❌
```

The demo credentials documented in this README are intentionally public testing credentials and are separate from private application secrets.

---

# 🔄 Application Flow

A simplified representation of InsightFlow is:

```text
                    USER
                      │
                      ▼
              ┌──────────────┐
              │  .NET MAUI   │
              │ Application  │
              └──────┬───────┘
                     │
                  HTTP/JSON
                     │
                     ▼
             ┌───────────────┐
             │ ASP.NET Core  │
             │    Web API    │
             └───────┬───────┘
                     │
         ┌───────────┼────────────┐
         │           │            │
         ▼           ▼            ▼
 Authentication  Business      Reporting
                 Logic
         │           │            │
         └───────────┼────────────┘
                     │
                     ▼
            Entity Framework
                     │
                     ▼
              ┌────────────┐
              │   MySQL    │
              │  Database  │
              └────────────┘
```

---

# 💡 Project Motivation

Businesses generate information every day through employees, customers, products, sales, and other operational activities.

However, simply collecting data does not automatically make that information useful.

InsightFlow explores how operational data can be transformed into information that supports business understanding and decision-making.

```text
Operational Data
       ↓
Data Capture
       ↓
Database
       ↓
Processing
       ↓
Analysis
       ↓
Reports & Dashboards
       ↓
Performance Insights
       ↓
Decision Support
```

This concept inspired the name **InsightFlow**:

> Business information flows through the system and is transformed into useful insights.

---

# 🎓 Learning Objectives

InsightFlow was developed as both a functional software project and an opportunity to gain practical software development experience.

Through the development of the project, I gained experience with:

- C# programming
- Object-oriented programming
- .NET development
- .NET MAUI
- XAML
- ASP.NET Core
- REST API development
- MySQL
- Entity Framework Core
- Relational database design
- Database migrations
- Database seeding
- Authentication and authorization
- JWT authentication
- Password hashing
- Role-based access
- CRUD operations
- Business reporting
- Data analysis
- Forecasting concepts
- Dashboard development
- PDF report generation
- Asynchronous programming
- Data validation
- Error handling
- Git
- GitHub
- Client-server architecture

---

# 🚀 Development Journey

InsightFlow represents part of my practical software development journey as an Information Technology student.

Building InsightFlow provided experience developing a system containing:

```text
Frontend
   +
Backend API
   +
Database
   +
Authentication
   +
Role Management
   +
Business Logic
   +
Reporting
   +
Analytics
```

The project helped strengthen my understanding of how different parts of a software system communicate with each other.

It also provided practical experience applying concepts learned during my Information Technology studies to a larger software application.

The experience gained while developing InsightFlow has also influenced the way I approach newer projects and software architectures.

---

# 🔮 Future Improvements

InsightFlow can continue to evolve as my software development experience grows.

Possible future improvements include:

- [ ] Improved dashboard visualisations
- [ ] Additional business analytics
- [ ] More advanced forecasting
- [ ] Additional employee performance metrics
- [ ] Improved report customisation
- [ ] Additional charts and visualisations
- [ ] Improved mobile user experience
- [ ] More comprehensive automated testing
- [ ] Improved exception handling
- [ ] Improved deployment process
- [ ] Cloud database support
- [ ] Production deployment
- [ ] CI/CD pipeline
- [ ] Additional security improvements

---

# ⚠️ Project Notice

InsightFlow is primarily a **learning and portfolio project**.

It demonstrates the design and implementation of a business information system using technologies including:

```text
C#
.NET
.NET MAUI
XAML
ASP.NET Core
REST APIs
MySQL
Entity Framework Core
JWT Authentication
Password Hashing
Git
GitHub
```

The project demonstrates practical software development concepts and should not be interpreted as a production-ready commercial business intelligence platform.

---

# 👨‍💻 Developer

**Martin Hleketani**

BSc Information Technology Student  
North-West University

InsightFlow forms part of my software development portfolio and demonstrates practical experience applying software development, database, system design, and information technology concepts.

---

# 📄 License

This project is currently maintained as a development and portfolio project.

All rights reserved.
