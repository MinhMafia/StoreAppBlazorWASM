namespace StoreApp.Shared
{
    public class ImportResultDTO
    {
        public int TotalRows { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<ImportErrorDTO> Errors { get; set; } = new List<ImportErrorDTO>();
        public bool HasErrors => Errors.Any();
    }
}

