# SmartConsole

Smart console is a quick way to build self-describing console apps.  This library lets a developer quickly build console apps that will have attribute driven help and tasks.

Quick Code Example:
```C#
    class Program
    {
        static void Main(string[] args)
        {
            // start the console
            // if there is no argument, the console
            // will start a shell
            ConsoleProgram.Start(args);  
        }
    }

	// supporting a discoverable task
	[TaskHelp("Some Help for the Task")]
    public class DoSomething: ConsoleTask   
    {
        [ArgumentHelp( IsRequired = true, HelpText = "This will do something." ,ErrorText = "Invalid value" )]
        public string RequiredValue { get; set; }

        [ArgumentHelp("This is a parameter", IsRequired = false)]
        public string ParamValue { get; set; }
		
        [ArgumentHelp]
        public int BatchSize { get; set; }
        
        [ArgumentHelp]
        public string Filename { get; set; }
        
        [ArgumentHelp]
        public bool IsFinal { get; set; }
        
        public override void Start()
        {
            Console.WriteLine("Started!");
        }

    }
```
## Example usage/execution on command line:
### Run a one-time command

`C:\>MyApp DoSomething -RequiredValue MyValue -BatchSize 20`
  Executes (or tries to execute) the task. Argument validation (checking required) happens first, as well
  as confirming execution if applicable.

### Start a command shell  
`C:\>MyApp`  

No arguments will start command shell
  
C:\>myApp help DoSomething
  Shows help associated with DoSomething


### Additional Information:
  When the app runs, it will pause to confirm execution UNLESS the task class has a [NoConfirmation] attribute, 
  or the commandline specified "-Silent".

 


	
