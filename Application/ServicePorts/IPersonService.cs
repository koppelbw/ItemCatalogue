using Application.DTOs;

namespace Application.ServicePorts;

public interface IPersonService
{
    Task<PersonResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PersonResponse> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default);

    Task<PersonResponse> UpdateAsync(UpdatePersonRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
