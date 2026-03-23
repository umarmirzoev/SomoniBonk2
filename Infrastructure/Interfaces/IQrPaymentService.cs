using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IQrPaymentService
{
    Task<Response<QrPaymentGetDto>> GenerateQrAsync(Guid userId, GenerateQrDto dto);
    Task<Response<QrPaymentGetDto>> PayByQrAsync(Guid userId, PayByQrDto dto);
    Task<Response<QrPaymentGetDto>> GetQrStatusAsync(string qrCode);
}
