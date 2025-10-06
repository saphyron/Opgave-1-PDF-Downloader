using PdfDownloader.App;

try
{
    var options = AppOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    var runner = new ApplicationRunner(options);
    await runner.RunAsync(cts.Token);
    return 0;
}
catch (OptionParsingException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(AppOptions.Usage);
    return 1;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Annulleret af bruger.");
    return 2;
}