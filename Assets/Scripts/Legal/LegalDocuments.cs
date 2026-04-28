using UnityEngine;

public enum LegalDocumentType
{
    PrivacyPolicy,
    TermsOfUse
}

public static class LegalDocuments
{
    public const string PrivacyPolicyVersion = "2026-04-28";
    public const string TermsOfUseVersion = "2026-04-28";

    private const string PrivacyAcceptedVersionKey = "legal_privacy_accepted_version";
    private const string TermsAcceptedVersionKey = "legal_terms_accepted_version";

    public static bool HasAcceptedLatestDocuments()
    {
        return PlayerPrefs.GetString(PrivacyAcceptedVersionKey, string.Empty) == PrivacyPolicyVersion &&
               PlayerPrefs.GetString(TermsAcceptedVersionKey, string.Empty) == TermsOfUseVersion;
    }

    public static void AcceptLatestDocuments()
    {
        PlayerPrefs.SetString(PrivacyAcceptedVersionKey, PrivacyPolicyVersion);
        PlayerPrefs.SetString(TermsAcceptedVersionKey, TermsOfUseVersion);
        PlayerPrefs.Save();
    }

    public static bool UseFrench()
    {
        return LanguageManager.CurrentLanguage == Language.French;
    }

    public static string GetWindowTitle(bool requireAcceptance)
    {
        if (UseFrench())
            return requireAcceptance ? "Informations légales" : "Documents légaux";

        return requireAcceptance ? "Legal Information" : "Legal Documents";
    }

    public static string GetIntroText(bool requireAcceptance)
    {
        if (UseFrench())
        {
            return requireAcceptance
                ? "Avant de continuer, merci de consulter notre Politique de confidentialité et nos Conditions d'utilisation, puis d'appuyer sur le bouton d'acceptation. Tu pourras les relire à tout moment depuis le menu."
                : "Consulte ici la Politique de confidentialité et les Conditions d'utilisation du jeu.";
        }

        return requireAcceptance
            ? "Before continuing, please review our Privacy Policy and Terms of Use, then confirm with the acceptance button. You will be able to read them again later from the menu."
            : "Review the game's Privacy Policy and Terms of Use here.";
    }

    public static string GetAcceptButtonLabel()
    {
        return UseFrench() ? "J'accepte et je continue" : "Accept and Continue";
    }

    public static string GetCloseButtonLabel()
    {
        return UseFrench() ? "Fermer" : "Close";
    }

    public static string GetPrivacyButtonLabel()
    {
        return UseFrench() ? "Politique de confidentialité" : "Privacy Policy";
    }

    public static string GetTermsButtonLabel()
    {
        return UseFrench() ? "Conditions d'utilisation" : "Terms of Use";
    }

    public static string GetSectionHeaderLabel()
    {
        return UseFrench() ? "Légal" : "Legal";
    }

    public static string GetDocumentTitle(LegalDocumentType documentType)
    {
        return documentType == LegalDocumentType.PrivacyPolicy
            ? GetPrivacyButtonLabel()
            : GetTermsButtonLabel();
    }

    public static string GetDocumentBody(LegalDocumentType documentType)
    {
        string suffix = UseFrench() ? "fr" : "en";
        string fileName = documentType == LegalDocumentType.PrivacyPolicy
            ? $"Legal/PrivacyPolicy_{suffix}"
            : $"Legal/TermsOfUse_{suffix}";

        TextAsset asset = Resources.Load<TextAsset>(fileName);
        if (asset != null)
            return asset.text;

        string fallbackFileName = documentType == LegalDocumentType.PrivacyPolicy
            ? "Legal/PrivacyPolicy_en"
            : "Legal/TermsOfUse_en";

        TextAsset fallback = Resources.Load<TextAsset>(fallbackFileName);
        return fallback != null ? fallback.text : string.Empty;
    }
}
