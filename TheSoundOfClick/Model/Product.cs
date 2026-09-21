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

  public Product(Int128 id, int stock, string name, decimal price)
  {
    ID = id;
    Stock = stock;
    Name = name;
    Price = price;
  }
}
