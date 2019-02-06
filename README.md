# CommandLineSwitches

C# class that simplifies reading command line arguments as switches.

If you start your app with

    YourApp.exe --FOO true --bar 7

In YourApp.cs

    static void Main(string[] args)
    {
        CommandLineSwitches switches = new CommandLineSwitches(args, "--", true);

        string foo = switches.Contains("foo") ? p.Get("foo") : null;

        int bar = switches.Contains("bar") ? Int32.Parse(p.Get("bar")) : 0;
    }

## How to use

Copy the code into your project. Change the namespace to whatever. There are no external dependencies. It's compatible with most .Net framework versions. 

## License

MIT