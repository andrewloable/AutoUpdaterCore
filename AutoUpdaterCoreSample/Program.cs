using AutoUpdaterCore;
using LoableTech;
using System;
using System.Collections.Generic;

namespace AutoUpdaterCoreSample
{
    class Program
    {
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
            AutoUpdater.StartAsync(@"https://storage.googleapis.com/butanganan.loable.tech/updates/AutoUpdatedCoreSample/update.json").Wait();
            Console.WriteLine("Done Running");
        }
    }
}
