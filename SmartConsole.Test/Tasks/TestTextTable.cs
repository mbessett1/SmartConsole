using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bessett.SmartConsole;
using Bessett.SmartConsole.Text;

namespace SmartConsole.Test.Tasks
{
    [TaskAlias("tt")]
    public class TestTextTable: ConsoleTask
    {
        class Bag
        {
            public string Name { get; set; }
            public int Length { get; set; }
            public string Decription { get; set; }
        }

        public override bool ConfirmStart()
        {
            //Console.BackgroundColor = ConsoleColor.Cyan;
            return true;
        }

        public override TaskResult StartTask()
        {
            var data =  new List<string[]>()
            {
                new string[] { "Darth", "Vader","Sith", "Empire"  },
                new string[] { "Luke", "Skywalker","Jedi", "Rebellion"  },
                new string[] { "Leia", "Organa","Princess", "Rebellion"  },
            };

            var data2 = new List<Bag>()
            {
                new Bag() { Name="X-Wing", Length = 105, Decription = "Cool Starfighter"},
                new Bag() { Name="TIE Fighter",Length = 162, Decription = "Freaky fast and meneuverable fighter"},
                new Bag() { Name="Millenium Falcon",Length = 783,Decription = "THe ship that made the Kessel Run in less than 12 parsecs"},

            };

            var table = new TextTable()
                    .AddColumn("Col1", 12)
                    .AddColumn("col2", 4, AlignmentType.Center)
                    .AddColumn("col3", 12, AlignmentType.Right)
                ;

            var table2 = new TextTable()
                    .AddColumn("Col1", 12)
                    .AddColumn("col2", 4, AlignmentType.Center)
                    .AddColumn("col3", 12, AlignmentType.Right)
                ;

            Console.WriteLine(table.Render(data));
            Console.WriteLine(table2.Render(data2));
            Console.WriteLine(new TextTable().Render(data2));
            Console.WriteLine(new TextTable().Render(data2.Select(o => new { Vehicle = o.Name, LOA = o.Length, CatchPhrase = o.Decription })));
            Console.WriteLine(new TextTable().Render(data2.Select(o => new { Vehicle = o.Name, Catch_Phrase = o.Decription })));

            return TaskResult.Complete();
        }
    }
}
