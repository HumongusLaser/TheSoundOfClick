using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;


namespace TheSoundOfClick;

public class ValidationService
{
    public bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, @"^[\p{L}]+(?:[ '-][\p{L}]+)*$");
    }
    public bool IsValidAddress(string address)
    {
        return !string.IsNullOrWhiteSpace(address)
            && Regex.IsMatch(address, @"^(?=.*\p{L})(?=.*\d)[\p{L}\d\s.'/-]+$");
    }
    public bool IsValidPostalCode(string postalCode)
    {
        return Regex.IsMatch(postalCode ?? "", @"^[0-9]{4}$");
    }

    public bool IsValidCity(string city)
    {
        return !string.IsNullOrWhiteSpace(city);
    }
    public bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && new EmailAddressAttribute().IsValid(email);
    }
    public bool IsValidPhoneNumber(string phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber) && Regex.IsMatch(phoneNumber, @"^[0-9]{8}$");
    }
    public bool IsValidPassword(string password)
    {
        return !string.IsNullOrEmpty(password)
            && password.Length >= 16
            && password.Count(char.IsUpper) >= 2
            && password.Count(char.IsDigit) >= 2
            && password.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)) >= 2;
    }
}
