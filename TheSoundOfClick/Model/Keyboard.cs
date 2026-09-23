namespace TheSoundOfClick
{
  public class Keyboard : Product
  {
    private string layout = "";
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
    public bool IsWireless { get; set; }
    private string switchType = "";
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

    public string Size { get; set; }

    public Keyboard(Int128 id, int stock, string name, decimal price, string color, string layout, bool isWireless, string switchType, string description, string size, int netWeight, int grossWeight) : 
      base(id, stock, name, price, color, description, netWeight, grossWeight)
    {
      Layout = layout;
      IsWireless = isWireless;
      SwitchType = switchType;
      Size = size;
    }

    public override string GetProductDetails()
    {
      return $"Keyboard: {Name}, Color: {Color}, Layout: {Layout}, Wireless: {IsWireless}, Switch Type: {SwitchType}";
    }
  }
}
