using Nexus.Shared.Domain;

namespace Nexus.UserService.Domain.Entities;

public enum WalletTxType { Credit, Debit }

public class Wallet : BaseEntity
{
    public Guid    UserId  { get; private set; }
    public decimal Balance { get; private set; }

    private readonly List<WalletTransaction> _transactions = new();
    public IReadOnlyList<WalletTransaction> Transactions => _transactions.AsReadOnly();

    private Wallet() { }

    public static Wallet Create(Guid userId) => new() { UserId = userId, Balance = 0 };

    public void Credit(decimal amount, string description, string reference)
    {
        if (amount <= 0) throw new InvalidOperationException("Số tiền hoàn phải dương.");
        Balance += amount;
        _transactions.Add(WalletTransaction.Create(Id, amount, WalletTxType.Credit, description, reference));
        Touch();
    }

    public void Debit(decimal amount, string description, string reference)
    {
        if (amount <= 0) throw new InvalidOperationException("Số tiền phải dương.");
        if (amount > Balance) throw new InvalidOperationException("Số dư ví không đủ.");
        Balance -= amount;
        _transactions.Add(WalletTransaction.Create(Id, amount, WalletTxType.Debit, description, reference));
        Touch();
    }
}

public class WalletTransaction : BaseEntity
{
    public Guid          WalletId    { get; private set; }
    public decimal       Amount      { get; private set; }
    public WalletTxType  Type        { get; private set; }
    public string        Description { get; private set; } = default!;
    public string        Reference   { get; private set; } = default!;

    private WalletTransaction() { }

    public static WalletTransaction Create(Guid walletId, decimal amount,
        WalletTxType type, string description, string reference) => new()
    {
        WalletId = walletId, Amount = amount, Type = type,
        Description = description, Reference = reference
    };
}
