using Application.DTOs;
using Application.Mapping;
using Domain.Entities;
using Domain.RepositoryPorts;
using FluentValidation;

namespace Application.Services;

// Entities ready to insert plus the per-index errors for the rows that are not.
// Entities.Count + Errors.Count == requests.Count.
public sealed record ItemBulkPrepareResult(IReadOnlyList<Item> Entities, IReadOnlyList<BulkRowError> Errors);

// The validate-and-map half of bulk item creation, shared by both insert paths:
// ItemService.CreateManyAsync (prepare -> InsertRangeAsync) and the import chunk processor
// (prepare -> ImportJobRepository.RecordChunkAsync). It deliberately inserts NOTHING itself —
// the chunk path must write items and the chunk marker in one transaction, so persistence stays
// with the caller.
public sealed class ItemBulkPreparer(
    IValidator<CreateItemRequest> createValidator,
    IRoomRepository roomRepository,
    IContainerRepository containerRepository,
    IPersonRepository personRepository)
{
    public async Task<ItemBulkPrepareResult> PrepareAsync(IReadOnlyList<CreateItemRequest> requests, CancellationToken cancellationToken = default)
    {
        var errorsByIndex = new Dictionary<int, List<string>>();

        // Per-row validation with ValidateAsync (not ValidateAndThrow): one bad row must not
        // reject its neighbors, so failures are accumulated instead of thrown.
        for (var i = 0; i < requests.Count; i++)
        {
            var result = await createValidator.ValidateAsync(requests[i], cancellationToken);
            if (!result.IsValid)
            {
                errorsByIndex[i] = result.Errors.Select(e => e.ErrorMessage).ToList();
            }
        }

        // FK existence pre-check, batched to one query per referenced table. A single-row insert
        // would surface a dangling reference as an unhandled SqlException 547; here it becomes a
        // per-row error and the rest of the batch still inserts.
        var candidates = Enumerable.Range(0, requests.Count).Where(i => !errorsByIndex.ContainsKey(i)).ToList();
        var missingRooms = await MissingIdsAsync(roomRepository, candidates.Where(i => requests[i].RoomId.HasValue).Select(i => requests[i].RoomId!.Value), cancellationToken);
        var missingContainers = await MissingIdsAsync(containerRepository, candidates.Where(i => requests[i].ContainerId.HasValue).Select(i => requests[i].ContainerId!.Value), cancellationToken);
        var missingOwners = await MissingIdsAsync(personRepository, candidates.Where(i => requests[i].OwnerId.HasValue).Select(i => requests[i].OwnerId!.Value), cancellationToken);

        foreach (var i in candidates)
        {
            var request = requests[i];
            var rowErrors = new List<string>();

            if (request.RoomId is int roomId && missingRooms.Contains(roomId))
                rowErrors.Add($"Room {roomId} does not exist.");
            if (request.ContainerId is int containerId && missingContainers.Contains(containerId))
                rowErrors.Add($"Container {containerId} does not exist.");
            if (request.OwnerId is int ownerId && missingOwners.Contains(ownerId))
                rowErrors.Add($"Person {ownerId} does not exist.");

            if (rowErrors.Count > 0)
            {
                errorsByIndex[i] = rowErrors;
            }
        }

        var entities = Enumerable.Range(0, requests.Count)
            .Where(i => !errorsByIndex.ContainsKey(i))
            .Select(i => requests[i].ToEntity())
            .ToList();

        return new ItemBulkPrepareResult(
            entities,
            errorsByIndex.OrderBy(kv => kv.Key).Select(kv => new BulkRowError(kv.Key, kv.Value)).ToList());
    }

    private static async Task<HashSet<int>> MissingIdsAsync<TEntity>(
        IGenericRepository<TEntity> repository, IEnumerable<int> referencedIds, CancellationToken cancellationToken)
        where TEntity : class, IEntity
    {
        var wanted = referencedIds.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return [];
        }

        var existing = await repository.FilterExistingIdsAsync(wanted, cancellationToken);
        return wanted.Except(existing).ToHashSet();
    }
}
