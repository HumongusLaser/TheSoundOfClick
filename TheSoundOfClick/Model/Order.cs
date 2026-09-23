namespace TheSoundOfClick;

public class Order
{
  public Int128 ID { get; init; }
  public DateTime DateCreated { get; init; }
  public string Status { get; set; }
  public decimal TotalPrice { get; set; }
  public string ShippingAddress { get; set; }
  public Dictionary<Product, int> OrderList { get; set; }
  public IShippingProvider ShippingProvider { get; set; }

  public Order(Int128 id, DateTime dateCreated, string status, decimal totalPrice, string shippingAddress, Dictionary<Product, int> orderList, IShippingProvider shippingProvider)
  {
    ID = id;
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