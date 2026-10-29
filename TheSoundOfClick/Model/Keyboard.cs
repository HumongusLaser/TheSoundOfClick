namespace TheSoundOfClick
{
  [ProductCategory("keyboard")]
  [ProductCategoryUserView("Keyboard")]
  public class Keyboard : Product
  {
    private string layout = "";
    [UserPageNameProp("Layout")]
    public string Layout 
    {
      get { return layout; }
      set
      {
        if (string.IsNullOrWhiteSpace(value))
        {
          throw new ArgumentException("Layout cannot be empty.");
        }
        layout = value;
      }
    }
    [UserPageNameProp("Is wireless")]
    public bool IsWireless { get; set; }
    private string switchType = "";
    [UserPageNameProp("SwitchType")]
    public string SwitchType
    {
      get { return switchType; }
      set
      {
        if (string.IsNullOrWhiteSpace(value))
        {
          throw new ArgumentException("Switch type cannot be empty.");
        }
        switchType = value;
      }
    }

    public string Size { get; set; } = "";

    public Keyboard(Guid id, int stock, string name, decimal price, string color, string layout, bool isWireless, string switchType, string description, string size, int netWeight, int grossWeight) : 
      base(id, stock, name, price, color, description, netWeight)
    {
      Layout = layout;
      IsWireless = isWireless;
      SwitchType = switchType;
      Size = size;
    }
    public Keyboard(){}

    public override string GetProductDetails()
    {
      return $"Keyboard: {Name}, Color: {Color}, Layout: {Layout}, Wireless: {IsWireless}, Switch Type: {SwitchType}";
    }
  }
}
