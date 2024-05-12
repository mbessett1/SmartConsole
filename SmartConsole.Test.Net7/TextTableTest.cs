using Bessett.SmartConsole;
using Bessett.SmartConsole.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartConsole.Test.Net7
{
    [NoConfirmation, TaskAlias("tt")]
    internal class TextTableTest : ConsoleTask
    {
        public class TableData
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Country { get; set; }
            public string Zip { get; set; }
        }

        override public TaskResult StartTask()
        {

            var table = new TextTable()
                .AddColumn("Name", 22, AlignmentType.Center)
                .AddColumn("Age", 5, AlignmentType.Center)
                .AddColumn("City", 20, AlignmentType.Center)
                .AddColumn("State", 5, AlignmentType.Center)
                .AddColumn("Country", 10, AlignmentType.Center)
                .AddColumn("Zip", 10, AlignmentType.Center);

            List<TableData> tabledata = new List<TableData>()
            {
                new TableData()
                {
                    Name = "John Doe John Doe John Doe John Doe John Doe ",
                    Age = 25,
                    City = "New York",
                    State = "NY",
                    Country = "USA",
                    Zip = "10001"
                },
                new TableData()
                {
                    Name = "Jane Doe",
                    Age = 30,
                    City = "Los Angeles",
                    State = "CA",
                    Country = "USA",
                    Zip = "90001"
                }
            };

            Console.WriteLine(table.Render(tabledata));
            
            var tableDataArray = new List<string[]>
            {
                new string[] { "John Doe asdas asddda sadasdas", "25", "New York", "NY", "USA", "10001" },
                new string[] { "Jane Doe", "30", "Los Angeles", "CA", "USA", "90001" }
            };

            Console.WriteLine(table.Render(tableDataArray));

            return TaskResult.Complete();
        }

    }
}
