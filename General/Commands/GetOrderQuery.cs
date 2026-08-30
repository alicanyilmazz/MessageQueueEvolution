using General.Messages;

namespace General.Commands;

public class GetOrderQuery : IQuery<OrderResponse>
{
    public int OrderId { get; }

    public GetOrderQuery(int orderId)
    {
        OrderId = orderId;
    }
}