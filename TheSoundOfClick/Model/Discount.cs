namespace TheSoundOfClick;

public class FixedDiscount : IDiscount
{
  private readonly decimal m_amount;

  public FixedDiscount(decimal amount)
  {
    if (amount < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), "Discount can't be negative");
    }
    m_amount = amount;
  }

  public decimal Apply(decimal price)
  {
    if (m_amount > price)
    {
      throw new ArgumentOutOfRangeException(nameof(price), "Discount can't be greater than the item's value");
    }
    return price - m_amount;
  }
}

public class PercentageDiscount : IDiscount
{
  private readonly decimal m_percentage;

  public PercentageDiscount(decimal percentage)
  {
    if (percentage < 0 || percentage > 100)
    {
      throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100");
    }
    m_percentage = percentage;
  }

  public decimal Apply(decimal price)
  {
    return price - price * m_percentage / 100;
  }
}

public abstract class Customer
{
  protected abstract IEnumerable<IDiscount> Discounts { get; }

  public decimal GetPrice(decimal price)
  {
    return Discounts.Aggregate(price, (current, discount) => discount.Apply(current));
  }
}

public class StudentCustomer : Customer
{
  protected override IEnumerable<IDiscount> Discounts =>
  [
    new PercentageDiscount(10)
  ];
}

public class AdminCustomer : Customer
{
  protected override IEnumerable<IDiscount> Discounts =>
  [
    new PercentageDiscount(20),
    new FixedDiscount(50)
  ];
}