# RaceDay - Event Management System (Part 1: Planning & Database)

RaceDay is a full-stack event management platform for the South African road running, walking,
and cycling community. It lets **Event Organisers** create and manage events, categories, and
participant results, while **Participants** browse upcoming events, enter categories, track their
personal performance history, and check live weather and route information ahead of race day.

This repository currently contains **Part 1** of a three-part Portfolio of Evidence: system
planning and database design. No application code has been written yet, per the assignment
requirements - Part 2 will implement the API against this plan.

## Roles

- **Organiser** - creates, edits, and deletes their own events; manages categories under those
  events; captures and corrects participant results; views all enrolments for their events.
- **Participant** - registers an account; browses published events; enters an event by selecting
  a category; views their own enrolments; tracks their own result history; checks route and
  weather information for events they're entered in.

Role-based access is planned at the endpoint level in [docs/api-endpoint-plan.md](docs/api-endpoint-plan.md)
and will be enforced in the API in Part 2.

## Contents of /docs

| File | Description |
|---|---|
| [erd.png](docs/erd.png) | Entity Relationship Diagram - 7 entities, all primary/foreign keys and cardinalities labelled. |
| [erd-source.mmd](docs/erd-source.mmd) | Mermaid source used to generate `erd.png`, kept for traceability. |
| [api-endpoint-plan.md](docs/api-endpoint-plan.md) | Full endpoint table: method, route, description, role required, request body, expected response. |
| [raceday-schema.sql](docs/raceday-schema.sql) | SQL Server script: creates the schema and seeds sample data. |

**The SQL script matches the ERD exactly** - same 7 entities, same keys, same relationships. There
are no deliberate differences to disclose.

## Data model summary

- `Users` holds both Organisers and Participants, distinguished by a `Role` column.
- `Events` belongs to one Organiser (one-to-many).
- `Categories` belongs to one Event (one-to-many), e.g. "21km Half Marathon".
- `Routes` belongs to one Category (one-to-one) - start/end point, elevation, map link.
- `WeatherSnapshots` belongs to one Event (one-to-many) - recorded forecast history.
- `Enrolments` is the junction table resolving the many-to-many relationship between
  Participants and Categories, carrying the bib number, enrolment date, and status.
- `Results` belongs to one Enrolment (one-to-one) and records which Organiser captured it.

## Running the SQL script

Requires SQL Server (Express, Developer, or LocalDB) and SSMS or `sqlcmd`.

```
sqlcmd -S "(localdb)\MSSQLLocalDB" -i docs/raceday-schema.sql
```

The script drops and recreates the `RaceDayDb` database, so it is safe to re-run on a clean
instance. It has been tested end-to-end against a local SQL Server Express instance: all tables,
constraints, and seed rows (2 Organisers, 2 Participants, 3 Events, 6 Categories, 4 Enrolments,
plus sample Routes, Weather, and Results) load without error.

## CI/CD

A GitHub Actions workflow at [.github/workflows/validate-docs.yml](.github/workflows/validate-docs.yml)
validates that the required Part 1 documentation is present on every push and pull request to
`main` (the `/docs` folder, the ERD image, the endpoint plan, the SQL script, and this README).

**CI/CD green build screenshot:** _add screenshot here before submission_

## Video walkthrough

**YouTube (unlisted):** _add link here before submission_

The video covers: the planning documents, the ERD design decisions, the endpoint plan choices,
and a live run of the SQL script in SSMS.

## AI tool disclosure

AI assistance (Claude) was used during planning to help draft the initial ERD structure, the
endpoint plan table, and the SQL script skeleton, and to generate the ERD diagram image from a
Mermaid source file. All entities, relationships, endpoints, and constraints were reviewed,
understood, and adjusted by the author, who is responsible for and can explain every design
decision in the video walkthrough.
