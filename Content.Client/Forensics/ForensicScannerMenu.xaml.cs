using System.Text;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;
using Content.Shared.Forensics;

namespace Content.Client.Forensics
{
    [GenerateTypedNameReferences]
    public sealed partial class ForensicScannerMenu : DefaultWindow
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public ForensicScannerMenu()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
        }

        public void UpdatePrinterState(bool disabled)
        {
            Print.Disabled = disabled;
        }

        public void UpdateState(ForensicScannerBoundUserInterfaceState msg)
        {
            if (string.IsNullOrEmpty(msg.LastScannedName))
            {
                Print.Disabled = true;
                Clear.Disabled = true;
                Name.Text = string.Empty;
                Diagnostics.Text = string.Empty;
                return;
            }

            Print.Disabled = (msg.PrintReadyAt > _gameTiming.CurTime);
            Clear.Disabled = false;

            Name.Text = msg.LastScannedName;

            var text = new StringBuilder();

            text.AppendLine(Loc.GetString("forensic-scanner-interface-fingerprints"));
            foreach (var fingerprint in msg.Fingerprints)
            {
                text.AppendLine(fingerprint);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-fibers"));
            foreach (var fiber in msg.Fibers)
            {
                text.AppendLine(fiber);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-dnas"));
            foreach (var dna in msg.DNAs)
            {
                text.AppendLine(dna);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-residues"));
            foreach (var residue in msg.Residues)
            {
                text.AppendLine(residue);
            }
            Diagnostics.Text = text.ToString();
        }
    }
}
