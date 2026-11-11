namespace TheSoundOfClick;

public class Order : IOrder
{
  public Guid ID { get; init; }
  public Guid CustomerID { get; init; }
  public DateTime DateCreated { get; init; }
  public string Status { get; set; }
  public decimal TotalPrice { get; set; }
  public string ShippingAddress { get; set; }
  public Dictionary<Product, int> OrderList { get; set; }
  public IShippingProvider? ShippingProvider { get; set; }

  public Order(
    Guid id,
    Guid customerId,
    DateTime dateCreated,
    string status,
    decimal totalPrice,
    string shippingAddress,
    Dictionary<Product, int> orderList,
    IShippingProvider? shippingProvider = null)
  {
    ID = id;
    CustomerID = customerId;
    DateCreated = dateCreated;
    Status = status;
    TotalPrice = totalPrice;
    ShippingAddress = shippingAddress;
    OrderList = orderList;
    ShippingProvider = shippingProvider;
  }

  public int ItemsAmount()
  {
    int items = 0;

    foreach (var product in OrderList)
    {
      items += product.Value;
    }
    return items;
  }
}

public sealed class OrderHistoryEntry
{
  public required Guid ID { get; init; }
  public required DateTime DateCreated { get; init; }
  public required string Status { get; init; }
  public required decimal TotalPrice { get; init; }
  public required string ShippingAddress { get; init; }
  public List<OrderHistoryItem> Items { get; } = [];
  public int ItemsAmount => Items.Sum(item => item.Quantity);
}

public sealed class OrderHistoryItem
{
  public required string ProductName { get; init; }
  public required decimal UnitPrice { get; init; }
  public required int Quantity { get; init; }
}