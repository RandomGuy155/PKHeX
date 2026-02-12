using System;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.WinForms.Controls;
using static PKHeX.Core.MessageStrings;

namespace PKHeX.WinForms;

public partial class EntitySearchSetup : Form
{
    private EntityInstructionBuilder? UC_Builder;
    private SaveFile? CurrentSave;
    private SAVEditor? SaveEditor;
    public Func<PKM, bool>? SearchFilter { get; private set; }

    public EntitySearchSetup()
    {
        InitializeComponent();
        WinFormsUtil.TranslateInterface(this, Main.CurrentLanguage);
    }

    /// <summary>
    /// Occurs when the Search action is requested.
    /// </summary>
    public event EventHandler? SearchRequested;

    /// <summary>
    /// Occurs when the Reset action is requested.
    /// </summary>
    public event EventHandler? ResetRequested;

    /// <summary>
    /// Initializes the search setup controls using the provided save file.
    /// </summary>
    /// <param name="sav">Save file used to configure search settings.</param>
    /// <param name="edit">Editor to provide the current PKM.</param>
    public void Initialize(SaveFile sav, IPKMView edit, SAVEditor savEditor)
    {
        ArgumentNullException.ThrowIfNull(sav);

        UC_EntitySearch.MaxFormat = Latest.Generation;
        UC_EntitySearch.SaveGeneration = sav.Generation;
        UC_EntitySearch.PopulateComboBoxes();
        UC_EntitySearch.SetFormatAnyText(MsgAny);
        UC_EntitySearch.FormatComparatorSelectedIndex = 3; // <=
        CurrentSave = sav;
        SaveEditor = savEditor;
        EnsureBuilder(edit);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UC_EntitySearch.ResetComboBoxSelections();
        ActiveControl = RTB_Instructions;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // Search with Enter (unless in the RTB_Instructions which uses Enter for newlines)
        if (e.KeyCode == Keys.Enter)
        {
            if (RTB_Instructions.Focused)
                return;

            B_Search_Click(this, EventArgs.Empty);
            e.Handled = true;
        }

        // Quick close with Ctrl+W
        if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
            Hide();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        CurrentSave = null;
        SearchFilter = null;
        base.OnFormClosing(e);
    }

    private void EnsureBuilder(IPKMView edit)
    {
        if (UC_Builder is not null)
            return;

        UC_Builder = new EntityInstructionBuilder(() => edit.PreparePKM())
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Dock = DockStyle.Top,
            ReadOnly = true,
        };
        Tab_Advanced.Controls.Add(UC_Builder);
        UC_Builder.SendToBack();
    }

    private void B_Search_Click(object? sender, EventArgs e)
    {
        SearchFilter = UC_EntitySearch.GetFilter(RTB_Instructions.Text);
        SearchRequested?.Invoke(this, EventArgs.Empty);

        // Automatically seek to first match
        if (SearchFilter is not null && SaveEditor is not null)
        {
            SaveEditor.Box.SeekNext(SearchFilter);
        }
    }

    private void B_Reset_Click(object? sender, EventArgs e)
    {
        UC_EntitySearch.ResetFilters();
        RTB_Instructions.Clear();
        SearchFilter = null;
        ResetRequested?.Invoke(this, EventArgs.Empty);
        System.Media.SystemSounds.Asterisk.Play();
    }

    private void B_Add_Click(object? sender, EventArgs e)
    {
        if (UC_Builder is null)
            return;

        var s = UC_Builder.Create();
        if (s.Length == 0)
        {
            WinFormsUtil.Alert(MsgBEPropertyInvalid);
            return;
        }

        var tb = RTB_Instructions;
        var batchText = tb.Text;
        if (batchText.Length != 0 && !batchText.EndsWith('\n'))
            tb.AppendText(Environment.NewLine);
        tb.AppendText(s);
    }

    private void B_Previous_Click(object? sender, EventArgs e)
    {
        if (SearchFilter is not null && SaveEditor is not null)
        {
            SaveEditor.Box.SeekPrevious(SearchFilter);
        }
    }

    private void B_Next_Click(object? sender, EventArgs e)
    {
        if (SearchFilter is not null && SaveEditor is not null)
        {
            SaveEditor.Box.SeekNext(SearchFilter);
        }
    }

    public bool IsSameSaveFile(SaveFile sav) => CurrentSave is not null && CurrentSave == sav;

    public void ForceReset()
    {
        SearchFilter = null;
        UC_EntitySearch.ResetFilters();
        RTB_Instructions.Clear();
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }
}
