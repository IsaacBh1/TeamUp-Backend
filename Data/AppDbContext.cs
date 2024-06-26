using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Models;

class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<VerificationCode> VerificationCodes { get; set; }
    public Dbset<Project> Projects { get; set; } 
}

public class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public VerificationCode EmailVerificationCode { get; set; } = new VerificationCode();
    public VerificationCode PasswordRestCode { get; set; } = new VerificationCode();

} 
public partial class VerificationCode
{
    [Key]
    public int Id { get; set; } 
    public string? Code { get; set; } = null; 
    public DateTime? ExpireTime { get; set; } = null;
    public int? CodeTriesCount { get; set; } = null;
    public VerificationCodeTypes VerificationCodeType { get; set; }
}

partial class VerificationCode
{
    public enum CodeMaxLifeInMin { EmailVerification = 10, PasswordRest = 5 }
    public enum CodeMaxTries { EmailVerification = 4, PasswordRest = 3 }
    public enum VerificationCodeTypes {EmailVerification = 1, PasswordRest = 2}
    public VerificationCode()
    {

    }

    /// <summary>
    /// Get max code life time in minutes
    /// </summary>
    /// <returns>integer represent code max life in minutes</returns>
    public int GetCodeMaxLife()
    {
        return Convert.ToInt32(VerificationCodeType switch
        {
            VerificationCodeTypes.EmailVerification => CodeMaxLifeInMin.EmailVerification,
            VerificationCodeTypes.PasswordRest      => CodeMaxLifeInMin.PasswordRest,
            _ => 0,
        });
    }

    public int GetCodeMaxTries()
    {
        return Convert.ToInt32(VerificationCodeType switch
        {
            VerificationCodeTypes.EmailVerification => CodeMaxTries.EmailVerification,
            VerificationCodeTypes.PasswordRest      => CodeMaxTries.PasswordRest,
            _ => 0,
        });
    }

    public bool IsEmailVerificationCode()
    {
        return VerificationCodeType == VerificationCodeTypes.EmailVerification;
    }

    public bool IsPasswordRestCode()
    {
        return VerificationCodeType == VerificationCodeTypes.PasswordRest;
    }
    public bool IsExpired()
    {
        if(Code is null || ExpireTime is null)
            return true;

        return ExpireTime < DateTime.UtcNow;
    }

    public bool IsValid(string code)
    {
        if(IsExpired())
        {
            return false;
        }

        return this.Code == code;
    }

    
    public string GenerateEmailVerificationCode(string userId)
    {
        VerificationCodeType = VerificationCodeTypes.EmailVerification;
        Code = GenerateRandomCode(userId); // user userId to generate user speacific code for security perposes (may be deleted if I find that it is useless)
        ExpireTime = DateTime.UtcNow.AddMinutes(GetCodeMaxLife());
        CodeTriesCount = GetCodeMaxTries();

        return Code;
    }

    public string GeneratePasswordRestCode(string userId)
    {
        VerificationCodeType = VerificationCodeTypes.PasswordRest;
        Code = GenerateRandomCode(userId); // user userId to generate user speacific code for security perposes (may be deleted if I find that it is useless)
        ExpireTime = DateTime.UtcNow.AddMinutes(GetCodeMaxLife());
        CodeTriesCount = GetCodeMaxTries();

        return Code;
    }


    private static string GenerateRandomCode(string userId)
    {
        Random rand = new Random();
        int randomNumber = rand.Next(100000, 999999); 

        string combinedString = userId + randomNumber.ToString();

        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(combinedString));

            int hashInteger = BitConverter.ToInt32(bytes, 0);

            hashInteger = Math.Abs(hashInteger);

            int sixDigitNumber = hashInteger % 1000000;

            return sixDigitNumber.ToString();
        }
    }
}
