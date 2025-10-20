namespace scheidingsdesk_document_generator.Models
{
    /// <summary>
    /// Represents information specific to the ouderschapsplan (parenting plan)
    /// </summary>
    public class OuderschapsplanInfoData
    {
        public int Id { get; set; }
        public int DossierId { get; set; }
        public int Partij1PersoonId { get; set; }
        public int Partij2PersoonId { get; set; }
        
        // Relationship information
        public string? SoortRelatie { get; set; }
        public DateTime? DatumAanvangRelatie { get; set; }
        public string? SoortRelatieVerbreking { get; set; }
        public string? BetrokkenheidKind { get; set; }
        public string? Kiesplan { get; set; }
        
        // Party choices (1 = partij_1, 2 = partij_2, 3 = kinderrekening for kinderbijslag)
        public int? GezagPartij { get; set; }
        public int? WaOpNaamVanPartij { get; set; }
        public string? KeuzeDevices { get; set; }
        public int? ZorgverzekeringOpNaamVanPartij { get; set; }
        public int? KinderbijslagPartij { get; set; }
        
        // JSON fields for BRP and KGB data
        public string? BrpPartij1 { get; set; }
        public string? BrpPartij2 { get; set; }
        public string? KgbPartij1 { get; set; }
        public string? KgbPartij2 { get; set; }
        
        // Additional information
        public string? Hoofdverblijf { get; set; }
        public string? Zorgverdeling { get; set; }
        public string? OpvangKinderen { get; set; }
        public string? BankrekeningnummersOpNaamVanKind { get; set; }
        public string? ParentingCoordinator { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Gets the display name for the party that has gezag (custody)
        /// </summary>
        public string GezagPartijNaam => GezagPartij switch
        {
            1 => "Partij 1",
            2 => "Partij 2",
            _ => "Onbekend"
        };
        
        /// <summary>
        /// Gets the display name for the party that has WA insurance
        /// </summary>
        public string WaPartijNaam => WaOpNaamVanPartij switch
        {
            1 => "Partij 1",
            2 => "Partij 2",
            _ => "Onbekend"
        };
        
        /// <summary>
        /// Gets the display name for the party that has health insurance
        /// </summary>
        public string ZorgverzekeringPartijNaam => ZorgverzekeringOpNaamVanPartij switch
        {
            1 => "Partij 1",
            2 => "Partij 2",
            _ => "Onbekend"
        };
        
        /// <summary>
        /// Gets the display name for who receives child benefit
        /// </summary>
        public string KinderbijslagPartijNaam => KinderbijslagPartij switch
        {
            1 => "Partij 1",
            2 => "Partij 2",
            3 => "Kinderrekening",
            _ => "Onbekend"
        };
    }
}