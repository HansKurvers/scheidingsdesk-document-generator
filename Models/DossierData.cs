using System;

namespace scheidingsdesk_document_generator.Models
{
    public class DossierData
    {
        /// <summary>
        /// Basic dossier information
        /// </summary>
        public int Id { get; set; }
        public string DossierNummer { get; set; } = string.Empty;
        public DateTime AangemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }
        public string Status { get; set; } = string.Empty;
        public int GebruikerId { get; set; }
        public bool? IsAnoniem { get; set; }

        /// <summary>
        /// Parties involved in the dossier (rol_id 1 and 2)
        /// </summary>
        public List<PersonData> Partijen { get; set; } = new List<PersonData>();

        /// <summary>
        /// All children associated with this dossier
        /// </summary>
        public List<ChildData> Kinderen { get; set; } = new List<ChildData>();

        /// <summary>
        /// Visitation/contact arrangements
        /// </summary>
        public List<OmgangData> Omgang { get; set; } = new List<OmgangData>();

        /// <summary>
        /// Care arrangements and agreements
        /// </summary>
        public List<ZorgData> Zorg { get; set; } = new List<ZorgData>();

        /// <summary>
        /// Alimentatie (alimony) information
        /// </summary>
        public AlimentatieData? Alimentatie { get; set; }

        /// <summary>
        /// Ouderschapsplan specific information
        /// </summary>
        public OuderschapsplanInfoData? OuderschapsplanInfo { get; set; }

        /// <summary>
        /// Gets party 1 (rol_id = 1)
        /// </summary>
        public PersonData? Partij1 => Partijen.FirstOrDefault(p => p.RolId == 1);

        /// <summary>
        /// Gets party 2 (rol_id = 2)
        /// </summary>
        public PersonData? Partij2 => Partijen.FirstOrDefault(p => p.RolId == 2);
    }

    public class OmgangData
    {
        public int Id { get; set; }
        public int DagId { get; set; }
        public string? DagNaam { get; set; }
        public int DagdeelId { get; set; }
        public string? DagdeelNaam { get; set; }
        public int VerzorgerId { get; set; }
        public string? VerzorgerNaam { get; set; }
        public string? WisselTijd { get; set; }
        public int WeekRegelingId { get; set; }
        public string? WeekRegelingOmschrijving { get; set; }
        public string? WeekRegelingAnders { get; set; }
        public int DossierId { get; set; }
        public DateTime AangemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }

        /// <summary>
        /// Gets the effective week arrangement (custom override or standard description)
        /// </summary>
        public string EffectieveRegeling => !string.IsNullOrEmpty(WeekRegelingAnders) ? WeekRegelingAnders : WeekRegelingOmschrijving ?? string.Empty;
    }

    public class ZorgData
    {
        public int Id { get; set; }
        public int ZorgCategorieId { get; set; }
        public string? ZorgCategorieNaam { get; set; }
        public int ZorgSituatieId { get; set; }
        public string? ZorgSituatieNaam { get; set; }
        public string Overeenkomst { get; set; } = string.Empty;
        public string? SituatieAnders { get; set; }
        public int DossierId { get; set; }
        public DateTime AangemaaktOp { get; set; }
        public int AangemaaktDoor { get; set; }
        public DateTime GewijzigdOp { get; set; }
        public int? GewijzigdDoor { get; set; }

        /// <summary>
        /// Gets the effective situation (custom override or standard situation)
        /// </summary>
        public string EffectieveSituatie => !string.IsNullOrEmpty(SituatieAnders) ? SituatieAnders : ZorgSituatieNaam ?? string.Empty;
    }

    public class AlimentatieData
    {
        public int Id { get; set; }
        public int DossierId { get; set; }
        public decimal? NettoBesteedbaarGezinsinkomen { get; set; }
        public decimal? KostenKinderen { get; set; }
        public decimal? BijdrageKostenKinderen { get; set; }
        public int? BijdrageTemplate { get; set; }
        public string? BijdrageTemplateOmschrijving { get; set; }

        /// <summary>
        /// Kinderrekening velden (voor alle kinderen samen)
        /// </summary>
        public decimal? StortingOuder1Kinderrekening { get; set; }
        public decimal? StortingOuder2Kinderrekening { get; set; }
        public List<string> KinderrekeningKostensoorten { get; set; } = new List<string>();
        public bool? KinderrekeningMaximumOpname { get; set; }
        public decimal? KinderrekeningMaximumOpnameBedrag { get; set; }
        public bool? KinderbijslagStortenOpKinderrekening { get; set; }
        public bool? KindgebondenBudgetStortenOpKinderrekening { get; set; }

        /// <summary>
        /// Alimentatie settings
        /// </summary>
        public bool? BedragenAlleKinderenGelijk { get; set; }
        public decimal? AlimentatiebedragPerKind { get; set; }
        public string? Alimentatiegerechtigde { get; set; }
        public decimal? ZorgkortingPercentageAlleKinderen { get; set; }

        /// <summary>
        /// Sync settings for all children
        /// </summary>
        public bool? AfsprakenAlleKinderenGelijk { get; set; }
        public string? HoofdverblijfAlleKinderen { get; set; }
        public string? InschrijvingAlleKinderen { get; set; }
        public string? KinderbijslagOntvangerAlleKinderen { get; set; }
        public string? KindgebondenBudgetAlleKinderen { get; set; }

        /// <summary>
        /// Child cost contributions per person
        /// </summary>
        public List<BijdrageKostenKinderenData> BijdragenKostenKinderen { get; set; } = new List<BijdrageKostenKinderenData>();

        /// <summary>
        /// Financial agreements per child
        /// </summary>
        public List<FinancieleAfsprakenKinderenData> FinancieleAfsprakenKinderen { get; set; } = new List<FinancieleAfsprakenKinderenData>();

        /// <summary>
        /// Detects if the current betaalwijze is a kinderrekening type
        /// </summary>
        public bool IsKinderrekeningBetaalwijze =>
            BijdrageTemplateOmschrijving?.ToLowerInvariant().Contains("kinderrekening") ?? false;

        /// <summary>
        /// Detects if the current betaalwijze is an alimentatieplichtige type
        /// </summary>
        public bool IsAlimentatieplichtBetaalwijze =>
            BijdrageTemplateOmschrijving?.ToLowerInvariant().Contains("alimentatieplichtige") ?? false;
    }

    public class BijdrageKostenKinderenData
    {
        public int Id { get; set; }
        public int AlimentatieId { get; set; }
        public int PersonenId { get; set; }
        public string? PersoonNaam { get; set; }
        public decimal? EigenAandeel { get; set; }
    }

    public class FinancieleAfsprakenKinderenData
    {
        public int Id { get; set; }
        public int AlimentatieId { get; set; }
        public int KindId { get; set; }
        public string? KindNaam { get; set; }
        public decimal? AlimentatieBedrag { get; set; }
        public string? Hoofdverblijf { get; set; }
        public string? KinderbijslagOntvanger { get; set; }
        public decimal? ZorgkortingPercentage { get; set; }
        public string? Inschrijving { get; set; }
        public string? KindgebondenBudget { get; set; }
    }

    public class RegelingTemplate
    {
        public int Id { get; set; }
        public string TemplateNaam { get; set; } = string.Empty;
        public string TemplateTekst { get; set; } = string.Empty;
        public bool MeervoudKinderen { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}