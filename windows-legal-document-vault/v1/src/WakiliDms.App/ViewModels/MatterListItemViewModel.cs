using WakiliDms.Core.Domain;

namespace WakiliDms.App.ViewModels;

public sealed class MatterListItemViewModel
{
    public MatterListItemViewModel(Matter matter)
    {
        Id = matter.Id;
        Name = matter.Name;
        ClientName = matter.ClientName ?? "No client recorded";
        CourtCaseNumber = matter.CourtCaseNumber ?? "No case number";
        UpdatedAt = matter.UpdatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
    }

    public Guid Id { get; }

    public string Name { get; }

    public string ClientName { get; }

    public string CourtCaseNumber { get; }

    public string UpdatedAt { get; }
}
