using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZadanieGrafy
{
    class Program
    {


        static List<ZgrupowaneLitery> charList = new List<ZgrupowaneLitery>();
        static int wynikOstateczny = 0;
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                start(args[0]);
                while (wynikOstateczny == 0) { }

                SuffixTree temp = new SuffixTree();
                temp.writeLine(wynikOstateczny.ToString());
            }
            else Console.WriteLine("Podano nieprawidlowa liczbe argumentow");
        }

        static async void start(string arg)
        {
            var tree2 = new SuffixTree();
            string text = tree2.ReadStringV2(arg);

            Task<int> task1 = Task<int>.Factory.StartNew(() => {

                return test(text);


            });

            Task<int> task2 = Task<int>.Factory.StartNew(() => {
                var tree = new SuffixTree();
                tree.AddString(text);

                return tree.znajdzK(text);
            });

            Task<int> completedTask = await Task.WhenAny(task1, task2);

            wynikOstateczny = await completedTask;
        }

        unsafe static int test(string text)
        {

            charList.Add(new ZgrupowaneLitery(text[0], 1));
            int poziom = 0;
            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == charList[poziom].litera) charList[poziom].liczba++;
                else
                {
                    poziom++;
                    charList.Add(new ZgrupowaneLitery(text[i], 1));
                }
            }

            //pilnuje czy nie wyrzuci pamieci
            int topOfStack;
            const int stackSize = 1000000;
            const int spaceRequired = 18 * 1024;
            int var;
            topOfStack = (int)&var;
            return repeat(0, 0, 0);


            int repeat(int x, int sum, int result)
            {

                int remaining;
                remaining = stackSize - (topOfStack - (int)&remaining);
                if (remaining < spaceRequired)
                {
                    Thread.Sleep(60000);
                    Thread.CurrentThread.Abort();
                }
                sum += charList[x].liczba;
                if (sum < text.Length / 2) result = repeat(x + 1, sum, result);
                else sum -= charList[x].liczba;

                if (result == 0)
                {
                    for (int j = 0; j < x; j++)
                    {
                        if ((charList[j].litera != charList[j + x].litera) && (charList[j].liczba != charList[j + x].liczba)) break;
                        if (j + 1 == x) result = sum;
                    }
                }

                return result;
            }
        }


    }
}