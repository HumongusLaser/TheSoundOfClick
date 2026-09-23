using System.Globalization;

namespace TheSoundOfClick;

public abstract class Product
{
  public Int128 ID { get; init; }

  private int stock;
  public int Stock 
  { get { return stock; } 
    set
    {
      if (value < 0)
      {
        throw new ArgumentException("stock cant be less than 0");
      }
      stock = value;
    }
  }

  private string name = "";
  public string Name 
  { get { return name; }
    set
    {
      if (value.Length < 2)
      {
        throw new ArgumentException("Name of product must be a least 2 charactors >:)");
      }
      name = value;
    } 
  }
  
  private decimal price;
  public decimal Price 
  { get { return price; } 
    set
    {
      if (value < 0)
      {
        throw new ArgumentException("the price for a product cant be less than 0");
      }
      price = value;
    }
  }

  private string color = ""; 
  public string Color
  {
    get { return color; }
    set
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        throw new ArgumentException("Color cannot be empty.");
      }
      color = value;
    }
  }

  public string Description { get; set; }

  // Weights are in grams
  private int netWeight;
  public int NetWeight { get; set; }

  private int grossWeight;
  public int GrossWeight 
  { 
    get { return grossWeight; }
    set
    {
      if (value < NetWeight)
      {
        throw new ArgumentException("Gross weight cant be less than netweight");
      }
      grossWeight = value;
    }
  }

  public Product(Int128 id, int stock, string name, decimal price, string color, string description, int netWeight, int grossWeight)
  {
    ID = id;
    Stock = stock;
    Name = name;
    Price = price;
    Color = color;
    Description = description;
    NetWeight = netWeight;
    GrossWeight = grossWeight;
  }

  public abstract string GetProductDetails();
}
