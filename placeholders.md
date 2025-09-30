  Text Placeholders (Use any format: [[Variable]], {Variable}, [Variable], <<Variable>>)

  Party 1 Information:

  - Partij1Naam - Full name
  - Partij1Voornaam - First name(s)
  - Partij1Achternaam - Last name
  - Partij1Tussenvoegsel - Name prefix (van, de, etc.)
  - Partij1Adres - Street address
  - Partij1Postcode - Postal code
  - Partij1Plaats - City
  - Partij1Telefoon - Phone number
  - Partij1Email - Email address
  - Partij1Geboortedatum - Birth date (dd-MM-yyyy)
  - Partij1VolledigAdres - Complete address (street, postal, city)

  Party 2 Information:

  - Partij2Naam - Full name
  - Partij2Voornaam - First name(s)
  - Partij2Achternaam - Last name
  - Partij2Tussenvoegsel - Name prefix
  - Partij2Adres - Street address
  - Partij2Postcode - Postal code
  - Partij2Plaats - City
  - Partij2Telefoon - Phone number
  - Partij2Email - Email address
  - Partij2Geboortedatum - Birth date (dd-MM-yyyy)
  - Partij2VolledigAdres - Complete address

  Dossier Information:

  - DossierNummer - Dossier number
  - DossierDatum - Creation date (dd-MM-yyyy)
  - HuidigeDatum - Today's date in Dutch (e.g., "07 augustus 2025")

  Children Information:

  - AantalKinderen - Number of children
  - KinderenNamen - Comma-separated list of first names
  - KinderenVolledigeNamen - Comma-separated list of full names

  Individual Child Data (replace # with 1, 2, 3, etc.):

  - Kind1Naam - Child 1 full name
  - Kind1Voornaam - Child 1 first name(s)
  - Kind1Achternaam - Child 1 last name
  - Kind1Geboortedatum - Child 1 birth date (dd-MM-yyyy)
  - Kind1Leeftijd - Child 1 age
  - Kind1Geslacht - Child 1 gender

  (Same pattern for Kind2, Kind3, etc.)

  Alimentatie/Financial Information:

  General Alimentatie Data:
  - NettoBesteedbaarGezinsinkomen - Net disposable family income (formatted as Euro currency)
  - KostenKinderen - Total costs for children (formatted as Euro currency)
  - BijdrageKostenKinderen - Contribution to children costs (formatted as Euro currency)
  - BijdrageTemplateOmschrijving - Description of contribution template used

  Per Person Contributions:
  - Partij1EigenAandeel - Party 1's own contribution share (formatted as Euro currency)
  - Partij2EigenAandeel - Party 2's own contribution share (formatted as Euro currency)

  Dynamic List - All Children's Financial Information:
  - KinderenAlimentatie - Automatically generates a formatted list of all children with their complete financial agreements
    Example output:
    Jan de Vries:
      - Alimentatie: € 350,00
      - Hoofdverblijf: Maria
      - Kinderbijslag: Maria
      - Zorgkorting: 50%
      - Inschrijving bij: Maria
      - Kindgebonden budget: Maria

    Piet de Vries:
      - Alimentatie: € 300,00
      - Hoofdverblijf: Jan
      - Kinderbijslag: Kinderrekening
      - Zorgkorting: 50%
      - Inschrijving bij: Jan
      - Kindgebonden budget: Kinderrekening

  Note: All alimentatie data is automatically retrieved from the database tables:
  - dbo.alimentaties (main alimentatie data)
  - dbo.bijdrage_kosten_kinderen (per person contributions)
  - dbo.financiele_afspraken_kinderen (per child financial agreements)
  - dbo.bijdrage_templates (contribution template descriptions)

  Grammar Rules (Dutch singular/plural):

  - meervoud onze kinderen - "onze kinderen" (plural) or "ons kind" (singular)
  - meervoud heeft/hebben - "hebben" (plural) or "heeft" (singular)
  - meervoud is/zijn - "zijn" (plural) or "is" (singular)
  - meervoud verblijft/verblijven - "verblijven" (plural) or "verblijft" (singular)
  - meervoud kan/kunnen - "kunnen" (plural) or "kan" (singular)
  - meervoud zal/zullen - "zullen" (plural) or "zal" (singular)
  - meervoud moet/moeten - "moeten" (plural) or "moet" (singular)
  - meervoud wordt/worden - "worden" (plural) or "wordt" (singular)
  - meervoud blijft/blijven - "blijven" (plural) or "blijft" (singular)
  - meervoud gaat/gaan - "gaan" (plural) or "gaat" (singular)
  - meervoud komt/komen - "komen" (plural) or "komt" (singular)
  - meervoud hem/haar/hen - "hen" (plural), "hem" (male), "haar" (female)
  - meervoud hij/zij/ze - "ze" (plural), "hij" (male), "zij" (female)

  Table Placeholders (Put on its own line in the document):

  - [[TABEL_OMGANG]] - Generates visitation schedule table
  - [[TABEL_ZORG]] - Generates care arrangements table
  - [[TABEL_VAKANTIES]] - Generates holiday schedule table
  - [[TABEL_FEESTDAGEN]] - Generates public holidays table
  - [[TABEL_ALIMENTATIE]] - Generates alimony/financial agreements table

  Additional Available Fields (with dot notation):

  You can also use these with dots:
  - Partij1.VolledigeNaam
  - Partij1.Adres
  - Partij2.VolledigeNaam
  - Dossier.DossierNummer
  - Dossier.AangemaaktOp
  - etc.

  Example Template Usage:

  OUDERSCHAPSPLAN

  Ondergetekenden:

  1. [[Partij1Naam]]
     Geboren: [[Partij1Geboortedatum]]
     Wonende: [[Partij1VolledigAdres]]

  2. [[Partij2Naam]]
     Geboren: [[Partij2Geboortedatum]]
     Wonende: [[Partij2VolledigAdres]]

  Verklaren het volgende te zijn overeengekomen met betrekking tot [[meervoud onze kinderen]]:
  [[KinderenVolledigeNamen]]

  [[meervoud onze kinderen]] [[meervoud verblijft/verblijven]] afwisselend bij beide ouders.

  OMGANGSREGELING:
  [[TABEL_OMGANG]]

  ZORGAFSPRAKEN:
  [[TABEL_ZORG]]

  VAKANTIES:
  [[TABEL_VAKANTIES]]