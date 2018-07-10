using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;

namespace CTL
{
    public class CommandLineOption
    {
        [Option("topic")]
        public int topics { get; set; }

        [Option("savestep")]
        public int savestep { get; set; }

        [Option("alpha")]
        public double alpha { get; set; }

        [Option("beta")]
        public double beta { get; set; }

        [Option("niters")]
        public int niters { get; set; }

        [Option("info")]
        public string info { get; set; }

        [Option("papers")]
        public string papers { get; set; }

        [Option("authors")]
        public string authors { get; set; }

        [Option("test")]
        public string test { get; set; }

        [Option("twords")]
        public int twords { get; set; }
    }
}
