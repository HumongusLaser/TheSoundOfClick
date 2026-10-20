namespace TheSoundOfClick;

public enum DiscountType
{
  FixedAmount,
  Percentage
}

public enum DiscountTargetType
{
  AllProducts,
  Product,
  Category
}

public enum DiscountThresholdType
{
  Amount,
  Price
}

public class DiscountCampaign
{
  public Int128 ID { get; init; }
  private string m_name = "";
  public string Name 
  { 
    get { return m_name; } 
    set
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        throw new ArgumentException("Name can not be null or whitespace");
      }
      m_name = value;
    }
  }
  public DiscountType DiscountType { get; set; }
  private decimal m_value;
  public decimal Value 
  { 
    get { return m_value; } 
    set
    {
      if (DiscountType == DiscountType.Percentage && value < 0 || value > 100)
      {
        throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100");
      }
      m_value = value;
    } 
  }
  public DiscountThresholdType ThresholdType { get; set; }
  private decimal m_threshold;
  public decimal Threshold
  {
    get { return m_threshold; }
    set
    {
      if (value < 0)
      {
        throw new ArgumentOutOfRangeException(nameof(value), "Threshold can't be negative");
      }
      m_threshold = value;
    }
  }
  public DiscountRole Role { get; set; }
  public Int128? Category { get; set; }
  public Int128? TargetID { get; init; }
  public DiscountTargetType TargetType { get; set; }
  private DateTime m_startDate;
  public DateTime StartDate
  {
    get { return m_startDate; }
    set
    {
      if (value.Date < DateTime.Today)
      {
        throw new ArgumentOutOfRangeException(nameof(value), "Start date can't be before today");
      }
      m_startDate = value;
    }
  }
  public DateTime EndDate { get; set; }

  public DiscountCampaign
  (
    Int128 campainID,
    string name,
    DiscountType discountType,
    decimal value,
    DiscountThresholdType thresholdType,
    decimal threshold,
    DiscountRole role,
    Int128? category,
    Int128? targetID,
    DiscountTargetType targetType,
    DateTime statDate,
    DateTime endDate
  )
  {
    ID = campainID;
    Name = name;
    DiscountType = discountType;
    Value = value;
    ThresholdType = thresholdType;
    Threshold = threshold;
    Role = role;
    Category = category;
    TargetID = targetID;
    TargetType = targetType;
    StartDate = statDate;
    EndDate = endDate;
  }
}
