using BB.Common;

namespace FormManagement.Application.Services;

public interface IMetadataService
{
    Task<Result<IReadOnlyList<MetadataDto>>> SearchAsync(string? keyword, string? group, CancellationToken ct = default);
    Task<Result<MetadataDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<MetadataDto>> CreateAsync(CreateMetadataRequest request, CancellationToken ct = default);
    Task<Result<MetadataDto>> UpdateAsync(Guid id, UpdateMetadataRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
