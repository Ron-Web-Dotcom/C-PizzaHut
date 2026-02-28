using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace PizzaTopping
{
    class Program
    {
        static void Main(string[] args)
        {


            /*Creating a JSON FILE 

            Pizza pizza = new Pizza()
            {
                toppings = "pepperoni"

            };
            Pizza pizza1 = new Pizza()
            {
                toppings = "feta cheese"

            };
            Pizza pizza2 = new Pizza()
            {
                toppings = "bacon"

            };
            Pizza pizza3 = new Pizza()
            {
                toppings = "sausage, pepperoni"

            };
            Pizza pizza4 = new Pizza()
            {
                toppings = "mozzarella cheese bacon,beef,onions,pineapple"

            };
            Pizza pizza5 = new Pizza()
            {
                toppings = "sausage ,beef"

            };
            Pizza pizza6 = new Pizza()
            {
                toppings = "four cheese",

            };
            Pizza pizza7 = new Pizza()
            {
                toppings = "ham",

            };
            Pizza pizza8 = new Pizza()
            {
                toppings = "beef"

            };
            Pizza pizza9 = new Pizza()
            {
                toppings = "black olives"

            };
            Pizza pizza10 = new Pizza()
            {
                toppings = "jalapenos"


            };

            Pizza pizza11 = new Pizza()
            {
                toppings = "italian sausage"

            };
            Pizza pizza12 = new Pizza()
            {
                toppings = " peperoni,feta cheese"

            };
            Pizza pizza13 = new Pizza()
            {
                toppings = "bacon,four cheese"

            };
            Pizza pizza14 = new Pizza()
            {
                toppings = "bacon, pepperoni"

            };
            Pizza pizza15 = new Pizza()
            {
                toppings = "mozzarella cheese ,beef,pineapple"

            };
            Pizza pizza16 = new Pizza()
            {
                toppings = "sausage ,beef"

            };
            Pizza pizza17 = new Pizza()
            {
                toppings = "four cheese,sausage",

            };
            Pizza pizza18 = new Pizza()
            {
                toppings = "ham ,pineapple",

            };
            Pizza pizza19 = new Pizza()
            {
                toppings = "beef"

            };
            Pizza pizza20 = new Pizza()
            {
                toppings = "black olives, pepperoni"

            };
            Pizza pizza21 = new Pizza()
            {
                toppings = "jalapenos,  mozzarella cheese, onions,pineapple"



            };
            Pizza pizza22 = new Pizza()
            {
                toppings = "italian sausage,onions,pineapple"

            };
            Pizza pizza23 = new Pizza()
            {
                toppings = " italian sausage,feta cheese"

            };
            Pizza pizza24 = new Pizza()
            {
                toppings = "pineapple,four cheese"

            };
            Pizza pizza25 = new Pizza()
            {
                toppings = "sausage, pepperoni"

            };
            Pizza pizza26 = new Pizza()
            {
                toppings = "mozzarella cheese ,pepperoni,pineapple"

            };
            Pizza pizza27 = new Pizza()
            {
                toppings = "mozzarella cheese ,beef"

            };
            Pizza pizza28 = new Pizza()
            {
                toppings = "four cheese,onions",

            };
            Pizza pizza29 = new Pizza()
            {
                toppings = "ham ,four cheese,mozzarella cheese",

            };
            Pizza pizza30 = new Pizza()
            {
                toppings = "beef ,black olives"

            };
            Pizza pizza31 = new Pizza()
            {
                toppings = "black olives, pineapple ,jalapenos"

            };
            Pizza pizza32 = new Pizza()
            {
                toppings = "jalapenos, pepperoni, onions,pineapple"



            };
            Pizza pizza33 = new Pizza()
            {
                toppings = "mushrooms ,black olives"

            };
            Pizza pizza34 = new Pizza()
            {
                toppings = "ham, pineapple ,jalapenos"

            };
            Pizza pizza35 = new Pizza()
            {
                toppings = "hot peppers, pepperoni, onions,pineapple"



            };
            Pizza pizza36 = new Pizza()
            {
                toppings = "beef ,black olives"

            };
            Pizza pizza37 = new Pizza()
            {
                toppings = "black olives, ham ,jalapenos"

            };
            Pizza pizza38 = new Pizza()
            {
                toppings = " Hot peppers,chichken, onions,pineapple"



            };
            Pizza pizza39 = new Pizza()
            {
                toppings = "chicken ,black olives"

            };
            Pizza pizza40 = new Pizza()
            {
                toppings = "black olives, pineapple ,mushrooms"

            };
        // creating a series of element to convert the object that was set above 

            string strPizzaJson = JsonConvert.SerializeObject(pizza);
            string strPizzaJson2 = JsonConvert.SerializeObject(pizza1);
            string strPizzaJson3 = JsonConvert.SerializeObject(pizza2);
            string strPizzaJson4 = JsonConvert.SerializeObject(pizza3);
            string strPizzaJson5 = JsonConvert.SerializeObject(pizza4);
            string strPizzaJson6 = JsonConvert.SerializeObject(pizza5);
            string strPizzaJson7 = JsonConvert.SerializeObject(pizza6);
            string strPizzaJson8 = JsonConvert.SerializeObject(pizza7);
            string strPizzaJson9 = JsonConvert.SerializeObject(pizza8);
            string strPizzaJson10 = JsonConvert.SerializeObject(pizza9);
            string strPizzaJson11 = JsonConvert.SerializeObject(pizza10);
            string strPizzaJson12 = JsonConvert.SerializeObject(pizza11);
            string strPizzaJson13 = JsonConvert.SerializeObject(pizza12);
            string strPizzaJson14 = JsonConvert.SerializeObject(pizza13);
            string strPizzaJson15 = JsonConvert.SerializeObject(pizza14);
            string strPizzaJson16 = JsonConvert.SerializeObject(pizza15);
            string strPizzaJson17 = JsonConvert.SerializeObject(pizza16);
            string strPizzaJson18 = JsonConvert.SerializeObject(pizza17);
            string strPizzaJson19 = JsonConvert.SerializeObject(pizza18);
            string strPizzaJson20 = JsonConvert.SerializeObject(pizza19);
            string strPizzaJson21 = JsonConvert.SerializeObject(pizza20);
            string strPizzaJson22 = JsonConvert.SerializeObject(pizza21);
            string strPizzaJson23 = JsonConvert.SerializeObject(pizza22);
            string strPizzaJson24 = JsonConvert.SerializeObject(pizza23);
            string strPizzaJson25 = JsonConvert.SerializeObject(pizza24);
            string strPizzaJson26 = JsonConvert.SerializeObject(pizza25);
            string strPizzaJson27 = JsonConvert.SerializeObject(pizza26);
            string strPizzaJson28 = JsonConvert.SerializeObject(pizza27);
            string strPizzaJson29 = JsonConvert.SerializeObject(pizza28);
            string strPizzaJson30 = JsonConvert.SerializeObject(pizza29);
            string strPizzaJson31 = JsonConvert.SerializeObject(pizza30);
            string strPizzaJson32 = JsonConvert.SerializeObject(pizza31);
            string strPizzaJson33 = JsonConvert.SerializeObject(pizza32);
            string strPizzaJson34 = JsonConvert.SerializeObject(pizza33);
            string strPizzaJson35 = JsonConvert.SerializeObject(pizza34);
            string strPizzaJson36 = JsonConvert.SerializeObject(pizza35);
            string strPizzaJson37 = JsonConvert.SerializeObject(pizza36);
            string strPizzaJson38 = JsonConvert.SerializeObject(pizza37);
            string strPizzaJson39 = JsonConvert.SerializeObject(pizza38);
            string strPizzaJson40 = JsonConvert.SerializeObject(pizza39);
            string strPizzaJson41 = JsonConvert.SerializeObject(pizza40);

         Stored each expression in   the file that is created below

            File.WriteAllText(@"topping.json", strPizzaJson + strPizzaJson2 + strPizzaJson3 + strPizzaJson4 + strPizzaJson5 + strPizzaJson6 + strPizzaJson7 + strPizzaJson8 + strPizzaJson9 + strPizzaJson10 + strPizzaJson11 + strPizzaJson12 + strPizzaJson13 + strPizzaJson14 + strPizzaJson15 + strPizzaJson16 + strPizzaJson17 + strPizzaJson18 + strPizzaJson19 + strPizzaJson20 + strPizzaJson21 + strPizzaJson22 + strPizzaJson23 + strPizzaJson24 + strPizzaJson25 + strPizzaJson26 + strPizzaJson27 + strPizzaJson28 + strPizzaJson29 + strPizzaJson30 + strPizzaJson31 + strPizzaJson32 + strPizzaJson33 + strPizzaJson34 + strPizzaJson35 + strPizzaJson36 + strPizzaJson37 + strPizzaJson38 + strPizzaJson39 + strPizzaJson40 + strPizzaJson41);
            Console.WriteLine("stored");

            */
            /*

           List<Pizza> GetPizza()
            {
                //Create a variable  to capture data from the link
                string url = "C:/Users/RON TAYLOR/Desktop/School Infomtation/MVC/PizzaTopping/PizzaTopping/bin/Debug/netcoreapp3.1/topping.json";
                //Creating  the Request for the variable 
                HttpWebRequest httpWebRequest = System.Net.WebRequest.Create(url) as HttpWebRequest;


                List<Pizza> toppings;

                // When the request is sent  from the url variable and the check if the web request get called to
                using (HttpWebResponse httpWebResponse = httpWebRequest.GetResponse()as HttpWebResponse)
                {

                // throw and expection or throw and catch if needed
                   if (httpWebResponse.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(string.Format("Error Occured", httpWebResponse.StatusCode, httpWebResponse.StatusDescription));
                    }
                   //Creating an instance for the the request that is called it creates a smooth additional layer between layer and application
                    Stream create = httpWebResponse.GetResponseStream();
                    string eattopping = new StreamReader(create).ReadToEnd();
                    create.Close();

                }

                return toppings;
            }
          
            */



            // Parse command-line options
            string filePath   = null;
            int    topN       = 15;
            int    minOrders  = 1;
            string exportPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--file":
                        if (i + 1 < args.Length) filePath = args[++i];
                        break;
                    case "--top":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int n)) topN = n;
                        break;
                    case "--min-orders":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int m)) minOrders = m;
                        break;
                    case "--export":
                        if (i + 1 < args.Length) exportPath = args[++i];
                        break;
                    case "--help":
                        Console.WriteLine("Usage: PizzaTopping [options]");
                        Console.WriteLine("  --file <path>       Read pizza data from a local JSON file");
                        Console.WriteLine("  --top <n>           Show top N combinations (default: 15)");
                        Console.WriteLine("  --min-orders <n>    Only show combos ordered at least N times (default: 1)");
                        Console.WriteLine("  --export <file>     Export results to a .csv or .json file");
                        return;
                }
            }

            // Call a function from the class name Pizza
            Pizza p = new Pizza();
            p.PizzaTop(filePath, topN, minOrders, exportPath);

        }
    }
}
