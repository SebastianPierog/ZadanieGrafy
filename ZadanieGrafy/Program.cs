using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZadanieGrafy
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length == 1)
            {
                var tree = new SuffixTree();
                var text = tree.readFile(args[0]);
                tree.AddString(text);
                tree.znajdzK(text);
            }
        }
    }
}
