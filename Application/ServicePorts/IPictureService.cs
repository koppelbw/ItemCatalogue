using Application.DTOs;
using Domain.Enums;

namespace Application.ServicePorts;

public interface IPictureService
{
    Task<PictureResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<PictureResponse>> GetForOwnerAsync(
        PictureOwnerType ownerType, int ownerId, PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<PictureResponse> UploadAsync(UploadPictureRequest request, CancellationToken cancellationToken = default);

    Task<PictureResponse> UpdateAsync(UpdatePictureRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
