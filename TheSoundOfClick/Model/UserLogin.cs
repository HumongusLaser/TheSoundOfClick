using Microsoft.AspNetCore.Identity;

namespace TheSoundOfClick;

public class UserLogin
{
  public Guid ID { get; set; }
  public Guid UserID { get; init; }
  public string PhcString { get; set; } = "";

  public UserLogin(){}

  public UserLogin(Guid id, Guid userID, string phcString)
  {
    ID = id;
    UserID = userID;
    PhcString = phcString;
  }
}
