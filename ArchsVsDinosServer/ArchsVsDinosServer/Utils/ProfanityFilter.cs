using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace ArchsVsDinosServer.Utils
{
    public class ProfanityFilter
    {
        private readonly HashSet<string> bannedWords;
        private readonly ILoggerHelper loggerHelper;

        public ProfanityFilter(ILoggerHelper loggerHelper, string bannedWordsFilePath)
        {
            this.loggerHelper = loggerHelper;
            bannedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LoadBannedWords(bannedWordsFilePath);
        }

        public ProfanityFilter()
        {
            bannedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private void LoadBannedWords(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    loggerHelper?.LogWarning("Banned words file path is null or empty");
                    return;
                }

                if (!File.Exists(filePath))
                {
                    loggerHelper?.LogWarning($"Banned words file not found at: {filePath}");
                    return;
                }

                var words = File.ReadAllLines(filePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                    .Select(word => word.Trim().ToLower());

                foreach (var word in words)
                {
                    bannedWords.Add(word);
                }

                loggerHelper?.LogInfo($"Successfully loaded {bannedWords.Count} banned words from file");
            }
            catch (UnauthorizedAccessException ex)
            {
                loggerHelper?.LogError($"Access denied when reading banned words file: {filePath}", ex);
            }
            catch (FileNotFoundException ex)
            {
                loggerHelper?.LogError($"Banned words file not found: {filePath}", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                loggerHelper?.LogError($"Directory not found for banned words file: {filePath}", ex);
            }
            catch (IOException ex)
            {
                loggerHelper?.LogError($"IO error reading banned words file: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                loggerHelper?.LogWarning($"Unexpected error loading banned words: {ex.Message}");
            }
        }

        public bool ContainsProfanity(string message, out List<string> foundWords)
        {
            foundWords = new List<string>();

            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return false;
                }

                if (bannedWords == null || bannedWords.Count == 0)
                {
                    return false;
                }

                string normalizedMessage = NormalizeMessage(message);
                var words = Regex.Split(normalizedMessage, @"\s+");

                foreach (var word in words)
                {
                    string cleanWord = Regex.Replace(word, @"[^\w]", "").ToLower();

                    if (string.IsNullOrEmpty(cleanWord))
                        continue;

                    if (bannedWords.Contains(cleanWord))
                    {
                        foundWords.Add(cleanWord);
                    }
                    else if (bannedWords.Any(banned => cleanWord.Contains(banned)))
                    {
                        foundWords.Add(cleanWord);
                    }
                }

                return foundWords.Any();
            }
            catch (RegexMatchTimeoutException ex)
            {
                loggerHelper?.LogError($"Regex timeout while checking profanity in message", ex);
                return false;
            }
            catch (ArgumentException ex)
            {
                loggerHelper?.LogError($"Invalid regex pattern in profanity filter: {ex.Message}", ex);
                return false;
            }
            catch (Exception ex)
            {
                loggerHelper?.LogWarning($"Unexpected error checking profanity: {ex.Message}");
                return false;
            }
        }

        private string NormalizeMessage(string message)
        {
            try
            {
                return message
                    .Replace("@", "a")
                    .Replace("4", "a")
                    .Replace("3", "e")
                    .Replace("1", "i")
                    .Replace("0", "o")
                    .Replace("$", "s")
                    .Replace("!", "i")
                    .Replace("7", "t")
                    .Replace("|", "i")
                    .Replace("*", "");
            }
            catch (Exception ex)
            {
                loggerHelper?.LogError($"Error normalizing message: {ex.Message}", ex);
                return message;
            }
        }
    }
}
