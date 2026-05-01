using BB.Common;

namespace CustomField.Application;

public interface ICustomFieldService
{
    Task<Result<CustomFieldDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CustomFieldDto>> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CustomFieldDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<CustomFieldDto>>> ResolveForAsync(Guid projectId, Guid issueTypeId, CancellationToken ct = default);

    Task<Result<CustomFieldDto>> CreateAsync(CreateCustomFieldRequest request, CancellationToken ct = default);
    Task<Result<CustomFieldDto>> UpdateAsync(Guid id, UpdateCustomFieldRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<Result<CustomFieldDto>> AddOptionAsync(Guid id, AddOptionRequest request, CancellationToken ct = default);
    Task<Result<CustomFieldDto>> UpdateOptionAsync(Guid id, Guid optionId, UpdateOptionRequest request, CancellationToken ct = default);
    Task<Result<CustomFieldDto>> RemoveOptionAsync(Guid id, Guid optionId, CancellationToken ct = default);

    Task<Result<CustomFieldDto>> AddContextAsync(Guid id, AddContextRequest request, CancellationToken ct = default);
    Task<Result<CustomFieldDto>> RemoveContextAsync(Guid id, Guid contextId, CancellationToken ct = default);
}
