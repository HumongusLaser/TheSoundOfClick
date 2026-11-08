using System.Text.RegularExpressions;

namespace TheSoundOfClick;

public class UserRoleDBV
{
  public Guid ID { get; set; }
  public string Role { get; set; } = "";
}

public class DiscountRoleDBV
{
  public Guid ID { get; set; }
  public string Role { get; set; } = "";
}


public enum UserRole
{
  Employee,
  Admin,
  Customer
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
  public Guid ID { get; set; }
  public DateOnly RegisterDate { get; set; }
  public string FirstName { get; set; } = "";
  public string LastName { get; set; } = "";
  private string email = "";
  public string Email 
  { 
    get { return email; }
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
  public string StreetAddress { get; set; } = "";
  public string? Apartment { get; set; }
  public string PostalCode { get; set; } = "";
  public string City { get; set; } = "";
  private string phoneNumber = "";
  public string PhoneNumber
  { 
    get { return phoneNumber; }
    set
    {
      string strRegex = @"^[0-9]{8}$";
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

  public User(){}

  public User
  (
    Guid id, DateOnly registerDate, string firstName, string lastName, string email, string streetAddress, string? apartment, string postalCode, string city, string phoneNumber, 
    UserRole role, DiscountRole discountRole
  )
  {
    ID = id;
    RegisterDate = registerDate;
    FirstName = firstName;
    LastName = lastName;
    Email = email;
    StreetAddress = streetAddress;
    Apartment = apartment;
    PostalCode = postalCode;
    City = city;
    PhoneNumber = phoneNumber;
    Role = role;
    DiscountRole = discountRole;
  }
}
