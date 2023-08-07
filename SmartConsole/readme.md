# SmartConsole README

Smart console is a quick way to build self-describing, descrete task console apps. 
This library lets a developer quickly build console apps that will have attribute driven help and tasks.

## Quick Code Example:

    class Program
    {
        static void Main(string[] args)
        {
            ConsoleProgram.Start(args);  
        }
    }

	// supporting a discoverable task
	[TaskHelp]
    public class DoSomething: ConsoleTask   
    {
        [ArgumentHelp( IsRequired = true, HelpText = "This will do something." ,ErrorText = "Invalid value" )]
        public string RequiredValue { get; set; }

        [ArgumentHelp("This is a parameter", IsRequired = false)]
        public string ParamValue { get; set; }
		
        [ArgumentHelp, DefaultValue(12)]
        public int BatchSize { get; set; }
        
        [ArgumentHelp]
        public string Filename { get; set; }
        
        [ArgumentHelp]
        public bool IsFinal { get; set; }
        
        public override TaskResult void StartTask()
        {
            try {
            
                // Do work here
                bool success = DoWork();

                if (success)
				    return TaskResult.Complete("Final Message to console");
                else
                    return TaskResult.Failure("Fail Message to console");

			} catch (Exception ex) {
				return TaskResult.Exception(ex);
			}}

            // Do work here
            return TaskResult.Complete("Final Message to console");
        }

    }

## Example usage/execution on command line:

MyApp DoSomething -RequiredValue MyValue -BatchSize 20
  Executes (or tries to execute) the task. Argument validation (checking required) happens first, as well
  as confirming execution if applicable.
  
MyApp  {No arguments will show help}
  Shows help for all tasks available (those derived from Consoletask)
  
myApp help DoSomething
  Shows help associated with DoSomething


Additional Information:
  When the app runs, it will pause to confirm execution UNLESS the task class has a [NoConfirmation] attribute, 
  or the commandline specified "-Silent".

## Release Notes

### 1.1 Release Notes:

- Added shell task that will allow task commands to operate within a shell
- Added TaskPackage support that allows tasks to be composed and executed as a package/group
- Added support to load task package/script from a file
  
### 2.0 Release

- Added support for subclassing Console Task
- Added support for Task Packaging
- Added support for Task aliases
- Improved help
- Added return upjects and new overload StartTask
- Added supportability for SmartConsole.SmartTasks (Dynamic Tasks)

### 3.0 Release

- Added support for Core 2.1 
- Support 4.5 backwards compatibility

### 3.1 Release

- Added support netStandard

### 3.2 Release
- Minor bug fixes

### 3.3 Release
- Added Hosted service support net6.0+ (SmartConsole.Service nuget package)
- Added Asynch support for tasks



