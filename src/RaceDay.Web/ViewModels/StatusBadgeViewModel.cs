namespace RaceDay.Web.ViewModels;

/// <summary>Model for the shared _StatusBadge partial. "Kind" picks which status->colour map
/// applies (Enrolment/Event/Result), so the same status word (e.g. the two unrelated meanings
/// of "Cancelled") never risks the wrong colour by accident.</summary>
public record StatusBadgeViewModel(string Kind, string Status)
{
    public (string CssClass, string Icon, string Label) Resolve()
    {
        return (Kind, Status) switch
        {
            ("Enrolment", "Confirmed") => ("rd-badge-green", "bi-check-circle-fill", "Confirmed"),
            ("Enrolment", "Pending") => ("rd-badge-amber", "bi-hourglass-split", "Pending"),
            ("Enrolment", "Cancelled") => ("rd-badge-grey", "bi-x-circle", "Cancelled"),

            ("Event", "Published") => ("rd-badge-blue", "bi-broadcast", "Published"),
            ("Event", "Draft") => ("rd-badge-outline", "bi-pencil-square", "Draft"),
            ("Event", "Completed") => ("rd-badge-dark", "bi-flag-fill", "Completed"),
            ("Event", "Cancelled") => ("rd-badge-red", "bi-x-octagon", "Cancelled"),

            ("Result", "Finished") => ("rd-badge-green", "bi-check-circle-fill", "Finished"),
            ("Result", "DNF") => ("rd-badge-amber", "bi-exclamation-triangle", "DNF"),
            ("Result", "DSQ") => ("rd-badge-red", "bi-x-octagon-fill", "DSQ"),

            _ => ("rd-badge-outline", "bi-question-circle", Status),
        };
    }
}
