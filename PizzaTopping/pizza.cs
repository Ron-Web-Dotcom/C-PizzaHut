using System;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;


//Created a Class name Pizza
class Pizza : ToppingCombination
{
    //Created Properties  get 
    public string toppings
    {
        get;
        set;

    }

    public void PizzaTop()
    {

        // Create Type list of Objects that can be accessed by index
        List<Pizza> toppings = GetPizza();
        // if the  object is  empty it will return 
        if (toppings == null) return;
        //The  
        IEnumerable<ToppingCombination> topcombination = GetTopCombo(toppings);
        // Declare a variable  intianlize  by the number 
        int number = 15;
        //The top toppings in Desending Order by   creating a variable  count the squence  the take extension method return the specified number of element starting from the first element 
        IEnumerable<ToppingCombination> mostPopularToppings = topcombination.OrderByDescending(oi => oi.countt).Take(number);
        // Declare a variable  intianlize  by the number

        int num = 0;
        // Declare a variable  intianlize  by the number 

        string letter = "Rank:{0} \t PizzaTaken: {2} \t HowManyOrdered:{1,5}";
        //This is my for loop
        foreach (ToppingCombination taste in mostPopularToppings)
        {
            //the string  value if the value of the variable into another variable or string 
            Console.WriteLine(String.Format(letter, num, taste.countt, taste.toppingss));
            // increment the number by 1
            num++;
        }



        // This help to  get current elements from the collection
        List<Pizza> GetPizza()
        {

            //Create a variable  to capture data from the link
            string url = "http://brightway.com/CodeTests/pizzas.json";
            // string url = "C:/Users/RON TAYLOR/Desktop/School Infomtation/MVC/PizzaTopping/PizzaTopping/bin/Debug/netcoreapp3.1/topping.json";
            //Creating  the Request for the variable 
            HttpWebRequest httpWebRequest = System.Net.WebRequest.Create(url) as HttpWebRequest;



            List<Pizza> pizzas;

            // When the request is sent  from the url variable and the check if the web request get called to
            using (HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse)
            {

                // throw and expection or throw and catch if needed
                if (httpWebResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("Error Occured ", httpWebResponse.StatusCode, httpWebResponse.StatusDescription));
                }
                //Creating an instance for the the request that is called it creates a smooth additional layer between layer and application
                Stream create = httpWebResponse.GetResponseStream();
                string eattopping = new StreamReader(create).ReadToEnd();


                ///  jtop = JsonConvert.DeserializeObject<List<Pizza[]>>(eattopping);


                pizzas = JsonConvert.DeserializeObject<List<Pizza>>(eattopping);
            }
            return pizzas;
        }
        // This help to  get current elements from the collection
        IEnumerable<ToppingCombination> GetTopCombo(List<Pizza> toppings)
        {

            //Create a variable using the datatype 
            var Pizzahut = toppings.Select(pizza => pizza.toppings.Split(',').Select(t => t.Trim()).OrderBy(toppin => toppin));
            

            IEnumerable<string> aggregated = Pizzahut.Select(sortedToppings => sortedToppings.Aggregate("", (pepper, sauces) => pepper + ',' + sauces));

            //DAta is Grouped and  Displayed 
            IEnumerable<ToppingCombination> grouped = aggregated
               .GroupBy(toppingsGroup => toppingsGroup)
            .Select(toppingsGroup => new ToppingCombination()
            {
                toppingss = toppingsGroup.Key,
                countt = toppingsGroup.Count()
            });
            return grouped;

        }

    }
}
