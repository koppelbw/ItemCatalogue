using Application.DTOs;

namespace Application.ServicePorts;

public interface IPersonService
{
    Task<PersonResponse> GetByIdAsync(int id);

    Task<IReadOnlyList<PersonResponse>> GetAllAsync();

    Task<PersonResponse> CreateAsync(CreatePersonRequest request);

    Task<PersonResponse> UpdateAsync(UpdatePersonRequest request);

    Task<int> DeleteAsync(int id);
}
