namespace SomoniBank.Domain.DTOs;

public class BillCategoryGetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
}

public class BillProviderGetDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? LogoUrl { get; set; }
}

public class BillPaymentInsertDto
{
    public Guid AccountId { get; set; }
    public Guid ProviderId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string? Description { get; set; }
}

public class BillPaymentGetDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
