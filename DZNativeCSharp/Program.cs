using System;
using System.Threading;

namespace DZNativeCSharp
{

    /// <summary>
    /// Notes:
    /// The .NET target framework must be 3.5!
    /// This program is compiled for x64 and uses the deezer x64 library.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Thread a = new Thread(new ThreadStart(() =>
            {
                var app = new Deezer();
                app.Setup();

                //for testing, the track is loaded in ConnectionOnEventCallback();
                //track is played automatically after it is loaded in PlayerOnEventCallback();
            }));

            a.Start();

            Console.ReadLine();
        }
    }
}
