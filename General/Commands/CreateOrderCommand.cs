using General.Messages;

namespace General.Commands;

public class CreateOrderCommand : ICommand
{
    public int CustomerId { get; }

    public decimal Amount { get; }

    public CreateOrderCommand(int customerId, decimal amount)
    {
        CustomerId = customerId;
        Amount = amount;
    }
}