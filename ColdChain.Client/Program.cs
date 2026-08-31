namespace ColdChain.Client;

internal static class Program
{
    // Co-authored by Claude
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
