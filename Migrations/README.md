# Common Migration Tasks

_All commands are run from the **Migrations** project directory._

---

## 1. Creating a New Migration

To add a new migration after making changes to the data model:

```bash
dotnet ef migrations add [MigrationName] --context SensorMonitoringContext
```

---

## 2. Removing a Migration

To revert the last migration that hasn't been applied to the database:

```bash
dotnet ef migrations remove
```

---

## 3. Applying Migrations to the Database

To apply all pending migrations to the database, launch the Migrations tool.  
Alternatively, use the command line:

```bash
dotnet ef database update
```

To update to a specific migration:

```bash
dotnet ef database update [MigrationName]
```

---

## 4. Reverting a Migration

To roll back to a previous migration:

```bash
dotnet ef database update [PreviousMigrationName]
```

To revert all migrations:

```bash
dotnet ef database update 0
```

---

## 5. Generating SQL Scripts

To generate a SQL script for a migration without applying it:

```bash
dotnet ef migrations script [MigrationName]
```
