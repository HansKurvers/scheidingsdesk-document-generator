# Database Schema Documentation

## Overview
This document contains the complete database schema for the SQL Server database on Azure. All tables use snake_case naming convention and are properly normalized with foreign key relationships.

## Core Tables

### dbo.dossiers
Main table for case files/dossiers.
- **id** (int, PK, Identity) - Primary key
- **dossier_nummer** (nvarchar(50), NOT NULL) - Unique dossier number
- **aangemaakt_op** (datetime, NOT NULL, default: getdate()) - Creation date
- **gewijzigd_op** (datetime, NOT NULL, default: getdate()) - Last modified date
- **status** (nvarchar(50), NOT NULL) - Dossier status
- **gebruiker_id** (int, NOT NULL, FK → dbo.gebruikers.id) - Mediator/user responsible

### dbo.gebruikers
Users table (mediators, admins, etc.) - prepared for Auth0 integration.
- **id** (int, PK, Identity) - Primary key
- *Additional columns to be added for Auth0 integration*

### dbo.personen
Unified table for all persons (parents, children, parties).
- **id** (int, PK, Identity) - Primary key
- **voorletters** (nvarchar(10), nullable) - Initials
- **voornamen** (nvarchar(100), nullable) - First names
- **roepnaam** (nvarchar(50), nullable) - Nickname/calling name
- **geslacht** (nvarchar(10), nullable) - Gender
- **tussenvoegsel** (nvarchar(20), nullable) - Name prefix (van, de, etc.)
- **achternaam** (nvarchar(100), NOT NULL) - Last name
- **adres** (nvarchar(200), nullable) - Address
- **postcode** (nvarchar(10), nullable) - Postal code
- **plaats** (nvarchar(100), nullable) - City
- **geboorte_plaats** (nvarchar(100), nullable) - Birth place
- **geboorte_datum** (date, nullable) - Birth date
- **nationaliteit_1** (nvarchar(50), nullable) - Primary nationality
- **nationaliteit_2** (nvarchar(50), nullable) - Secondary nationality
- **telefoon** (nvarchar(20), nullable) - Phone number
- **email** (nvarchar(100), nullable) - Email address
- **beroep** (nvarchar(100), nullable) - Profession

## Junction Tables

### dbo.dossiers_partijen
Links persons to dossiers with specific roles.
- **id** (int, PK, Identity) - Primary key
- **dossier_id** (int, NOT NULL, FK → dbo.dossiers.id) - Dossier reference
- **rol_id** (int, NOT NULL, FK → dbo.rollen.id) - Role in the dossier
- **persoon_id** (int, NOT NULL, FK → dbo.personen.id) - Person reference

### dbo.dossiers_kinderen
Links children to dossiers.
- **id** (int, PK, Identity) - Primary key
- **dossier_id** (int, NOT NULL, FK → dbo.dossiers.id) - Dossier reference
- **kind_id** (int, NOT NULL, FK → dbo.personen.id) - Child reference

### dbo.kinderen_ouders
Defines parent-child relationships with relationship types.
- **id** (int, PK, Identity) - Primary key
- **kind_id** (int, NOT NULL, FK → dbo.personen.id) - Child reference
- **ouder_id** (int, NOT NULL, FK → dbo.personen.id) - Parent reference
- **relatie_type_id** (int, NOT NULL, FK → dbo.relatie_types.id, default: 1) - Type of relationship

## Visitation Schedule Tables

### dbo.omgang
Visitation/contact arrangements.
- **id** (int, PK, Identity) - Primary key
- **dag_id** (int, NOT NULL, FK → dbo.dagen.id) - Day of week
- **dagdeel_id** (int, NOT NULL, FK → dbo.dagdelen.id) - Part of day
- **verzorger_id** (int, NOT NULL, FK → dbo.personen.id) - Caregiver
- **wissel_tijd** (nvarchar(50), nullable) - Exchange time (e.g., "09:00")
- **week_regeling_id** (int, NOT NULL, FK → dbo.week_regelingen.id) - Week arrangement type
- **week_regeling_anders** (nvarchar(255), nullable) - Custom week arrangement override
- **dossier_id** (int, NOT NULL, FK → dbo.dossiers.id) - Related dossier
- **aangemaakt_op** (datetime, NOT NULL, default: getdate()) - Creation date
- **gewijzigd_op** (datetime, NOT NULL, default: getdate()) - Last modified date

### dbo.dagen
Days of the week lookup table.
- **id** (int, PK, Identity) - Primary key
- **naam** (nvarchar(20), NOT NULL, UNIQUE) - Day name (Maandag, Dinsdag, etc.)

### dbo.dagdelen
Parts of day lookup table.
- **id** (int, PK, Identity) - Primary key
- **naam** (nvarchar(20), NOT NULL, UNIQUE) - Part name (Ochtend, Middag, Avond, Nacht)

### dbo.week_regelingen
Week arrangement types.
- **id** (int, PK, Identity) - Primary key
- **omschrijving** (nvarchar(200), NOT NULL) - Description (Elke week, Even weken, etc.)

## Care Arrangement Tables

### dbo.zorg
Care arrangements and agreements.
- **id** (int, PK, Identity) - Primary key
- **zorg_categorie_id** (int, NOT NULL, FK → dbo.zorg_categorieen.id) - Care category
- **zorg_situatie_id** (int, NOT NULL, FK → dbo.zorg_situaties.id) - Care situation
- **overeenkomst** (nvarchar(MAX), NOT NULL) - Agreement text
- **situatie_anders** (nvarchar(500), nullable) - Custom situation override
- **dossier_id** (int, NOT NULL, FK → dbo.dossiers.id) - Related dossier
- **aangemaakt_op** (datetime, NOT NULL, default: getdate()) - Creation date
- **aangemaakt_door** (int, NOT NULL, FK → dbo.gebruikers.id) - Created by user
- **gewijzigd_op** (datetime, NOT NULL, default: getdate()) - Last modified date
- **gewijzigd_door** (int, nullable, FK → dbo.gebruikers.id) - Modified by user

### dbo.zorg_categorieen
Care categories lookup table.
- **id** (int, PK, Identity) - Primary key
- **naam** (nvarchar(100), NOT NULL, UNIQUE) - Category name

### dbo.zorg_situaties
Care situations lookup table.
- **id** (int, PK, Identity) - Primary key
- **naam** (nvarchar(200), NOT NULL) - Situation name
- **zorg_categorie_id** (int, nullable, FK → dbo.zorg_categorieen.id) - Related category

## Lookup Tables

### dbo.rollen
Roles for persons in dossiers.
- **id** (int, PK, Identity) - Primary key
- **naam** (nvarchar(50), nullable) - Role name

### dbo.relatie_types
Types of parent-child relationships.
- **id** (int, PK, Identity) - Primary key
- **naam** (nvarchar(50), nullable) - Relationship type name

### dbo.schoolvakanties
School vacation periods lookup table.
- **id** (int, PK, Identity) - Primary key
- **naam** (nvarchar(100), NOT NULL) - Vacation name (e.g., Kerstvakantie, Zomervakantie)

### dbo.regelingen_templates
Templates for custody and visitation arrangements.
- **id** (int, PK, Identity) - Primary key
- **template_naam** (nvarchar(50), NOT NULL) - Template identifier (e.g., partij1, partij2, partij1_even)
- **template_tekst** (nvarchar(500), NOT NULL) - Template text with placeholders (e.g., {KIND}, {PARTIJ1}, {FEESTDAG})
- **meervoud_kinderen** (bit, NOT NULL) - Whether the template is for multiple children (singular vs plural)
- **type** (nvarchar(50), NOT NULL) - Template type (Feestdag, Vakantie)

## Foreign Key Relationships

### Dossier Relationships
- `dossiers.gebruiker_id` → `gebruikers.id`
- `dossiers_partijen.dossier_id` → `dossiers.id`
- `dossiers_partijen.rol_id` → `rollen.id`
- `dossiers_partijen.persoon_id` → `personen.id`
- `dossiers_kinderen.dossier_id` → `dossiers.id`
- `dossiers_kinderen.kind_id` → `personen.id`

### Person Relationships
- `kinderen_ouders.kind_id` → `personen.id`
- `kinderen_ouders.ouder_id` → `personen.id`
- `kinderen_ouders.relatie_type_id` → `relatie_types.id`

### Visitation Relationships
- `omgang.dag_id` → `dag.id`
- `omgang.dagdeel_id` → `dagdeel.id`
- `omgang.verzorger_id` → `personen.id`
- `omgang.week_regeling_id` → `week_regeling.id`
- `omgang.dossier_id` → `dossiers.id`

### Care Arrangement Relationships
- `zorg.zorg_categorie_id` → `zorg_categorieen.id`
- `zorg.zorg_situatie_id` → `zorg_situaties.id`
- `zorg.dossier_id` → `dossiers.id`
- `zorg.aangemaakt_door` → `gebruikers.id`
- `zorg.gewijzigd_door` → `gebruikers.id`
- `zorg_situaties.zorg_categorie_id` → `zorg_categorieen.id`

## Business Rules

1. **Override Fields**: Both `omgang.week_regeling_anders` and `zorg.situatie_anders` are used when the standard options don't fit (when "Anders" is selected)
2. **Audit Trail**: Most tables include `aangemaakt_op` and `gewijzigd_op` for tracking changes
3. **Soft References**: The `gebruikers` table is minimal, prepared for Auth0 integration
4. **Unified Person Model**: All persons (children, parents, parties) are stored in the `personen` table with relationships defined in junction tables

## Common Queries

### Get all children in a dossier
```sql
SELECT p.* 
FROM personen p
INNER JOIN dossiers_kinderen dk ON p.id = dk.kind_id
WHERE dk.dossier_id = @dossier_id;
```

### Get visitation schedule for a dossier
```sql
SELECT 
    d.naam AS dag,
    dd.naam AS dagdeel,
    p.voornamen + ' ' + p.achternaam AS verzorger,
    o.wissel_tijd,
    COALESCE(o.week_regeling_anders, wr.omschrijving) AS regeling
FROM omgang o
INNER JOIN dag d ON o.dag_id = d.id
INNER JOIN dagdeel dd ON o.dagdeel_id = dd.id
INNER JOIN personen p ON o.verzorger_id = p.id
INNER JOIN week_regeling wr ON o.week_regeling_id = wr.id
WHERE o.dossier_id = @dossier_id
ORDER BY d.id, dd.id;
```

### Get care arrangements for a dossier
```sql
SELECT 
    zc.naam AS categorie,
    COALESCE(z.situatie_anders, zs.naam) AS situatie,
    z.overeenkomst
FROM zorg z
INNER JOIN zorg_categorieen zc ON z.zorg_categorie_id = zc.id
INNER JOIN zorg_situaties zs ON z.zorg_situatie_id = zs.id
WHERE z.dossier_id = @dossier_id
ORDER BY zc.naam, zs.naam;
```