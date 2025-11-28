using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public static class SocialMediaValidator
    {
        public static bool IsValidFacebookLink(string url)
        {
            return IsValidSocialMediaLink(url, SocialMediaPlatform.Facebook);
        }

        public static bool IsValidInstagramLink(string url)
        {
            return IsValidSocialMediaLink(url, SocialMediaPlatform.Instagram);
        }

        public static bool IsValidTikTokLink(string url)
        {
            return IsValidSocialMediaLink(url, SocialMediaPlatform.TikTok);
        }

        public static bool IsValidXLink(string url)
        {
            return IsValidSocialMediaLink(url, SocialMediaPlatform.X);
        }
        public static bool IsValidSocialMediaLink(string url, SocialMediaPlatform platform)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
                return false;

            if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
                return false;

            string lowerUrl = url.ToLower();

            switch (platform)
            {
                case SocialMediaPlatform.Facebook:
                    return lowerUrl.Contains("facebook.com") || lowerUrl.Contains("fb.com");
                case SocialMediaPlatform.Instagram:
                    return lowerUrl.Contains("instagram.com");
                case SocialMediaPlatform.X:
                    return lowerUrl.Contains("twitter.com") || lowerUrl.Contains("x.com");
                case SocialMediaPlatform.TikTok:
                    return lowerUrl.Contains("tiktok.com");
                default:
                    return false;
            }
        }
        public static string GetValidationErrorMessage(SocialMediaPlatform platform)
        {
            switch (platform)
            {
                case SocialMediaPlatform.Facebook:
                    return Lang.SocialMedia_InvalidFacebookLink;
                case SocialMediaPlatform.Instagram:
                    return Lang.SocialMedia_InvalidInstagramLink;
                case SocialMediaPlatform.TikTok:
                    return Lang.SocialMedia_InvalidTiktokLink;
                case SocialMediaPlatform.X:
                    return Lang.SocialMedia_InvalidXLink;
                default:
                    return Lang.SocialMedia_InvalidLink;
            }
        }
    }
}
