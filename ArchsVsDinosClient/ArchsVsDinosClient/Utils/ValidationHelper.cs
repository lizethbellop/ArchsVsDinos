using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Utils
{
    public static class ValidationHelper
    {

        public static bool isEmpty(string input)
        {
            return string.IsNullOrEmpty(input);
        }

        public static bool isWhiteSpace(string input)
        {
            return string.IsNullOrWhiteSpace(input);
        }

        public static bool MinLengthPassword(string input)
        {
            const int Minimum = 8;
            return !isEmpty(input) && input.Length >= Minimum;
        }

        public static bool HasPasswordAllCharacters(string input)
        {
            if (isEmpty(input) || !MinLengthPassword(input))
                return false;

            bool hasUpper = false;
            bool hasLower = false;
            bool hasNumber = false;
            bool hasSpecialCharacter = false;

            foreach (char c in input)
            {
                if (char.IsUpper(c))
                    hasUpper = true;
                else if (char.IsLower(c))
                    hasLower = true;
                else if (char.IsDigit(c))
                    hasNumber = true;
                else hasSpecialCharacter = true;

            }

            return hasUpper && hasLower && hasNumber && hasSpecialCharacter;

        }

        public static bool IsAValidEmail(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            try 
            {
                var email = new MailAddress(input);
                return email.Address == input;
            }
            catch 
            {
                return false; 
            }

        }

    }
}
