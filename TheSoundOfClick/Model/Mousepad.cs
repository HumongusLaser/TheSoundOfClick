namespace TheSoundOfClick;

public class Mousepad : Product
{
  // All sizes are in mm (milimeter)
  private int length;
  public int Length
  {
    get { return length; }
    set
    {
      if (value <= 0)
      {
        throw new ArgumentException("Length must be greater than 0");
      }
      length = value;
    }
  }

  private int width;
  public int Width
  {
    get { return width; }
    set
    {
      if (value <= 0)
      {
        throw new ArgumentException("Width must be greater than 0");
      }
      width = value;
    }
  }

  private int thickness;
  public int Thickness
  {
    get { return thickness; }
    set
    {
      if (value <= 0)
      {
        throw new ArgumentException("Thickness must be greater than 0");
      }
      thickness = value;
    }
  }

  public Mousepad(Int128 id, int stock, string name, decimal price, string color, int length, int width, int thickness, string description, int netWeight, int grossWeight) : 
    base(id, stock, name, price, color, description, netWeight, grossWeight)
  {
    Length = length;
    Width = width;
    Thickness = thickness;
  }

  int GetSize()
  {
    return (int)Math.Round(Math.Sqrt(Math.Pow(length, 2) + Math.Pow(width, 2)));
  }
 public override string GetProductDetails()
    {
        return $"Mousepad: {Name}, Color: {Color}, Size: {Length}x{Width}mm, Thickness: {Thickness}mm";
    }

}
