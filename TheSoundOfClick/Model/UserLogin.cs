using Microsoft.AspNetCore.Identity;

namespace TheSoundOfClick;

public class UserLogin
{
  public Guid ID { get; set; }
  public Guid UserID { get; init; }
  public string Email { get; set; } = "";
  public string PhcString { get; set; } = "";

  public UserLogin(){}

  public UserLogin(Guid id, Guid userID, string email, string phcString)
  {
    ID = id;
    UserID = userID;
    Email = email;
    PhcString = phcString;
  }
}
