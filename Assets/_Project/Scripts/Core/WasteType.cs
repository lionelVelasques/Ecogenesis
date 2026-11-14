namespace EcoGenesis.Core
{
    /// <summary>Categorías de residuos del juego (GDD: Recolección y Clasificación).</summary>
    public enum WasteType
    {
        Organic,
        Plastic,
        Paper,
        Glass,
        Hazardous
    }

    public static class WasteTypeExtensions
    {
        /// <summary>Nombre corto para UI ("Plástico").</summary>
        public static string DisplayName(this WasteType type)
        {
            switch (type)
            {
                case WasteType.Organic: return "Orgánico";
                case WasteType.Plastic: return "Plástico";
                case WasteType.Paper: return "Papel y cartón";
                case WasteType.Glass: return "Vidrio";
                case WasteType.Hazardous: return "Peligroso";
                default: return type.ToString();
            }
        }

        /// <summary>Frase en plural para describir objetivos ("plásticos").</summary>
        public static string PluralName(this WasteType type)
        {
            switch (type)
            {
                case WasteType.Organic: return "residuos orgánicos";
                case WasteType.Plastic: return "plásticos";
                case WasteType.Paper: return "papeles y cartones";
                case WasteType.Glass: return "vidrios";
                case WasteType.Hazardous: return "residuos peligrosos";
                default: return type.ToString();
            }
        }
    }
}
