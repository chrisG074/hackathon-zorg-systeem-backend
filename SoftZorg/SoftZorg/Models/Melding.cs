using System;
using System.ComponentModel.DataAnnotations;

namespace SoftZorg.Models
{
    public class Melding
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty; // "Facilitair", "MIC", of "MIM"

        [Required]
        public string Categorie { get; set; } = string.Empty; // Bijv. "Valincident", "Kapot bed"

        [Required]
        public string Beschrijving { get; set; } = string.Empty; // Wat is de storing / Wat is er gebeurd?

        public string? Betrokkene { get; set; } // Om welke zorgvrager/medewerker/kamer gaat het?

        public DateTime Datum { get; set; } = DateTime.Now;

        // --- Specifieke velden (Optioneel, vandaar de '?') ---
        public string? Locatie { get; set; } // Waar vond het incident plaats?
        public bool? IsSpoed { get; set; } // Voor facilitair
        public string? Letsel { get; set; } // Voor MIC en MIM
        public bool? BehoefteAanGesprek { get; set; } // Voor MIM
        public string? Oplossing { get; set; } // Voor P (Plan) in de SOEP methode
    }
}