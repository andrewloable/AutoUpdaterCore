# AutoUpdaterCore
AutoUpdater Library for DotNet Core applications based on https://github.com/ravibpatel/AutoUpdater.NET

## Sample

### Update JSON File
```
{
  "version": "1.0.0.0",
  "url": "https://loable.tech/something.zip",
  "checksum": "SHA512_Checksum_of_ZipFile",
  "hash":  "SHA512"
}
```

### Code
```
        static void Main(string[] args)
        {
            AutoUpdater.UpdateDownloadedEvent += (args) =>
            {
                if (args.IsDownloaded)
                {
                    Console.WriteLine("Update Downloaded. Closing.");
                    return;
                }
                Console.WriteLine("Update Download Failed");
            };
            AutoUpdater.UpdateErrorEvent += (args) =>
            {
                Console.WriteLine("Error : " + args.Message);
            };
            AutoUpdater.StartAsync(@"https://example.com/update.json").Wait();
            Console.WriteLine("Done Running");
        }
```
