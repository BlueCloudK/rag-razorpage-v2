namespace ServiceLayer.Models
{
    public sealed class DocumentChunkingOptions
    {
        public const string Balanced = "balanced";
        public const string Precise = "precise";
        public const string WideContext = "wide-context";
        public const string Custom = "custom";

        public string Profile { get; init; } = Balanced;
        public int ChunkSize { get; init; } = 850;
        public int ChunkOverlap { get; init; } = 120;

        public static bool TryCreate(string? profile, int? chunkSize, int? chunkOverlap, out DocumentChunkingOptions options, out string error)
        {
            var normalizedProfile = (profile ?? Balanced).Trim().ToLowerInvariant();
            options = normalizedProfile switch
            {
                Precise => new DocumentChunkingOptions { Profile = Precise, ChunkSize = 550, ChunkOverlap = 90 },
                WideContext => new DocumentChunkingOptions { Profile = WideContext, ChunkSize = 1250, ChunkOverlap = 180 },
                Custom => new DocumentChunkingOptions
                {
                    Profile = Custom,
                    ChunkSize = chunkSize ?? 0,
                    ChunkOverlap = chunkOverlap ?? 0
                },
                _ => new DocumentChunkingOptions { Profile = Balanced, ChunkSize = 850, ChunkOverlap = 120 }
            };

            if (normalizedProfile is not (Balanced or Precise or WideContext or Custom))
            {
                error = "The selected chunking profile is not supported.";
                return false;
            }

            if (options.ChunkSize is < 300 or > 1800)
            {
                error = "Chunk size must be between 300 and 1800 characters.";
                return false;
            }

            var maximumOverlap = Math.Min(400, options.ChunkSize - 80);
            if (options.ChunkOverlap < 0 || options.ChunkOverlap > maximumOverlap)
            {
                error = $"Chunk overlap must be between 0 and {maximumOverlap} characters for this chunk size.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
