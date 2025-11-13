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
- [[Partij1Adres]] - Straatnaam en huisnummer
- [[Partij1Postcode]] - Postcode
- [[Partij1Plaats]] - Woonplaats
- [[Partij1Geboorteplaats]] - Geboorteplaats
- [[Partij1Telefoon]] - Telefoonnummer
- [[Partij1Email]] - E-mailadres
- [[Partij1Geboortedatum]] - Geboortedatum (d MMMM yyyy, bijv. "15 januari 2024")
- [[Partij1VolledigAdres]] - Volledig adres (straat, postcode, plaats)
- [[Partij1Benaming]] - Contextafhankelijke benaming (roepnaam of "de vader"/"de moeder" bij anoniem)

Partij 2 Informatie:

- [[Partij2Naam]] - Volledige naam
- [[Partij2Voornaam]] - Voornaam/voornamen
- [[Partij2Roepnaam]] - Roepnaam
- [[Partij2Achternaam]] - Achternaam
- [[Partij2Tussenvoegsel]] - Tussenvoegsel
- [[Partij2VolledigeNaamMetTussenvoegsel]] - Voornamen + tussenvoegsel + achternaam (bijv. "Maria van der Berg")
- [[Partij2VolledigeAchternaam]] - Tussenvoegsel + achternaam (bijv. "van der Berg")
- [[Partij2Adres]] - Straatnaam en huisnummer
- [[Partij2Postcode]] - Postcode
- [[Partij2Plaats]] - Woonplaats
- [[Partij2Geboorteplaats]] - Geboorteplaats
- [[Partij2Telefoon]] - Telefoonnummer
- [[Partij2Email]] - E-mailadres
- [[Partij2Geboortedatum]] - Geboortedatum (d MMMM yyyy, bijv. "15 januari 2024")
- [[Partij2VolledigAdres]] - Volledig adres
- [[Partij2Benaming]] - Contextafhankelijke benaming (roepnaam of "de vader"/"de moeder" bij anoniem)

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
- [[Kiesplan]] - Kiesplan
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

Opmerking: Alle alimentatie gegevens worden automatisch opgehaald uit de database tabellen:
- dbo.alimentaties (algemene alimentatie gegevens, inclusief nieuwe kinderrekening velden)
- dbo.bijdrage_kosten_kinderen (bijdragen per persoon)
- dbo.financiele_afspraken_kinderen (financiële afspraken per kind)
- dbo.bijdrage_templates (omschrijvingen bijdrage templates)

Grammatica Regels (Nederlands enkelvoud/meervoud):

Opmerking: Gebruik de volledige vorm in je template (bijv. [[heeft/hebben]], niet [[meervoud heeft/hebben]])

- [[ons kind/onze kinderen]] - "onze kinderen" (meervoud) of "ons kind" (enkelvoud)
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
