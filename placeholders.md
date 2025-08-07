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