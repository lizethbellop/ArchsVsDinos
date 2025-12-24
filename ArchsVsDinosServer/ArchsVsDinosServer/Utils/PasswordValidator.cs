using ArchsVsDinosServer.Interfaces;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class PasswordValidator : IPasswordValidator
    {
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 128;
        private const string SpecialCharacters = "!@#$%^&*()_+-=[]{}|;:',.<>?/~`";

        public ValidationResult ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return new ValidationResult(false, UpdateResultCode.Profile_EmptyFields);
            }

            if (password.Length < MinPasswordLength)
            {
                return new ValidationResult(false, UpdateResultCode.Profile_PasswordTooShort);
            }

            if (password.Length > MaxPasswordLength)
            {
                return new ValidationResult(false, UpdateResultCode.Profile_PasswordTooLong);
            }

            if (!password.Any(char.IsLower))
            {
                return new ValidationResult(false, UpdateResultCode.Profile_PasswordNeedsLowercase);
            }

            if (!password.Any(char.IsUpper))
            {
                return new ValidationResult(false, UpdateResultCode.Profile_PasswordNeedsUppercase);
            }

            if (!password.Any(char.IsDigit))
            {
                return new ValidationResult(false, UpdateResultCode.Profile_PasswordNeedsNumber);
            }

            if (!ContainsSpecialCharacter(password))
            {
                return new ValidationResult(false, UpdateResultCode.Profile_PasswordNeedsSpecialCharacter);
            }

            return new ValidationResult(true, UpdateResultCode.Profile_ChangePasswordSuccess);
        }

        public bool ContainsSpecialCharacter(string password)
        {
            return password.Any(c => SpecialCharacters.Contains(c));
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public UpdateResultCode ResultCode { get; }

        public ValidationResult(bool isValid, UpdateResultCode resultCode)
        {
            IsValid = isValid;
            ResultCode = resultCode;
        }
    }
}
