Tekst Placeholders (Gebruik elk formaat: [[Variable]], {Variable}, [Variable], <<Variable>>)

NIEUWE PLACEHOLDER SYSTEEM VOOR TEMPLATES:
- {KIND} - Gebruikt enkelvoud vorm als er 1 kind is, anders meervoud (bijv. "Emma" of "Emma en Luuk")
- {KINDEREN} - Altijd meervoud vorm van de kindernamen (bijv. "Emma en Luuk")

Let op: Deze {KIND} en {KINDEREN} placeholders worden ook herhaald verderop in de documentatie onder "Template-Specifieke Placeholders"

Partij 1 Informatie:

- [[Partij1Naam]] - Volledige naam
- [[Partij1Voornaam]] - Voornaam/voornamen
- [[Partij1Roepnaam]] - Roepnaam
- [[Partij1Achternaam]] - Achternaam
- [[Partij1Tussenvoegsel]] - Tussenvoegsel (van, de, etc.)
- [[Partij1VolledigeNaamMetTussenvoegsel]] - Voornamen + tussenvoegsel + achternaam (bijv. "Jan Peter de Vries")
- [[Partij1VolledigeAchternaam]] - Tussenvoegsel + achternaam (bijv. "de Vries")
- [[Partij1VoorlettersAchternaam]] - Voorletters + tussenvoegsel + achternaam (bijv. "J.P. de Vries")
- [[Partij1Adres]] - Straatnaam en huisnummer
- [[Partij1Postcode]] - Postcode
- [[Partij1Plaats]] - Woonplaats
- [[Partij1Geboorteplaats]] - Geboorteplaats
- [[Partij1Telefoon]] - Telefoonnummer
- [[Partij1Email]] - E-mailadres
- [[Partij1Geboortedatum]] - Geboortedatum (d MMMM yyyy, bijv. "15 januari 2024")
- [[Partij1VolledigAdres]] - Volledig adres (straat, postcode, plaats)
- [[Partij1Benaming]] - Contextafhankelijke benaming (roepnaam of "de vader"/"de moeder" bij anoniem)
- [[Partij1Nationaliteit1]] - Eerste nationaliteit (basisvorm, bijv. "Nederlands")
- [[Partij1Nationaliteit2]] - Tweede nationaliteit (basisvorm)
- [[Partij1Nationaliteit1Bijvoeglijk]] - Eerste nationaliteit bijvoeglijk (bijv. "Nederlandse" voor "de Nederlandse nationaliteit")
- [[Partij1Nationaliteit2Bijvoeglijk]] - Tweede nationaliteit bijvoeglijk

Partij 2 Informatie:

- [[Partij2Naam]] - Volledige naam
- [[Partij2Voornaam]] - Voornaam/voornamen
- [[Partij2Roepnaam]] - Roepnaam
- [[Partij2Achternaam]] - Achternaam
- [[Partij2Tussenvoegsel]] - Tussenvoegsel
- [[Partij2VolledigeNaamMetTussenvoegsel]] - Voornamen + tussenvoegsel + achternaam (bijv. "Maria van der Berg")
- [[Partij2VolledigeAchternaam]] - Tussenvoegsel + achternaam (bijv. "van der Berg")
- [[Partij2VoorlettersAchternaam]] - Voorletters + tussenvoegsel + achternaam (bijv. "M. van der Berg")
- [[Partij2Adres]] - Straatnaam en huisnummer
- [[Partij2Postcode]] - Postcode
- [[Partij2Plaats]] - Woonplaats
- [[Partij2Geboorteplaats]] - Geboorteplaats
- [[Partij2Telefoon]] - Telefoonnummer
- [[Partij2Email]] - E-mailadres
- [[Partij2Geboortedatum]] - Geboortedatum (d MMMM yyyy, bijv. "15 januari 2024")
- [[Partij2VolledigAdres]] - Volledig adres
- [[Partij2Benaming]] - Contextafhankelijke benaming (roepnaam of "de vader"/"de moeder" bij anoniem)
- [[Partij2Nationaliteit1]] - Eerste nationaliteit (basisvorm, bijv. "Belgisch")
- [[Partij2Nationaliteit2]] - Tweede nationaliteit (basisvorm)
- [[Partij2Nationaliteit1Bijvoeglijk]] - Eerste nationaliteit bijvoeglijk (bijv. "Belgische" voor "de Belgische nationaliteit")
- [[Partij2Nationaliteit2Bijvoeglijk]] - Tweede nationaliteit bijvoeglijk

Dossier Informatie:

- [[DossierNummer]] - Dossiernummer
- [[DossierDatum]] - Aanmaakdatum (d MMMM yyyy, bijv. "15 januari 2024")
- [[HuidigeDatum]] - Huidige datum in het Nederlands (d MMMM yyyy, bijv. "7 augustus 2025")
- [[IsAnoniem]] - Of het dossier anoniem is (true/false)

Kinderen Informatie:

- [[AantalKinderen]] - Aantal kinderen
- [[AantalMinderjarigeKinderen]] - Aantal minderjarige kinderen (jonger dan 18)
- [[KinderenNamen]] - Komma-gescheiden lijst van voornamen
- [[KinderenRoepnamen]] - Komma-gescheiden lijst van roepnamen
- [[KinderenVolledigeNamen]] - Komma-gescheiden lijst van volledige namen
- [[RoepnamenMinderjarigeKinderen]] - Komma-gescheiden lijst van roepnamen van minderjarige kinderen

Individuele Kind Gegevens (vervang # met 1, 2, 3, etc.):

- [[Kind1Naam]] - Kind 1 volledige naam
- [[Kind1Voornaam]] - Kind 1 voornaam/voornamen
- [[Kind1Roepnaam]] - Kind 1 roepnaam
- [[Kind1Achternaam]] - Kind 1 achternaam
- [[Kind1Tussenvoegsel]] - Kind 1 tussenvoegsel (van, de, etc.)
- [[Kind1RoepnaamAchternaam]] - Kind 1 roepnaam + tussenvoegsel + achternaam (bijv. "Jan de Vries")
- [[Kind1Geboortedatum]] - Kind 1 geboortedatum (d MMMM yyyy, bijv. "15 januari 2024")
- [[Kind1Geboorteplaats]] - Kind 1 geboorteplaats
- [[Kind1Leeftijd]] - Kind 1 leeftijd
- [[Kind1Geslacht]] - Kind 1 geslacht

(Zelfde patroon voor [[Kind2Naam]], [[Kind3Naam]], etc.)

Ouderschapsplan Informatie:

Relatie & Juridisch:
- [[SoortRelatie]] - Soort relatie (bijv. "gehuwd", "geregistreerd_partnerschap", "samenwonend")
- [[DatumAanvangRelatie]] - Datum aanvang huwelijk/relatie (d MMMM yyyy, bijv. "15 januari 2024")
- [[PlaatsRelatie]] - Plaats van huwelijk of registratie partnerschap
- [[SoortRelatieVoorwaarden]] - Afgeleide placeholder: voorwaarden behorend bij de relatie
  * "gehuwd" → "huwelijkse voorwaarden"
  * "geregistreerd_partnerschap" → "partnerschapsvoorwaarden"
  * "samenwonend" → "samenlevingsovereenkomst"
  * anders → "overeenkomst"
- [[SoortRelatieVerbreking]] - Afgeleide placeholder: verbreking behorend bij de relatie
  * "gehuwd" → "echtscheiding"
  * "geregistreerd_partnerschap" → "ontbinding van het geregistreerd partnerschap"
  * "samenwonend" → "beëindiging van de samenleving"
- [[RelatieAanvangZin]] - Afgeleide placeholder: volledige zin over aanvang relatie
  * "gehuwd" → "Wij zijn op [datum] te [plaats] met elkaar gehuwd."
  * "geregistreerd_partnerschap" → "Wij zijn op [datum] te [plaats] met elkaar een geregistreerd partnerschap aangegaan."
  * "samenwonend" / "lat_relatie" / "ex_partners" / "anders" → "Wij hebben een affectieve relatie gehad."
- [[OuderschapsplanDoelZin]] - Afgeleide placeholder: zin over het doel van het ouderschapsplan
  * "gehuwd" → "In dit ouderschapsplan hebben we afspraken gemaakt over [ons kind/onze kinderen] omdat we gaan scheiden."
  * "geregistreerd_partnerschap" → "In dit ouderschapsplan hebben we afspraken gemaakt over [ons kind/onze kinderen] omdat we ons geregistreerd partnerschap willen laten ontbinden."
  * "samenwonend" → "In dit ouderschapsplan hebben we afspraken gemaakt over [ons kind/onze kinderen] omdat we onze samenleving willen beëindigen."
  * "lat_relatie" / "ex_partners" / "anders" → "In dit ouderschapsplan hebben we afspraken gemaakt over [ons kind/onze kinderen]."
- [[BetrokkenheidKind]] - Betrokkenheid kind
- [[Kiesplan]] - Kiesplan (ruwe database waarde: kindplan, kies_professional, kindbehartiger, nee)
- [[KiesplanZin]] - Volledige zin over KIES plan op basis van gekozen optie (leeg bij "nee")
  * "kindplan" → "Bij het maken van de afspraken in dit ouderschapsplan hebben we [kindnamen] gevraagd een KIES Kindplan te maken dat door ons is ondertekend, zodat wij rekening kunnen houden met [zijn/haar/hun] wensen. Het KIES Kindplan van [kindnamen] is opgenomen als bijlage van dit ouderschapsplan."
  * "kies_professional" → "Bij het maken van de afspraken in dit ouderschapsplan [is/zijn] [kindnamen] ondersteund door een KIES professional met een KIES kindgesprek om [zijn/haar/hun] vragen te kunnen stellen en behoeftes en wensen aan te geven, zodat wij hiermee rekening kunnen houden. [Het/De] door ons ondertekende KIES [Kindplan is/Kindplannen zijn] daarbij gemaakt en bijlage van dit ouderschapsplan."
  * "kindbehartiger" → "Bij het maken van de afspraken in dit ouderschapsplan [heeft/hebben] [kindnamen] hulp gekregen van een Kindbehartiger om [zijn/haar/hun] wensen in kaart te brengen zodat wij hiermee rekening kunnen houden."
  * "nee" of leeg → geen tekst
  * Let op: Werkwoorden (is/zijn, heeft/hebben) en bezittelijk voornaamwoord (zijn/haar/hun) worden automatisch aangepast op basis van het aantal kinderen en geslacht.
- [[ParentingCoordinator]] - Parenting coordinator informatie

Ouderlijk Gezag (Parental Authority):
- [[GezagRegeling]] - Volledige zin over ouderlijk gezag op basis van gekozen optie
  * Optie 1: "[Partij1] en [Partij2] hebben samen het ouderlijk gezag over [kinderen]."
  * Optie 2: "[Partij1] heeft alleen het ouderlijk gezag over [kinderen]. Dit blijft zo."
  * Optie 3: "[Partij2] heeft alleen het ouderlijk gezag over [kinderen]. Dit blijft zo."
  * Optie 4: "[Partij1] heeft alleen het ouderlijk gezag over [kinderen]. Partijen spreken af dat zij binnen [X] weken na ondertekening van dit ouderschapsplan gezamenlijk gezag zullen regelen."
  * Optie 5: "[Partij2] heeft alleen het ouderlijk gezag over [kinderen]. Partijen spreken af dat zij binnen [X] weken na ondertekening van dit ouderschapsplan gezamenlijk gezag zullen regelen."
- [[GezagPartij]] - Nummer van gekozen gezagsoptie (1-5)
- [[GezagTermijnWeken]] - Aantal weken termijn (alleen bij optie 4 of 5)

Woonplaats (Residence Arrangements):
- [[WoonplaatsRegeling]] - Volledige zin over woonplaatsregeling op basis van gekozen optie
  * Optie 1: "De woonplaatsen van partijen blijven hetzelfde. [Partij1] blijft wonen in [huidige plaats 1] en [Partij2] blijft wonen in [huidige plaats 2]."
  * Optie 2: "[Partij1] gaat verhuizen naar [nieuwe plaats 1]. [Partij2] blijft wonen in [huidige plaats 2]."
  * Optie 3: "[Partij1] blijft wonen in [huidige plaats 1]. [Partij2] gaat verhuizen naar [nieuwe plaats 2]."
  * Optie 4: "[Partij1] gaat verhuizen naar [nieuwe plaats 1] en [Partij2] gaat verhuizen naar [nieuwe plaats 2]."
  * Optie 5: "Het is nog onduidelijk waar de ouders gaan wonen."
- [[WoonplaatsOptie]] - Nummer van gekozen woonplaatsoptie (1-5)
- [[WoonplaatsPartij1]] - Nieuwe woonplaats van partij 1 (alleen bij optie 2 of 4)
- [[WoonplaatsPartij2]] - Nieuwe woonplaats van partij 2 (alleen bij optie 3 of 4)
- [[HuidigeWoonplaatsPartij1]] - Huidige woonplaats van partij 1 (uit personen.plaats)
- [[HuidigeWoonplaatsPartij2]] - Huidige woonplaats van partij 2 (uit personen.plaats)

Zorg & Verblijf:
- [[Hoofdverblijf]] - Hoofdverblijfplaats
- [[Zorgverdeling]] - Zorgverdeling
- [[OpvangKinderen]] - Opvang kinderen

Financieel & Administratief:
- [[WaOpNaamVan]] - Partij met WA verzekering (geeft roepnaam terug)
- [[ZorgverzekeringOpNaamVan]] - Partij met zorgverzekering (geeft roepnaam terug)
- [[KinderbijslagOntvanger]] - Ontvanger kinderbijslag (geeft roepnaam of "Kinderrekening" terug)
- [[BankrekeningnummersKind]] - Bankrekeningnummers kind

Overig:
- [[KeuzeDevices]] - Keuze apparaten

Alimentatie/Financiële Informatie:

Algemene Alimentatie Gegevens:
- [[NettoBesteedbaarGezinsinkomen]] - Netto besteedbaar gezinsinkomen (geformatteerd als Euro bedrag)
- [[KostenKinderen]] - Totale kosten voor kinderen (geformatteerd als Euro bedrag)
- [[BijdrageKostenKinderen]] - Bijdrage aan kosten kinderen (geformatteerd als Euro bedrag)
- [[BijdrageTemplateOmschrijving]] - Omschrijving van gebruikte bijdrage template

Bijdragen Per Persoon:
- [[Partij1EigenAandeel]] - Eigen aandeel van Partij 1 (geformatteerd als Euro bedrag)
- [[Partij2EigenAandeel]] - Eigen aandeel van Partij 2 (geformatteerd als Euro bedrag)

Kinderrekening Gegevens (nieuw):
- [[StortingOuder1Kinderrekening]] - Maandelijkse storting ouder 1 op kinderrekening (geformatteerd als Euro bedrag)
- [[StortingOuder2Kinderrekening]] - Maandelijkse storting ouder 2 op kinderrekening (geformatteerd als Euro bedrag)
- [[KinderrekeningKostensoorten]] - Lijst van kostensoorten die van kinderrekening mogen worden betaald (geformatteerd met bullets)
  Voorbeeld output:
  - Kinderopvang kosten (onder werktijd)
  - Kleding, schoenen, kapper en persoonlijke verzorging
  - Schoolgeld, schoolbenodigdheden en andere schoolkosten
- [[KinderrekeningMaximumOpname]] - Of er een maximum opnamebedrag is (Ja/Nee)
- [[KinderrekeningMaximumOpnameBedrag]] - Het maximum opnamebedrag (geformatteerd als Euro bedrag)
- [[KinderbijslagStortenOpKinderrekening]] - Of kinderbijslag op kinderrekening wordt gestort (Ja/Nee)
- [[KindgebondenBudgetStortenOpKinderrekening]] - Of kindgebonden budget op kinderrekening wordt gestort (Ja/Nee)

Alimentatie Settings (nieuw):
- [[BedragenAlleKinderenGelijk]] - Of alle kinderen hetzelfde alimentatiebedrag krijgen (Ja/Nee)
- [[AlimentatiebedragPerKind]] - Het bedrag per kind als alle bedragen gelijk zijn (geformatteerd als Euro bedrag)
- [[Alimentatiegerechtigde]] - Wie de alimentatiegerechtigde is (naam ouder)

Synchronisatie Settings Alle Kinderen (nieuw):
- [[AfsprakenAlleKinderenGelijk]] - Of alle kinderen dezelfde afspraken hebben (Ja/Nee)
- [[HoofdverblijfAlleKinderen]] - Hoofdverblijf voor alle kinderen (roepnaam van de ouder)
- [[InschrijvingAlleKinderen]] - Inschrijving voor alle kinderen (roepnaam van de ouder)
- [[KinderbijslagOntvangerAlleKinderen]] - Kinderbijslag ontvanger voor alle kinderen (roepnaam van de ouder of "Kinderrekening")
- [[KindgebondenBudgetAlleKinderen]] - Kindgebonden budget ontvanger voor alle kinderen (roepnaam van de ouder of "Kinderrekening")

Template Detectie Flags (nieuw):
- [[IsKinderrekeningBetaalwijze]] - Of de betaalwijze een kinderrekening type is (Ja/Nee)
- [[IsAlimentatieplichtBetaalwijze]] - Of de betaalwijze een alimentatieplichtige type is (Ja/Nee)

Betaalwijze Beschrijving (nieuw):
- [[BetaalwijzeBeschrijving]] - Volledige tekst over de gekozen betaalwijze (kinderrekening of alimentatie)

  **Variant 1: Kinderrekening** (als IsKinderrekeningBetaalwijze = Ja)
  Genereert tekst met:
  - Intro: "We hebben ervoor gekozen om gebruik te maken van een gezamenlijke kinderrekening."
  - Wie kinderbijslag ontvangt en of deze wordt gestort op kinderrekening
  - Wie kindgebonden budget ontvangt en of deze wordt gestort op kinderrekening
  - "We betalen allebei de eigen verblijfskosten."
  - Verblijfsoverstijgende kosten (met kostensoorten) van kinderrekening
  - Stortingsbedrag ouder 1 per maand
  - Stortingsbedrag ouder 2 per maand
  - Controle en tekort afspraken
  - Opheffingsoptie (helft/verhouding/spaarrekening)

  **Variant 2: Alimentatie** (als IsAlimentatieplichtBetaalwijze = Ja)
  Genereert tekst met:
  - Intro: "We hebben ervoor gekozen om een maandelijkse kinderalimentatie af te spreken."
  - Alimentatiegerechtigde ontvangt kinderbijslag en kindgebonden budget
  - "We betalen allebei de eigen verblijfskosten."
  - Zorgkortingspercentage
  - Alimentatiegerechtigde betaalt verblijfsoverstijgende kosten
  - Alimentatieplichtige betaalt vanaf ingangsdatum een bedrag per kind per maand
  - Wettelijke indexering
  - Eerste indexeringsjaar

Dynamische Lijst - Financiële Afspraken Alle Kinderen:
- [[KinderenAlimentatie]] - Genereert automatisch een geformatteerde lijst van alle kinderen met hun complete financiële afspraken
  Voorbeeld output:
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

Dynamische Hoofdverblijf Verdeling (nieuw):
- [[HoofdverblijfVerdeling]] - Genereert automatisch een intelligente samenvatting van waar de kinderen hun hoofdverblijf hebben, inclusief co-ouderschap scenario's

  Voorbeeld outputs:

  **Scenario 1: Alle kinderen co-ouderschap**
  "Wij hebben een zorgregeling afgesproken waarbij onze kinderen ongeveer evenveel tijd bij ieder van ons verblijven. Zij hebben dus geen hoofdverblijf."

  **Scenario 2: Alle kinderen bij één ouder**
  "Jan, Piet en Lisa hebben hun hoofdverblijf bij Maria."

  **Scenario 3: Gemengde situatie (verschillende ouders)**
  "Jan en Piet hebben hun hoofdverblijf bij Maria. Lisa heeft haar hoofdverblijf bij Hans."

  **Scenario 4: Gemengd met co-ouderschap**
  "Jan en Piet hebben hun hoofdverblijf bij Maria. Voor Lisa hebben wij een zorgregeling afgesproken waarbij zij ongeveer evenveel tijd bij ieder van ons verblijft. Zij heeft dus geen hoofdverblijf."

  **Scenario 5: Eén kind co-ouderschap**
  "Wij hebben een zorgregeling afgesproken waarbij ons kind ongeveer evenveel tijd bij ieder van ons verblijft. Het kind heeft dus geen hoofdverblijf."

  Kenmerken:
  - Gebruikt roepnamen van kinderen
  - Gebruikt contextafhankelijke benaming van ouders ([[Partij1Benaming]], [[Partij2Benaming]])
  - Correcte grammatica voor enkelvoud/meervoud (heeft/hebben, zijn/haar/hun)
  - Automatische detectie van co-ouderschap per kind
  - Handelt gemengde scenario's intelligent af met aparte zinnen per situatie

Dynamische BRP Inschrijving Verdeling (nieuw):
- [[InschrijvingVerdeling]] - Genereert automatisch een opsomming van waar de kinderen in de Basisregistratie Personen (BRP) zijn ingeschreven

  Voorbeeld outputs:

  **Scenario 1: Alle kinderen bij partij1**
  "Jan, Piet en Lisa zullen ingeschreven staan in de Basisregistratie Personen aan het adres van Maria in Amsterdam."

  **Scenario 2: Eén kind bij partij1**
  "Jan zal ingeschreven staan in de Basisregistratie Personen aan het adres van Maria in Amsterdam."

  **Scenario 3: Gemengde situatie (verschillende ouders)**
  "Jan en Piet zullen ingeschreven staan in de Basisregistratie Personen aan het adres van Maria in Amsterdam. Lisa zal ingeschreven staan in de Basisregistratie Personen aan het adres van Hans in Rotterdam."

  **Scenario 4: Anoniem dossier**
  "Jan en Piet zullen ingeschreven staan in de Basisregistratie Personen aan het adres van de vader in Utrecht."

  Kenmerken:
  - Gebruikt roepnamen van kinderen
  - Gebruikt contextafhankelijke benaming van ouders ([[Partij1Benaming]], [[Partij2Benaming]])
  - Toont de plaats/woonplaats van elke ouder
  - Correcte grammatica voor enkelvoud/meervoud (zal/zullen)
  - Handelt gemengde scenario's af met aparte zinnen per ouder
  - Een kind kan slechts op één adres ingeschreven staan (geen co-ouderschap optie voor BRP)

Opmerking: Alle alimentatie gegevens worden automatisch opgehaald uit de database tabellen:
- dbo.alimentaties (algemene alimentatie gegevens, inclusief nieuwe kinderrekening velden)
- dbo.bijdrage_kosten_kinderen (bijdragen per persoon)
- dbo.financiele_afspraken_kinderen (financiële afspraken per kind)
- dbo.bijdrage_templates (omschrijvingen bijdrage templates)

Grammatica Regels (Nederlands enkelvoud/meervoud):

Opmerking: Gebruik de volledige vorm in je template (bijv. [[heeft/hebben]], niet [[meervoud heeft/hebben]])

- [[ons kind/onze kinderen]] - "onze kinderen" (meervoud) of "ons kind" (enkelvoud)
- [[het kind/de kinderen]] - "de kinderen" (meervoud) of "het kind" (enkelvoud)
- [[kind/kinderen]] - "kinderen" (meervoud) of "kind" (enkelvoud)
- [[heeft/hebben]] - "hebben" (meervoud) of "heeft" (enkelvoud)
- [[is/zijn]] - "zijn" (meervoud) of "is" (enkelvoud)
- [[verblijft/verblijven]] - "verblijven" (meervoud) of "verblijft" (enkelvoud)
- [[kan/kunnen]] - "kunnen" (meervoud) of "kan" (enkelvoud)
- [[zal/zullen]] - "zullen" (meervoud) of "zal" (enkelvoud)
- [[moet/moeten]] - "moeten" (meervoud) of "moet" (enkelvoud)
- [[wordt/worden]] - "worden" (meervoud) of "wordt" (enkelvoud)
- [[blijft/blijven]] - "blijven" (meervoud) of "blijft" (enkelvoud)
- [[gaat/gaan]] - "gaan" (meervoud) of "gaat" (enkelvoud)
- [[komt/komen]] - "komen" (meervoud) of "komt" (enkelvoud)
- [[zou/zouden]] - "zouden" (meervoud) of "zou" (enkelvoud)
- [[wil/willen]] - "willen" (meervoud) of "wil" (enkelvoud)
- [[mag/mogen]] - "mogen" (meervoud) of "mag" (enkelvoud)
- [[doet/doen]] - "doen" (meervoud) of "doet" (enkelvoud)
- [[krijgt/krijgen]] - "krijgen" (meervoud) of "krijgt" (enkelvoud)
- [[neemt/nemen]] - "nemen" (meervoud) of "neemt" (enkelvoud)
- [[brengt/brengen]] - "brengen" (meervoud) of "brengt" (enkelvoud)
- [[haalt/halen]] - "halen" (meervoud) of "haalt" (enkelvoud)
- [[hem/haar/hen]] - "hen" (meervoud), "hem" (mannelijk), "haar" (vrouwelijk)
- [[hij/zij/ze]] - "ze" (meervoud), "hij" (mannelijk), "zij" (vrouwelijk)
- [[zijn/haar/hun]] - "hun" (meervoud), "zijn" (mannelijk), "haar" (vrouwelijk)
- [[diens/dier/hun]] - "hun" (meervoud), "diens" (mannelijk), "dier" (vrouwelijk)

Tabel Placeholders (Plaats op een eigen regel in het document):

- [[TABEL_ALIMENTATIE]] - Genereert alimentatie/financiële afspraken tabel
- [[TABEL_OMGANG]] - Genereert omgangsregeling tabel
- [[TABEL_ZORG]] - Genereert zorgafspraken tabel
- [[TABEL_FEESTDAGEN]] - Genereert feestdagen tabel
- [[TABEL_VAKANTIES]] - Genereert vakanties tabel
- [[LIJST_KINDEREN]] - Genereert geformatteerde lijst van kinderen met details

Template-Specifieke Placeholders (Voor gebruik in bijzondere dag/feestdag/vakantie templates):

- {KIND} - Gebruikt enkelvoud vorm als er 1 kind is, anders meervoud (bijv. "Emma" of "Emma en Luuk")
- {KINDEREN} - Altijd meervoud vorm van de kindernamen (bijv. "Emma en Luuk")
- {BIJZONDERE_DAG} - Naam van de bijzondere dag
- {FEESTDAG} - Naam van de feestdag
- {VAKANTIE} - Naam van de vakantie
- {DATUM} - Datum placeholder voor templates
- {JAAR} - Jaar placeholder voor templates
- {PARTIJ1} - Partij 1 naam in template context
- {PARTIJ2} - Partij 2 naam in template context

Placeholder Aliassen:

- [[GezagZin]] - Alias voor [[GezagRegeling]]

Overige Placeholders:

- [[ZorgkortingPercentageAlleKinderen]] - Zorgkortingspercentage dat voor alle kinderen geldt

Aanvullende Beschikbare Velden (met punt notatie):

Je kunt deze ook gebruiken met punten:
- [[Partij1.VolledigeNaam]]
- [[Partij1.Adres]]
- [[Partij2.VolledigeNaam]]
- [[Dossier.DossierNummer]]
- [[Dossier.AangemaaktOp]]
- etc.

Voorbeeld Template Gebruik:

OUDERSCHAPSPLAN

Ondergetekenden:

1. [[Partij1Naam]]
   Geboren: [[Partij1Geboortedatum]]
   Wonende: [[Partij1VolledigAdres]]

2. [[Partij2Naam]]
   Geboren: [[Partij2Geboortedatum]]
   Wonende: [[Partij2VolledigAdres]]

Verklaren het volgende te zijn overeengekomen met betrekking tot [[ons kind/onze kinderen]]:
[[KinderenVolledigeNamen]]

[[ons kind/onze kinderen]] [[verblijft/verblijven]] afwisselend bij beide ouders.

OMGANGSREGELING:
[[TABEL_OMGANG]]

ZORGAFSPRAKEN:
[[TABEL_ZORG]]

FINANCIËLE AFSPRAKEN:
[[TABEL_ALIMENTATIE]]

Voorbeeld Partij Benaming Gebruik:

OUDERSCHAPSPLAN

[[Partij1Benaming]] en [[Partij2Benaming]] zijn overeengekomen dat [[ons kind/onze kinderen]]
afwisselend bij beide ouders [[verblijft/verblijven]].

Bij niet-anoniem dossier wordt dit bijvoorbeeld:
"Jan en Maria zijn overeengekomen dat hun kinderen afwisselend bij beide ouders verblijven."

Bij anoniem dossier wordt dit bijvoorbeeld:
"De vader en de moeder zijn overeengekomen dat hun kinderen afwisselend bij beide ouders verblijven."

Technische Werking Partij Benaming:

- Als IsAnoniem = false (niet anoniem):
  [[Partij1Benaming]] → Roepnaam van Partij 1 (bijv. "Jan")
  [[Partij2Benaming]] → Roepnaam van Partij 2 (bijv. "Maria")

- Als IsAnoniem = true (anoniem):
  [[Partij1Benaming]] → "de vader" (geslacht = M), "de moeder" (geslacht = V), of "de persoon" (onbekend)
  [[Partij2Benaming]] → "de vader" (geslacht = M), "de moeder" (geslacht = V), of "de persoon" (onbekend)

Communicatie Afspraken Placeholders:

Basis Afspraken:
- [[VillaPinedoKinderen]] - Villa Pinedo methode voor kinderen (ruwe database waarde: ja/nee)
- [[VillaPinedoZin]] - Volledige zin over Villa Pinedo op basis van gekozen optie
  * "ja" → "Wij hebben [kindnamen] op de hoogte gebracht van Villa Pinedo, waar [hij/zij] terecht kan met [zijn/haar/hun] vragen, voor het delen van ervaringen, het krijgen van tips en steun om met de scheiding om te gaan."
  * "nee" → "Wij hebben [kindnamen] nog niet op de hoogte gebracht van Villa Pinedo, waar [hij/zij] terecht kan met [zijn/haar/hun] vragen, voor het delen van ervaringen, het krijgen van tips en steun om met de scheiding om te gaan. Als daar aanleiding toe is zullen wij [hen/hem/haar] daar zeker op attenderen."
  * leeg → geen tekst
  * Let op: Persoonlijk voornaamwoord (hij/zij), bezittelijk voornaamwoord (zijn/haar/hun) en lijdend voorwerp (hem/haar/hen) worden automatisch aangepast op basis van geslacht en aantal kinderen.
- [[KinderenBetrokkenheid]] - Betrokkenheid kinderen bij beslissingen (ruwe database waarde)
- [[BetrokkenheidKindZin]] - Volledige zin over betrokkenheid kinderen op basis van gekozen optie
  * "samen" → "Wij hebben samen met [kindnamen] gesproken zodat wij rekening kunnen houden met [zijn/haar/hun] wensen."
  * "los_van_elkaar" → "Wij hebben los van elkaar met [kindnamen] gesproken zodat wij rekening kunnen houden met [zijn/haar/hun] wensen."
  * "jonge_leeftijd" → "[kindnamen] [is/zijn] gezien de jonge leeftijd niet betrokken bij het opstellen van het ouderschapsplan."
  * "niet_betrokken" → "[kindnamen] [is/zijn] niet betrokken bij het opstellen van het ouderschapsplan."
  * Let op: Werkwoorden (is/zijn) en bezittelijk voornaamwoord (zijn/haar/hun) worden automatisch aangepast.
- [[KiesMethode]] - Gekozen methode voor ouderschapsplan
- [[OmgangTekstOfSchema]] - Omgangsregeling als tekst of schema
- [[OmgangsregelingBeschrijving]] - Volledige omgangsregeling tekst op basis van keuze:
  * Tekst: "Wij verdelen de zorg en opvoeding van ons kind / onze kinderen op de volgende manier: [beschrijving]"
  * Beiden: Zoals tekst, plus verwijzing naar bijlage schema
  * Schema/leeg/anders: "Wij verdelen de zorg en opvoeding van ons kind / onze kinderen volgens het vaste schema van bijlage 1." (default)
- [[Opvang]] - Kinderopvang afspraken
- [[OpvangBeschrijving]] - Volledige opvang tekst op basis van keuze:
  * 1: "We blijven ieder zelf verantwoordelijk voor de opvang van onze kinderen op de dagen dat ze volgens het schema bij ieder van ons verblijven."
  * 2: "Als opvang of een afwijking van het schema nodig is, vragen we altijd eerst aan de andere ouder of die beschikbaar is, voordat we anderen vragen voor de opvang van onze kinderen."
- [[InformatieUitwisseling]] - Methode voor informatie-uitwisseling
- [[InformatieUitwisselingBeschrijving]] - Volledige tekst over hoe ouders informatie delen (met roepnamen kinderen):
  * email: "Wij delen de informatie over [roepnamen] met elkaar via de e-mail."
  * telefoon: "Wij delen de informatie over [roepnamen] met elkaar telefonisch."
  * app: "Wij delen de informatie over [roepnamen] met elkaar via een app (zoals WhatsApp)."
  * oudersapp: "Wij delen de informatie over [roepnamen] met elkaar via een speciale ouders-app."
  * persoonlijk: "Wij delen de informatie over [roepnamen] met elkaar in een persoonlijk gesprek."
  * combinatie: "Wij delen de informatie over [roepnamen] met elkaar via een combinatie van methoden."
- [[BijlageBeslissingen]] - Bijlage voor belangrijke beslissingen
- [[IdBewijzen]] - Beheer van identiteitsbewijzen (ruwe waarde)
- [[IdBewijzenBeschrijving]] - Volledige zin over wie de identiteitsbewijzen bewaart:
  * ouder_1/partij1: "De identiteitsbewijzen van [kinderen] worden bewaard door [Ouder 1 naam]."
  * ouder_2/partij2: "De identiteitsbewijzen van [kinderen] worden bewaard door [Ouder 2 naam]."
  * beide_ouders/beiden: "De identiteitsbewijzen van [kinderen] worden bewaard door beide ouders."
  * kinderen_zelf/kinderen: "[Kind] bewaart zijn/haar eigen identiteitsbewijs." of "[Kinderen] bewaren hun eigen identiteitsbewijs."
  * nvt/niet_van_toepassing: "Niet van toepassing."
- [[Aansprakelijkheidsverzekering]] - Beheer aansprakelijkheidsverzekering (ruwe waarde)
- [[AansprakelijkheidsverzekeringBeschrijving]] - Volledige zin over aansprakelijkheidsverzekering:
  * beiden/beide_ouders: "Wij zorgen ervoor dat [kinderen] bij ons beiden tegen wettelijke aansprakelijkheid zijn verzekerd."
  * ouder_1/partij1: "[Ouder 1 naam] zorgt ervoor dat [kinderen] tegen wettelijke aansprakelijkheid zijn verzekerd."
  * ouder_2/partij2: "[Ouder 2 naam] zorgt ervoor dat [kinderen] tegen wettelijke aansprakelijkheid zijn verzekerd."
  * nvt/niet_van_toepassing: "Niet van toepassing."
- [[Ziektekostenverzekering]] - Beheer ziektekostenverzekering (ruwe waarde)
- [[ZiektekostenverzekeringBeschrijving]] - Volledige zin over ziektekostenverzekering:
  * ouder_1/partij1: "[Kinderen] zijn verzekerd op de ziektekostenverzekering van [Ouder 1 naam]."
  * ouder_2/partij2: "[Kinderen] zijn verzekerd op de ziektekostenverzekering van [Ouder 2 naam]."
  * hoofdverblijf: "[Kinderen] zijn verzekerd op de ziektekostenverzekering van de ouder waar zij hun hoofdverblijf hebben."
  * nvt/niet_van_toepassing: "Niet van toepassing."
- [[ToestemmingReizen]] - Afspraken over toestemming reizen (ruwe waarde)
- [[ToestemmingReizenBeschrijving]] - Volledige zin over toestemming voor reizen:
  * altijd_overleggen/altijd: "Voor reizen met [kinderen] is altijd vooraf overleg tussen de ouders vereist."
  * eu_vrij: "Met [kinderen] mag binnen de EU vrij worden gereisd. Voor reizen buiten de EU is vooraf overleg tussen de ouders vereist."
  * vrij: "Met [kinderen] mag vrij worden gereisd zonder vooraf overleg."
  * schriftelijk: "Voor reizen met [kinderen] is schriftelijke toestemming van de andere ouder vereist."
  * nvt: leeg (geen tekst)
- [[Jongmeerderjarige]] - Afspraken voor jongvolwassenen (18+)
- [[Studiekosten]] - Afspraken over studiekosten
- [[Evaluatie]] - Frequentie evaluatie afspraken
- [[ParentingCoordinator]] - Inzet parenting coordinator
- [[MediationClausule]] - Mediation clausule

Social Media Afspraken:
- [[SocialMedia]] - Volledige waarde (bijv. "wel_13" of "geen")
- [[SocialMediaKeuze]] - Alleen de keuze (wel/geen/bepaalde_leeftijd/afspraken_later)
- [[SocialMediaLeeftijd]] - Alleen de leeftijd (geëxtraheerd uit "wel_13")
- [[SocialMediaBeschrijving]] - Volledige zin over social media (met roepnamen kinderen):
  * geen: "Wij spreken als ouders af dat [kinderen] geen social media mogen gebruiken."
  * wel: "Wij spreken als ouders af dat [kinderen] social media mogen gebruiken, op voorwaarde dat het op een veilige manier gebeurt."
  * wel_13: "Wij spreken als ouders af dat [kinderen] social media mogen gebruiken vanaf hun 13e jaar, op voorwaarde dat het op een veilige manier gebeurt."
  * later: "Wij maken als ouders later afspraken over het gebruik van social media door [kinderen]."

Voorbeeld Social Media Gebruik:
Als SocialMedia = "wel_13":
  [[SocialMedia]] → "wel_13"
  [[SocialMediaKeuze]] → "wel"
  [[SocialMediaLeeftijd]] → "13"

Als SocialMedia = "geen":
  [[SocialMedia]] → "geen"
  [[SocialMediaKeuze]] → "geen"
  [[SocialMediaLeeftijd]] → ""

Device Afspraken (Leeftijdsgrenzen):
- [[MobielTablet]] - Geformatteerde lijst van alle devices (met leeftijden)
- [[DeviceSmartphone]] - Leeftijd voor smartphone (bijv. "12")
- [[DeviceTablet]] - Leeftijd voor tablet (bijv. "14")
- [[DeviceSmartwatch]] - Leeftijd voor smartwatch (bijv. "13")
- [[DeviceLaptop]] - Leeftijd voor laptop (bijv. "16")
- [[DevicesBeschrijving]] - Volledige zinnen over devices met roepnamen kinderen:
  * Voorbeeld: "Jan en Lisa krijgen een smartphone vanaf hun 12e jaar."
  * Per device (smartphone, tablet, smartwatch, laptop) een aparte zin

Voorbeeld Device Gebruik:
Als MobielTablet JSON = {"smartphone":12,"tablet":14} en kinderen = Jan en Lisa:
  [[MobielTablet]] → "- Smartphone: 12 jaar\n- Tablet: 14 jaar"
  [[DeviceSmartphone]] → "12"
  [[DeviceTablet]] → "14"
  [[DeviceSmartwatch]] → ""
  [[DeviceLaptop]] → ""
  [[DevicesBeschrijving]] → "Jan en Lisa krijgen een smartphone vanaf hun 12e jaar.\nJan en Lisa krijgen een tablet vanaf hun 14e jaar."

Toezicht Apps (Ouderlijk Toezicht):
- [[ToezichtApps]] - Keuze voor toezicht apps (wel/geen)
- [[ToezichtAppsBeschrijving]] - Volledige zin over toezicht apps:
  * wel: "We spreken als ouders af wel ouderlijk toezichtapps te gebruiken."
  * geen: "We spreken als ouders af geen ouderlijk toezichtapps te gebruiken."

Voorbeeld Toezicht Apps Gebruik:
Als ToezichtApps = "wel":
  [[ToezichtApps]] → "wel"
  [[ToezichtAppsBeschrijving]] → "We spreken als ouders af wel ouderlijk toezichtapps te gebruiken."

Als ToezichtApps = "geen":
  [[ToezichtApps]] → "geen"
  [[ToezichtAppsBeschrijving]] → "We spreken als ouders af geen ouderlijk toezichtapps te gebruiken."

Locatie Delen (Location Sharing):
- [[LocatieDelen]] - Keuze voor locatie delen (wel/geen)
- [[LocatieDelenBeschrijving]] - Volledige zin over locatie delen:
  * wel: "Wij spreken als ouders af om de locatie van onze kinderen wel te delen via digitale apparaten."
  * geen: "Wij spreken als ouders af om de locatie van onze kinderen niet te delen via digitale apparaten."

Voorbeeld Locatie Delen Gebruik:
Als LocatieDelen = "wel":
  [[LocatieDelen]] → "wel"
  [[LocatieDelenBeschrijving]] → "Wij spreken als ouders af om de locatie van onze kinderen wel te delen via digitale apparaten."

Als LocatieDelen = "geen":
  [[LocatieDelen]] → "geen"
  [[LocatieDelenBeschrijving]] → "Wij spreken als ouders af om de locatie van onze kinderen niet te delen via digitale apparaten."

Bankrekeningen voor Kinderen:
- [[BankrekeningKinderen]] - Geformatteerde lijst van alle bankrekeningen
- [[BankrekeningenCount]] - Aantal bankrekeningen (bijv. "2")
- [[Bankrekening1IBAN]] - IBAN van rekening 1 (geformatteerd met spaties)
- [[Bankrekening1Tenaamstelling]] - Tenaamstelling rekening 1 (vertaald naar leesbare tekst)
- [[Bankrekening1BankNaam]] - Banknaam rekening 1
- [[Bankrekening2IBAN]] - IBAN van rekening 2
- [[Bankrekening2Tenaamstelling]] - Tenaamstelling rekening 2
- [[Bankrekening2BankNaam]] - Banknaam rekening 2
- (etc. voor meer rekeningen)

Voorbeeld Bankrekeningen Gebruik:
Als BankrekeningKinderen JSON = [
  {"iban":"NL91ABNA0417164300","tenaamstelling":"ouder_1","bankNaam":"ABN AMRO"},
  {"iban":"NL89RABO0300065264","tenaamstelling":"kind_123","bankNaam":"Rabobank"}
]:

[[BankrekeningenCount]] → "2"
[[BankrekeningKinderen]] →
  "Rekening 1:
    IBAN: NL91 ABNA 0417 1643 00
    Bank: ABN AMRO
    Ten name van: Op naam van Jan

  Rekening 2:
    IBAN: NL89 RABO 0300 0652 64
    Bank: Rabobank
    Ten name van: Op naam van Emma"

[[Bankrekening1IBAN]] → "NL91 ABNA 0417 1643 00"
[[Bankrekening1Tenaamstelling]] → "Op naam van Jan"
[[Bankrekening1BankNaam]] → "ABN AMRO"

Tenaamstelling Codes en Vertaling:
- "ouder_1" → "Op naam van [Partij1 Roepnaam]"
- "ouder_2" → "Op naam van [Partij2 Roepnaam]"
- "ouders_gezamenlijk" → "Op gezamenlijke naam van [Partij1] en [Partij2]"
- "kind_123" → "Op naam van [Kind met ID 123 Roepnaam]"
- "kinderen_alle" → "Op naam van [alle minderjarige kinderen met roepnamen]"

IBAN Formatting:
IBANs worden automatisch geformatteerd met spaties elke 4 karakters:
- Database: "NL91ABNA0417164300"
- Placeholder: "NL91 ABNA 0417 1643 00"

Voorbeeld Template Gebruik Communicatie Afspraken:

COMMUNICATIEAFSPRAKEN

Social Media:
De ouders zijn overeengekomen dat de kinderen [[SocialMediaKeuze]] social media mogen gebruiken
[[#if SocialMediaLeeftijd]]vanaf [[SocialMediaLeeftijd]] jaar[[/if]].

Devices:
[[MobielTablet]]

Bankrekeningen:
Er zijn [[BankrekeningenCount]] bankrekening(en) voor de kinderen aangemaakt:

[[BankrekeningKinderen]]

Verzekeringen:
- Aansprakelijkheidsverzekering: [[Aansprakelijkheidsverzekering]]
- Ziektekostenverzekering: [[Ziektekostenverzekering]]

Evaluatie:
De afspraken worden [[Evaluatie]] geëvalueerd.
