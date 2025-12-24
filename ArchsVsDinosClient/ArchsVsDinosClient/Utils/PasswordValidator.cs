using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public class PasswordValidator
    {
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 20;

        public static ValidationResult ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return new ValidationResult(false, Lang.GlobalEmptyField);
            }

            if (password.Length < MinPasswordLength)
            {
                return new ValidationResult(false, Lang.Profile_PasswordTooShort);
            }

            if (password.Length > MaxPasswordLength)
            {
                return new ValidationResult(false, Lang.Profile_PasswordTooLong);
            }

            if (!password.Any(char.IsLower))
            {
                return new ValidationResult(false, Lang.Profile_PasswordNeedsLowercase);
            }

            if (!password.Any(char.IsUpper))
            {
                return new ValidationResult(false, Lang.Profile_PasswordNeedsUppercase);
            }

            if (!password.Any(char.IsDigit))
            {
                return new ValidationResult(false, Lang.Profile_PasswordNeedsNumber);
            }

            if (!ContainsSpecialCharacter(password))
            {
                return new ValidationResult(false, Lang.Profile_PasswordNeedsSpecialCharacter);
            }

            return new ValidationResult(true, string.Empty);
        }

        private static bool ContainsSpecialCharacter(string password)
        {
            string specialCharacters = "!@#$%&*()_+-=[]{};:',.<>?/";
            return password.Any(c => specialCharacters.Contains(c));
        }

        public static ValidationResult ValidatePasswordsMatch(string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                return new ValidationResult(false, Lang.Profile_SamePasswordValue);
            }
            return new ValidationResult(true, string.Empty);
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
}
