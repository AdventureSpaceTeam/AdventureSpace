using Content.Shared.StationRecords;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.StationRecords;

[GenerateTypedNameReferences]
public sealed partial class GeneralRecord : Control
{
    public Action<uint>? OnDeletePressed;
    public GeneralRecord(GeneralStationRecord record, bool canDelete, uint? id)
    {
        RobustXamlLoader.Load(this);
        RecordName.Text = record.Name;
        Age.Text = Loc.GetString("general-station-record-console-record-age", ("age", record.Age.ToString()));
        Title.Text = Loc.GetString("general-station-record-console-record-title",
            ("job", Loc.GetString(record.JobTitle)));
        Species.Text = Loc.GetString("general-station-record-console-record-species", ("species", record.Species));
        Gender.Text = Loc.GetString("general-station-record-console-record-gender",
            ("gender", record.Gender.ToString()));
        Fingerprint.Text = Loc.GetString("general-station-record-console-record-fingerprint",
            ("fingerprint", record.Fingerprint ?? Loc.GetString("generic-not-available-shorthand")));
        Dna.Text = Loc.GetString("general-station-record-console-record-dna",
            ("dna", record.DNA ?? Loc.GetString("generic-not-available-shorthand")));

        if (canDelete && id != null )
        {
            DeleteButton.Visible = true;
            DeleteButton.OnPressed += _ => OnDeletePressed?.Invoke(id.Value);
        }
    }
}
