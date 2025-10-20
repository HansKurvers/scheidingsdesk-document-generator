Tekst Placeholders (Gebruik elk formaat: [[Variable]], {Variable}, [Variable], <<Variable>>)

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
- [[Partij1Benaming]] - Contextafhankelijke benaming (roepnaam of "de man"/"de vrouw" bij anoniem)

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
- [[Partij2Benaming]] - Contextafhankelijke benaming (roepnaam of "de man"/"de vrouw" bij anoniem)

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
- [[Kind1Leeftijd]] - Kind 1 leeftijd
- [[Kind1Geslacht]] - Kind 1 geslacht

(Zelfde patroon voor [[Kind2Naam]], [[Kind3Naam]], etc.)

Ouderschapsplan Informatie:

Relatie & Juridisch:
- [[SoortRelatie]] - Soort relatie (bijv. "gehuwd", "geregistreerd_partnerschap", "samenwonend")
- [[DatumAanvangRelatie]] - Datum aanvang huwelijk/relatie (d MMMM yyyy, bijv. "15 januari 2024")
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
  * "gehuwd" → "Wij zijn op [datum] met elkaar getrouwd."
  * "geregistreerd_partnerschap" → "Wij zijn op [datum] met elkaar een geregistreerd partnerschap aangegaan."
  * "samenwonend" → "Wij hebben vanaf [datum] met elkaar samengewoond."
  * "lat_relatie" / "ex_partners" / "anders" → "Wij hebben vanaf [datum] een relatie met elkaar gehad."
- [[OuderschapsplanDoelZin]] - Afgeleide placeholder: zin over het doel van het ouderschapsplan
  * "gehuwd" → "In dit ouderschapsplan hebben we afspraken gemaakt over [ons kind/onze kinderen] omdat we gaan scheiden."
  * "geregistreerd_partnerschap" → "In dit ouderschapsplan hebben we afspraken gemaakt over [ons kind/onze kinderen] omdat we ons geregistreerd partnerschap willen laten ontbinden."
  * "samenwonend" → "In dit ouderschapsplan hebben we afspraken gemaakt over [ons kind/onze kinderen] omdat we onze samenleving willen beëindigen."
  * "lat_relatie" / "ex_partners" / "anders" → "In dit ouderschapsplan hebben we afspraken gemaakt over [ons kind/onze kinderen]."
- [[BetrokkenheidKind]] - Betrokkenheid kind
- [[Kiesplan]] - Kiesplan
- [[GezagPartij]] - Partij met gezag (geeft roepnaam terug)
- [[ParentingCoordinator]] - Parenting coordinator informatie

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
- dbo.alimentaties (algemene alimentatie gegevens)
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
- [[hem/haar/hen]] - "hen" (meervoud), "hem" (mannelijk), "haar" (vrouwelijk)
- [[hij/zij/ze]] - "ze" (meervoud), "hij" (mannelijk), "zij" (vrouwelijk)

Tabel Placeholders (Plaats op een eigen regel in het document):

- [[TABEL_ALIMENTATIE]] - Genereert alimentatie/financiële afspraken tabel
- [[TABEL_OMGANG]] - Genereert omgangsregeling tabel
- [[TABEL_ZORG]] - Genereert zorgafspraken tabel
- [[LIJST_KINDEREN]] - Genereert geformatteerde lijst van kinderen met details

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
"De man en de vrouw zijn overeengekomen dat hun kinderen afwisselend bij beide ouders verblijven."

Technische Werking Partij Benaming:

- Als IsAnoniem = false (niet anoniem):
  [[Partij1Benaming]] → Roepnaam van Partij 1 (bijv. "Jan")
  [[Partij2Benaming]] → Roepnaam van Partij 2 (bijv. "Maria")

- Als IsAnoniem = true (anoniem):
  [[Partij1Benaming]] → "de man" (geslacht = M), "de vrouw" (geslacht = V), of "de persoon" (onbekend)
  [[Partij2Benaming]] → "de man" (geslacht = M), "de vrouw" (geslacht = V), of "de persoon" (onbekend)
