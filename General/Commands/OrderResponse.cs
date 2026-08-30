namespace General.Commands;

public class OrderResponse
{
    public int Id { get; }

    public string Status { get; }

    public decimal Amount { get; }

    public OrderResponse(int id, string status, decimal amount)
    {
        Id = id;
        Status = status;
        Amount = amount;
    }
}