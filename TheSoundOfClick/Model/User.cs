using System.Text.RegularExpressions;

namespace TheSoundOfClick;

public enum UserRole
{
  Employee,
  Admin,
  Customer,
  Student
}

public enum DiscountRole
{
  Employee,
  Admin,
  Customer,
  Student
}

public class User
{
  public Int128 ID { get; init; }
  public DateOnly RegisterDate { get; init; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  private string email = "";
  public string Email 
  { get { return email; }
    set
    {
      string strRegex = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
      Regex regex = new Regex(strRegex);

      if (!regex.IsMatch(value))
      {
        throw new FormatException("Invalid email");
      }
      email = value;
    } 
  }
  public string Address { get; set; }
  private string phoneNumber = "";
  public string PhoneNumber
  { get { return phoneNumber; }
    set
    {
      string strRegex = @"^\+?[1-9]\d{1,14}$";
      Regex regex = new Regex(strRegex);

      if (!regex.IsMatch(value))
      {
        throw new FormatException("Invalid phone number");
      }
      phoneNumber = value;
    } 
  }
  public UserRole Role { get; set; }
  public DiscountRole DiscountRole { get; set; }

  public User
  (
    Int128 id, DateOnly registerDate, string firstName, string lastName, string email, string address, string phoneNumber, UserRole role, DiscountRole discountRole
  )
  {
    ID = id;
    RegisterDate = registerDate;
    FirstName = firstName;
    LastName = lastName;
    Email = email;
    Address = address;
    PhoneNumber = phoneNumber;
    Role = role;
    DiscountRole = discountRole;
  }
}
