using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.UserService.Domain.Entities;
using Nexus.UserService.Application.Persistence;

namespace Nexus.UserService.Application.UseCases.ManageAddress;

// ── DTOs ────────────────────────────────────────────────────────────────────
public record AddressDto(
    Guid Id, string Label, string RecipientName, string Phone,
    string AddressLine, string Province, string District, string Ward,
    int ToDistrictId, string ToWardCode, bool IsDefault);

// ── Add ──────────────────────────────────────────────────────────────────────
public record AddAddressCommand(
    Guid UserId, string Label, string RecipientName, string Phone,
    string AddressLine, string Province, string District, string Ward,
    int ToDistrictId, string ToWardCode, bool IsDefault = false
) : IRequest<Guid>;

public class AddAddressHandler : IRequestHandler<AddAddressCommand, Guid>
{
    private readonly IUserDatabase _db;
    public AddAddressHandler(IUserDatabase db) => _db = db;

    public async Task<Guid> Handle(AddAddressCommand cmd, CancellationToken ct)
    {
        if (cmd.IsDefault)
        {
            var existing = await _db.Addresses
                .Where(a => a.UserId == cmd.UserId && a.IsDefault).ToListAsync(ct);
            foreach (var a in existing) a.UnsetDefault();
        }

        var address = UserAddress.Create(cmd.UserId, cmd.Label, cmd.RecipientName,
            cmd.Phone, cmd.AddressLine, cmd.Province, cmd.District, cmd.Ward,
            cmd.ToDistrictId, cmd.ToWardCode, cmd.IsDefault);

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync(ct);
        return address.Id;
    }
}

// ── Update ────────────────────────────────────────────────────────────────────
public record UpdateAddressCommand(
    Guid UserId, Guid AddressId, string Label, string RecipientName, string Phone,
    string AddressLine, string Province, string District, string Ward,
    int ToDistrictId, string ToWardCode
) : IRequest;

public class UpdateAddressHandler : IRequestHandler<UpdateAddressCommand>
{
    private readonly IUserDatabase _db;
    public UpdateAddressHandler(IUserDatabase db) => _db = db;

    public async Task Handle(UpdateAddressCommand cmd, CancellationToken ct)
    {
        var addr = await _db.Addresses.FirstOrDefaultAsync(
            a => a.Id == cmd.AddressId && a.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Địa chỉ không tồn tại.");
        addr.Update(cmd.Label, cmd.RecipientName, cmd.Phone,
            cmd.AddressLine, cmd.Province, cmd.District, cmd.Ward,
            cmd.ToDistrictId, cmd.ToWardCode);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────
public record DeleteAddressCommand(Guid UserId, Guid AddressId) : IRequest;

public class DeleteAddressHandler : IRequestHandler<DeleteAddressCommand>
{
    private readonly IUserDatabase _db;
    public DeleteAddressHandler(IUserDatabase db) => _db = db;

    public async Task Handle(DeleteAddressCommand cmd, CancellationToken ct)
    {
        var addr = await _db.Addresses.FirstOrDefaultAsync(
            a => a.Id == cmd.AddressId && a.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Địa chỉ không tồn tại.");
        _db.Addresses.Remove(addr);
        await _db.SaveChangesAsync(ct);
    }
}

// ── Set Default ───────────────────────────────────────────────────────────────
public record SetDefaultAddressCommand(Guid UserId, Guid AddressId) : IRequest;

public class SetDefaultAddressHandler : IRequestHandler<SetDefaultAddressCommand>
{
    private readonly IUserDatabase _db;
    public SetDefaultAddressHandler(IUserDatabase db) => _db = db;

    public async Task Handle(SetDefaultAddressCommand cmd, CancellationToken ct)
    {
        var all = await _db.Addresses.Where(a => a.UserId == cmd.UserId).ToListAsync(ct);
        foreach (var a in all) a.UnsetDefault();
        var target = all.FirstOrDefault(a => a.Id == cmd.AddressId)
            ?? throw new KeyNotFoundException("Địa chỉ không tồn tại.");
        target.SetDefault();
        await _db.SaveChangesAsync(ct);
    }
}

// ── Get All ───────────────────────────────────────────────────────────────────
public record GetAddressesQuery(Guid UserId) : IRequest<List<AddressDto>>;

public class GetAddressesHandler : IRequestHandler<GetAddressesQuery, List<AddressDto>>
{
    private readonly IUserDatabase _db;
    public GetAddressesHandler(IUserDatabase db) => _db = db;

    public async Task<List<AddressDto>> Handle(GetAddressesQuery q, CancellationToken ct)
        => await _db.Addresses.Where(a => a.UserId == q.UserId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.CreatedAt)
            .Select(a => new AddressDto(a.Id, a.Label, a.RecipientName, a.Phone,
                a.AddressLine, a.Province, a.District, a.Ward,
                a.ToDistrictId, a.ToWardCode, a.IsDefault))
            .ToListAsync(ct);
}
