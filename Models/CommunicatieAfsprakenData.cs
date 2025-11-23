using System;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Models
{
    /// <summary>
    /// Communication agreements (Communicatie Afspraken)
    /// </summary>
    public class CommunicatieAfsprakenData
    {
        public int Id { get; set; }
        public int DossierId { get; set; }

        /// <summary>
        /// Villa Pinedo method for children
        /// </summary>
        public string? VillaPinedoKinderen { get; set; }

        /// <summary>
        /// Children involvement in decision making
        /// </summary>
        public string? KinderenBetrokkenheid { get; set; }

        /// <summary>
        /// Chosen method for parenting plan
        /// </summary>
        public string? KiesMethode { get; set; }

        /// <summary>
        /// Agreement format: text or schema
        /// </summary>
        public string? OmgangTekstOfSchema { get; set; }

        /// <summary>
        /// Childcare arrangements
        /// </summary>
        public string? Opvang { get; set; }

        /// <summary>
        /// Information exchange method
        /// </summary>
        public string? InformatieUitwisseling { get; set; }

        /// <summary>
        /// Important decisions attachment
        /// </summary>
        public string? BijlageBeslissingen { get; set; }

        /// <summary>
        /// Social media usage - can contain age: "wel_13" or simple choice: "geen"
        /// </summary>
        public string? SocialMedia { get; set; }

        /// <summary>
        /// Device age restrictions - JSON object with device:age pairs
        /// Format: {"smartphone":12,"tablet":14,"smartwatch":13,"laptop":16}
        /// </summary>
        public string? MobielTablet { get; set; }

        /// <summary>
        /// ID documents management
        /// </summary>
        public string? IdBewijzen { get; set; }

        /// <summary>
        /// Liability insurance management
        /// </summary>
        public string? Aansprakelijkheidsverzekering { get; set; }

        /// <summary>
        /// Health insurance management
        /// </summary>
        public string? Ziektekostenverzekering { get; set; }

        /// <summary>
        /// Permission for travel
        /// </summary>
        public string? ToestemmingReizen { get; set; }

        /// <summary>
        /// Young adult (18+) arrangements
        /// </summary>
        public string? Jongmeerderjarige { get; set; }

        /// <summary>
        /// Study costs arrangements
        /// </summary>
        public string? Studiekosten { get; set; }

        /// <summary>
        /// Bank accounts for children - JSON array of Kinderrekening objects
        /// Format: [{"iban":"NL91ABNA0417164300","tenaamstelling":"ouder_1","bankNaam":"ABN AMRO"}]
        /// </summary>
        public string? BankrekeningKinderen { get; set; }

        /// <summary>
        /// Evaluation frequency
        /// </summary>
        public string? Evaluatie { get; set; }

        /// <summary>
        /// Parenting coordinator
        /// </summary>
        public string? ParentingCoordinator { get; set; }

        /// <summary>
        /// Mediation clause
        /// </summary>
        public string? MediationClausule { get; set; }

        public DateTime? AangemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
    }

    /// <summary>
    /// Bank account for a child (Kinderrekening)
    /// </summary>
    public class Kinderrekening
    {
        /// <summary>
        /// IBAN number (stored without spaces, formatted with spaces for display)
        /// </summary>
        public string Iban { get; set; } = string.Empty;

        /// <summary>
        /// Account holder code - needs translation to readable text
        /// Possible values: "ouder_1", "ouder_2", "ouders_gezamenlijk", "kind_123", "kinderen_alle"
        /// </summary>
        public string Tenaamstelling { get; set; } = string.Empty;

        /// <summary>
        /// Bank name
        /// </summary>
        public string BankNaam { get; set; } = string.Empty;
    }

    /// <summary>
    /// Device age restrictions
    /// </summary>
    public class DeviceAfspraken
    {
        /// <summary>
        /// Age restriction for smartphone (6-18 years, null if not set)
        /// </summary>
        public int? Smartphone { get; set; }

        /// <summary>
        /// Age restriction for tablet (6-18 years, null if not set)
        /// </summary>
        public int? Tablet { get; set; }

        /// <summary>
        /// Age restriction for smartwatch (6-18 years, null if not set)
        /// </summary>
        public int? Smartwatch { get; set; }

        /// <summary>
        /// Age restriction for laptop (6-18 years, null if not set)
        /// </summary>
        public int? Laptop { get; set; }
    }
}
