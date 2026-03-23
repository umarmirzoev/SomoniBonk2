namespace SomoniBank.Domain.Filtres;

public class BeneficiaryFilter
{
    public bool? IsFavorite { get; set; }
    public string? TransferType { get; set; }
    public string? Search { get; set; }
}
