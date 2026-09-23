namespace TheSoundOfClick;

public interface IShippingProvider
{
  public string TrackingID { get; set; }
  public string ShippingCost(int totalItems, int weight);
}
