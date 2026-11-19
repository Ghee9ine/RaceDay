namespace RaceDay.Api.Services.Storage;

/// <summary>One shared set of 400 messages for both upload endpoints, so a missing/oversized/
/// wrong-type file reads identically whether it was a banner or a profile picture.</summary>
public static class ImageValidationMessages
{
    public static string For(ImageValidationOutcome outcome) => outcome switch
    {
        ImageValidationOutcome.Missing => "No file was uploaded.",
        ImageValidationOutcome.TooLarge => "The file is larger than the 5 MB limit.",
        ImageValidationOutcome.UnsupportedType =>
            "Only JPEG, PNG or WebP images are accepted, and the file's actual content must match its declared type.",
        _ => "The uploaded file is not a valid image.",
    };
}
