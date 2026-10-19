# Pharmacy Compounding

A web app for a compounding pharmacy: it tracks orders for medicines made to prescription, from the
prescription check through manufacturing and quality control to payment and pickup.

Built with ASP.NET Core MVC (.NET 8), Entity Framework Core 8 (Code First) and PostgreSQL 17.
PostgreSQL and pgAdmin run in Docker.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or any Docker engine with Compose)

## Getting started

```bash
git clone https://github.com/snpkhy/pharmacy_compounding.git
cd pharmacy_compounding

cp .env.example .env            # then set your own POSTGRES_PASSWORD in .env
docker compose up -d            # PostgreSQL on localhost:5432, pgAdmin on http://localhost:5051
cd PharmacyCompounding
dotnet run                      # http://localhost:5050
```

The database name, user and password live in `.env`, which is not committed. Docker Compose reads it to create the
database and the pgAdmin connection, and the app loads it on startup (real environment variables take precedence).
The host and port are in `PharmacyCompounding/appsettings.json`.

Open http://localhost:5050. On the first start the app applies the EF Core migrations, creates the tables in the
`pharmacy` database and fills them with test data.

You can also open `pharmacy_compounding.sln` in Visual Studio or Rider and run the `http` profile.

### Demo accounts

| Role                | Email                    | Password  |
|---------------------|--------------------------|-----------|
| Pharmacy manager    | `manager@pharmacy.ua`    | `manager` |
| Pharmacist          | `lytvynenko@pharmacy.ua` | `pharm`   |
| Pharm technologist  | `savchenko@pharmacy.ua`  | `tech`    |
| Client              | `bondarenko@example.com` | `client1` |

Clients can also sign up at `/Account/Register`. The manager adds employees on the Employees page.

### Reset the database

```bash
docker compose down -v && docker compose up -d
```

Then run `dotnet run` again, and the database is recreated with fresh test data.
Do the same after changing the credentials in `.env`: PostgreSQL applies them only when it creates a new volume.

## Database (pgAdmin)

Open http://localhost:5051. No login is needed, and the **Pharmacy** server is already connected
(Servers → Pharmacy → Databases → pharmacy → Schemas → public → Tables).

- Table data: right-click a table → View/Edit Data → All Rows.
- SQL: right-click the `pharmacy` database → Query Tool.
- ER diagram: right-click the `pharmacy` database → ERD For Database.

To connect from another client (DBeaver, DataGrip, psql), use host `localhost:5432` and the database,
user and password from `.env`.

## LINQ queries

The **Queries** page (`/Queries`) runs 11 LINQ queries against the database: filtering, revenue for a chosen
day, month or half-year, grouping, joins, PLINQ, and insert, update (`ExecuteUpdate`) and delete (`ExecuteDelete`).
Each result shows the SQL that EF Core sent to PostgreSQL.

## Migrations

After changing the model, add a migration from the repository root:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project PharmacyCompounding
```

## Project structure

```
pharmacy_compounding/
├── pharmacy_compounding.sln
├── .env.example             database credentials template (copy to .env)
├── docker-compose.yml       PostgreSQL + pgAdmin with a pre-configured connection
├── docs/ARCHITECTURE.md     architecture notes
└── PharmacyCompounding/     ASP.NET Core MVC app
    ├── Models/  Data/  Migrations/  ViewModels/
    ├── Controllers/  Views/  wwwroot/
    └── Program.cs
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for how requests flow through the app and where things live.
