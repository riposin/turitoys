// See https://aka.ms/new-console-template for more information
using MySqlConnector;

string? header, option;

header = """
    Turistore toys - QA
    -------------------
    Select an option or empty to close

    a: Set materials s4hid from csv to db pos based on sku
    """;

Console.WriteLine(header);
option = Console.ReadLine();
option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

switch (option)
{
    case "a":
        optiona();
        break;
    default:
        break;
}

Console.WriteLine("""

    Done, press any key to exit...
    """);
Console.ReadLine();

static void optiona(){
    string? option;

    Console.Clear();
    Console.WriteLine("""
    Selected option was A
    
    Prerequisites:
    1 - A csv file named optionain.csv with 3 columns [mat,des,sku]
    2 - A text file named optionacnn.txt with the connection string to the MySQL database of an All Retail POS installation

    Results:
    1 -

    Press y to proceed or empty to exit
    """);
    option = Console.ReadLine();
    option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

    if (option != "y") { return; }

    MySqlConnection cnn = new("Server=lapqa1.off;User ID=root;Password=Brutus22");
    try
    {
        cnn.OpenAsync().Wait();
        cnn.Close();
    }
    catch (Exception e)
    { Console.WriteLine("Error during connection: " + e.Message); }

    return;
}